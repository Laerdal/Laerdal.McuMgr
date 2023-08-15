using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Xunit;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public class FileDownloaderShould
    {
        [Fact]
        public void ShouldReturnSuccessVerdictOnBeginDownload_GivenEmptyDataBytes()
        {
            // Arrange
            var mockedFileData = new byte[] { };
            const string remoteFilePath = "/some/path/to/file.bin";
            
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<EFileDownloaderVerdict>(() => fileDownloader.BeginDownload(remoteFilePath: remoteFilePath));

            // Assert
            work.Should().NotThrow().Subject.Should().Be(EFileDownloaderVerdict.Success);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();
            
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.DownloadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        [Fact]
        public void ShouldThrowArgumentExceptionExceptionOnBeginDownload_GivenEmptyRemoteFilePath()
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "";
            
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
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
        
        [Fact]
        public async Task ShouldThrowArgumentExceptionExceptionOnDownloadAsync_GivenEmptyRemoteFilePath()
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "";
            
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: remoteFilePath));

            // Assert
            await work.Should().ThrowExactlyAsync<ArgumentException>().WithTimeoutInMs(50);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeFalse();
            
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.DownloadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        [Fact]
        public async Task ShouldCompleteSuccessfullyOnDownloadAsync_GivenGreenNativeFileDownloader()
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";
            
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: remoteFilePath));

            // Assert
            await work.Should().CompleteWithinAsync(5.Seconds());

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Downloading);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Complete);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.DownloadCompleted))
                .WithSender(fileDownloader)
                .WithArgs<DownloadCompletedEventArgs>(args => args.Resource == remoteFilePath && args.Data.SequenceEqual(mockedFileData));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy : MockedNativeFileDownloaderProxySpy
        {
            private readonly byte[] _mockedFileData;
            
            public MockedGreenNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(() => //00 vital
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    Task.Delay(20).GetAwaiter().GetResult();
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete);
                    
                    DownloadCompletedAdvertisement(remoteFilePath, _mockedFileData);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }

        [Fact]
        public async Task ShouldThrowFirmwareErasureErroredOutExceptionOnDownloadAsync_GivenErroneousNativeFileDownloader()
        {
            // Arrange
            var mockedNativeFileDownloaderProxy = new MockedErroneousNativeFileDownloaderProxySpy(new GenericNativeFileDownloaderCallbacksProxy_());
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: "/path/to/file.bin"));

            // Assert
            (await work.Should().ThrowAsync<DownloadErroredOutException>()).WithInnerExceptionExactly<Exception>("foobar");

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFileDownloaderProxySpy : MockedNativeFileDownloaderProxySpy
        {
            public MockedErroneousNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy) : base(downloaderCallbacksProxy)
            {
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                base.BeginDownload(remoteFilePath);

                Thread.Sleep(100);

                throw new Exception("foobar");
            }
        }
        
        [Fact]
        public async Task ShouldThrowTimeoutExceptionOnDownloadAsync_GivenTooSmallTimeout()
        {
            // Arrange
            const string remoteFilePath = "/path/to/file.bin";
            
            var mockedNativeFileDownloaderProxy = new MockedGreenButSlowNativeFileDownloaderProxySpy(new GenericNativeFileDownloaderCallbacksProxy_());
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: remoteFilePath, timeoutForDownloadInMs: 100));

            // Assert
            await work.Should().ThrowAsync<DownloadErroredOutException>().WithTimeoutInMs((int) 5.Seconds().TotalMilliseconds);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

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
        
        private class MockedGreenButSlowNativeFileDownloaderProxySpy : MockedNativeFileDownloaderProxySpy
        {
            public MockedGreenButSlowNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(() => //00 vital
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    StateChangedAdvertisement(resource: remoteFilePath, oldState: EFileDownloaderState.Idle, newState: EFileDownloaderState.Downloading);

                    Task.Delay(1_000).GetAwaiter().GetResult();
                    StateChangedAdvertisement(resource: remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Complete);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }

        private class MockedNativeFileDownloaderProxySpy : INativeFileDownloaderProxy
        {
            private readonly INativeFileDownloaderCallbacksProxy _downloaderCallbacksProxy;

            public bool CancelCalled { get; private set; }
            public bool DisconnectCalled { get; private set; }
            public bool BeginDownloadCalled { get; private set; }

            public string LastFatalErrorMessage => "";

            public IFileDownloaderEventEmitters FileDownloader //keep this to conform to the interface
            {
                get => _downloaderCallbacksProxy?.FileDownloader;
                set
                {
                    if (_downloaderCallbacksProxy == null)
                        return;

                    _downloaderCallbacksProxy.FileDownloader = value;
                }
            }

            protected MockedNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy)
            {
                _downloaderCallbacksProxy = downloaderCallbacksProxy;
            }

            public virtual EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                BeginDownloadCalled = true;

                return EFileDownloaderVerdict.Success;
            }

            public virtual void Cancel()
            {
                CancelCalled = true;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void CancelledAdvertisement() 
                => _downloaderCallbacksProxy.CancelledAdvertisement(); //raises the actual event
            
            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => _downloaderCallbacksProxy.LogMessageAdvertisement(message, category, level, resource); //raises the actual event

            public void StateChangedAdvertisement(string resource, EFileDownloaderState oldState, EFileDownloaderState newState)
                => _downloaderCallbacksProxy.StateChangedAdvertisement(resource: resource, newState: newState, oldState: oldState); //raises the actual event

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _downloaderCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event
            
            public void DownloadCompletedAdvertisement(string resource, byte[] data)
                => _downloaderCallbacksProxy.DownloadCompletedAdvertisement(resource, data); //raises the actual event

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => _downloaderCallbacksProxy.FatalErrorOccurredAdvertisement(errorMessage); //raises the actual event
            
            public void FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput)
                => _downloaderCallbacksProxy.FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(progressPercentage, averageThroughput); //raises the actual event
        }
    }
}