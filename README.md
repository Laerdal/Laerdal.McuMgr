# 🏠 Laerdal.McuMgr

![Platforms](https://img.shields.io/badge/Platforms-_MAUI_|_iOS_|_MacCatalyst_|_Android_-blue.svg)

( other platforms like Windows will only compile but they will throw NotImplemented exceptions at runtime )

- Release Build Status (main branch):

  [![Build, Pack & Deploy Nugets](https://github.com/Laerdal/Laerdal.McuMgr/actions/workflows/github-actions.yml/badge.svg?branch=main)](https://github.com/Laerdal/Laerdal.McuMgr/actions/workflows/github-actions.yml)

- Beta Build Status (develop branch):

  [![Build, Pack & Deploy Nugets](https://github.com/Laerdal/Laerdal.McuMgr/actions/workflows/github-actions.yml/badge.svg?branch=develop)](https://github.com/Laerdal/Laerdal.McuMgr/actions/workflows/github-actions.yml)


# Forward Licensing Disclaimer

Read the LICENSE file before you begin.

# Summary

The project generates multiple Nugets called 'Laerdal.McuMgr' & 'Laerdal.McuMgr.Bindings.iOS|Android|NetStandard' (note: NetStandard is still WIP).
The goal is to have 'Laerdal.McuMgr' provide an elegant high-level C# abstraction for the native device-managers that Nordic provides us with for
iOS and Android respectively to interact with [nRF5x series of BLE chips](https://embeddedcentric.com/nrf5x-soc-overview/) **as long as they run on
firmware that has been built using 'nRFConnect SDK' or the 'Zephyr SDK'** (devices running on firmware built with the 'nRF5 SDK' however are inherently incompatible!):

- [IOS-nRF-Connect-Device-Manager](https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager)

- [Android-nRF-Connect-Device-Manager](https://github.com/NordicSemiconductor/Android-nRF-Connect-Device-Manager)

From the respective 'Readme' files of these projects:

<< nRF Connect Device Manager library is compatible with [McuManager (McuMgr, for short)](https://docs.zephyrproject.org/3.2.0/services/device_mgmt/mcumgr.html#overview), a management subsystem
supported by [nRF Connect SDK](https://developer.nordicsemi.com/nRF_Connect_SDK/doc/latest/nrf/index.html), [Zephyr](https://docs.zephyrproject.org/3.2.0/introduction/index.html) and Apache Mynewt.

**It is the recommended protocol for Device Firmware Update(s) on new Nordic-powered devices going forward and should not be confused with the previous protocol, 
NordicDFU, serviced by the [Old DFU Library](https://github.com/NordicSemiconductor/IOS-DFU-Library)**.

McuManager uses the [Simple Management Protocol, or SMP](https://docs.zephyrproject.org/3.2.0/services/device_mgmt/smp_protocol.html), to send and receive message requests from compatible devices.
The SMP Transport definition for Bluetooth Low Energy, which this library implements, [can be found here](https://docs.zephyrproject.org/latest/services/device_mgmt/smp_transport.html).

The library provides a transport agnostic implementation of the McuManager protocol. It contains a default implementation for BLE transport. >>

The following types of operations are supported on devices running on Nordic's nRF5x series of BLE chips:

- Upgrading the firmware
- Erasing one of the firmware images stored in the device
- Rebooting ('Resetting') the device
- Downloading one or more files from the device
- Uploading one or more files over to the device

      Note: The library doesn't support "Windows Desktop" applications (Windows/UWP) just yet (WIP).

      Note: In theaory all nRF5x chipsets support 'dual bank firmware storage (active / backup)', but in practice this co-depends on the custom firmware being installed in the sense
      that if the firmware uses more than half of the flash-bank-memory then only a single flask-bank will be available (no backup flash bank). Same if the firmware-devs explicitly
      disable the 'dual flask-bank' feature programmatically!


## ✅ Nuget Platform-Support Matrix

Note that even though the Laerdal.McuMgr.Bindings.* have been built on dotnet8 using Android-SDK=34 and iPhoneOS-SDK=17.0 the generated nugets have been tested and they do work
even on Dotnet10-preview7 MAUI-Apps targeting Android-SDK=36 and iOS-SDK=18.5

| Stack     | Android                                                                   | iOS                                      | MacCatalyst (MacOS / iPad / iOS)                | Windows / UWP (NetStandard2.0) |
|-----------|---------------------------------------------------------------------------|------------------------------------------|-------------------------------------------------|--------------------------------|   
| DotNet 8+ | ✅ Min 5.0 / Recommended 11.0+ / Max 15.0 <br/> (api-levels: 20 / 30 / 35) | ✅ 14.5+ <br/> ( sdk: iphoneos-sdk 18.1 ) | ✅ 14.6+ <br/> ( MacOS: 14.6+, iOS/iPadOS: 13+ ) | 🚧 (Much much later ...)       | 

## ⚡ FW Installation Performance: File-Uploading Stage

Using iPhone Xs Max (18.5) and Laerdal.McuMgr 2.55.x (Nordic iOS Libs ver. 1.9.2+) vs an nRF52840-based device (Zephyr 3.2.0):

| Initial MTU Size | Pipeline Depth | Memory Byte Alignment | Avg. Throughput (kb/sec) | Notes                   |
|------------------|----------------|-----------------------|--------------------------|-------------------------|
| 495 (max)        | 2              | 2                     | ~60                      | Spikes above 100 kb/sec |
| 495              | 3              | 2                     | ~63.5                    |                         |
| 495              | 4              | 2                     | ~63.7                    |                         |
| 495              | 4              | 4                     | ~75.6                    |                         |
| 80               | 2              | -                     | ~33                      |                         |
| 80               | 3              | 2                     | ~54.3                    |                         |
| 80               | 4              | 4                     | ~66.5                    |                         |
| 250              | 4              | 4                     | ~86                      | Best performance!       |


## ❗️ Salient Points

- **For the firmware-upgrade to actually persist through the rebooting of the device it's absolutely vital to set the upgrade mode to 'Test & Confirm'. If you set it to just 'Test' then the effects of the firmware-upgrade will only last up to the next reboot and the the device will revert back to its previous firmware image.**

- **Make sure to explicitly un-bond any app (including the NRF apps!) from the devices you are trying to upgrade. Any device in the vicinity that's still bonded will cause problems
in case you try to perform a firmware-upgrade on the desired device.**

- **Make sure to clean up after your apps when using the firmware-upgrader, device-resetter or firmware-eraser. Calling .Disconnect() is vital to avoid leaving behind latent connections
to the device.**

- **At the time of this writing the generated ios-nugets are built based on the iphoneos16.2 sdk**

- **For the time being Nordics' Android/Java libs are compiled in a way that emits Java1.8 bytecode so as to keep the libraries backwards compatible with versions of Android all the way back to 7. Our Java "glue-code" under 'Laerdal.McuMgr.Bindings.Android.Native' is compiled in the same fashion.**
  
- **To compile the iOS/MacCatalyst libs on localdev with their default settings you will need MacOS with XCode version 16.2 and iPhoneOS SDK 18.1.**
    The reason McuMgr libs only support iPhones that can run iOS17 or better is simply because as of April 2024 all iOS and iPadOS apps submitted to the App Store must be built with a minimum of Xcode 15.x and the iOS 17.x SDK! The iOS 17.x SDK only supports iPhones/iPads that can run version 17.x of their respective OSes or better. 

## 🚀 Using the Nugets in your Projects

Add the following Nuget packages.

       Laerdal.McuMgr
       Laerdal.McuMgr.Bindings.iOS                 (only add this to those projects of yours that target iOS)
       Laerdal.McuMgr.Bindings.Android             (only add this to those projects of yours that target Android)
       Laerdal.McuMgr.Bindings.MacCatalyst         (only add this to those projects of yours that target MacCatalyst aka MacDesktop+iPad)
       Laerdal.McuMgr.Bindings.NetStandard (WIP!)  (only add this to those projects of yours that target Windows/UWP)

Make sure to always get the latest versions of the above packages.

       [Note] For MacCatalyst you will also need to add this to your MAUI .csproj file so as to avoid compilation issues:

              <!-- https://github.com/xamarin/xamarin-macios/issues/19451#issuecomment-1811959873 -->
              <_UseClassicLinker>false</_UseClassicLinker>

## 🤖 Android

- Installing a firmware:

```c#

private Laerdal.McuMgr.FirmwareInstallation.IFirmwareInstaller _firmwareInstaller;

public async Task InstallFirmwareAsync()
{
    var firmwareRawBytes = ...; //byte[]
    var desiredBluetoothDevice = ...; //android/ios bluetooth device here 

    try
    {
        _firmwareInstaller = new FirmwareInstaller.FirmwareInstaller(desiredBluetoothDevice);

        ToggleSubscriptionsOnFirmwareInstallerEvents(subscribeNotUnsubscribe: true);

        await _firmwareInstaller.InstallAsync(
            data: rawFirmwareBytes,
            hostDeviceModel: DeviceInfo.Model,
            hostDeviceManufacturer: DeviceInfo.Manufacturer,
            maxTriesCount: FirmwareInstallationMaxTries,
            initialMtuSize: FirmwareInstallationInitialMtuSize <= 0 ? null : FirmwareInstallationInitialMtuSize, // both for android and ios
            pipelineDepth: FirmwareInstallationPipelineDepth <= 0 ? null : FirmwareInstallationPipelineDepth, //       ios only
            byteAlignment: FirmwareInstallationByteAlignment <= 0 ? null : FirmwareInstallationByteAlignment, //       ios only
            windowCapacity: FirmwareInstallationWindowCapacity <= 0 ? null : FirmwareInstallationWindowCapacity, //    android only
            memoryAlignment: FirmwareInstallationMemoryAlignment <= 0 ? null : FirmwareInstallationMemoryAlignment, // android only
            estimatedSwapTimeInMilliseconds: FirmwareInstallationEstimatedSwapTimeInSecs * 1000
        ).ConfigureAwait(false); //vital to avoid deadlocks
    }
    catch (FirmwareInstallationCancelledException) //order
    {
        App.DisplayAlert(
            title: "Firmware-Installation Cancelled",
            message: "Operation cancelled!"
        );
        return;
    }
    catch (FirmwareInstallationConfirmationStageTimeoutException) //order
    {
        App.DisplayAlert(
            title: "Firmware-Installation Failed",
            message: $"The firmware was installed but the device didn't confirm it within {FirmwareInstallationEstimatedSwapTimeInSecs}secs. " +
                     "This means that the new firmware will only last for just one power-cycle of the device."
        );
        return;
    }
    catch (FirmwareInstallationErroredOutException ex) //order
    {
        App.DisplayAlert(
            title: "Firmware-Installation Failed",
            message: $"An error occurred:{Environment.NewLine}{Environment.NewLine}{ex}"
        );
        return;
    }
    catch (Exception ex) //order
    {
        App.DisplayAlert(
            title: "[BUG] Firmware-Installation Failed",
            message: $"An unexpected error occurred:{Environment.NewLine}{Environment.NewLine}{ex}"
        );
        return;
    }
    finally
    {
        ToggleSubscriptionsOnFirmwareInstallerEvents(subscribeNotUnsubscribe: false);
        CleanupFirmwareInstaller();
    }
}

private void ToggleSubscriptionsOnFirmwareInstallerEvents(bool subscribeNotUnsubscribe)
{
    if (_firmwareInstaller == null)
        return;
    
    if (subscribeNotUnsubscribe)
    {
        _firmwareInstaller.LogEmitted += FirmwareInstaller_LogEmitted;
        _firmwareInstaller.OverallProgressPercentageChanged += FirmwareInstaller_OverallProgressPercentageChanged;
    }
    else
    {
        _firmwareInstaller.LogEmitted -= FirmwareInstaller_LogEmitted;
        _firmwareInstaller.OverallProgressPercentageChanged -= FirmwareInstaller_OverallProgressPercentageChanged;
    }
}

private static void FirmwareInstaller_IdenticalFirmwareCachedOnTargetDeviceDetected(object sender, IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs ea)
{
    switch (ea)
    {
        case { CachedFirmwareType: ECachedFirmwareType.CachedButInactive }:
            App.DisplayAlert(title: "Info", message: "The firmware you're trying to install appears to be cached on the device. Will use that instead of re-uploading it.");
            break;

        case { CachedFirmwareType: ECachedFirmwareType.CachedAndActive }:
            App.DisplayAlert(title: "Info", message: "The firmware you're trying to install appears to be already active on the device. Will not re-install it.");
            break;
    }
}

private void FirmwareInstaller_OverallProgressPercentageChanged(object sender, OverallProgressPercentageChangedEventArgs ea)
{
    FirmwareInstallationOverallProgressPercentage = ea.ProgressPercentage;
}

private void CleanupFirmwareInstaller()
{
    _firmwareInstaller?.Disconnect();
    _firmwareInstaller = null;
}
```

- To erase a specific firmware:

```c#
private IFirmwareEraser _firmwareEraser;

public async Task EraseFirmwareAsync()
{
    var desiredBluetoothDevice = ...; //android bluetooth device here 

    try
    {
        _firmwareEraser = new FirmwareEraser.FirmwareEraser(desiredBluetoothDevice);
        
        ToggleSubscriptionsOnFirmwareEraserEvents(subscribeNotUnsubscribe: true);

        await _firmwareEraser.EraseAsync(imageIndex: IndexOfFirmwareImageToErase).ConfigureAwait(false);
    }
    catch (FirmwareErasureErroredOutException ex)
    {
        App.DisplayAlert(
            title: "File-Erasure Failed",
            message: $"An error occurred:{Environment.NewLine}{Environment.NewLine}{ex}"
        );
        return;
    }
    catch (Exception ex)
    {
        App.DisplayAlert(
            title: "[BUG] File-Erasure Failed",
            message: $"An unexpected error occurred:{Environment.NewLine}{Environment.NewLine}{ex}"
        );
        return;
    }
    finally
    {
        ToggleSubscriptionsOnFirmwareEraserEvents(subscribeNotUnsubscribe: false); //  order
        CleanupFirmwareEraser(); //                                                    order
    }
    
    App.DisplayAlert(title: "Erasure Complete", message: "Firmware Erasure Completed Successfully!");
}

private void ToggleSubscriptionsOnFirmwareEraserEvents(bool subscribeNotUnsubscribe)
{
    if (_firmwareEraser == null)
        return;

    if (subscribeNotUnsubscribe)
    {
        _firmwareEraser.LogEmitted += FirmwareEraser_LogEmitted;
        _firmwareEraser.StateChanged += FirmwareEraser_StateChanged;
        // _firmwareEraser.BusyStateChanged += FirmwareEraser_BusyStateChanged;
        // _firmwareEraser.FatalErrorOccurred += FirmwareEraser_FatalErrorOccurred;
    }
    else
    {
        _firmwareEraser.LogEmitted -= FirmwareEraser_LogEmitted;
        _firmwareEraser.StateChanged -= FirmwareEraser_StateChanged;
        // _firmwareEraser.BusyStateChanged -= FirmwareEraser_BusyStateChanged;
        // _firmwareEraser.FatalErrorOccurred -= FirmwareEraser_FatalErrorOccurred;
    }
}

private void FirmwareEraser_LogEmitted(object sender, LogEmittedEventArgs ea)
{
    Console.Error.WriteLine($"** {nameof(FirmwareEraser_LogEmitted)} [category={ea.Category}, level={ea.Level}]: {ea.Message}");
}

private void FirmwareEraser_StateChanged(object sender, StateChangedEventArgs ea)
{
    FirmwareErasureStage = ea.NewState.ToString();
}

private void CleanupFirmwareEraser()
{
    _firmwareEraser?.Disconnect();
    _firmwareEraser = null;
}
```

- To reboot ('reset') the device:

```c#
private IDeviceResetter _deviceResetter;

private void ResetDevice()
{
    var desiredBluetoothDevice = /*... grab your ble-device your device's ble-scanner ... */; 

    _deviceResetter = new Laerdal.McuMgr.DeviceResetting.DeviceResetter(desiredBluetoothDevice.BluetoothDevice);

    ToggleSubscriptionsOnDeviceResetterEvents(subscribeNotUnsubscribe: true);

    _deviceResetter.BeginReset();
}

private void ShowDeviceResetterStateButtonClicked()
{
    App.DisplayAlert(title: "Resetter State", message: $"State is: '{_deviceResetter?.State.ToString() ?? "(N/A)"}'");
}

private void ToggleSubscriptionsOnDeviceResetterEvents(bool subscribeNotUnsubscribe)
{
    if (_deviceResetter == null)
        return;
    
    if (subscribeNotUnsubscribe)
    {
        _deviceResetter.Error += DeviceResetter_Error;
        _deviceResetter.StateChanged += DeviceResetter_StateChanged;
    }
    else
    {
        _deviceResetter.Error -= DeviceResetter_Error;
        _deviceResetter.StateChanged -= DeviceResetter_StateChanged;
    }
}

private void DeviceResetter_Error(object sender, DeviceResetter.Events.ErrorEventArgs ea)
{
    CleanupDeviceResetter();
    
    App.DisplayAlert(title: "Reset Error", message: ea.ErrorMessage);
}

private void DeviceResetter_StateChanged(object sender, DeviceResetter.Events.StateChangedEventArgs ea)
{
    DeviceResettingStage = ea.NewState.ToString();
    if (ea.NewState != EDeviceResetterState.Complete)
        return;

    ToggleSubscriptionsOnFirmwareUpgraderEvents(subscribeNotUnsubscribe: false);

    App.DisplayAlert(title: "Reset / Reboot Complete", message: "Firmware Reset / Reboot Completed Successfully!");

    CleanupDeviceResetter();
}

private void CleanupDeviceResetter()
{
    _deviceResetter?.Disconnect();
    _deviceResetter = null;
}
```

- To perform file-uploading on the device:

         Note:

         1. The very first data-upload might feel slow to start because certain Nordic chipsets perform filesystem cleanup on the very first file-upload.
            There is nothing we can do about this. The culprit lies in issues plaguing littlefs itself:

            https://github.com/littlefs-project/littlefs/issues/797
            https://github.com/littlefs-project/littlefs/issues/783
            https://github.com/littlefs-project/littlefs/issues/810

         2. The library doesn't support streaming files. You must load each file's bytes as an array in memory before you can upload it. As long as you stay
            within reasonable limits (a few MBs) this shouldn't be a problem for most smartphones / tablets.

```c#

    private Dictionary<string, byte[]> _massFileUploadSelectedFileNamesAndTheirRawBytes; //set this appropriately
    private async Task PickLocalFilesToMassUploadButtonClickedAsync()
    {
        try
        {
            var selectedFiles = (await FilePicker.PickMultipleAsync(options: PickOptions.Default))?.ToArray();
            if (selectedFiles == null)
                return; //nothing selected

            var selectedFilesAndTheirRawBytes = new Dictionary<string, byte[]>(selectedFiles.Length);
            foreach (var x in selectedFiles)
            {
                using var stream = await x.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                
                await stream.CopyToAsync(memoryStream);
                
                var rawBytes = memoryStream.ToArray();

                selectedFilesAndTheirRawBytes.Add(
                    key: Path.GetFileName(x.FullPath),
                    value: rawBytes
                );
            }

            _massFileUploadSelectedFileNamesAndTheirRawBytes = selectedFilesAndTheirRawBytes;

            MassFileUploaderTotalNumberOfFilesToUpload = _massFileUploadSelectedFileNamesAndTheirRawBytes.Count;
            MassFileUploaderNumberOfFilesUploadedSuccessfully = 0;

            MassFileUploadSelectedLocalFilesStringified = string.Join(
                Environment.NewLine,
                _massFileUploadSelectedFileNamesAndTheirRawBytes.Select((x, i) => $"{i + 1}.) {x.Key}")
            );
        }
        catch (Exception ex)
        {
            App.DisplayAlert(title: "Error", message: $"Failed to pick local files!\r\n\r\n{ex}");
        }
        
        //00  in ios using openreadasync is the only way to get the raw bytes out of the file    if we use the standard readers of C#
        //    we will get an unauthorizedaccessexception 
    }

    private async Task MassUploadSelectedFilesButtonClickedAsync()
    {
        if (_massFileUploadSelectedFileNamesAndTheirRawBytes == null || !_massFileUploadSelectedFileNamesAndTheirRawBytes.Any())
        {
            App.DisplayAlert(title: "Forbidden", message: "No files specified for uploading!");
            return;
        }

        try
        {            
            _massFileUploader = new FileUploader.FileUploader(/*native ble-device*/);

            ToggleSubscriptionsOnMassFileUploaderEvents(subscribeNotUnsubscribe: true);

            MassFileUploaderNumberOfFilesUploadedSuccessfully = 0;

            var remoteFilePathsAndTheirData = _massFileUploadSelectedFileNamesAndTheirRawBytes.ToDictionary(
                keySelector: x => $"{MassFileUploadRemoteTargetFolderPath.TrimEnd('/')}/{x.Key}", //dont use path.combine here   it would be a bad idea
                elementSelector: x => x.Value
            );
            
            await _massFileUploader.UploadAsync(
                remoteFilePathsAndTheirData: remoteFilePathsAndTheirData,
                
                hostDeviceModel: DeviceInfo.Model,
                hostDeviceManufacturer: DeviceInfo.Manufacturer,
                
                initialMtuSize: MassFileUploaderInitialMtuSize <= 0 ? null : MassFileUploaderInitialMtuSize,
                maxTriesPerUpload: MassFileUploadingMaxRetriesPerUpload,
                timeoutPerUploadInMs: MassFileUploadingTimeoutPerUploadInSeconds * 1_000,
                sleepTimeBetweenUploadsInMs: MassFileUploadingSleepTimeBetweenUploadsInMs,
                sleepTimeBetweenRetriesInMs: MassFileUploadingSleepTimeBetweenRetriesInSecs * 1_000,

                moveToNextUploadInCaseOfError: MassFileUploadingMoveToNextUploadInCaseOfError
            ).ConfigureAwait(false); //vital to avoid deadlocks
        }
        catch (UploadCancelledException) //order
        {
            App.DisplayAlert(
                title: "File-Upload Cancelled",
                message: "The operation was cancelled!"
            );
            return;
        }
        catch (UploadErroredOutRemoteFolderNotFoundException ex) //order
        {
            App.DisplayAlert(
                title: "File-Upload Failed",
                message: $"Directory '{MassFileUploadRemoteTargetFolderPath}' doesn't exist in the remote device:" +
                         $"{Environment.NewLine}{Environment.NewLine}-------{Environment.NewLine}{Environment.NewLine}{ex}"
            );
            return;
        }
        catch (UploadErroredOutException ex) //order
        {
            App.DisplayAlert(
                title: "File-Upload Failed",
                message: $"A generic error occurred:\r\n\r\n{ex}"
            );
            return;
        }
        catch (UploadTimeoutException) //order
        {
            App.DisplayAlert(
                title: "File-Upload Failed",
                message: "The operation didn't complete in time"
            );
            return;
        }
        catch (Exception ex) //order
        {
            App.DisplayAlert(
                title: "[BUG] File-Upload Failed",
                message: $"An unexpected error occurred:\r\n\r\n{ex}"
            );
            return;
        }
        finally
        {
            ToggleSubscriptionsOnMassFileUploaderEvents(subscribeNotUnsubscribe: false); //     order
            CleanupFileUploader(); //                                                           order    
        }

        App.DisplayAlert(
            title: "File-Uploading Complete",
            message: $"{_massFileUploadSelectedFileNamesAndTheirRawBytes.Count} file(s) uploaded successfully at:\r\n\r\n{MassFileUploadRemoteTargetFolderPath}"
        );
    }

    private void MassUploadResetUIToDefaultValues()
    {
        MassFileUploaderStage = "";
        MassFileUploadProgressPercentage = 0;
        MassFileUploadCurrentlyUploadedFile = "";
        MassFileUploadCurrentThroughputInKBps = 0;
        MassFileUploaderNumberOfFilesUploadedSuccessfully = 0;
        MassFileUploaderNumberOfFailuresToUploadCurrentFile = 0;
    }

    private bool ValidateFileForMassUploadingForDevice(string selectedFileForUploading)
    {
        // validation logic here...
    
        return true;
    }

    private void CancelMassFileUploadingButtonClicked()
    {
        _massFileUploader?.Cancel();
    }
    
    private void ToggleSubscriptionsOnMassFileUploaderEvents(bool subscribeNotUnsubscribe)
    {
        if (_massFileUploader == null)
            return;

        if (subscribeNotUnsubscribe)
        {
            _massFileUploader.LogEmitted += MassFileUploader_LogEmitted;
            _massFileUploader.StateChanged += MassFileUploader_StateChanged;
            _massFileUploader.FileUploadProgressPercentageAndDataThroughputChanged += MassFileUploader_FileUploadProgressPercentageAndDataThroughputChanged;

            //_massFileUploader.Cancelled += MassFileUploader_Cancelled;
            //_massFileUploader.BusyStateChanged += MassFileUploader_BusyStateChanged;
            //_massFileUploader.FatalErrorOccurred += MassFileUploader_FatalErrorOccurred;
        }
        else
        {
            _massFileUploader.LogEmitted -= MassFileUploader_LogEmitted;
            _massFileUploader.StateChanged -= MassFileUploader_StateChanged;
            _massFileUploader.FileUploadProgressPercentageAndDataThroughputChanged -= MassFileUploader_FileUploadProgressPercentageAndDataThroughputChanged;

            //_massFileUploader.Cancelled -= MassFileUploader_Cancelled;
            //_massFileUploader.BusyStateChanged -= MassFileUploader_BusyStateChanged;
            //_massFileUploader.FatalErrorOccurred -= MassFileUploader_FatalErrorOccurred;
        }
    }

    private void CleanupFileUploader()
    {
        MassFileUploadProgressPercentage = 0;
        MassFileUploadCurrentThroughputInKBps = 0;

        _massFileUploader?.Disconnect();
        _massFileUploader = null;
    }

    private void MassFileUploader_LogEmitted(object sender, LogEmittedEventArgs ea)
    {
        Console.Error.WriteLine($"** {nameof(MassFileUploader_LogEmitted)} [category={ea.Category}, level={ea.Level}]: {ea.Message}");
    }

    private void MassFileUploader_StateChanged(object sender, StateChangedEventArgs ea)
    {
        MassFileUploaderStage = ea.NewState.ToString();

        switch (ea.NewState)
        {
            case EFileUploaderState.Idle:
                MassUploadResetUIToDefaultValues();
                return;
            
            case EFileUploaderState.Error:
                MassFileUploaderNumberOfFailuresToUploadCurrentFile += 1;
                return;
            
            case EFileUploaderState.Complete:
                MassFileUploaderNumberOfFilesUploadedSuccessfully += 1;
                return;
        }
    }

    private void MassFileUploader_FileUploadProgressPercentageAndDataThroughputChanged(object sender, FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea)
    {
        MassFileUploadProgressPercentage = ea.ProgressPercentage;
        MassFileUploadCurrentlyUploadedFile = Path.GetFileName(ea.RemoteFilePath);
        MassFileUploadCurrentThroughputInKBps = ea.CurrentThroughputInKBps;
    }
```

- To perform file-downloading from the device:

         Note:
 
         The library doesn't support streaming files. The bytes of each file you download will be stored
         in memory as a byte-array before being returned to you. As long as you stay within reasonable limits
         (a few MBs) this shouldn't be a problem for most smartphones / tablets.

         We might add streaming support in the future if there's a serious reason for it.

```csharp
    private async Task DownloadSingleFileButtonClickedAsync()
    {
        var bytes = (byte[]?)null;
        try
        {
            _fileDownloader = new FileUploader(/*native ble-device*/);

            ToggleSubscriptionsOnSingleFileDownloaderEvents(_fileDownloader, subscribeNotUnsubscribe: true); //  order
            
            SingleFileDownloaderStage = "";
            bytes = await _fileDownloader!.DownloadAsync( // order
                maxTriesCount: SingleFileDownloaderMaxTries,
                hostDeviceModel: DeviceInfo.Model,
                hostDeviceManufacturer: DeviceInfo.Manufacturer,

                remoteFilePath: remoteFilePathToDownload,
                initialMtuSize: SingleFileDownloaderInitialMtuSize < 0 ? null : SingleFileDownloaderInitialMtuSize
            ).ConfigureAwait(false); //good practice to avoid deadlocks
            if (bytes == null) //shouldnt happen but just in case 
                return;
        }
        catch (DownloadCancelledException) //order
        {
            App.DisplayAlert(
                title: "File-Download Cancelled",
                message: "The operation was cancelled"
            );
            return;
        }
        catch (DownloadErroredOutRemotePathPointsToDirectoryException) //order
        {
            App.DisplayAlert(
                title: "File-Download Failed",
                message: $"Filepath '{remoteFilePathToDownload}' points to a directory on the remote device!"
            );
            return;
        }
        catch (DownloadErroredOutRemoteFileNotFoundException) //order
        {
            App.DisplayAlert(
                title: "File-Download Failed",
                message: $"File '{remoteFilePathToDownload}' not found in the remote device!"
            );
            return;
        }
        catch (DownloadTimeoutException) //order
        {
            App.DisplayAlert(
                title: "File-Download Failed",
                message: "The operation didn't complete within the appropriate amount time!"
            );
            return;
        }
        catch (DownloadErroredOutException ex) //order
        {
            App.DisplayAlert(
                title: "File-Download Failed",
                message: $"An error occurred:{S.nl2}{ex}"
            );
            return;
        }
        catch (Exception ex) //order
        {
            App.DisplayAlert(
                title: "[BUG] File-Download Failed",
                message: $"An unexpected error occurred:{S.nl2}{ex}"
            );
            return;
        }
        finally
        {
            ToggleSubscriptionsOnSingleFileDownloaderEvents(_fileDownloader, subscribeNotUnsubscribe: false); // order
            CleanupFileDownloader(); //                                                                          order

            // if (shouldRestartScannerAfterInstallation)
            // {
            //     await Scanner.Instance.StartScanningAsync(); //order
            // }
        }

        var localSavePath = (string?)null;
        try
        {
            SingleFileDownloaderStage += "-> Saving to local file ...";

            localSavePath = await SaveBytesToLocalFileAsync(
                bytes: bytes,
                filename: Path.GetFileName(SingleFileDownloaderRemoteFilePathToDownload),
                directory: SingleFileDownloaderLocalSavePath
            );
        }
        catch (Exception ex)
        {
            App.DisplayAlert(
                title: "File-Save Error",
                message: $"File downloaded successfully but saving it to local file-system failed:\r\n\r\n{ex}"
            );
            return;
        }
        
        SingleFileDownloaderStage += " -> Done";
        
        App.DisplayAlert(
            title: "File-Download Complete",
            message: $"File downloaded and saved successfully at:\r\n\r\n{localSavePath}"
        );

        //00 file downloading on aed devices requires the connection to be authed to work
    }
    
    private void CancelSingleFileDownloadButtonClicked()
    {
        _fileDownloader?.TryCancel();
    }
    
    private void ToggleSubscriptionsOnSingleFileDownloaderEvents(IFileDownloader? singleFileDownloaderSnapshot, bool subscribeNotUnsubscribe)
    {
        if (singleFileDownloaderSnapshot == null)
            return;

        if (subscribeNotUnsubscribe)
        {
            singleFileDownloaderSnapshot.LogEmitted += SingleFileDownloader_LogEmitted;
            singleFileDownloaderSnapshot.StateChanged += SingleFileDownloader_StateChanged;
            
            this.Try(() => _massFileUploaderFUPPADTCEventStreamSubscription?.Dispose());
            _massFileUploaderFUPPADTCEventStreamSubscription = Observable
                .FromEventPattern<FileDownloadProgressPercentageAndDataThroughputChangedEventArgs>(
                    addHandler: h => singleFileDownloaderSnapshot.FileDownloadProgressPercentageAndDataThroughputChanged += h,
                    removeHandler: h => singleFileDownloaderSnapshot.FileDownloadProgressPercentageAndDataThroughputChanged -= h
                )
                .Throttle(TimeSpan.FromMilliseconds(75))
                .SubscribeAndHandleAllExceptions(
                    onNext: SingleFileDownloader_FileDownloadProgressPercentageAndDataThroughputChanged,
                    onError: ErrorHandler
                );

            //_fileDownloader.Cancelled += SingleFileDownloader_Cancelled;
            //_fileDownloader.BusyStateChanged += SingleFileDownloader_BusyStateChanged;
            //_fileDownloader.DownloadCompleted += SingleFileDownloader_DownloadCompleted;
            //_fileDownloader.FatalErrorOccurred += SingleFileDownloader_FatalErrorOccurred;
        }
        else
        {
            
            singleFileDownloaderSnapshot.LogEmitted -= SingleFileDownloader_LogEmitted;
            singleFileDownloaderSnapshot.StateChanged -= SingleFileDownloader_StateChanged;
            
            this.Try(() => _massFileUploaderFUPPADTCEventStreamSubscription?.Dispose());

            //_fileDownloader.Cancelled -= SingleFileDownloader_Cancelled;
            //_fileDownloader.BusyStateChanged -= SingleFileDownloader_BusyStateChanged;
            //_fileDownloader.DownloadCompleted -= SingleFileDownloader_DownloadCompleted;
            //_fileDownloader.FatalErrorOccurred -= SingleFileDownloader_FatalErrorOccurred;
        }
        
        return;
        
        void ErrorHandler(Exception exception, bool isComingFromSourceNotFromOnNext)
        {
            this.Error($"""[SDPVM.SFD.EH.010] isComingFromSourceNotFromOnNext={isComingFromSourceNotFromOnNext} Error :\n\n{exception.Message}\n\nFull Details :\n\n{exception}""", ExtraLoggingFlags.FileDownloader);
        }
    }

    static private async Task<string> SaveBytesToLocalFileAsync(byte[] bytes, string directory, string filename)
    {
        Directory.CreateDirectory(directory);

        var localSavePath = Path.Combine(directory, filename);

        await using var fileStream = new FileStream(
            path: localSavePath,
            mode: FileMode.Create,
            access: FileAccess.Write
        );

        await fileStream.WriteAsync(
            buffer: bytes,
            count: bytes.Length,
            offset: 0
        );

        return localSavePath;
    }

    private void CleanupFileDownloader()
    {
        SingleFileDownloadProgressPercentage = 0;
        SingleFileDownloadCurrentThroughputInKBps = 0;

        this.Try(() => _fileDownloader?.Dispose()); //calls disconnect under the hood
        _fileDownloader = null;
    }

    private void SingleFileDownloader_LogEmitted(object? sender, in LogEmittedEventArgs ea)
    {
        SingleFileDownloaderLogProperly(ea.Level.ToLaerdalLogLevel(),  $"[category={ea.Category}] [resource={ea.Resource}] {ea.Message}");
    }
```

## 📱 iOS

Same as in Android. Just make sure to pass a CBPeripheral to the constructors change a bit:

```c#
_fileUploader = new Laerdal.McuMgr.FileUploading.FileUploader(desiredBluetoothDevice); // must be a CBPeripheral
_firmwareEraser   = new Laerdal.McuMgr.FirmwareErasure.FirmwareEraser(desiredBluetoothDevice); // must be a CBPeripheral
_firmwareUpgrader = new Laerdal.McuMgr.FirmwareInstallation.FirmwareInstaller(desiredBluetoothDevice); // must be a CBPeripheral

_deviceResetter = new Laerdal.McuMgr.DeviceResetting.DeviceResetter(desiredBluetoothDevice); // must be a CBPeripheral
```

Note that the constructors support passing both a native device (CBPeripheral on iOS and BluetoothDevice on Android) or simply an 'object' that is castable to either of these types.
This is done to help you write more uniform code across platforms. You might want to create your own factory-service to smoothen things even further on your:

```c#
using Laerdal.Ble.Abstraction;
using Laerdal.McuMgr.DeviceResetting.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FirmwareErasure.Contracts;
using Laerdal.McuMgr.FirmwareInstallation.Contracts;
using YourApp.Contracts;

namespace YourApp.Services;

public interface IPlatformSpecificMcumgrFactoryService
{
    IFileUploader? SpawnFileUploader(IDevice device);
    IFileDownloader? SpawnFileDownloader(IDevice device);
    IFirmwareEraser? SpawnFirmwareEraser(IDevice device);
    IDeviceResetter? SpawnDeviceResetter(IDevice device);
    IFirmwareInstaller? SpawnFirmwareInstaller(IDevice device);
}

public sealed class McumgrFactoryService : IPlatformSpecificMcumgrFactoryService
{
    public IFileDownloader SpawnFileDownloader(IYourAbstractBleDevice yourAbstractBleDevice)
    {
        return new FileDownloading.FileDownloader(nativeBluetoothDevice: yourAbstractBleDevice.NativeDevice); // .NativeDevice is defined as 'object' and points to either iOS/CBPeripheral or Android/BluetoothDevice depending on the underlying platform
    }
    
    public IFileUploader SpawnFileUploader(IDevice device)
    {
        return new FileUploading.FileUploader(nativeBluetoothDevice: yourAbstractBleDevice.NativeDevice);
    }

    public IFirmwareEraser SpawnFirmwareEraser(IDevice device)
    {
        return new FirmwareErasing.FirmwareEraser(nativeBluetoothDevice: yourAbstractBleDevice.NativeDevice);
    }

    public IDeviceResetter SpawnDeviceResetter(IDevice device)
    {
        return new DeviceResetting.DeviceResetter(nativeBluetoothDevice: yourAbstractBleDevice.NativeDevice);
    }

    public IFirmwareInstaller SpawnFirmwareInstaller(IDevice device)
    {
        return new FirmwareInstallation.FirmwareInstaller(nativeBluetoothDevice: yourAbstractBleDevice.NativeDevice);
    }
}

// and then use it inside your UI classes

public class YourViewModel
{
    private readonly IPlatformSpecificMcumgrFactoryService _mcumgrFactoryService;

    public YourViewModel(IPlatformSpecificMcumgrFactoryService mcumgrFactoryService) //injected via DI
    {
        _mcumgrFactoryService = mcumgrFactoryService;
    }

    public void DoSomething()
    {
        var fileUploader = _mcumgrFactoryService.SpawnFileUploader(yourAbstractBleDevice); //works the same across all platforms
        // ...
    }
}
````

## 💻 Windows / UWP

Not supported yet.



## 🏗 IDE Setup / Generating Builds on Local-dev


    Note#1 There's an github-actions.yml file which you can use as a template to integrate the build in your github workflows. With said .yml the generated nugets will work on both Android and iOS.
_

    Note#2 To build full-blown nugets that work both on iOS and Android you must use MacOS as your build-machine with XCode 14.3+ and JDK17 installed - have a look at the .yml file to see how you
    can install java easily using 'brew'.

_

    Note#3 If you build on Windows the build system will work but the generated nugets *will only work on Android with MAUI apps* but they will error out on iOS considering that the 'iOS part'
           of the build gets **skipped** in Windows quite simply because in Windows we cannot use tools like 'sharpie' and the 'iPhoneOS' SDK that comes with XCode.

To build the nugets from source follow these instructions:

#### 1) Checkout

```bash
git   clone   git@github.com:Laerdal-Medical/Laerdal.McuMgr.git    mcumgr.mst

# or for develop

git   clone   git@github.com:Laerdal-Medical/Laerdal.McuMgr.git    --branch develop      mcumgr.dev
```

#### 2) Make sure you have .Net7 and .Net-Framework 4.8+ installed on your machine along with the workloads for maui, android and ios

```bash
# cd into the root folder of the repo
WORKLOAD_VERSION=8.0.402                                 \
&&                                                       \
sudo    dotnet                                           \
             workload                                    \
             install                                     \
                 ios                                     \
                 android                                 \
                 maccatalyst                             \
                 maui                                    \
                 maui-ios                                \
                 maui-tizen                              \
                 maui-android                            \
                 maui-maccatalyst                        \
                 --version   "${WORKLOAD_VERSION}"       \
&&                                                       \
cd "Laerdal.McuMgr.Bindings.iOS"                         \
&&                                                       \
sudo    dotnet                                           \
             workload                                    \
             restore                                     \
             --version   "${WORKLOAD_VERSION}"           \
&&                                                       \
cd -                                                     \
&&                                                       \
cd "Laerdal.McuMgr.Bindings.MacCatalyst"                 \
&&                                                       \
sudo    dotnet                                           \
             workload                                    \
             restore                                     \
             --version   "${WORKLOAD_VERSION}"           \
&&                                                       \
cd -                                                     \
&&                                                       \
cd "Laerdal.McuMgr.Bindings.Android"                     \
&&                                                       \
sudo    dotnet                                           \
             workload                                    \
             restore                                     \
             --version   "${WORKLOAD_VERSION}"           \
&&                                                       \
cd -

# note   theoretically 'dotnet workload restore' on the root level should also do the trick but in practice it sometimes runs into problems
```

After running the above command running 'dotnet workload list' should print out something like this on Windows:

```bash
> dotnet workload list

Installed Workload Id      Manifest Version       Installation Source            
---------------------------------------------------------------------------------
android                    34.0.113/8.0.100       SDK 8.0.300, VS 17.10.35027.167
aspire                     8.0.2/8.0.100          SDK 8.0.300, VS 17.10.35027.167
ios                        17.2.8078/8.0.100      SDK 8.0.300, VS 17.10.35027.167
maccatalyst                17.2.8078/8.0.100      SDK 8.0.300, VS 17.10.35027.167
maui                       8.0.61/8.0.100         SDK 8.0.300
maui-android               8.0.61/8.0.100         SDK 8.0.300
maui-ios                   8.0.61/8.0.100         SDK 8.0.300
maui-maccatalyst           8.0.61/8.0.100         SDK 8.0.300
maui-tizen                 8.0.61/8.0.100         SDK 8.0.300
maui-windows               8.0.61/8.0.100         SDK 8.0.300, VS 17.10.35027.167
```

#### 3) Make sure that Java17 is installed on your machine along with Gradle 7.6 (Gradle 8.x or above will NOT work!)

On a MacOS you can install Java17 and Gradle 7.6 using 'brew' like so:

```bash
# brew will install the latest version of jdk17 under '/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home'
# 
# export PATH="/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home/bin/:$PATH"
# export JAVA_HOME="/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home"
#
# also note that if you use a flaky antivirus then your openjdk installation might get silently deleted by it behind your back!
#
brew install --cask microsoft-openjdk@17
brew install gradle@7
```

#### 4) Make sure you have installed Android SDKs starting from 31 up. You will need to install them using the Visual Studio installer. If you use Rider you will need to install them a second time using the Rider Android SDK manager too!   

#### 5) Set MSBuild version to ver.17+

#### 6) On Mac make sure to install XCode 16.2 (16.3 doesn't work atm - if you have multiple XCodes installed then make 16.2 the default by running 'sudo xcode-select --switch /Applications/Xcode_16.2.app/Contents/Developer' assuming that xcode 16.2 is installed in that fs-path).

#### 7) On Windows you will probably have to also enable in the OS (registry) 'Long Path Support' otherwise the build will most probably fail due to extremely long paths being involved during the build process.

#### 8) Open 'Laerdal.McuMgr.sln' and build it.

You'll find the resulting nugets in the folder `Artifacts/`.

    Note: For software development you might want to consider bumping the version of Laerdal.McuMgr.Bindings.* first and building just that project
    and then bumping the package version of the package reference towards Laerdal.McuMgr.Bindings inside Laerdal.McuMgr.csproj and then building said project.

    If you don't follow these steps then any changes you make in Laerdal.McuMgr.Bindings.* won't be picked up by Laerdal.McuMgr because it will still
    use the cached nuget package of Laerdal.McuMgr.Bindings based on its current version.

    To make this process a bit easier you can use the following script at the top level directory (on branches other than 'main' or 'develop' to keep yourself on the safe side):

```bash
# on macos *sh
dotnet                                              \
         msbuild                                    \
         Laerdal.Builder.targets                    \
         '"/m:1"'                                   \
         '"/p:Laerdal_Version_Full=1.0.x.0"'

# on windows powershell
& dotnet                                            ^
          msbuild                                   ^
          Laerdal.Builder.targets                   ^
          '"/m:1"'                                  ^
          '"/p:Laerdal_Version_Full=1.0.x.0"'

# Note: Make sure to +1 the 'x' number each time in the aforementioned scripts before running them.
```

## Known issues

- Intercepting logs emitted by the underlying McuMgr libs is supported in iOS through the 'LogEmitted' family of events. 
  But the same family of events in Android is never triggered from the underlying McuMgr libs of Nordic (it's only triggered when we want to emit certain warnings ourselves) so logging
  in Android is very limited.
- Trying to use the iOS/Android flavours of this library in desktop-simulators for iOS/Android will probably result in compilation errors.
  If you want to perform general purpose UI-testing on your desktop using such simulators you need to tweak your nuget references to use the `-force-dud` nuget of `Laerdal.McuMgr` like so:

```xml
<PackageReference Include="Laerdal.McuMgr" Version="2.3.4-force-dud">
    <NoWarn>$(NoWarn);NU1605</NoWarn>
</PackageReference>
```

## Contributing

We welcome contributions to this project in the form of bug reports, feature requests, and pull requests.

- Before working on a branch or submitting a pull request, please open an issue describing the bug or feature request so as to expedite brainstorming.
- Commits should follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification.
- Pull requests should be made against the `develop` branch.
- Pull requests should be made from a fork of the repository, not a clone.
- Pull requests should have a descriptive title and include a link to the relevant issue.
- Pull requests affecting Laerdal.McuMgr.csproj should try (to the extent possible) to preserve API backwards-compatibility and be accompanied by appropriate tests, pertinent to 
the aspects being affected.

## Lead Maintainers

- [Kyriakos Sidiropoulos (@dsidirop)](https://github.com/dsidirop)

- [Francois Raminosona (@framinosona)](https://github.com/framinosona)


## Resources

- [Nordic nRF Connect Device Manager](https://github.com/NordicSemiconductor/Android-nRF-Connect-Device-Manager)
- [Nordic Infocenter](https://infocenter.nordicsemi.com/index.jsp?topic=%2Fstruct_welcome%2Fstruct%2Fwelcome.html)
- [iPhone models and supported iOS versions](https://iosref.com/ios)

## Credits & Acknowledgements

Special thanks goes to:

- [Francois Raminosona](https://www.linkedin.com/in/francois-raminosona/) for his insights and guidance on the entire spectrum of Xamarin development and underlying build system. This project
  would have been impossible to bring to fruition in such a short period of time without Francois' know-how.  

- [Geir-Inge T.](https://www.linkedin.com/in/geir-inge-t-68749629) for his immense contributions in terms of field-testing the library and providing invaluable feedback and insights.
