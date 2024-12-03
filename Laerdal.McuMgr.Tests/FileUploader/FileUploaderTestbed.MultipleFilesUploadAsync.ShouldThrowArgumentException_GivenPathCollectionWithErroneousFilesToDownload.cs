using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.010", new[] { "/foo/bar.bin", "" })]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.020", new[] { "/foo/bar.bin", null })]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.030", new[] { "/foo/bar.bin", "/ping\f/pong.bin" })]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.030", new[] { "/foo/bar.bin", "/ping\r/pong.bin" })]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.040", new[] { "/foo/bar.bin", "/ping\n/pong.bin" })]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.050", new[] { "/foo/bar.bin", "/ping\r\n/pong.bin" })]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.030", new[] { "/foo/bar.bin", "ping/pong.bin/" })]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.040", new[] { "/foo/bar.bin", "/ping/pong.bin/" })]
        [InlineData("FUT.MFDA.STAE.GPCWEFTD.050", new[] { "/foo/bar.bin", "  ping/pong.bin/  " })] //2nd path gets normalized to  "/ping/pong.bin/" which is invalid due to the trailing slash 
        public async Task MultipleFilesUploadAsync_ShouldThrowArgumentException_GivenPathCollectionWithErroneousFilesToUpload(string testcaseNickname, IEnumerable<string> remoteFilePaths)
        {
            // Arrange
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy11(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(async () => await fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                remoteFilePathsAndTheirData: remoteFilePaths.ToDictionary(x => x, _ => new byte[] { 1 })
            ));

            // Assert
            await work.Should().ThrowWithinAsync<ArgumentException>(500.Milliseconds()); //dont use throwexactlyasync<> here

            eventsMonitor.OccurredEvents.Should().HaveCount(0);

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeFalse();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy11 : MockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy11(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict BeginUpload(
                string remoteFilePath,
                byte[] data,
                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only
                int? initialMtuSize = null, //  android only
                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                var verdict = base.BeginUpload(
                    data: data,
                    remoteFilePath: remoteFilePath,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    initialMtuSize: initialMtuSize, //   android only
                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);
                    
                    await Task.Delay(20);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete);
                    FileUploadedAdvertisement(remoteFilePath);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}