using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public async Task SingleFileDownloadAsync_ShouldThrowArgumentException_GivenEmptyRemoteFilePath()
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy2(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                remoteFilePath: remoteFilePath
            ));

            // Assert
            await work.Should().ThrowWithinAsync<ArgumentException>(500.Milliseconds());

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeFalse();

            eventsMonitor.Should().NotRaise(nameof(fileDownloader.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.FileDownloadStarted));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.FileDownloadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class MockedGreenNativeFileDownloaderProxySpy2 : MockedNativeFileDownloaderProxySpy
        {
            private readonly byte[] _mockedFileData;
            
            public MockedGreenNativeFileDownloaderProxySpy2(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
            }

            public override EFileDownloaderVerdict NativeBeginDownload(string remoteFilePath, ELogLevel? minimumLogLevel = null, int? initialMtuSize = null)
            {
                base.NativeBeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize
                );
                
                StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.None, EFileDownloaderState.None, totalBytesToBeDownloaded: 0, null);
                StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.None, EFileDownloaderState.Idle, totalBytesToBeDownloaded: 0, null);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading, _mockedFileData.Length, null);

                    await Task.Delay(20);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete, 0, _mockedFileData);
                });

                return EFileDownloaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}