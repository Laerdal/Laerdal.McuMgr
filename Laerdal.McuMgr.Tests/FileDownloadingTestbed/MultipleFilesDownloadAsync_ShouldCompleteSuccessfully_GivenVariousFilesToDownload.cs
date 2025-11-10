using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public async Task MultipleFilesDownloadAsync_ShouldCompleteSuccessfully_GivenVariousFilesToDownload()
        {
            // Arrange
            var expectedResults = new Dictionary<string, byte[]>
            {
                { "/some/file/that/exists.bin", [1] },
                { "/Some/File/That/Exists.bin", [2] },
                { "/some/file/that/doesnt/exist.bin", null },
                { "/some/file/that/exist/and/completes/after/a/couple/of/attempts.bin", [3] },
                { "/some/file/that/exist/but/is/erroring/out/when/we/try/to/download/it.bin", null },
                { "/some/file/path/pointing/to/a/Directory", null },
                { "/some/file/path/pointing/to/a/directory", null },
            };
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy6(new GenericNativeFileDownloaderCallbacksProxy_(), expectedResults);
            
            var fileDownloader = new FileDownloader(mockedNativeFileDownloaderProxy);

            var remoteFilePathsToTest = new[]
            {
                "some/file/that/exists.bin",
                "/some/file/that/exists.bin",
                "/some/file/that/exists.bin",
                "  some/file/that/exists.bin   ", //intentionally included multiple times to test whether the mechanism will attempt to download the file only once 
                "Some/File/That/Exists.bin",
                "/Some/File/That/Exists.bin",
                "/Some/File/That/Exists.bin", //intentionally included multiple times to test that we handle case sensitivity correctly
                "some/file/that/doesnt/exist.bin",
                "/some/file/that/doesnt/exist.bin", //intentionally included multiple times to test whether the mechanism will attempt to download the file only once
                "/some/file/that/exist/and/completes/after/a/couple/of/attempts.bin",
                "/some/file/that/exist/and/completes/after/a/couple/of/attempts.bin", //intentionally included multiple times to test whether the mechanism will attempt to download the file only once
                "/some/file/that/exist/but/is/erroring/out/when/we/try/to/download/it.bin",
                "/some/file/that/exist/but/is/erroring/out/when/we/try/to/download/it.bin", //intentionally included multiple times to test whether the mechanism will attempt to download the file only once
                "some/file/path/pointing/to/a/Directory",
                "/some/file/path/pointing/to/a/directory",
            };

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
            var work = new Func<Task<IDictionary<string, byte[]>>>(async () => await fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                remoteFilePaths: remoteFilePathsToTest,
                maxTriesPerDownload: 4
            ));

            // Assert
            var results = (await work.Should().CompleteWithinAsync(20.Seconds())).Which;

            results.Should().BeEquivalentTo(expectedResults);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();
            
            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileDownloader.FileDownloadStarted))
                .Should()
                .Be(13); //7 files to download with multiple attempts for some of them
            
            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileDownloader.FileDownloadCompleted))
                .Should()
                .Be(expectedResults.Count(x => x.Value != null));
            
            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should()
                .Be(10);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy6 : MockedNativeFileDownloaderProxySpy
        {
            private readonly IDictionary<string, byte[]> _expectedResults;
            
            public MockedGreenNativeFileDownloaderProxySpy6(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, IDictionary<string, byte[]> expectedResults) : base(downloaderCallbacksProxy)
            {
                _expectedResults = expectedResults;
            }

            private int _retryCountForProblematicFile; 
            public override EFileDownloaderVerdict NativeBeginDownload(string remoteFilePath, ELogLevel? minimumNativeLogLevel = null, int? initialMtuSize = null)
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
                        _expectedResults.TryGetValue(remoteFilePath, out var expectedFileContent);

                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete, 0, expectedFileContent);
                    }
                });

                return EFileDownloaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}