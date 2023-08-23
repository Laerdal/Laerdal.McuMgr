using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using Xunit;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public async Task SingleFileDownloadAsync_ShouldThrowDownloadCancelledException_GivenCancellationRequestMidflight()
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy3(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            _ = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);

                fileDownloader.Cancel();
            });
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: remoteFilePath));

            // Assert
            await work.Should().ThrowExactlyAsync<DownloadCancelledException>().WithTimeoutInMs((int)10.Seconds().TotalMilliseconds);

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

            private readonly byte[] _mockedFileData;
            private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            public MockedGreenNativeFileDownloaderProxySpy3(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
            }

            public override void Cancel()
            {
                base.Cancel();
                
                Task.Run(async () => // under normal circumstances the native implementation will bubble up these events in this exact order
                {
                    StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileDownloaderState.Idle, newState: EFileDownloaderState.Cancelling); //  order

                    await Task.Delay(100);
                    StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileDownloaderState.Idle, newState: EFileDownloaderState.Cancelled); //   order
                    CancelledAdvertisement(); //                                                                                                           order
                });
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                _currentRemoteFilePath = remoteFilePath;
                _cancellationTokenSource = new CancellationTokenSource();
                
                (FileDownloader as IFileDownloaderEvents)!.Cancelled += (sender, args) =>
                {
                    _cancellationTokenSource.Cancel();
                };
                
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    await Task.Delay(2_000, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    DownloadCompletedAdvertisement(remoteFilePath, _mockedFileData);

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete);
                }, _cancellationTokenSource.Token);

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}