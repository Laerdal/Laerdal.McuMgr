using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloader
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
            };
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy6(new GenericNativeFileDownloaderCallbacksProxy_(), expectedResults);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

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
            };

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task<IDictionary<string, byte[]>>>(async () => await fileDownloader.DownloadAsync(
                remoteFilePaths: remoteFilePathsToTest,
                maxRetriesPerDownload: 4
            ));

            // Assert
            var results = (await work.Should().CompleteWithinAsync(5.Seconds())).Which;

            results.Should().BeEquivalentTo(expectedResults);

            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileDownloader.DownloadCompleted))
                .Should()
                .Be(expectedResults.Count(x => x.Value != null));
            
            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should()
                .Be(8);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

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
            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null, int? windowCapacity = null, int? memoryAlignment = null)
            {
                var verdict = base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    await Task.Delay(20);

                    var remoteFilePathUppercase = remoteFilePath.ToUpperInvariant();
                    if (remoteFilePathUppercase.Contains("some/file/that/exist/but/is/erroring/out/when/we/try/to/download/it.bin".ToUpperInvariant()))
                    {
                        StateChangedAdvertisement(remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Error);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "foobar");
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/doesnt/exist.bin".ToUpperInvariant()))
                    {
                        StateChangedAdvertisement(remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Error);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "NO ENTRY (5)");
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/exist/and/completes/after/a/couple/of/attempts.bin".ToUpperInvariant())
                             && _retryCountForProblematicFile++ < 3)
                    {
                        StateChangedAdvertisement(remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Error);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "ping pong");
                    }
                    else
                    {
                        _expectedResults.TryGetValue(remoteFilePath, out var expectedFileContent);
                    
                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete); //  order
                        DownloadCompletedAdvertisement(remoteFilePath, expectedFileContent); //                                         order
                    }
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}