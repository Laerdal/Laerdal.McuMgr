using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.SFUA.STUCE.GCRM.010", true)]
        [InlineData("FUT.SFUA.STUCE.GCRM.020", false)]
        public async Task SingleFileUploadAsync_ShouldThrowUploadCancelledException_GivenCancellationRequestMidflight(string testcaseNickname, bool isCancellationLeadingToSoftLanding)
        {
            // Arrange
            var resourceId = "foobar";
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";
            const string cancellationReason = "blah blah foobar";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy3(resourceId, new GenericNativeFileUploaderCallbacksProxy_(), isCancellationLeadingToSoftLanding);
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);

                fileUploader.TryCancel(reason: cancellationReason);
            });
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                data: mockedFileData,
                resourceId: resourceId,
                remoteFilePath: remoteFilePath
            ));

            // Assert
            await work.Should().ThrowWithinAsync<UploadCancelledException>(5.Seconds());

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeTrue();
            mockedNativeFileUploaderProxy.CancellationReason.Should().Be(cancellationReason);

            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor.Should().Raise(nameof(fileUploader.Cancelled));
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Uploading);

            eventsMonitor
                .Should()
                .NotRaise(nameof(fileUploader.FileUploadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy3 : BaseMockedNativeFileUploaderProxySpy
        {
            private string _currentRemoteFilePath;

            private readonly bool _isCancellationLeadingToSoftLanding;
            private readonly string _resourceId;

            private CancellationTokenSource _cancellationTokenSource;
            
            public MockedGreenNativeFileUploaderProxySpy3(string resourceId, INativeFileUploaderCallbacksProxy uploaderCallbacksProxy, bool isCancellationLeadingToSoftLanding) : base(uploaderCallbacksProxy)
            {
                _resourceId = resourceId;
                _isCancellationLeadingToSoftLanding = isCancellationLeadingToSoftLanding;
            }
            
            public override bool TryCancel(string reason = "")
            {
                base.TryCancel(reason);

                Task.Run(async () => // under normal circumstances the native implementation will bubble up these events in this exact order
                {
                    CancellingAdvertisement(reason); //                                                                                                                                        order
                    StateChangedAdvertisement(_resourceId, _currentRemoteFilePath, oldState: EFileUploaderState.Idle, newState: EFileUploaderState.Cancelling, totalBytesToBeUploaded: 0); //  order

                    await Task.Delay(100);
                    if (_isCancellationLeadingToSoftLanding) //00
                    {
                        StateChangedAdvertisement(_resourceId, _currentRemoteFilePath, oldState: EFileUploaderState.Idle, newState: EFileUploaderState.Cancelled, totalBytesToBeUploaded: 0); //   order
                        CancelledAdvertisement(reason); //                                                                                                                                         order    
                    }
                });

                return true;

                //00   if the cancellation doesnt lead to a soft landing due to for example a broken ble connection the the native implementation will not call
                //     the cancelled event at all   in this case the csharp logic will wait for a few seconds and then throw the cancelled exception manually on
                //     a best effort basis and this is exactly what we are testing here
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
                (FileUploader as IFileUploaderEventSubscribable)!.Cancelled += (_, _) =>
                {
                    _cancellationTokenSource.Cancel();
                };
                
                _currentRemoteFilePath = remoteFilePath;
                _cancellationTokenSource = new CancellationTokenSource();

                base.NativeBeginUpload(
                    data: data,
                    resourceId: _resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    minimumNativeLogLevel: minimumNativeLogLevel,
                    
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
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;

                    StateChangedAdvertisement(_resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading, totalBytesToBeUploaded: data.Length);

                    await Task.Delay(20_000, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    StateChangedAdvertisement(_resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, totalBytesToBeUploaded: 0);
                }, _cancellationTokenSource.Token);

                return EFileUploaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}