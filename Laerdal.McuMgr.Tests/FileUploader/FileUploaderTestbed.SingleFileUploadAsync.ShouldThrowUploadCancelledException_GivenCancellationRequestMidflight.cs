using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploader.Contracts;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.SFUA.STUCE.GCRM.010", true)]
        [InlineData("FUT.SFUA.STUCE.GCRM.020", false)]
        public async Task SingleFileUploadAsync_ShouldThrowUploadCancelledException_GivenCancellationRequestMidflight(string testcaseNickname, bool isCancellationLeadingToSoftLanding)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy3(new GenericNativeFileUploaderCallbacksProxy_(), isCancellationLeadingToSoftLanding);
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);

                fileUploader.Cancel();
            });
            var work = new Func<Task>(() => fileUploader.UploadAsync(mockedFileData, remoteFilePath));

            // Assert
            await work.Should().ThrowExactlyAsync<UploadCancelledException>().WithTimeoutInMs((int)5.Seconds().TotalMilliseconds);

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeTrue();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor.Should().Raise(nameof(fileUploader.Cancelled));
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Uploading);

            eventsMonitor
                .Should()
                .NotRaise(nameof(fileUploader.FileUploaded));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy3 : MockedNativeFileUploaderProxySpy
        {
            private string _currentRemoteFilePath;
            private readonly bool _isCancellationLeadingToSoftLanding;
            private CancellationTokenSource _cancellationTokenSource;
            
            public MockedGreenNativeFileUploaderProxySpy3(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy, bool isCancellationLeadingToSoftLanding) : base(uploaderCallbacksProxy)
            {
                _isCancellationLeadingToSoftLanding = isCancellationLeadingToSoftLanding;
            }
            
            public override void Cancel()
            {
                base.Cancel();

                Task.Run(async () => // under normal circumstances the native implementation will bubble up these events in this exact order
                {
                    StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileUploaderState.Idle, newState: EFileUploaderState.Cancelling); //  order

                    await Task.Delay(100);
                    if (_isCancellationLeadingToSoftLanding) //00
                    {
                        StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileUploaderState.Idle, newState: EFileUploaderState.Cancelled); //   order
                        CancelledAdvertisement(); //                                                                                                       order    
                    }
                });
                
                //00   if the cancellation doesnt lead to a soft landing due to for example a broken ble connection the the native implementation will not call
                //     the cancelled event at all   in this case the csharp logic will wait for a few seconds and then throw the cancelled exception manually on
                //     a best effort basis and this is exactly what we are testing here
            }
            
            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                (FileUploader as IFileUploaderEventSubscribable)!.Cancelled += (sender, args) =>
                {
                    _cancellationTokenSource.Cancel();
                };
                
                _currentRemoteFilePath = remoteFilePath;
                _cancellationTokenSource = new CancellationTokenSource();

                var verdict = base.BeginUpload(remoteFilePath, data);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(20_000, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete);
                    FileUploadedAdvertisement(remoteFilePath);
                }, _cancellationTokenSource.Token);

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}