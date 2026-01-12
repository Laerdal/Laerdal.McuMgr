using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Theory] //@formatter:off           remoteFilePath     hostDeviceModel     hostDeviceManufacturer
        [InlineData("FUT.BD.STAE.GIP.010",  "",                "foobar",           "acme corp."          )]
        [InlineData("FUT.BD.STAE.GIP.020",  null,              "foobar",           "acme corp."          )]
        [InlineData("FUT.BD.STAE.GIP.030",  "foo/bar/",        "foobar",           "acme corp."          )] //  paths are not allowed
        [InlineData("FUT.BD.STAE.GIP.040",  "/foo/bar/",       "foobar",           "acme corp."          )] //  to end with a slash
        [InlineData("FUT.BD.STAE.GIP.050",  "/foo/bar",        "",                 "acme corp."          )] //  invalid hostDeviceModel
        [InlineData("FUT.BD.STAE.GIP.060",  "/foo/bar",        "  ",               "acme corp."          )] //  invalid hostDeviceModel
        [InlineData("FUT.BD.STAE.GIP.070",  "/foo/bar",        "foobar",           ""                    )] //  invalid hostDeviceManufacturer
        [InlineData("FUT.BD.STAE.GIP.080",  "/foo/bar",        "foobar",           "  "                  )] //  invalid hostDeviceManufacturer     @formatter:on
        public void BeginUpload_ShouldThrowArgumentException_GivenInvalidParameters(string testcaseNickname, string remoteFilePath, string hostDeviceModel, string hostDeviceManufacturer)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy1(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(async () => await fileUploader.BeginUploadAsync(
                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer,

                data: mockedFileData,
                resourceId: "foobar",
                remoteFilePath: remoteFilePath
            ));

            // Assert
            work.Should().ThrowWithinAsync<ArgumentException>(TimeSpan.FromSeconds(3));

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeFalse();

            eventsMonitor.Should().NotRaise(nameof(fileUploader.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(fileUploader.FileUploadStarted));
            eventsMonitor.Should().NotRaise(nameof(fileUploader.FileUploadCompleted));
            eventsMonitor.Should().NotRaise(nameof(fileUploader.FileUploadProgressPercentageAndDataThroughputChanged));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class MockedGreenNativeFileUploaderProxySpy1 : BaseMockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy1(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
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
                    resourceId: resourceId,
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
                    await Task.Delay(10);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading, data.Length);

                    await Task.Delay(20);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, 0);
                });

                return EFileUploaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}