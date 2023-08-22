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

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy3(new GenericNativeFileUploaderCallbacksProxy_(), mockedFileData);
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
            private readonly byte[] _mockedFileData;

            public MockedGreenNativeFileUploaderProxySpy3(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy, byte[] mockedFileData) : base(uploaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
            }

            public override void Cancel()
            {
                base.Cancel();
                
                CancelledAdvertisement();
            }

            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                
                (FileUploader as IFileUploaderEvents).Cancelled += (sender, args) =>
                {
                    cancellationTokenSource.Cancel();
                };
                
                var verdict = base.BeginUpload(remoteFilePath, data);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100, cancellationTokenSource.Token);
                    if (cancellationTokenSource.IsCancellationRequested)
                        return;

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(2_000, cancellationTokenSource.Token);
                    if (cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    UploadCompletedAdvertisement(remoteFilePath);

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete);
                }, cancellationTokenSource.Token);

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}