using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.SFUA.STUAAFE.GFEM.010", 1)]
        [InlineData("FUT.SFUA.STUAAFE.GFEM.020", 2)]
        public async Task SingleFileUploadAsync_ShouldThrowAllUploadAttemptsFailedException_GivenFatalErrorMidflight(string testcaseDescription, int maxTriesCount)
        {
            // Arrange
            var allLogEas = new List<LogEmittedEventArgs>(8);
            var resourceId = "foobar";
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy4(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() =>
            {
                fileUploader.LogEmitted += (object _, in LogEmittedEventArgs ea) => allLogEas.Add(ea);
                
                return fileUploader.UploadAsync(
                    hostDeviceModel: "foobar",
                    hostDeviceManufacturer: "acme corp.",
                    
                    data: mockedFileData,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    maxTriesCount: maxTriesCount
                );
            });

            // Assert
            await work.Should()
                .ThrowWithinAsync<AllUploadAttemptsFailedException>(((maxTriesCount + 1) * 3).Seconds())
                .WithMessage("*failed to upload*");

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(fileUploader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileUploader.FileUploadCompleted));

            eventsMonitor
                .Should().Raise(nameof(fileUploader.FatalErrorOccurred))
                .WithSender(fileUploader)
                .WithArgs<FatalErrorOccurredEventArgs>(args => args.ErrorMessage == "fatal error occurred");
            
            // eventsMonitor
            //     .OccurredEvents
            //     .Where(x => x.EventName == nameof(deviceResetter.LogEmitted))
            //     .SelectMany(x => x.Parameters)
            //     .OfType<LogEmittedEventArgs>() //xunit or fluent-assertions has memory corruption issues with this probably because of the zero-copy delegate! :(

            allLogEas
                .Count(l => l is {Level: ELogLevel.Error} && l.Message.Contains("fatal error occurred", StringComparison.InvariantCulture))
                .Should()
                .BeGreaterThanOrEqualTo(1);
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Uploading);
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Error);
            
            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy4 : MockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy4(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict BeginUpload(
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
                var verdict = base.BeginUpload(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    initialMtuSize: initialMtuSize,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100);

                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(2_000);
                    
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //  order
                    FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "fatal error occurred", EGlobalErrorCode.Generic); //  order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}