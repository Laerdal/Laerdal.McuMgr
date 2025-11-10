using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task MultipleFilesUploadAsync_ShouldPauseAndResumeSuccessfully_GivenVariousInputFiles()
        {
            // Arrange
            var filesThatShouldBeSuccessfullyUploaded = new[] {"/some/file/path.bin"};
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy60(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploaderSpy60(mockedNativeFileUploaderProxy, onBeforeCheckIfPausedCallback: self => //called right before each call to the native .beginUpload()
            {
                self.TryPause();

                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    self.TryResume();
                });
            });

            var remoteFilePathsToTest = new Dictionary<string, (string, byte[])> //@formatter:off
            {
                { "/some/file/path.bin", ("./path1.bin", [0]) },
            }; //@formatter:on

            using var eventsMonitor = fileUploader.Monitor();
            fileUploader.FileUploadPaused += (_, _) => throw new Exception($"{nameof(fileUploader.FileUploadStarted)} -> oops!"); //should be immune to such exceptions in user-land
            fileUploader.FileUploadResumed += (_, _) => throw new Exception($"{nameof(fileUploader.FatalErrorOccurred)} -> oops!");
            fileUploader.FileUploadProgressPercentageAndDataThroughputChanged += (_, ea_) =>
            {
                if (!(ea_.ProgressPercentage is >= 30 and <= 35))
                    return; // we want to pause only after the download has started
                
                fileUploader.TryPause();
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    fileUploader.TryResume();
                });
            };

            // Act
            var work = new Func<Task<IEnumerable<string>>>(async () => await fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                maxTriesPerUpload: 4,
                remoteFilePathsAndTheirData: remoteFilePathsToTest
            ));

            // Assert
            await work.Should().CompleteWithinAsync(3.Seconds());
            
            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == "./path1.bin" && args.RemoteFilePath == "/some/file/path.bin" && args.NewState == EFileUploaderState.Uploading);

            eventsMonitor // checking for pause
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == "./path1.bin"
                                                         && args.RemoteFilePath == "/some/file/path.bin"
                                                         && args.OldState == EFileUploaderState.None
                                                         && args.NewState == EFileUploaderState.Paused);
            
            eventsMonitor
                .Should()
                .Raise(nameof(fileUploader.FileUploadPaused))
                .WithSender(fileUploader)
                .WithArgs<FileUploadPausedEventArgs>(args => args.ResourceId == "./path1.bin" && args.RemoteFilePath == "/some/file/path.bin");

            eventsMonitor //checking for resume
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == "./path1.bin"
                                                         && args.RemoteFilePath == "/some/file/path.bin"
                                                         && args.OldState == EFileUploaderState.Paused
                                                         && args.NewState == EFileUploaderState.None); // in this case we skip the 'resuming' state completely
            
            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileUploader.FileUploadPaused))
                .Should().Be(2);
            
            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileUploader.FileUploadResumed))
                .Should()
                .Be(2);
            
            eventsMonitor.OccurredEvents
                .Where(args => args.EventName == nameof(fileUploader.FileUploadStarted))
                .Select(x => x.Parameters.OfType<FileUploadStartedEventArgs>().FirstOrDefault().RemoteFilePath)
                .Count()
                .Should()
                .Be(1);
            
            eventsMonitor.OccurredEvents
                .Where(args => args.EventName == nameof(fileUploader.FileUploadCompleted))
                .Select(x => x.Parameters.OfType<FileUploadCompletedEventArgs>().FirstOrDefault().RemoteFilePath)
                .Should()
                .BeEquivalentTo(filesThatShouldBeSuccessfullyUploaded);

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class FileUploaderSpy60 : FileUploader
        {
            private readonly Action<FileUploader> _onBeforeCheckIfPausedCallback;

            internal FileUploaderSpy60(INativeFileUploaderProxy nativeFileUploaderProxy, Action<FileUploader> onBeforeCheckIfPausedCallback) : base(nativeFileUploaderProxy)
            {
                _onBeforeCheckIfPausedCallback = onBeforeCheckIfPausedCallback;
            }

            protected override Task CheckIfPausedOrCancelledAsync(string resourceId, string remoteFilePath)
            {
                _onBeforeCheckIfPausedCallback(this);
                
                return base.CheckIfPausedOrCancelledAsync(resourceId: resourceId, remoteFilePath: remoteFilePath);
            }
        }

        private class MockedGreenNativeFileUploaderProxySpy60 : BaseMockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy60(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            private int _retryCountForProblematicFile;
            public override EFileUploaderVerdict NativeBeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,

                ELogLevel? minimumNativeLogLevel = null,
                int? initialMtuSize = null,

                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only

                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                base.NativeBeginUpload(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    initialMtuSize: initialMtuSize,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.None, 0);
                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.Idle, 0);
                
                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading, totalBytesToBeUploaded: data.Length);

                    await Task.Delay(20);

                    var remoteFilePathUppercase = remoteFilePath.ToUpperInvariant();
                    if (remoteFilePathUppercase.Contains("some/file/that/succeeds/after/a/couple/of/attempts.bin", StringComparison.InvariantCultureIgnoreCase)
                             && _retryCountForProblematicFile++ < 3)
                    {
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0);
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "ping pong", EGlobalErrorCode.Generic);
                    }
                    else //@formatter:off
                    {
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:00, currentThroughputInKBps:00, totalAverageThroughputInKBps:00);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:10, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:20, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:30, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:40, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:50, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:60, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:70, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:80, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:90, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        PauseGuard.Wait(); await Task.Delay(005); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId:resourceId, remoteFilePath:remoteFilePath, progressPercentage:100, currentThroughputInKBps:10, totalAverageThroughputInKBps:10);
                        
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, totalBytesToBeUploaded: 0);
                    } //@formatter:off
                });

                return EFileUploaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}