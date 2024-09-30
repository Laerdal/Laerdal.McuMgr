using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.SSUA.SHSA.GBAP.010", true)]
        [InlineData("FUT.SSUA.SHSA.GBAP.020", false)]
        public async Task SingleFileUploadAsync_ShouldAutodisposeGivenStream_GivenBooleanAutodisposedParameter(string testcaseNickname, bool shouldAutodisposeStream)
        {
            // Arrange
            var stream = new MemoryStream([1, 2, 3]);
            
            const string remoteFilePath = "/foo/bar/ping.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy110(uploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                data: stream,
                maxTriesCount: 1,
                remoteFilePath: remoteFilePath,
                autodisposeStream: shouldAutodisposeStream
            ));

            // Assert
            await work.Should().CompleteWithinAsync(2.Seconds());

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();
            
            eventsMonitor
                .OccurredEvents.Where(x => x.EventName == nameof(fileUploader.FatalErrorOccurred))
                .Should().HaveCount(0);
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Uploading);

            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Complete);

            eventsMonitor
                .Should().Raise(nameof(fileUploader.FileUploaded))
                .WithSender(fileUploader)
                .WithArgs<FileUploadedEventArgs>(args => args.Resource == remoteFilePath);
            
            stream.CanRead.Should().Be(!shouldAutodisposeStream); //10

            //00 we dont want to disconnect the device regardless of the outcome
            //10 check if the stream was disposed or not based on the value of the autodisposeStream parameter
        }

        private class MockedGreenNativeFileUploaderProxySpy110 : MockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy110(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                var verdict = base.BeginUpload(remoteFilePath, data);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(00, 00);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(20, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(30, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(40, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(50, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(60, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(70, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(80, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(90, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 10);

                    await Task.Delay(20);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete); // order
                    FileUploadedAdvertisement(remoteFilePath); //                                                            order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}