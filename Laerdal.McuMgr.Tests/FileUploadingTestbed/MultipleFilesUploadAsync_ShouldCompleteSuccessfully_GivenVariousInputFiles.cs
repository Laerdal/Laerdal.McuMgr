using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task MultipleFilesUploadAsync_ShouldCompleteSuccessfully_GivenVariousInputFiles()
        {
            // Arrange
            var filesThatShouldBeSuccessfullyUploaded = new[]
            {
                "/some/file/path.bin",
                "/Some/File/Path.bin",
                "/some/file/that/succeeds/after/a/couple/of/attempts.bin",
            };
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy6(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            var remoteFilePathsToTest = new Dictionary<string, (string, byte[])> //@formatter:off
            {
                { "\r some/file/path.bin  ",                                          ("./path.bin", [0]) },
                { "  /some/file/path.bin  ",                                          ("./path.bin", [0]) },
                { "\t/some/file/path.bin  ",                                          ("./path.bin", [0]) },
                { "   some/file/path.bin  ",                                          ("./path.bin", [1]) }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once 
                { "   Some/File/Path.bin  ",                                          ("./path.bin", [0]) },
                { "\t/Some/File/Path.bin  ",                                          ("./path.bin", [0]) },
                { "  /Some/File/Path.bin  ",                                          ("./path.bin", [1]) }, //intentionally included multiple times to test that we handle case sensitivity correctly
                { "\t/some/file/that/succeeds/after/a/couple/of/attempts.bin       ", ("./path.bin", [0]) },
                { "  /some/file/that/succeeds/after/a/couple/of/attempts.bin       ", ("./path.bin", [1]) }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once

                { "  /some/file/to/a/folder/that/doesnt/exist.bin                  ", ("./path.bin", [0]) },
                { "\n some/file/that/is/erroring/out/when/we/try/to/upload/it.bin  ", ("./path.bin", [0]) },
                { "\r/some/file/that/is/erroring/out/when/we/try/to/upload/it.bin  ", ("./path.bin", [1]) }, //intentionally included multiple times to test whether the mechanism will attempt to upload the file only once
            }; //@formatter:off

            using var eventsMonitor = fileUploader.Monitor();
            fileUploader.Cancelled += (_, _) => throw new Exception($"{nameof(fileUploader.Cancelled)} -> oops!"); //order   these must be wired up after the events-monitor
            fileUploader.LogEmitted += (object _, in LogEmittedEventArgs _) => throw new Exception($"{nameof(fileUploader.LogEmitted)} -> oops!"); //library should be immune to any and all user-land exceptions 
            fileUploader.StateChanged += (_, _) => throw new Exception($"{nameof(fileUploader.StateChanged)} -> oops!");
            fileUploader.BusyStateChanged += (_, _) => throw new Exception($"{nameof(fileUploader.BusyStateChanged)} -> oops!");
            fileUploader.FileUploadStarted += (_, _) => throw new Exception($"{nameof(fileUploader.FileUploadStarted)} -> oops!");
            fileUploader.FatalErrorOccurred += (_, _) => throw new Exception($"{nameof(fileUploader.FatalErrorOccurred)} -> oops!");
            fileUploader.FileUploadCompleted += (_, _) => throw new Exception($"{nameof(fileUploader.FileUploadCompleted)} -> oops!");
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
                .Where(args => args.EventName == nameof(fileUploader.FileUploadStarted))
                .Select(x => x.Parameters.OfType<FileUploadStartedEventArgs>().FirstOrDefault().RemoteFilePath)
                .Count()
                .Should()
                .Be(11);
            
            eventsMonitor.OccurredEvents
                .Where(args => args.EventName == nameof(fileUploader.FileUploadCompleted))
                .Select(x => x.Parameters.OfType<FileUploadCompletedEventArgs>().FirstOrDefault().RemoteFilePath)
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

        private class MockedGreenNativeFileUploaderProxySpy6 : BaseMockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy6(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            private int _retryCountForProblematicFile;
            public override EFileUploaderVerdict NativeBeginUpload(
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
                base.NativeBeginUpload(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
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
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading, totalBytesToBeUploaded: data.Length);

                    await Task.Delay(20);

                    var remoteFilePathUppercase = remoteFilePath.ToUpperInvariant();
                    if (remoteFilePathUppercase.Contains("some/file/to/a/folder/that/doesnt/exist.bin", StringComparison.InvariantCultureIgnoreCase))
                    {
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0);
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "FOOBAR (3)", EGlobalErrorCode.SubSystemFilesystem_NotFound);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/succeeds/after/a/couple/of/attempts.bin", StringComparison.InvariantCultureIgnoreCase)
                             && _retryCountForProblematicFile++ < 3)
                    {
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0);
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "ping pong", EGlobalErrorCode.Generic);
                    }
                    else if (remoteFilePathUppercase.Contains("some/file/that/is/erroring/out/when/we/try/to/upload/it.bin", StringComparison.InvariantCultureIgnoreCase))
                    {
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0);
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "native symbols not loaded blah blah", EGlobalErrorCode.Generic);
                    }
                    else
                    {
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, totalBytesToBeUploaded: 0);
                    }
                });

                return EFileUploaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}