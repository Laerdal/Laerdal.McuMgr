using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareListDownloadingTestbed
{
    public partial class FirmwareListDownloaderTestbed
    {
        private class MockedNativeFirmwareListDownloaderProxySpy : INativeFirmwareListDownloaderProxy //template class for all spies
        {
            public bool DownloadFirmwareListCalled { get; private set; }

            protected MockedNativeFirmwareListDownloaderProxySpy()
            {
            }

            public virtual string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                DownloadFirmwareListCalled = true;

                return "[]";
            }

            public void Dispose()
            {
                // dud
            }
        }
    }
}
