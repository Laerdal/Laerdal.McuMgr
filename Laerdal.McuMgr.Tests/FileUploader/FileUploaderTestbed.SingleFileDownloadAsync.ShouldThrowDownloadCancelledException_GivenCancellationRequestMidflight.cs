using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Contracts;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using Xunit;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task SingleFileUploadAsync_ShouldThrowUploadCancelledException_GivenCancellationRequestMidflight()
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy3(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            _ = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);

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
                .NotRaise(nameof(fileUploader.UploadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy3 : MockedNativeFileUploaderProxySpy
        {
            private string _currentRemoteFilePath;
            private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            
            public MockedGreenNativeFileUploaderProxySpy3(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }
            
            public override void Cancel()
            {
                base.Cancel();
                
                // under normal circumstances the native implementation will bubble up these events in this exact order
                StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileUploaderState.Idle, newState: EFileUploaderState.Cancelling); //  order
                Thread.Sleep(100);
                StateChangedAdvertisement(_currentRemoteFilePath, oldState: EFileUploaderState.Idle, newState: EFileUploaderState.Cancelled); //   order
                CancelledAdvertisement(); //                                                                                                       order
            }
            
            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                (FileUploader as IFileUploaderEvents)!.Cancelled += (sender, args) =>
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

                    await Task.Delay(2_000, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    UploadCompletedAdvertisement(remoteFilePath);

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete);
                }, _cancellationTokenSource.Token);

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}