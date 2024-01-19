using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Helpers;
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
        [InlineData("FDT.SFUA.STRFNFE.GNEF.010", "UNKNOWN (1)", 1)] //android + ios
        [InlineData("FDT.SFUA.STRFNFE.GNEF.020", "UNKNOWN (1)", 2)] //android + ios
        [InlineData("FDT.SFUA.STRFNFE.GNEF.030", "UNKNOWN (1)", 3)] //android + ios
        public async Task SingleFileUploadAsync_ShouldThrowRemoteFolderNotFoundException_GivenNonExistFolderInPath(string testcaseNickname, string nativeErrorMessageForFileNotFound, int maxTriesCount)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/non-existent/folder/file.bin";

            var mockedNativeFileUploaderProxy = new MockedErroneousNativeFileUploaderProxySpy2(
                mockedFileData: mockedFileData,
                uploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy_(),
                nativeErrorMessageForFileNotFound: nativeErrorMessageForFileNotFound
            );
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                localData: mockedFileData,
                maxTriesCount: maxTriesCount, //doesnt really matter   we just want to ensure that the method fails early and doesnt retry
                remoteFilePath: remoteFilePath,
                sleepTimeBetweenRetriesInMs: 10
            ));

            // Assert
            await work.Should()
                .ThrowExactlyAsync<UploadErroredOutRemoteFolderNotFoundException>()
                .WithTimeoutInMs((int)3.Seconds().TotalMilliseconds);

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(fileUploader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileUploader.UploadCompleted));

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
            private readonly string _nativeErrorMessageForFileNotFound;
            
            public MockedErroneousNativeFileUploaderProxySpy2(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy, byte[] mockedFileData, string nativeErrorMessageForFileNotFound) : base(uploaderCallbacksProxy)
            {
                _ = mockedFileData;
                _nativeErrorMessageForFileNotFound = nativeErrorMessageForFileNotFound;
            }

            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                var verdict = base.BeginUpload(remoteFilePath, data);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100);

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(100);
                    
                    FatalErrorOccurredAdvertisement(remoteFilePath, _nativeErrorMessageForFileNotFound);

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}