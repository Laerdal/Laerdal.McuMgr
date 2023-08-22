using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FileUploader.Contracts;
using Xunit;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task MultipleFilesUploadAsync_ShouldCompleteSuccessfully_GivenVariousFilesToUpload()
        {
            // Arrange
            var expectedResults = new[]
            {
                "/some/file/path.bin",
                "/Some/File/Path.bin",
                "/some/file/that/doesnt/exist.bin",
                "/some/file/that/succeeds/after/a/couple/of/attempts.bin",
                "/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin",
            };
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy6(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            var remoteFilePathsToTest = new Dictionary<string, byte[]>
            {
                { "some/file/path.bin", new byte[] { 0 } },
                { "/some/file/path.bin", new byte[] { 0 } },
                { "/some/file/path.bin", new byte[] { 0 } },
                { "  some/file/path.bin   ", new byte[] { 1 } }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once 
                { "Some/File/Path.bin", new byte[] { 0 } },
                { "/Some/File/Path.bin", new byte[] { 0 } },
                { "/Some/File/Path.bin", new byte[] { 2 } }, //intentionally included multiple times to test that we handle case sensitivity correctly
                { "/some/file/that/succeeds/after/a/couple/of/attempts.bin", new byte[] { 0 } },
                { "/some/file/that/succeeds/after/a/couple/of/attempts.bin", new byte[] { 1 } }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once
                { "/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin", new byte[] { 0 } },
                { "/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin", new byte[] { 1 } }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once
            };

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(async () => await fileUploader.UploadAsync(remoteFilePathsToTest));

            // Assert
            await work.Should().CompleteWithinAsync(1.Seconds());

            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileUploader.UploadCompleted))
                .Should()
                .Be(expectedResults.Length);

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy6 : MockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy6(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                var verdict = base.BeginUpload(remoteFilePath, data);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);
                    
                    await Task.Delay(20);
                    
                    UploadCompletedAdvertisement(remoteFilePath);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}