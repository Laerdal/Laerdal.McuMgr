using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public async Task MultipleFilesDownloadAsync_ShouldPauseAndResumeSuccessfullyBetweenFileDownloads_GivenVariousFilesToDownload()
        {
            // Arrange
            var expectedResults = new Dictionary<string, byte[]> {
                {"/some/file/that/exists.bin", [1]},
            };
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy60(new GenericNativeFileDownloaderCallbacksProxy_(), expectedResults);

            var fileDownloader = new FileDownloaderSpy60(mockedNativeFileDownloaderProxy, onBeforeCheckIfPausedCallback: self => //called right before each call to the native .beginDownload()
            {
                self.TryPause();

                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    self.TryResume();
                });
            });

            var remoteFilePathsToTest = new[]
            {
                "/some/file/that/exists.bin",
            };

            using var eventsMonitor = fileDownloader.Monitor();

            fileDownloader.FileDownloadPaused += (_, _) => throw new Exception($"{nameof(fileDownloader.FileDownloadStarted)} -> oops!"); //should be immune to such exceptions in user-land
            fileDownloader.FileDownloadResumed += (_, _) => throw new Exception($"{nameof(fileDownloader.FatalErrorOccurred)} -> oops!");
            fileDownloader.FileDownloadProgressPercentageAndDataThroughputChanged += (_, ea_) =>
            {
                if (!(ea_.ProgressPercentage is >= 30 and <= 35))
                    return; // we want to pause only after the download has started

                fileDownloader.TryPause();

                Task.Run(async () =>
                {
                    await Task.Delay(010);
                    fileDownloader.TryResume();
                });
            };

            // Act
            var work = new Func<Task<IDictionary<string, byte[]>>>(async () => await fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                remoteFilePaths: remoteFilePathsToTest,
                maxTriesPerDownload: 4
            ));

            // Assert
            await work.Should().CompleteWithinAsync(5000.Seconds());

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.RemoteFilePath == "/some/file/that/exists.bin" && args.NewState == EFileDownloaderState.Downloading);

            eventsMonitor // checking for pause
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.RemoteFilePath == "/some/file/that/exists.bin"
                                                         && args.OldState == EFileDownloaderState.None
                                                         && args.NewState == EFileDownloaderState.Paused);

            eventsMonitor
                .Should()
                .Raise(nameof(fileDownloader.FileDownloadPaused))
                .WithSender(fileDownloader)
                .WithArgs<FileDownloadPausedEventArgs>(args => args.RemoteFilePath == "/some/file/that/exists.bin");

            eventsMonitor //checking for resume
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.RemoteFilePath == "/some/file/that/exists.bin"
                                                         && args.OldState == EFileDownloaderState.Paused
                                                         && args.NewState == EFileDownloaderState.None); // in this case we skip the 'resuming' state completely

            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileDownloader.FileDownloadPaused))
                .Should()
                .Be(2);

            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileDownloader.FileDownloadResumed))
                .Should()
                .Be(2);

            eventsMonitor.OccurredEvents
                .Where(args => args.EventName == nameof(fileDownloader.FileDownloadStarted))
                .Select(x => x.Parameters.OfType<FileDownloadStartedEventArgs>().FirstOrDefault().RemoteFilePath)
                .Count()
                .Should()
                .Be(1);

            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should().Be(0);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class FileDownloaderSpy60 : FileDownloader
        {
            private readonly Action<FileDownloader> _onBeforeCheckIfPausedCallback;

            internal FileDownloaderSpy60(INativeFileDownloaderProxy nativeFileDownloaderProxy, Action<FileDownloader> onBeforeCheckIfPausedCallback) : base(nativeFileDownloaderProxy)
            {
                _onBeforeCheckIfPausedCallback = onBeforeCheckIfPausedCallback;
            }

            protected override Task CheckIfPausedOrCancelledAsync(string remoteFilePath)
            {
                _onBeforeCheckIfPausedCallback(this);

                return base.CheckIfPausedOrCancelledAsync(remoteFilePath: remoteFilePath);
            }
        }

        private class MockedGreenNativeFileDownloaderProxySpy60 : MockedNativeFileDownloaderProxySpy
        {
            private readonly IDictionary<string, byte[]> _expectedResults;

            public MockedGreenNativeFileDownloaderProxySpy60(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, IDictionary<string, byte[]> expectedResults) : base(downloaderCallbacksProxy)
            {
                _expectedResults = expectedResults;
            }

            private int _retryCountForProblematicFile;

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
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading, totalBytesToBeDownloaded: 1_024, null);

                    await Task.Delay(20);

                    var remoteFilePathUppercase = remoteFilePath.ToUpperInvariant().Trim();
                    if (remoteFilePathUppercase.Contains("some/file/that/exist/but/is/erroring/out/when/we/try/to/download/it.bin".ToUpperInvariant()))
                    {
                        StateChangedAdvertisement(remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Error, 0, null);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "foobar", EGlobalErrorCode.Unset);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/doesnt/exist.bin".ToUpperInvariant()))
                    {
                        StateChangedAdvertisement(remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Error, 0, null);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "IN VALUE (3)", EGlobalErrorCode.SubSystemFilesystem_NotFound);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/exist/and/completes/after/a/couple/of/attempts.bin".ToUpperInvariant())
                             && _retryCountForProblematicFile++ < 3)
                    {
                        StateChangedAdvertisement(remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Error, 0, null);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "ping pong", EGlobalErrorCode.McuMgrErrorBeforeSmpV2_Corrupt);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/path/pointing/to/a/directory".ToUpperInvariant()))
                    {
                        StateChangedAdvertisement(remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Error, 0, null);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "BLAH BLAH (4)", EGlobalErrorCode.SubSystemFilesystem_IsDirectory);
                    }
                    else
                    {
                        _expectedResults.TryGetValue(remoteFilePath, out var expectedFileContent); //@formatter:off

                        PauseDownloadGuard.Wait(); await Task.Delay(015); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 00, 00, 00);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 10, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 20, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 30, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 40, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 50, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 60, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 70, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 80, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 90, 10, 10);
                        PauseDownloadGuard.Wait(); await Task.Delay(005); FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 100, 10, 10);

                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete, 0, expectedFileContent);
                    } //@formatter:on
                });

                return EFileDownloaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}