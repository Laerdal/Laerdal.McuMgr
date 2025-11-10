using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task SingleFileUploadAsync_ShouldPauseAndResumeSuccessfully_GivenPauseAndResumeRequestsDuringUpload()
        {
            // Arrange
            var resourceId = "foobar";
            var mockedFileData = new byte[] {1, 2, 3};
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy9(resourceId, new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);
            
            using var eventsMonitor = fileUploader.Monitor();
            fileUploader.FileUploadPaused += (_, _) => throw new Exception($"{nameof(fileUploader.FileUploadStarted)} -> oops!"); //should be immune to such exceptions in user-land
            fileUploader.FileUploadResumed += (_, _) => throw new Exception($"{nameof(fileUploader.FatalErrorOccurred)} -> oops!");
            fileUploader.FileUploadProgressPercentageAndDataThroughputChanged += async (_, ea_) =>
            {
                if (ea_.ProgressPercentage <= 30)
                    return; // we want to pause only after the upload has started
                
                fileUploader.TryPause();
                await Task.Delay(100);
                fileUploader.TryResume();
            };

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                data: mockedFileData,
                resourceId: resourceId,
                remoteFilePath: remoteFilePath
            ));

            // Assert
            await work.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));

            mockedNativeFileUploaderProxy.PauseCalled.Should().BeTrue();
            mockedNativeFileUploaderProxy.ResumeCalled.Should().BeTrue();

            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Uploading);
            
            eventsMonitor // checking for pause
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Paused);
            
            eventsMonitor
                .Should()
                .Raise(nameof(fileUploader.FileUploadPaused))
                .WithSender(fileUploader)
                .WithArgs<FileUploadPausedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath);

            eventsMonitor //checking for resume
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Resuming);
            
            eventsMonitor
                .Should()
                .Raise(nameof(fileUploader.FileUploadResumed))
                .WithSender(fileUploader)
                .WithArgs<FileUploadResumedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath);

            eventsMonitor // checking for completion
                .Should()
                .Raise(nameof(fileUploader.FileUploadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy9 : BaseMockedNativeFileUploaderProxySpy
        {
            private readonly string _resourceId;
            private readonly ManualResetEventSlim _manualResetEventSlim = new(initialState: true);

            public MockedGreenNativeFileUploaderProxySpy9(string resourceId, INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
                _resourceId = resourceId;
            }
            
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
                    resourceId: _resourceId,
                    remoteFilePath: remoteFilePath,

                    initialMtuSize: initialMtuSize,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.None, 0);
                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.Idle, 0);
                
                Task.Run(async () => //00 vital   @formatter:off
                {
                    _manualResetEventSlim.Wait(); await Task.Delay(010); StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading, totalBytesToBeUploaded: data.Length);
                    _manualResetEventSlim.Wait(); await Task.Delay(015); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 00, 00, 00);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 10, 10, 10);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 20, 10, 10);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 30, 10, 10);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 40, 10, 10); 
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 50, 10, 10);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 60, 10, 10);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 70, 10, 10);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 80, 10, 10);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 90, 10, 10);
                    _manualResetEventSlim.Wait(); await Task.Delay(100); FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 100, 10, 10);

                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, totalBytesToBeUploaded: 0);
                }); //@formatter:on

                return EFileUploaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}
