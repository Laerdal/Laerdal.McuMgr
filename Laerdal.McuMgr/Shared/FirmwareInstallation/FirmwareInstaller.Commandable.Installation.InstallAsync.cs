using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        private const int DefaultGracefulCancellationTimeoutInMs = 2_500;
        public async Task InstallAsync(
            byte[] data,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null,
            int? windowCapacity = null, //    android only
            int? memoryAlignment = null, //   android only
            int? pipelineDepth = null, //     ios only
            int? byteAlignment = null, //     ios only
            int timeoutInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 100,
            int gracefulCancellationTimeoutInMs = 2_500
        )
        {
            EnsureExclusiveOperationToken();
            
            try
            {
                await InstallCoreAsync_();
            }
            finally
            {
                ReleaseExclusiveOperationToken();
            }
            
            return;

            async Task InstallCoreAsync_()
            {
                if (maxTriesCount <= 0)
                    throw new ArgumentOutOfRangeException(nameof(maxTriesCount), maxTriesCount, "The maximum amount of tries must be greater than zero");
            
                if (string.IsNullOrWhiteSpace(hostDeviceModel))
                    throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

                if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                    throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));
            
                gracefulCancellationTimeoutInMs = gracefulCancellationTimeoutInMs >= 0 //we want to ensure that the timeout is always sane
                    ? gracefulCancellationTimeoutInMs
                    : DefaultGracefulCancellationTimeoutInMs;

                var isCancellationRequested = false;
                var suspiciousTransportFailuresCount = 0;
                var didWarnOnceAboutUnstableConnection = false;
                for (var triesCount = 1; !isCancellationRequested;)
                {
                    var taskCompletionSource = new TaskCompletionSourceRCA<bool>(state: false);
                    try
                    {
                        Cancelled += FirmwareInstaller_Cancelled_;
                        StateChanged += FirmwareInstaller_StateChanged_;
                        FatalErrorOccurred += FirmwareInstaller_FatalErrorOccurred_;

                        var failSafeSettingsToApply = ConnectionSettingsHelpers.GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable(
                            uploadingNotDownloading: true,
                            triesCount: triesCount,
                            maxTriesCount: maxTriesCount,
                            suspiciousTransportFailuresCount: suspiciousTransportFailuresCount
                        );
                        if (failSafeSettingsToApply != null)
                        {
                            byteAlignment = failSafeSettingsToApply.Value.byteAlignment;
                            pipelineDepth = failSafeSettingsToApply.Value.pipelineDepth;
                            initialMtuSize = failSafeSettingsToApply.Value.initialMtuSize;
                            windowCapacity = failSafeSettingsToApply.Value.windowCapacity;
                            memoryAlignment = failSafeSettingsToApply.Value.memoryAlignment;

                            if (!didWarnOnceAboutUnstableConnection)
                            {
                                didWarnOnceAboutUnstableConnection = true;
                                OnLogEmitted(new LogEmittedEventArgs(
                                    level: ELogLevel.Warning,
                                    message: $"[FI.IA.010] Attempt#{triesCount}: Connection is too unstable for uploading the firmware to the target device. Subsequent tries will use failsafe parameters on the connection " +
                                             $"just in case it helps (byteAlignment={byteAlignment}, pipelineDepth={pipelineDepth}, initialMtuSize={initialMtuSize}, windowCapacity={windowCapacity}, memoryAlignment={memoryAlignment})",
                                    resource: "Firmware",
                                    category: "FirmwareInstaller"
                                ));    
                            }
                        }

                        var verdict = BeginInstallationCore( //00 dont use task.run here for now
                            data: data,
                            hostDeviceModel: hostDeviceModel,
                            hostDeviceManufacturer: hostDeviceManufacturer,

                            mode: mode,
                            eraseSettings: eraseSettings,
                            estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds,
                        
                            initialMtuSize: initialMtuSize,

                            pipelineDepth: pipelineDepth, //      ios only
                            byteAlignment: byteAlignment, //      ios only

                            windowCapacity: windowCapacity, //    android only
                            memoryAlignment: memoryAlignment //   android only
                        );
                        if (verdict != EFirmwareInstallationVerdict.Success)
                            throw verdict == EFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress
                                ? new InvalidOperationException("Another installation operation is already in progress")
                                : new ArgumentException(verdict.ToString());

                        await taskCompletionSource.WaitAndFossilizeTaskOnOptionalTimeoutAsync(timeoutInMs);
                    }
                    catch (TimeoutException ex)
                    {
                        OnStateChanged(new StateChangedEventArgs( //for consistency
                            oldState: EFirmwareInstallationState.None, //better not use this.State here because the native call might fail
                            newState: EFirmwareInstallationState.Error
                        ));

                        throw new FirmwareInstallationTimeoutException(timeoutInMs, ex);
                    }
                    catch (FirmwareInstallationUploadingStageErroredOutException ex) //we only want to retry if the errors are related to the upload part of the process
                    {
                        if (++triesCount > maxTriesCount) //order
                            throw new AllFirmwareInstallationAttemptsFailedException(maxTriesCount, innerException: ex);

                        if (_fileUploadProgressEventsCount <= 10)
                        {
                            suspiciousTransportFailuresCount++;
                        }

                        if (sleepTimeBetweenRetriesInMs > 0) //order
                        {
                            await Task.Delay(sleepTimeBetweenRetriesInMs);
                        }

                        continue;
                    }
                    catch (Exception ex) when (
                        ex is not ArgumentException //10 wops probably missing native lib symbols!
                        && ex is not TimeoutException
                        && ex is not IFirmwareInstallationException //this accounts for both cancellations and installation errors
                    )
                    {
                        OnStateChanged(new StateChangedEventArgs( //for consistency
                            oldState: EFirmwareInstallationState.None,
                            newState: EFirmwareInstallationState.Error
                        ));

                        throw new FirmwareInstallationInternalErrorException(ex);
                    }
                    finally
                    {
                        Cancelled -= FirmwareInstaller_Cancelled_;
                        StateChanged -= FirmwareInstaller_StateChanged_;
                        FatalErrorOccurred -= FirmwareInstaller_FatalErrorOccurred_;

                        TryCleanupResourcesOfLastInstallation();
                    }

                    return;

                    void FirmwareInstaller_Cancelled_(object sender, CancelledEventArgs ea)
                    {
                        taskCompletionSource.TrySetException(new FirmwareInstallationCancelledException());
                    }

                    void FirmwareInstaller_StateChanged_(object sender, StateChangedEventArgs ea)
                    {
                        switch (ea.NewState)
                        {
                            case EFirmwareInstallationState.Complete:
                                taskCompletionSource.TrySetResult(true);
                                return;

                            case EFirmwareInstallationState.Cancelling:
                                if (isCancellationRequested)
                                    return;

                                isCancellationRequested = true;

                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        if (gracefulCancellationTimeoutInMs > 0) //keep this check here to avoid unnecessary task rescheduling
                                        {
                                            await Task.Delay(gracefulCancellationTimeoutInMs);
                                        }

                                        OnCancelled(new CancelledEventArgs()); //00
                                    }
                                    catch // (Exception ex)
                                    {
                                        // ignored
                                    }
                                });
                                return;
                        }

                        //00  we first wait to allow the cancellation to be handled by the underlying native code meaning that we should see OnCancelled()
                        //    getting called right above   but if that takes too long we give the killing blow by calling OnCancelled() manually here
                    }

                    void FirmwareInstaller_FatalErrorOccurred_(object _, FatalErrorOccurredEventArgs ea_)
                    {
                        taskCompletionSource.TrySetException(ea_ switch
                        {
                            {GlobalErrorCode: EGlobalErrorCode.McuMgrErrorBeforeSmpV2_AccessDenied}
                                => new UnauthorizedException(ea_.ErrorMessage, ea_.GlobalErrorCode), // no point to pass ea_.FatalErrorType here

                            {FatalErrorType: EFirmwareInstallerFatalErrorType.InstallationAlreadyInProgress}
                                => new AnotherFirmwareInstallationIsAlreadyOngoingException(ea_.ErrorMessage, ea_.FatalErrorType, ea_.GlobalErrorCode),

                            {FatalErrorType: EFirmwareInstallerFatalErrorType.GivenFirmwareDataUnhealthy or EFirmwareInstallerFatalErrorType.FirmwareExtendedDataIntegrityChecksFailed}
                                => new FirmwareInstallationUnhealthyFirmwareDataGivenException(ea_.ErrorMessage, ea_.FatalErrorType, ea_.GlobalErrorCode),

                            {FatalErrorType: EFirmwareInstallerFatalErrorType.FirmwareFinishingImageSwapTimeout}
                                => new FirmwareInstallationImageSwappingTimedOutException(estimatedSwapTimeInMilliseconds, ea_.FatalErrorType, ea_.GlobalErrorCode),

                            {FatalErrorType: EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut} or {State: EFirmwareInstallationState.Uploading}
                                => new FirmwareInstallationUploadingStageErroredOutException(ea_.ErrorMessage, ea_.FatalErrorType, ea_.GlobalErrorCode),

                            _ => new FirmwareInstallationErroredOutException($"{ea_.ErrorMessage} [state={ea_.State}]", ea_.FatalErrorType, ea_.GlobalErrorCode)
                        });
                    }
                }
            
                //00  we are aware that in order to be 100% accurate about timeouts we should use task.run() here without await and then await the
                //    taskcompletionsource right after    but if we went down this path we would also have to account for exceptions thus complicating
                //    the code considerably for little to no practical gain considering that the native call has trivial setup code and is very fast
                //
                //10  we dont want to wrap our own exceptions obviously   we only want to sanitize native exceptions from java and swift that stem
                //    from missing libraries and symbols because we dont want the raw native exceptions to bubble up to the managed code
            }
        }
    }
}
