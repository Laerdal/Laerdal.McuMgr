using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.SSUA.SCS.GGNFD.010", "path/to/file.bin", 01, +100)] // this should be normalized to /path/to/file.bin
        [InlineData("FUT.SSUA.SCS.GGNFD.020", "/path/to/file.bin", 2, -100)] // negative sleep time should be interpreted as 0
        [InlineData("FUT.SSUA.SCS.GGNFD.030", "/path/to/file.bin", 2, +000)]
        [InlineData("FUT.SSUA.SCS.GGNFD.040", "/path/to/file.bin", 2, +100)]
        [InlineData("FUT.SSUA.SCS.GGNFD.050", "/path/to/file.bin", 3, -100)]
        [InlineData("FUT.SSUA.SCS.GGNFD.060", "/path/to/file.bin", 3, +000)]
        [InlineData("FUT.SSUA.SCS.GGNFD.070", "/path/to/file.bin", 3, +100)]
        public async Task SingleFileUploadAsync_ShouldCompleteSuccessfully_GivenGreenStream(string testcaseNickname, string remoteFilePath, int maxTriesCount, int sleepTimeBetweenRetriesInMs)
        {
            // Arrange
            var stream = new MemoryStream([1, 2, 3]);
            
            var expectedRemoteFilepath = remoteFilePath.StartsWith('/')
                ? remoteFilePath
                : $"/{remoteFilePath}";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy100(
                uploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy_(),
                maxNumberOfTriesForSuccess: maxTriesCount
            );
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                data: stream,
                maxTriesCount: maxTriesCount,
                remoteFilePath: remoteFilePath,
                sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs
            ));

            // Assert
            await work.Should().CompleteWithinAsync(((maxTriesCount + 1) * 2).Seconds());

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();
            
            eventsMonitor
                .OccurredEvents.Where(x => x.EventName == nameof(fileUploader.FatalErrorOccurred))
                .Should().HaveCount(maxTriesCount - 1); //one error for each try except the last one
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == expectedRemoteFilepath && args.NewState == EFileUploaderState.Uploading);

            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == expectedRemoteFilepath && args.NewState == EFileUploaderState.Complete);

            eventsMonitor
                .Should().Raise(nameof(fileUploader.FileUploaded))
                .WithSender(fileUploader)
                .WithArgs<FileUploadedEventArgs>(args => args.Resource == expectedRemoteFilepath);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy100 : MockedNativeFileUploaderProxySpy
        {
            private readonly int _maxNumberOfTriesForSuccess;

            public MockedGreenNativeFileUploaderProxySpy100(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy, int maxNumberOfTriesForSuccess) : base(uploaderCallbacksProxy)
            {
                _maxNumberOfTriesForSuccess = maxNumberOfTriesForSuccess;
            }

            private int _tryCount;
            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                _tryCount++;
                
                var verdict = base.BeginUpload(remoteFilePath, data);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);
                    
                    await Task.Delay(20);
                    if (_tryCount < _maxNumberOfTriesForSuccess)
                    {
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                                     order
                        FatalErrorOccurredAdvertisement(remoteFilePath, "fatal error occurred", EMcuMgrErrorCode.Corrupt, EFileUploaderGroupReturnCode.Unset); // order
                        return;
                    }
                    
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete); // order
                    FileUploadedAdvertisement(remoteFilePath); //                                                            order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}