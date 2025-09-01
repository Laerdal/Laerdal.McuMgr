using FluentAssertions;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.BD.STAE.GIP.010", "", "foobar", "acme corp.")]
        [InlineData("FDT.BD.STAE.GIP.020", null, "foobar", "acme corp.")]
        [InlineData("FDT.BD.STAE.GIP.030", "foo/bar/", "foobar", "acme corp.")] //  paths are not allowed
        [InlineData("FDT.BD.STAE.GIP.040", "/foo/bar/", "foobar", "acme corp.")] // to end with a slash
        [InlineData("FDT.BD.STAE.GIP.050", "/foo/bar", "", "acme corp.")] //        invalid hostDeviceModel
        [InlineData("FDT.BD.STAE.GIP.060", "/foo/bar", "  ", "acme corp.")] //      invalid hostDeviceModel
        [InlineData("FDT.BD.STAE.GIP.070", "/foo/bar", "foobar", "")] //            invalid hostDeviceManufacturer
        [InlineData("FDT.BD.STAE.GIP.080", "/foo/bar", "foobar", "  ")] //          invalid hostDeviceManufacturer
        public void BeginDownload_ShouldThrowArgumentException_GivenInvalidParameters(string testcaseNickname, string remoteFilePath, string hostDeviceModel, string hostDeviceManufacturer)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
 
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy1(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Action(() => fileDownloader.BeginDownload(
                remoteFilePath: remoteFilePath,
                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer
            ));

            // Assert
            work.Should().ThrowExactly<ArgumentException>();

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeFalse();

            eventsMonitor.Should().NotRaise(nameof(fileDownloader.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.FileDownloadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class MockedGreenNativeFileDownloaderProxySpy1 : MockedNativeFileDownloaderProxySpy
        {
            private readonly byte[] _mockedFileData;
            
            public MockedGreenNativeFileDownloaderProxySpy1(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
            }

            public override EFileDownloaderVerdict NativeBeginDownload(string remoteFilePath, int? initialMtuSize = null)
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
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading, _mockedFileData.Length, completeDownloadedData: null);

                    await Task.Delay(20);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete, totalBytesToBeDownloaded: 0, _mockedFileData);
                });

                return EFileDownloaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}