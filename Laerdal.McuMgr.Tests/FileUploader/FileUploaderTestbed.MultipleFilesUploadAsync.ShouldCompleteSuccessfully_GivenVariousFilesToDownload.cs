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
                { "\r some/file/path.bin  ", [0] },
                { "  /some/file/path.bin  ", [0] },
                { "\t/some/file/path.bin  ", [0] },
                { "   some/file/path.bin  ", [1] }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once 
                { "   Some/File/Path.bin  ", [0] },
                { "\t/Some/File/Path.bin  ", [0] },
                { "  /Some/File/Path.bin  ", [1] }, //intentionally included multiple times to test that we handle case sensitivity correctly
                { "\t/some/file/that/succeeds/after/a/couple/of/attempts.bin       ", [0] },
                { "  /some/file/that/succeeds/after/a/couple/of/attempts.bin       ", [1] }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once

                { "  /some/file/to/a/folder/that/doesnt/exist.bin                  ", [0] },
                { "\n some/file/that/is/erroring/out/when/we/try/to/upload/it.bin  ", [0] },
                { "\r/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin  ", [1] }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once
            };

            using var eventsMonitor = fileUploader.Monitor();
            fileUploader.Cancelled += (_, _) => throw new Exception($"{nameof(fileUploader.Cancelled)} -> oops!"); //order   these must be wired up after the events-monitor
            fileUploader.LogEmitted += (_, _) => throw new Exception($"{nameof(fileUploader.LogEmitted)} -> oops!"); //library should be immune to any and all user-land exceptions 
            fileUploader.StateChanged += (_, _) => throw new Exception($"{nameof(fileUploader.StateChanged)} -> oops!");
            fileUploader.FileUploaded += (_, _) => throw new Exception($"{nameof(fileUploader.FileUploaded)} -> oops!");
            fileUploader.BusyStateChanged += (_, _) => throw new Exception($"{nameof(fileUploader.BusyStateChanged)} -> oops!");
            fileUploader.FatalErrorOccurred += (_, _) => throw new Exception($"{nameof(fileUploader.FatalErrorOccurred)} -> oops!");
            fileUploader.FileUploadProgressPercentageAndDataThroughputChanged += (_, _) => throw new Exception($"{nameof(fileUploader.FileUploadProgressPercentageAndDataThroughputChanged)} -> oops!");

            // Act
            var work = new Func<Task<IEnumerable<string>>>(async () => await fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                maxTriesPerUpload: 4,
                remoteFilePathsAndTheirData: remoteFilePathsToTest
            ));
            var filesThatFailedToBeUploaded = (await work.Should().CompleteWithinAsync(6.Seconds())).Which;

            // Assert
            filesThatFailedToBeUploaded.Should().BeEquivalentTo(expectation:
            [
                "/some/file/to/a/folder/that/doesnt/exist.bin",
                "/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin"
            ]);

            eventsMonitor.OccurredEvents
                .Where(args => args.EventName == nameof(fileUploader.FileUploaded))
                .Select(x => x.Parameters.OfType<FileUploadedEventArgs>().FirstOrDefault().Resource)
                .Should()
                .BeEquivalentTo(filesThatShouldBeSuccessfullyUploaded);

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

                    var remoteFilePathUppercase = remoteFilePath.ToUpperInvariant();
                    if (remoteFilePathUppercase.Contains("some/file/to/a/folder/that/doesnt/exist.bin".ToUpperInvariant()))
                    {
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "FOOBAR (3)", EGlobalErrorCode.SubSystemFilesystem_NotFound);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/succeeds/after/a/couple/of/attempts.bin".ToUpperInvariant())
                             && _retryCountForProblematicFile++ < 3)
                    {
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "ping pong", EGlobalErrorCode.Generic);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/is/erroring/out/when/we/try/to/upload/it.bin".ToUpperInvariant()))
                    {
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "native symbols not loaded blah blah", EGlobalErrorCode.Generic);
                    }
                    else
                    {
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete); //order
                        FileUploadedAdvertisement(remoteFilePath); //order
                    }
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}