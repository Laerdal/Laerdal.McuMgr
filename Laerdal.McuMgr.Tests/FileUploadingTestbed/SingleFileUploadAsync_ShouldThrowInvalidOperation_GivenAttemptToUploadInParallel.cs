using FluentAssertions;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task SingleFileUploadAsync_ShouldThrowInvalidOperation_GivenAttemptToUploadInParallel()
        {
            // Arrange
            var resourceId = "foobar";
            var mockedFileData = new byte[] {1, 2, 3};
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy99(resourceId, new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            fileUploader.FileUploadStarted += (_, _) =>
            {
                fileUploader.TryPause();
            };
            
            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(async () =>
            {
                _ = fileUploader.UploadAsync( //first attempt
                    hostDeviceModel: "foobar",
                    hostDeviceManufacturer: "acme corp.",
                    data: mockedFileData,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath
                );

                await Task.Delay(50);
                
                await fileUploader.UploadAsync( //second attempt in parallel should cause an exception
                    hostDeviceModel: "foobar",
                    hostDeviceManufacturer: "acme corp.",
                    data: mockedFileData,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath
                );
            });

            // Assert
            await work.Should().ThrowWithinAsync<InvalidOperationException>(TimeSpan.FromSeconds(3));
        }

        private class MockedGreenNativeFileUploaderProxySpy99 : BaseMockedNativeFileUploaderProxySpy
        {
            private string _currentRemoteFilePath;

            private readonly string _resourceId;
            private readonly ManualResetEventSlim _manualResetEventSlim = new(initialState: true);

            public MockedGreenNativeFileUploaderProxySpy99(string resourceId, INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
                _resourceId = resourceId;
            }

            public override bool TryPause()
            {
                base.TryPause();

                Task.Run(async () => // under normal circumstances the native implementation will bubble-up the following callbacks
                {
                    await Task.Delay(50);
                    _manualResetEventSlim.Reset(); //capture the lock
                    
                    StateChangedAdvertisement(resourceId: _resourceId, remoteFilePath: _currentRemoteFilePath, oldState: EFileUploaderState.Uploading, newState: EFileUploaderState.Paused, totalBytesToBeUploaded: 0);
                    BusyStateChangedAdvertisement(busyNotIdle: false);
                });

                return true;
            }

            public override bool TryResume()
            {
                base.TryResume();

                Task.Run(async () => // under normal circumstances the native implementation will bubble-up the following callbacks
                {
                    await Task.Delay(5);
                    StateChangedAdvertisement(resourceId: _resourceId, remoteFilePath: _currentRemoteFilePath, oldState: EFileUploaderState.Paused, newState: EFileUploaderState.Resuming, totalBytesToBeUploaded: 0);
                    BusyStateChangedAdvertisement(busyNotIdle: true);
                    
                    await Task.Delay(5);
                    StateChangedAdvertisement(resourceId: _resourceId, remoteFilePath: _currentRemoteFilePath, oldState: EFileUploaderState.Resuming, newState: EFileUploaderState.Uploading, totalBytesToBeUploaded: 0);

                    _manualResetEventSlim.Set(); //release the lock so that the upload can continue
                });

                return true;
            }
            
            public override EFileUploaderVerdict NativeBeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,

                int? initialMtuSize = null,

                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only

                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                _currentRemoteFilePath = remoteFilePath;

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
