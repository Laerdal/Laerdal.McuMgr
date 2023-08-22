using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using Xunit;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploader
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUS.SFDA.STDEOE.GFEM.010", 0)]
        [InlineData("FUS.SFDA.STDEOE.GFEM.020", 1)]
        public async Task SingleFileUploadAsync_ShouldThrowUploadErroredOutException_GivenFatalErrorMidflight(string testcaseDescription, int maxRetriesCount)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy4(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                localData: mockedFileData,
                remoteFilePath: remoteFilePath,
                maxRetriesCount: maxRetriesCount
            ));

            // Assert
            await work.Should()
                .ThrowExactlyAsync<UploadErroredOutException>()
                .WithMessage("*failed to upload*")
                .WithTimeoutInMs((int)((maxRetriesCount + 1) * 3).Seconds().TotalMilliseconds);

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(fileUploader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileUploader.UploadCompleted));

            eventsMonitor
                .Should().Raise(nameof(fileUploader.FatalErrorOccurred))
                .WithSender(fileUploader)
                .WithArgs<FatalErrorOccurredEventArgs>(args => args.ErrorMessage == "fatal error occurred");
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Uploading);
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Error);
            
            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy4 : MockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy4(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                var verdict = base.BeginUpload(remoteFilePath, data);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100);

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(2_000);
                    
                    FatalErrorOccurredAdvertisement(remoteFilePath, "fatal error occurred");

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}