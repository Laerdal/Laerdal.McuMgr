using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task MultipleFilesUploadAsync_ShouldPauseAndResumeSuccessfully_GivenVariousInputFiles()
        {
            // Arrange
            var filesThatShouldBeSuccessfullyUploaded = new[]
            {
                "/some/file/path.bin",
                "/some/file/that/succeeds/after/a/couple/of/attempts.bin",
            };
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy60(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploaderSpy60(mockedNativeFileUploaderProxy, onBeforeCheckIfPausedCallback: self => //called right before each call to the native .beginUpload()
            {
                self.TryPause();

                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    self.TryResume();
                });
            });

            var remoteFilePathsToTest = new Dictionary<string, (string, byte[])> //@formatter:off
            {
                { "/some/file/path.bin",                                              ("./path1.bin", [0]) },
                { "/some/file/that/succeeds/after/a/couple/of/attempts.bin",          ("./path2.bin", [1]) },
                { "/some/file/to/a/folder/that/doesnt/exist.bin",                     ("./path3.bin", [0]) },
                { "/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin",     ("./path4.bin", [0]) },
            }; //@formatter:on

            using var eventsMonitor = fileUploader.Monitor();
            fileUploader.FileUploadPaused += (_, _) => throw new Exception($"{nameof(fileUploader.FileUploadStarted)} -> oops!"); //should be immune to such exceptions in user-land
            fileUploader.FileUploadResumed += (_, _) => throw new Exception($"{nameof(fileUploader.FatalErrorOccurred)} -> oops!");
            fileUploader.FileUploadProgressPercentageAndDataThroughputChanged += async (_, ea_) =>
            {
                if (!(ea_.ProgressPercentage is >= 30 and <= 35))
                    return; // we want to pause only after the download has started
                
                fileUploader.TryPause();
                await Task.Delay(100);
                fileUploader.TryResume();
            };

            // Act
            var work = new Func<Task<IEnumerable<string>>>(async () => await fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                maxTriesPerUpload: 4,
                remoteFilePathsAndTheirData: remoteFilePathsToTest
            ));

            // Assert
            await work.Should().CompleteWithinAsync(6.Seconds());
            
            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == "./path1.bin" && args.RemoteFilePath == "/some/file/path.bin" && args.NewState == EFileUploaderState.Uploading);

            eventsMonitor // checking for pause
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == "./path1.bin"
                                                         && args.RemoteFilePath == "/some/file/path.bin"
                                                         && args.OldState == EFileUploaderState.None
                                                         && args.NewState == EFileUploaderState.Paused);
            
            eventsMonitor
                .Should()
                .Raise(nameof(fileUploader.FileUploadPaused))
                .WithSender(fileUploader)
                .WithArgs<FileUploadPausedEventArgs>(args => args.ResourceId == "./path1.bin" && args.RemoteFilePath == "/some/file/path.bin");

            eventsMonitor //checking for resume
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == "./path1.bin"
                                                         && args.RemoteFilePath == "/some/file/path.bin"
                                                         && args.OldState == EFileUploaderState.Paused
                                                         && args.NewState == EFileUploaderState.None); // in this case we skip the 'resuming' state completely
            
            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileUploader.FileUploadPaused))
                .Should()
                .Be(21);

            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileUploader.FileUploadResumed))
                .Should()
                .Be(
                    eventsMonitor.OccurredEvents.Count(args => args.EventName == nameof(fileUploader.FileUploadPaused))
                );
            
            eventsMonitor.OccurredEvents
                .Where(args => args.EventName == nameof(fileUploader.FileUploadStarted))
                .Select(x => x.Parameters.OfType<FileUploadStartedEventArgs>().FirstOrDefault().RemoteFilePath)
                .Count()
                .Should()
                .Be(10);
            
            eventsMonitor.OccurredEvents
                .Where(args => args.EventName == nameof(fileUploader.FileUploadCompleted))
                .Select(x => x.Parameters.OfType<FileUploadCompletedEventArgs>().FirstOrDefault().RemoteFilePath)
                .Should()
                .BeEquivalentTo(filesThatShouldBeSuccessfullyUploaded);

            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileUploader.FatalErrorOccurred))
                .Should().Be(8);

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class FileUploaderSpy60 : FileUploader
        {
            private readonly Action<FileUploader> _onBeforeCheckIfPausedCallback;

            internal FileUploaderSpy60(INativeFileUploaderProxy nativeFileUploaderProxy, Action<FileUploader> onBeforeCheckIfPausedCallback) : base(nativeFileUploaderProxy)
            {
                _onBeforeCheckIfPausedCallback = onBeforeCheckIfPausedCallback;
            }

            protected override Task CheckIfPausedOrCancelledAsync(string resourceId, string remoteFilePath)
            {
                _onBeforeCheckIfPausedCallback(this);
                
                return base.CheckIfPausedOrCancelledAsync(resourceId: resourceId, remoteFilePath: remoteFilePath);
            }
        }

        private class MockedGreenNativeFileUploaderProxySpy60 : BaseMockedNativeFileUploaderProxySpy
        {
            private string _currentResourceId;
            private string _currentRemoteFilePath;
            
            private readonly ManualResetEventSlim _manualResetEventSlim = new(initialState: true);
            
            public MockedGreenNativeFileUploaderProxySpy60(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }
            
            public override bool TryPause()
            {
                base.TryPause();

                Task.Run(async () => // under normal circumstances the native implementation will bubble-up the following callbacks
                {
                    await Task.Delay(50);
                    _manualResetEventSlim.Reset(); //capture the lock
                    
                    StateChangedAdvertisement(resourceId: _currentResourceId, remoteFilePath: _currentRemoteFilePath, oldState: EFileUploaderState.Uploading, newState: EFileUploaderState.Paused, totalBytesToBeUploaded: 0);
                    BusyStateChangedAdvertisement(busyNotIdle: false);
                });

                return true;
            }

            public override bool TryResume()
            {
                base.TryResume();

                Task.Run(async () => // under normal circumstances the native implementation will bubble-up the following callbacks
                {
                    await Task.Delay(5);
                    StateChangedAdvertisement(resourceId: _currentResourceId, remoteFilePath: _currentRemoteFilePath, oldState: EFileUploaderState.Paused, newState: EFileUploaderState.Resuming, totalBytesToBeUploaded: 0);
                    BusyStateChangedAdvertisement(busyNotIdle: true);

                    await Task.Delay(5);
                    StateChangedAdvertisement(resourceId: _currentResourceId, remoteFilePath: _currentRemoteFilePath, oldState: EFileUploaderState.Resuming, newState: EFileUploaderState.Uploading, totalBytesToBeUploaded: 0);

                    _manualResetEventSlim.Set(); //release the lock so that the upload can continue
                });

                return true;
            }

            private int _retryCountForProblematicFile;
            public override EFileUploaderVerdict BeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,

                int? initialMtuSize = null,

                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only

                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                _currentResourceId = resourceId;
                _currentRemoteFilePath = remoteFilePath;
                
                var verdict = base.BeginUpload(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    initialMtuSize: initialMtuSize,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading, totalBytesToBeUploaded: data.Length);

                    await Task.Delay(20);

                    var remoteFilePathUppercase = remoteFilePath.ToUpperInvariant();
                    if (remoteFilePathUppercase.Contains("some/file/to/a/folder/that/doesnt/exist.bin", StringComparison.InvariantCultureIgnoreCase))
                    {
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0);
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "FOOBAR (3)", EGlobalErrorCode.SubSystemFilesystem_NotFound);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/succeeds/after/a/couple/of/attempts.bin", StringComparison.InvariantCultureIgnoreCase)
                             && _retryCountForProblematicFile++ < 3)
                    {
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0);
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "ping pong", EGlobalErrorCode.Generic);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/is/erroring/out/when/we/try/to/upload/it.bin", StringComparison.InvariantCultureIgnoreCase))
                    {
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0);
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "native symbols not loaded blah blah", EGlobalErrorCode.Generic);
                    }
                    else //@formatter:off
                    {
                        _manualResetEventSlim.Wait(); await Task.Delay(015); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:00, currentThroughputInKBps:00, totalAverageThroughputInKBps:00);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:10, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:20, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:30, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:40, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:50, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:60, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:70, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:80, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:90, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        _manualResetEventSlim.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:100, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, totalBytesToBeUploaded: 0);
                    } //@formatter:off
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}