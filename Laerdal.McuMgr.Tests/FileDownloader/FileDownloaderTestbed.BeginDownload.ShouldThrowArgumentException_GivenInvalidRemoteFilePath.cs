using System;
using System.Threading.Tasks;
using FluentAssertions;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using Xunit;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.BD.STAE.GIRFP.010", "")]
        [InlineData("FDT.BD.STAE.GIRFP.020", null)]
        [InlineData("FDT.BD.STAE.GIRFP.030", "foo/bar/")] //  paths are not allowed
        [InlineData("FDT.BD.STAE.GIRFP.040", "/foo/bar/")] // to end with a slash 
        public void BeginDownload_ShouldThrowArgumentException_GivenInvalidRemoteFilePath(string testcaseNickname, string remoteFilePath)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy1(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<EFileDownloaderVerdict>(() => fileDownloader.BeginDownload(remoteFilePath: remoteFilePath));

            // Assert
            work.Should().ThrowExactly<ArgumentException>();

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeFalse();

            eventsMonitor.Should().NotRaise(nameof(fileDownloader.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.DownloadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class MockedGreenNativeFileDownloaderProxySpy1 : MockedNativeFileDownloaderProxySpy
        {
            private readonly byte[] _mockedFileData;
            
            public MockedGreenNativeFileDownloaderProxySpy1(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    Task.Delay(20).GetAwaiter().GetResult();
                    DownloadCompletedAdvertisement(remoteFilePath, _mockedFileData);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}