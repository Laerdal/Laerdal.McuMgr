# 🏠 Laerdal.McuMgr

![Platforms](https://img.shields.io/badge/Platforms-_MAUI_|_iOS_|_MacCatalyst_|_Android_-blue.svg)

- Latest Nugets:


      Laerdal.McuMgr Nugets:
  
  [![Laerdal.McuMgr package on Github](https://github.com/Laerdal/Laerdal.McuMgr/pkgs/nuget/Laerdal.McuMgr)](https://github.com/Laerdal/Laerdal.McuMgr/pkgs/nuget/Laerdal.McuMgr)

      Laerdal.McuMgr.Bindings.iOS Nugets:

  [![Laerdal.McuMgr.Bindings.iOS package on Github](https://github.com/Laerdal/Laerdal.McuMgr/pkgs/nuget/Laerdal.McuMgr.Bindings.iOS)](https://github.com/Laerdal/Laerdal.McuMgr/pkgs/nuget/Laerdal.McuMgr.Bindings.iOS)

      Laerdal.McuMgr.Bindings.Android Nugets:

  [![Laerdal.McuMgr.Bindings.Android package on Github](https://github.com/Laerdal/Laerdal.McuMgr/pkgs/nuget/Laerdal.McuMgr.Bindings.Android)](https://github.com/Laerdal/Laerdal.McuMgr/pkgs/nuget/Laerdal.McuMgr.Bindings.Android)

      Laerdal.McuMgr.Bindings.NetStandard Nugets (WIP!):

  [![Laerdal.McuMgr.Bindings.NetStandard package (WIP!) on Github](https://github.com/Laerdal/Laerdal.McuMgr/pkgs/nuget/Laerdal.McuMgr.Bindings.NetStandard)](https://github.com/Laerdal/Laerdal.McuMgr/pkgs/nuget/Laerdal.McuMgr.Bindings.NetStandard)


- Release Build Status (main branch):

  [![Build, Pack & Deploy Nugets](https://github.com/Laerdal/Laerdal.McuMgr/actions/workflows/github-actions.yml/badge.svg?branch=main)](https://github.com/Laerdal/Laerdal.McuMgr/actions/workflows/github-actions.yml)

- Beta Build Status (develop branch):

  [![Build, Pack & Deploy Nugets](https://github.com/Laerdal/Laerdal.McuMgr/actions/workflows/github-actions.yml/badge.svg?branch=develop)](https://github.com/Laerdal/Laerdal.McuMgr/actions/workflows/github-actions.yml)


# Forward Licensing Disclaimer

Read the LICENSE file before you begin.

# Summary

The project generates multiple Nugets called 'Laerdal.McuMgr' & 'Laerdal.McuMgr.Bindings.iOS|Android|NetStandard' (note: NetStandard is still WIP).
The goal is to have 'Laerdal.McuMgr' provide an elegant high-level C# abstraction for the native device-managers that Nordic provides us with for
iOS and Android respectively to interact with [nRF5x series of BLE chips](https://embeddedcentric.com/nrf5x-soc-overview/):

- [IOS-nRF-Connect-Device-Manager](https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager)

- [Android-nRF-Connect-Device-Manager](https://github.com/NordicSemiconductor/Android-nRF-Connect-Device-Manager)

From the respective 'Readme' files of these projects:

<< nRF Connect Device Manager library is compatible with [McuManager (McuMgr, for short)](https://docs.zephyrproject.org/3.2.0/services/device_mgmt/mcumgr.html#overview),
a management subsystem supported by [nRF Connect SDK](https://developer.nordicsemi.com/nRF_Connect_SDK/doc/latest/nrf/index.html),
[Zephyr](https://docs.zephyrproject.org/3.2.0/introduction/index.html) and Apache Mynewt.

**It is the recommended protocol for Device Firmware Update(s) on new Nordic-powered devices going forward and should not be confused with the previous protocol, NordicDFU,
serviced by the [Old DFU Library](https://github.com/NordicSemiconductor/IOS-DFU-Library)**.

McuManager uses the [Simple Management Protocol, or SMP](https://docs.zephyrproject.org/3.2.0/services/device_mgmt/smp_protocol.html), to send and receive message requests
from compatible devices. The SMP Transport definition for Bluetooth Low Energy, which this library implements,
[can be found here](https://docs.zephyrproject.org/latest/services/device_mgmt/smp_transport.html).

The library provides a transport agnostic implementation of the McuManager protocol. It contains a default implementation for BLE transport. >>

The following types of operations are supported on devices running on Nordic's nRF5x series of BLE chips:

- Upgrading the firmware
- Erasing one of the firmware images stored in the device
- Rebooting ('Resetting') the device
- Downloading one or more files from the device
- Uploading one or more files over to the device

      Note: The library doesn't support "Windows Desktop" applications (Windows/UWP) just yet (WIP).


## ✅ Nuget Platform-Support Matrix

| Stack     | Android                                                                   | iOS                               | MacCatalyst (MacOS / iPad / iOS)                | Windows / UWP (NetStandard2.0)                                                   |
|-----------|---------------------------------------------------------------------------|-----------------------------------|-------------------------------------------------|----------------------------------------------------------------------------------|   
| DotNet 8+ | ✅ Min 5.0 / Recommended 11.0+ / Max 14.0 <br/> (api-levels: 20 / 30 / 34) | ✅ 11.0+ <br/> (sdk: iphoneos16.2) | ✅ 13.1+ <br/> (MacOS: 10.15+, iOS/iPadOS: 13+ ) | 🚧 (Much much later ...)                                                         | 


## ❗️ Salient Points

- **For the firmware-upgrade to actually persist through the rebooting of the device it's absolutely vital to set the upgrade mode to 'Test & Confirm'. If you set it to just 'Test' then the effects of the firmware-upgrade will only last up to the next reboot and the the device will revert back to its previous firmware image.**

- **Make sure to explicitly un-bond any app (including the NRF apps!) from the devices you are trying to upgrade. Any device in the vicinity that's still bonded will cause problems
in case you try to perform a firmware-upgrade on the desired device.**

- **Make sure to clean up after your apps when using the firmware-upgrader, device-resetter or firmware-eraser. Calling .Disconnect() is vital to avoid leaving behind latent connections
to the device.**

- **At the time of this writing the generated ios-nugets are built based on the iphoneos16.2 sdk**


## 🚀 Using the Nugets in your Projects

Add the following Nuget packages.

       Laerdal.McuMgr
       Laerdal.McuMgr.Bindings.iOS                 (only add this to those projects of yours that target iOS)
       Laerdal.McuMgr.Bindings.Android             (only add this to those projects of yours that target Android)
       Laerdal.McuMgr.Bindings.MacCatalyst (WIP!)  (only add this to those projects of yours that target MacCatalyst aka MacDesktop+iPad)
       Laerdal.McuMgr.Bindings.NetStandard (WIP!)  (only add this to those projects of yours that target Windows/UWP)

Make sure to always get the latest versions of the above packages.

## 🤖 Android

- Installing a firmware:

```c#

private Laerdal.McuMgr.FirmwareInstaller.IFirmwareInstaller _firmwareInstaller;

public async Task InstallFirmwareAsync()
{
    var firmwareRawBytes = ...; //byte[]
    var desiredBluetoothDevice = ...; //android bluetooth device here 

    try
    {
        _firmwareInstaller = new FirmwareInstaller.FirmwareInstaller(desiredBluetoothDevice);
    
        ToggleSubscriptionsOnFirmwareInstallerEvents(subscribeNotUnsubscribe: true);
    
        await _firmwareInstaller.InstallAsync(
            data: firmwareRawBytes,
            pipelineDepth: FirmwareInstallationPipelineDepth, //ios only
            byteAlignment: FirmwareInstallationByteAlignment, //ios only
            windowCapacity: FirmwareInstallationWindowCapacity, //android only
            memoryAlignment: FirmwareInstallationMemoryAlignment, //android only
            estimatedSwapTimeInMilliseconds: FirmwareInstallationEstimatedSwapTimeInSecs * 1000
        );
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
        _firmwareInstaller.StateChanged += FirmwareInstaller_StateChanged;
        _firmwareInstaller.FirmwareUploadProgressPercentageAndDataThroughputChanged += FirmwareInstaller_FirmwareUploadProgressPercentageAndDataThroughputChanged;
    }
    else
    {
        _firmwareInstaller.LogEmitted -= FirmwareInstaller_LogEmitted;
        _firmwareInstaller.StateChanged -= FirmwareInstaller_StateChanged;
        _firmwareInstaller.FirmwareUploadProgressPercentageAndDataThroughputChanged -= FirmwareInstaller_FirmwareUploadProgressPercentageAndDataThroughputChanged;
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

private void FirmwareInstaller_StateChanged(object sender, StateChangedEventArgs ea)
{
    Console.Error.WriteLineAsync($"** {nameof(FirmwareInstaller_StateChanged)}: OldState='{ea.OldState}' NewState='{ea.NewState}'");

    if (ea.NewState == EFirmwareInstallationState.Idle) {
        FirmwareInstallationAttemptCount += 1; //00
    }

    FirmwareInstallationStage = ea.NewState.ToString();
    FirmwareInstallationOverallProgressPercentage = GetProgressMilestonePercentageForState(ea.NewState) ?? FirmwareInstallationOverallProgressPercentage;
    
    //00  if a firmware installation fails then we retry up to 10 times for a total of 11 times    each
    //    time we reattempt we start from scratch so the state will be reset back to being none again etc
}

private void FirmwareInstaller_FirmwareUploadProgressPercentageAndDataThroughputChanged(EventPattern<FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs> eventPattern)
{
    var ea = eventPattern.EventArgs;
    FirmwareUploadAverageThroughputInKilobytes = ea.AverageThroughput;

    if (FirmwareInstallationOverallProgressPercentage < 50) //10  hack
    {
        FirmwareInstallationOverallProgressPercentage = UploadingPhaseProgressMilestonePercent + (int)(ea.ProgressPercentage * 0.4f); //10% to 50%
    }

    //00  we could use a background task here per https://stackoverflow.com/a/15957165/863651 but we wouldnt notice a dramatic difference in performance
    //10  we noticed that there is a small race condition between state changes and the progress% updates   we first get a state change to 'resetting' (70%)
    //    and then a file-upload progress% update to 100%   we would like to fix this inside the native firmware installer library but its quite hard to do so
}

private static readonly int UploadingPhaseProgressMilestonePercent = GetProgressMilestonePercentageForState(EFirmwareInstallationState.Uploading)!.Value;
private static int? GetProgressMilestonePercentageForState(EFirmwareInstallationState state) => state switch
{
    EFirmwareInstallationState.None => 0,
    EFirmwareInstallationState.Idle => 1,
    EFirmwareInstallationState.Validating => 2,
    EFirmwareInstallationState.Uploading => 10, //00
    EFirmwareInstallationState.Testing => 50,
    EFirmwareInstallationState.Resetting => 70,
    EFirmwareInstallationState.Confirming => 80,
    EFirmwareInstallationState.Complete => 100,
    _ => null // .error .paused .cancelled .cancelling    we shouldnt throw an exception here
    
    //00   note that the progress% is further updated from 10% to 50% by the upload process via the event FirmwareUploadProgressPercentageAndDataThroughputChanged
};

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

        await _firmwareEraser.EraseAsync(imageIndex: IndexOfFirmwareImageToErase);
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
    var desiredBluetoothDevice = await Laerdal.Ble.Scanner.Instance.WaitForDeviceToAppearAsync(/*device id here*/); 

    _deviceResetter = new Laerdal.McuMgr.DeviceResetter.DeviceResetter(desiredBluetoothDevice.BluetoothDevice);

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

         The very first upload always feels slow and takes quite a bit of time to commence because Zephyr chipsets perform filesystem cleanup. There is nothing we can do about this.
         The culprit lies in issues plaguging littlefs

         https://github.com/littlefs-project/littlefs/issues/797
         https://github.com/littlefs-project/littlefs/issues/783
         https://github.com/littlefs-project/littlefs/issues/810

```c#

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
            App.DisplayAlert(
                title: "Error",
                message: $"Failed to pick local files!\r\n\r\n{ex}"
            );
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
            _massFileUploader = new FileUploader.FileUploader(/*Android or iOS device*/);

            ToggleSubscriptionsOnMassFileUploaderEvents(subscribeNotUnsubscribe: true);

            MassFileUploaderNumberOfFilesUploadedSuccessfully = 0;

            var remoteFilePathsAndTheirData = _massFileUploadSelectedFileNamesAndTheirRawBytes.ToDictionary(
                keySelector: x => $"{MassFileUploadRemoteTargetFolderPath.TrimEnd('/')}/{x.Key}", //dont use path.combine here   it would be a bad idea
                elementSelector: x => x.Value
            );
            
            await _massFileUploader.UploadAsync(
                maxTriesPerUpload: MassFileUploadingMaxTriesPerUpload,
                timeoutPerUploadInMs: 4 * 60 * 1_000, //4mins per upload
                sleepTimeBetweenRetriesInMs: MassFileUploadingSleepTimeBetweenRetriesInSecs * 1_000,
                remoteFilePathsAndTheirData: remoteFilePathsAndTheirData
            );
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
            await Device.DisconnectAsync();
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
        MassFileUploadAverageThroughputInKilobytes = 0;
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
        MassFileUploadAverageThroughputInKilobytes = 0;

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
        MassFileUploadAverageThroughputInKilobytes = ea.AverageThroughput;
    }
```



## 📱 iOS

Same as in Android with the only difference being that the constructors change a bit:

```c#
_fileUploader = new Laerdal.McuMgr.FileUploader.FileUploader(desiredBluetoothDevice.CbPeripheral);
_firmwareEraser   = new Laerdal.McuMgr.FirmwareEraser.FirmwareEraser(desiredBluetoothDevice.CbPeripheral);
_firmwareUpgrader = new Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller(desiredBluetoothDevice.CbPeripheral);

_deviceResetter = new Laerdal.McuMgr.DeviceResetter.DeviceResetter(desiredBluetoothDevice.CbPeripheral);
```



## 💻 Windows / UWP

Not supported yet.



## 🏗 IDE Setup / Generating Builds on Local-dev


    Note#1 There's an github-actions.yml file which you can use as a template to integrate the build in your github workflows. With said .yml the generated nugets will work on both Android and iOS.
_

    Note#2 To build full-blown nugets that work both on iOS and Android you must use MacOS as your build-machine with XCode 14.3+ and JDK17 installed - have a look at the .yml file to see how you
    can install java easily using 'brew'.

_

    Note#3 If you build on Windows the build system will work but the generated nugets *will only work on Android with MAUI apps* but they will error out on iOS considering that the 'iOS part'
           of the build gets skipped in Windows.

To build the nugets from source follow these instructions:

#### 1) Checkout

```bash
git   clone   git@github.com:Laerdal-Medical/scl-mcumgr.git    mcumgr.mst

# or for develop

git   clone   git@github.com:Laerdal-Medical/scl-mcumgr.git    --branch develop      mcumgr.dev
```

#### 2) Make sure you have .Net7 and .Net-Framework 4.8+ installed on your machine along with the workloads for maui, android and ios

```bash
# cd into the root folder of the repo
declare dotnet_7_workload_version="7.0.101"              \
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
                 --from-rollback-file=https://maui.blob.core.windows.net/metadata/rollbacks/${dotnet_7_workload_version}.json
&&                                                       \
cd "Laerdal.McuMgr.Bindings.iOS"                         \
&&                                                       \
sudo    dotnet                                           \
             workload                                    \
             restore                                     \
                 --from-rollback-file=https://maui.blob.core.windows.net/metadata/rollbacks/${dotnet_7_workload_version}.json
&&                                                       \
cd -                                                     \
&&                                                       \
cd "Laerdal.McuMgr.Bindings.Android"                     \
&&                                                       \
sudo    dotnet                                           \
             workload                                    \
             restore                                     \
                 --from-rollback-file=https://maui.blob.core.windows.net/metadata/rollbacks/${dotnet_7_workload_version}.json
cd -

# note#1   theoretically 'dotnet workload restore' on the root level should also do the trick but in practice it sometimes runs into problems
#
# note#2   microsoft encourages us to always update to and use the latest workloads   in practice devs have complained that they've
#          run into headaches with this approach and would rather pin versions explicitly as shown above   chances are you will have
#          one or more workloads forcibly updated to greater versions and if that's indeed the case then it's nearly impossible for
#          you to roll the workload versions back to the ones shown here   most probably your build system will still work and you won't
#          run into trouble   if you do you will either have to (a) reinstall .net7 from scratch or (b) use docker as your build system
#          to enforce strict workload-versioning on builds     
```

After running the above command running 'dotnet workload list' should print out something like this on Windows:

```bash
> dotnet workload list

Installed Workload Id      Manifest Version       Installation Source
--------------------------------------------------------------------------------
android                    33.0.95/7.0.100        SDK 7.0.400, VS 17.7.34202.233
ios                        16.4.7107/7.0.100      SDK 7.0.400, VS 17.7.34202.233
maui                       7.0.96/7.0.100         SDK 7.0.400
maui-android               7.0.96/7.0.100         SDK 7.0.400, VS 17.7.34202.233
maui-ios                   7.0.96/7.0.100         SDK 7.0.400, VS 17.7.34202.233
maui-maccatalyst           7.0.96/7.0.100         SDK 7.0.400, VS 17.7.34202.233
maui-windows               7.0.96/7.0.100         SDK 7.0.400, VS 17.7.34202.233
wasm-tools-net6            7.0.11/7.0.100         SDK 7.0.400
maccatalyst                16.4.7107/7.0.100      VS 17.7.34202.233
```

#### 3) Make sure that Java17 is installed on your machine along with Gradle 7.6 (Gradle 8.x or above will NOT work!)

#### 4) Make sure you have installed Android SDKs starting from 31 up. You will need to install them using the Visual Studio installer. If you use Rider you will need to install them a second time using the Rider Android SDK manager too!   

#### 5) (optional) If you want to develop locally without pulling nugets from the feed make sure you add to your nuget sources the local filesystem-path to the folder 'Artifacts'

Same goes for the testbed-ui app. If you want to build it locally you'll have to add to nuget sources the local file-system path 'Artifacts'.

#### 6) Set MSBuild version to ver.17

#### 7) On Mac make sure to install XCode 14.3+ (if you have multiple XCodes installed then make SDK 14.3+ the default by running 'sudo xcode-select -s /Applications/Xcode_XYZ.app/Contents/Developer').

#### 8) On Windows you have to also make sure you have enabled in the OS (registry) 'Long Path Support' otherwise the build will fail due to extremely long paths.

#### 9) Open 'Laerdal.McuMgr.sln' and build it.

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
         Laerdal.Builder.targets             \
         '"/m:1"'                                   \
         '"/p:Laerdal_Version_Full=1.0.x.0"'

# on windows powershell
& dotnet                                            ^
          msbuild                                   ^
          Laerdal.Builder.targets            ^
          '"/m:1"'                                  ^
          '"/p:Laerdal_Version_Full=1.0.x.0"'

# Note: Make sure to +1 the 'x' number each time in the aforementioned scripts before running them.
```

## Known issues

- Intercepting logs emitted by the underlying McuMgr libs is supported in iOS through the 'LogEmitted' family of events. 
  But the same family of events in Android is never triggered from the underlying McuMgr libs of Nordic (it's only triggered when we want to emit certain warnings ourselves) so logging
  in Android is very limited.

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


## Credits & Acknowledgements

Special thanks goes to:

- [Francois Raminosona](https://www.linkedin.com/in/francois-raminosona/) for his insights and guidance on the entire spectrum of Xamarin development and underlying build system. This project
  would have been impossible to bring to fruition in such a short period of time without Francois' know-how.  

- [Geir-Inge T.](https://www.linkedin.com/in/geir-inge-t-68749629) for his immense contributions in terms of field-testing the library and providing invaluable feedback and insights.
