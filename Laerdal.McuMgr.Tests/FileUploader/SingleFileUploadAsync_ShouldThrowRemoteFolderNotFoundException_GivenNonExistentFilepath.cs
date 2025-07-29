using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploader
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFUA.STRFNFE.GNEF.010", EGlobalErrorCode.SubSystemFilesystem_NotFound, "UNKNOWN (1)", 1)] //android + ios
        [InlineData("FDT.SFUA.STRFNFE.GNEF.020", EGlobalErrorCode.SubSystemFilesystem_NotFound, "UNKNOWN (1)", 2)] //android + ios
        [InlineData("FDT.SFUA.STRFNFE.GNEF.030", EGlobalErrorCode.SubSystemFilesystem_NotFound, "UNKNOWN (1)", 3)] //android + ios
        public async Task SingleFileUploadAsync_ShouldThrowRemoteFolderNotFoundException_GivenNonExistFolderInPath(
            string testcaseNickname,
            EGlobalErrorCode globalErrorCode,
            string nativeErrorMessageForFileNotFound,
            int maxTriesCount
        )
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/non-existent/folder/file.bin";

            var mockedNativeFileUploaderProxy = new MockedErroneousNativeFileUploaderProxySpy2(
                mockedFileData: mockedFileData,
                uploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy_(),
                globalErrorCode: globalErrorCode,
                nativeErrorMessageForFileNotFound: nativeErrorMessageForFileNotFound
            );
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                data: mockedFileData, //doesnt really matter   we just want to ensure that the method fails early and doesnt retry
                maxTriesCount: maxTriesCount,
                remoteFilePath: remoteFilePath,
                sleepTimeBetweenRetriesInMs: 10
            ));

            // Assert
            await work.Should()
                .ThrowWithinAsync<UploadErroredOutRemoteFolderNotFoundException>(3.Seconds());

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(fileUploader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileUploader.FileUploaded));

            eventsMonitor.OccurredEvents
                .Count(x => x.EventName == nameof(fileUploader.FatalErrorOccurred))
                .Should()
                .Be(1); //we just want to ensure that the method fails early and doesnt retry because there is no point to retry if the file doesnt exist

            eventsMonitor
                .Should().Raise(nameof(fileUploader.FatalErrorOccurred))
                .WithSender(fileUploader)
                .WithArgs<FatalErrorOccurredEventArgs>(args => args.ErrorMessage.ToUpperInvariant().Contains(nativeErrorMessageForFileNotFound.ToUpperInvariant()));

            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Uploading);

            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Error);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFileUploaderProxySpy2 : MockedNativeFileUploaderProxySpy
        {
            private readonly EGlobalErrorCode _globalErrorCode;
            private readonly string _nativeErrorMessageForFileNotFound;
            
            public MockedErroneousNativeFileUploaderProxySpy2(
                INativeFileUploaderCallbacksProxy uploaderCallbacksProxy,
                byte[] mockedFileData,
                EGlobalErrorCode globalErrorCode,
                string nativeErrorMessageForFileNotFound
            ) : base(uploaderCallbacksProxy)
            {
                _ = mockedFileData;
                _globalErrorCode = globalErrorCode;
                _nativeErrorMessageForFileNotFound = nativeErrorMessageForFileNotFound;
            }

            public override EFileUploaderVerdict BeginUpload(
                string remoteFilePath,
                byte[] data,
                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only
                int? initialMtuSize = null, //  android only
                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                var verdict = base.BeginUpload(
                    data: data,
                    remoteFilePath: remoteFilePath,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    initialMtuSize: initialMtuSize, //   android only
                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100);

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //      order
                    FatalErrorOccurredAdvertisement(remoteFilePath, _nativeErrorMessageForFileNotFound, _globalErrorCode); //  order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}