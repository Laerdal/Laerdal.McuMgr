using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.STRFNFE.GNEF.010", 1)] //android
        [InlineData("FDT.SFDA.STRFNFE.GNEF.020", 2)] //android
        [InlineData("FDT.SFDA.STRFNFE.GNEF.030", 3)] //android
        [InlineData("FDT.SFDA.STRFNFE.GNEF.040", 2)] //ios
        public async Task SingleFileDownloadAsync_ShouldThrowRemoteFileNotFoundException_GivenNonExistentFilepath(string testcaseNickname, int maxTriesCount)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/non-existent/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedErroneousNativeFileDownloaderProxySpy2(
                mockedFileData: mockedFileData,
                downloaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy_()
            );
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                maxTriesCount: maxTriesCount, //doesnt really matter   we just want to ensure that the method fails early and doesnt retry
                remoteFilePath: remoteFilePath,
                sleepTimeBetweenRetriesInMs: 10
            ));

            // Assert
            await work.Should()
                .ThrowWithinAsync<DownloadErroredOutRemoteFileNotFoundException>(3.Seconds());

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(fileDownloader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.DownloadCompleted));

            eventsMonitor.OccurredEvents
                .Count(x => x.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should()
                .Be(1); //we just want to ensure that the method fails early and doesnt retry because there is no point to retry if the file doesnt exist

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.FatalErrorOccurred))
                .WithSender(fileDownloader)
                .WithArgs<FatalErrorOccurredEventArgs>(args => args.GlobalErrorCode == EGlobalErrorCode.SubSystemFilesystem_NotFound);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Downloading);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Error);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFileDownloaderProxySpy2 : MockedNativeFileDownloaderProxySpy
        {
            public MockedErroneousNativeFileDownloaderProxySpy2(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _ = mockedFileData;
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null)
            {
                var verdict = base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100);

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error); //     order    simulates how the native code behaves
                    FatalErrorOccurredAdvertisement(remoteFilePath, "foobar", EGlobalErrorCode.SubSystemFilesystem_NotFound); //    order    simulates how the csharp wrapper behaves
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}