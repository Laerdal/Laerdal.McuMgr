using System;
using Laerdal.McuMgr.Bindings.Android;
using Laerdal.McuMgr.FirmwareList.Contracts.Native;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FirmwareList
{
    internal class AndroidNativeFirmwareListDownloaderAdapterProxy : AndroidFirmwareListDownloader, INativeFirmwareListDownloaderProxy
    {
        // ReSharper disable once UnusedMember.Local
        private AndroidNativeFirmwareListDownloaderAdapterProxy(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        internal AndroidNativeFirmwareListDownloaderAdapterProxy(Context context, BluetoothDevice bluetoothDevice)
            : base(context, bluetoothDevice)
        {
        }

        public string Get(int initialMtuSize, ELogLevel minimumNativeLogLevel)
        {
            var result = TryGetImageListJson(initialMtuSize, (int)minimumNativeLogLevel);

            if (result.Verdict != EAndroidFirmwareListDownloaderVerdict.Success)
                throw new InvalidOperationException($"[AndroidNativeFirmwareListDownloaderAdapterProxy.Get] Native call failed with verdict '{result.Verdict}':\n\n{result.PossibleErrorMessage}");

            return result.JsonData;
        }

        public new void Dispose()
        {
            Dispose(disposing: true); //doesnt throw

            try
            {
                base.Dispose();
            }
            catch
            {
                //ignored
            }
                
            GC.SuppressFinalize(this);
        }

        private bool _alreadyDisposed;
        protected override void Dispose(bool disposing)
        {
            if (_alreadyDisposed)
                return;

            if (!disposing)
                return;
                
            TryCleanupInfrastructure(); 
                
            _alreadyDisposed = true;

            try
            {
                base.Dispose(disposing: true);
            }
            catch
            {
                //ignored
            }
        }

        private void TryCleanupInfrastructure()
        {
            try
            {
                // ReSharper disable once RedundantBaseQualifier
                base.NativeDispose(); //java-glue-library method 
            }
            catch
            {
                // ignored
            }
        }
    }
}