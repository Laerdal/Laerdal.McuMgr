using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.STDCE.GCRM.010", true)]
        [InlineData("FDT.SFDA.STDCE.GCRM.020", false)]
        public async Task SingleFileUploadAsync_ShouldThrowUploadCancelledException_GivenCancellationRequestMidflight(string testcaseNickname, bool isCancellationLeadingToSoftLanding)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy3(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData, isCancellationLeadingToSoftLanding);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);

                fileDownloader.Cancel();
            });
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                remoteFilePath: remoteFilePath
            ));

            // Assert
            await work.Should().ThrowWithinAsync<DownloadCancelledException>(5.Seconds());

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeTrue();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor.Should().Raise(nameof(fileDownloader.Cancelled));
            
            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Downloading);

            eventsMonitor
                .Should()
                .NotRaise(nameof(fileDownloader.DownloadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy3 : MockedNativeFileDownloaderProxySpy
        {
            private string _currentRemoteFilePath;
            private readonly bool _isCancellationLeadingToSoftLanding;

            private readonly byte[] _mockedFileData;
            private CancellationTokenSource _cancellationTokenSource;

            public MockedGreenNativeFileDownloaderProxySpy3(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData, bool isCancellationLeadingToSoftLanding) : base(downloaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
                _isCancellationLeadingToSoftLanding = isCancellationLeadingToSoftLanding;
            }

            public override void Cancel()
            {
                base.Cancel();

                Task.Run(async () => // under normal circumstances the native implementation will bubble up these events in this exact order
                {
                    StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileDownloaderState.Idle, newState: EFileDownloaderState.Cancelling); //      order

                    await Task.Delay(100);
                    if (_isCancellationLeadingToSoftLanding) //00
                    {
                        StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileDownloaderState.Idle, newState: EFileDownloaderState.Cancelled); //   order
                        CancelledAdvertisement(); //                                                                                                           order    
                    }
                });

                //00   if the cancellation doesnt lead to a soft landing due to for example a broken ble connection the the native implementation will not call
                //     the cancelled event at all   in this case the csharp logic will wait for a few seconds and then throw the cancelled exception manually on
                //     a best effort basis and this is exactly what we are testing here
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null)
            {
                _currentRemoteFilePath = remoteFilePath;
                _cancellationTokenSource = new CancellationTokenSource();
                
                (FileDownloader as IFileDownloaderEventSubscribable)!.Cancelled += (sender, args) =>
                {
                    _cancellationTokenSource.Cancel();
                };

                var verdict = base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    await Task.Delay(20_000, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete); //   order
                    DownloadCompletedAdvertisement(remoteFilePath, _mockedFileData); //                                              order
                }, _cancellationTokenSource.Token);

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}