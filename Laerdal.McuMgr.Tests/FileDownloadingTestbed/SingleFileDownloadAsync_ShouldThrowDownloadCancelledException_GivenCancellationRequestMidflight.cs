using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.STDCE.GCRM.010", true)]
        [InlineData("FDT.SFDA.STDCE.GCRM.020", false)]
        public async Task SingleFileUploadAsync_ShouldThrowUploadCancelledException_GivenCancellationRequestMidflight(string testcaseNickname, bool isCancellationLeadingToSoftLanding)
        {
            // Arrange
            var mockedFileData = new byte[] {1, 2, 3};
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy3(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData, isCancellationLeadingToSoftLanding);
            var fileDownloader = new FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();
            fileDownloader.Cancelled += (_, _) => throw new Exception($"{nameof(fileDownloader.Cancelled)} -> oops!"); //order   these must be wired up after the events-monitor
            fileDownloader.LogEmitted += (object _, in LogEmittedEventArgs _) => throw new Exception($"{nameof(fileDownloader.LogEmitted)} -> oops!"); //library should be immune to any and all user-land exceptions 
            fileDownloader.StateChanged += (_, _) => throw new Exception($"{nameof(fileDownloader.StateChanged)} -> oops!");
            fileDownloader.BusyStateChanged += (_, _) => throw new Exception($"{nameof(fileDownloader.BusyStateChanged)} -> oops!");
            fileDownloader.FatalErrorOccurred += (_, _) => throw new Exception($"{nameof(fileDownloader.FatalErrorOccurred)} -> oops!");
            fileDownloader.FileDownloadStarted += (_, _) => throw new Exception($"{nameof(fileDownloader.FileDownloadStarted)} -> oops!");
            fileDownloader.FileDownloadCompleted += (_, _) => throw new Exception($"{nameof(fileDownloader.FileDownloadCompleted)} -> oops!");
            fileDownloader.FileDownloadProgressPercentageAndDataThroughputChanged += (_, _) => throw new Exception($"{nameof(fileDownloader.FileDownloadProgressPercentageAndDataThroughputChanged)} -> oops!");

            // Act
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);

                fileDownloader.TryCancel("foobar reason");
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

            eventsMonitor
                .Should()
                .Raise(nameof(fileDownloader.Cancelling))
                .WithSender(fileDownloader)
                .WithArgs<CancellingEventArgs>(args => args.Reason == "foobar reason");
            
            eventsMonitor
                .Should()
                .Raise(nameof(fileDownloader.Cancelled))
                .WithSender(fileDownloader)
                .WithArgs<CancelledEventArgs>(args => args.Reason == "foobar reason");
            
            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Downloading);

            eventsMonitor
                .Should()
                .NotRaise(nameof(fileDownloader.FileDownloadCompleted));

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

            public override bool TryCancel(string reason = "")
            {
                base.TryCancel(reason);

                Task.Run(async () => // under normal circumstances the native implementation will bubble up these events in this exact order
                {
                    StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileDownloaderState.Idle, newState: EFileDownloaderState.Cancelling); //    order
                    CancellingAdvertisement(reason); //                                                                                                      order

                    await Task.Delay(100);
                    if (_isCancellationLeadingToSoftLanding) //00
                    {
                        StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileDownloaderState.Idle, newState: EFileDownloaderState.Cancelled); //   order
                        CancelledAdvertisement(reason); //                                                                                                     order    
                    }
                });

                return true;

                //00   if the cancellation doesnt lead to a soft landing due to for example a broken ble connection the the native implementation will not call
                //     the cancelled event at all   in this case the csharp logic will wait for a few seconds and then throw the cancelled exception manually on
                //     a best effort basis and this is exactly what we are testing here
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null)
            {
                _currentRemoteFilePath = remoteFilePath;
                _cancellationTokenSource = new CancellationTokenSource();
                
                (FileDownloader as IFileDownloaderEventSubscribable)!.Cancelled += (_, _) =>
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
                    FileDownloadStartedAdvertisement(remoteFilePath, _mockedFileData.Length);

                    await Task.Delay(20_000, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete); //   order
                    FileDownloadCompletedAdvertisement(remoteFilePath, _mockedFileData); //                                          order
                }, _cancellationTokenSource.Token);

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}