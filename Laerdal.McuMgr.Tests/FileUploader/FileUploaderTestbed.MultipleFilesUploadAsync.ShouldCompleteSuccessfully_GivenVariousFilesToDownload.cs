using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
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
            var filesThatShouldBeSuccessfullyUploaded = new[]
            {
                "/some/file/path.bin",
                "/Some/File/Path.bin",
                "/some/file/that/succeeds/after/a/couple/of/attempts.bin",
            };
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy6(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            var remoteFilePathsToTest = new Dictionary<string, byte[]>
            {
                { "\r some/file/path.bin  ", new byte[] { 0 } },
                { "  /some/file/path.bin  ", new byte[] { 0 } },
                { "\t/some/file/path.bin  ", new byte[] { 0 } },
                { "   some/file/path.bin  ", new byte[] { 1 } }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once 
                { "   Some/File/Path.bin  ", new byte[] { 0 } },
                { "\t/Some/File/Path.bin  ", new byte[] { 0 } },
                { "  /Some/File/Path.bin  ", new byte[] { 1 } }, //intentionally included multiple times to test that we handle case sensitivity correctly
                { "\t/some/file/that/succeeds/after/a/couple/of/attempts.bin       ", new byte[] { 0 } },
                { "  /some/file/that/succeeds/after/a/couple/of/attempts.bin       ", new byte[] { 1 } }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once
                
                { "  /some/file/to/a/folder/that/doesnt/exist.bin                  ", new byte[] { 0 } },
                { "\n some/file/that/is/erroring/out/when/we/try/to/upload/it.bin  ", new byte[] { 0 } },
                { "\r/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin  ", new byte[] { 1 } }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once
            };

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task<IEnumerable<string>>>(async () => await fileUploader.UploadAsync(
                maxRetriesPerUpload: 3,
                remoteFilePathsAndTheirDataBytes: remoteFilePathsToTest
            ));

            // Assert
            var filesThatFailedToBeUploaded = (await work.Should().CompleteWithinAsync(3.Seconds())).Which;

            filesThatFailedToBeUploaded.Should().BeEquivalentTo(expectation: new[]
            {
                "/some/file/to/a/folder/that/doesnt/exist.bin",
                "/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin"
            });
            
            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileUploader.UploadCompleted))
                .Should()
                .Be(filesThatShouldBeSuccessfullyUploaded.Length);

            eventsMonitor.OccurredEvents
                .Count(args => args.EventName == nameof(fileUploader.FatalErrorOccurred))
                .Should().Be(8);
            
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

            private int _retryCountForProblematicFile;
            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                var verdict = base.BeginUpload(remoteFilePath, data);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);
                    
                    await Task.Delay(20);
                    
                    var remoteFilePathUppercase = remoteFilePath.ToUpperInvariant();
                    if (remoteFilePathUppercase.Contains("some/file/to/a/folder/that/doesnt/exist.bin".ToUpperInvariant()))
                    {
                        FatalErrorOccurredAdvertisement(remoteFilePath, "UNKNOWN (1)");
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/succeeds/after/a/couple/of/attempts.bin".ToUpperInvariant())
                             && _retryCountForProblematicFile++ < 3)
                    {
                        FatalErrorOccurredAdvertisement(remoteFilePath, "ping pong");
                    }  
                    else if (remoteFilePathUppercase.Contains("some/file/that/is/erroring/out/when/we/try/to/upload/it.bin".ToUpperInvariant()))
                    {
                        FatalErrorOccurredAdvertisement(remoteFilePath, "foobar");
                    }
                    else
                    {
                        UploadCompletedAdvertisement(remoteFilePath);
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete);
                    }
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}