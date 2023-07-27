# 🏠 Laerdal.McuMgr

- Latest Nugets:


      Laerdal.McuMgr
  
  [![Laerdal.McuMgr package in LaerdalNuGet feed in Azure Artifacts](https://feeds.dev.azure.com/LaerdalMedical/_apis/public/Packaging/Feeds/LaerdalNuGet/Packages/b382f36b-e768-40a9-8bb9-e905b85ff610/Badge)](https://dev.azure.com/LaerdalMedical/Laerdal%20Nuget%20Platform/_artifacts/feed/LaerdalNuGet/NuGet/Laerdal.McuMgr?preferRelease=true)

      Laerdal.McuMgr.Bindings

  [![Laerdal.McuMgr.Bindings package in LaerdalNuGet feed in Azure Artifacts](https://feeds.dev.azure.com/LaerdalMedical/_apis/public/Packaging/Feeds/LaerdalNuGet/Packages/7c0a4133-335f-4699-bec4-b0828d93df5f/Badge)](https://dev.azure.com/LaerdalMedical/Laerdal%20Nuget%20Platform/_artifacts/feed/LaerdalNuGet/NuGet/Laerdal.McuMgr.Bindings?preferRelease=true)


- Release Build Status:

   [![Build Status](https://dev.azure.com/LaerdalMedical/Laerdal%20Nuget%20Platform/_apis/build/status%2FLaerdal.xamarin-nordic-mcumgr?branchName=main)](https://dev.azure.com/LaerdalMedical/Laerdal%20Nuget%20Platform/_build/latest?definitionId=241&branchName=main)


- Beta Build Status:

   [![Build Status](https://dev.azure.com/LaerdalMedical/Laerdal%20Nuget%20Platform/_apis/build/status%2FLaerdal.xamarin-nordic-mcumgr?branchName=develop)](https://dev.azure.com/LaerdalMedical/Laerdal%20Nuget%20Platform/_build/latest?definitionId=241&branchName=develop)


# Summary

The project generates two Nugets called 'Laerdal.McuMgr' & 'Laerdal.McuMgr.Bindings' respectively. The goal is to have 'Laerdal.McuMgr'
provide an elegant high-level C# abstraction for the native device-managers that Nordic provides us with for iOS and Android respectively
to interact with [nRF5x series of BLE chips](https://embeddedcentric.com/nrf5x-soc-overview/):

- [IOS-nRF-Connect-Device-Manager](https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager)

- [Android-nRF-Connect-Device-Manager](https://github.com/NordicSemiconductor/Android-nRF-Connect-Device-Manager)

The following types of operations are supported on devices running on Nordic's nRF5x series of BLE chips:

- Upgrading the firmware
- Erasing one of the firmware images stored in the device
- Rebooting ('Resetting') the device
- Downloading one or more files from the device
- Uploading one or more files over to the device

## ❗️ Salient Points

- **This library requires .Net7+ runtime to run.**

- **At the time of this writing (2023) and for the next few years up it's meant to be used directly (as a nuget) by next-gen Laerdal apps such as SkillReporter.**

- **The long-term intention is to have the library cloud-hosted as an http-service or similar.**

- **If you're maintaining legacy C++ codebases you're probably better off using the original C++ library or (alternatively) you can try making http-calls over to the cloud-hosted service (but this is not something technically possible on all legacy C++ products and solutions of Laerdal!)**

- **For the firmware-upgrade to actually persist through the rebooting of the device it's absolutely vital to set the upgrade mode to 'Test & Confirm'. If you set it to just 'Test' then the effects of the firmware-upgrade will only last up to the next reboot and the the device will revert back to its previous firmware image.**

- **Make sure to explicitly un-bond any app (including the NRF apps!) from the devices you are trying to upgrade. Any device in the vicinity that's still bonded will cause problems
in case you try to perform a firmware-upgrade on the desired device.**

- **Make sure to clean up after your apps when using the firmware-upgrader, device-resetter or firmware-eraser. Calling .Disconnect() is vital to avoid leaving behind latent connections
to the device.**

## 🚀 Getting started

Add the following Nuget packages to ALL your projects, not just the Core/Forms/Shared one:

       Laerdal.McuMgr
       Laerdal.McuMgr.Bindings

Make sure to always get the latest versions of the above packages.

### 🤖 Android

- Upgrade the firmware:

```c#

private Laerdal.McuMgr.FirmwareUpgrader.IFirmwareUpgrader _firmwareUpgrader;

public void UpgradeFirmware()
{
    var firmwareRawBytes = ...; //byte[]
    var desiredBluetoothDevice = await Laerdal.Ble.Scanner.Instance.WaitForDeviceToAppearAsync(/*device id here*/); 

    _firmwareUpgrader = new Laerdal.McuMgr.FirmwareUpgrader.FirmwareUpgrader(desiredBluetoothDevice.BluetoothDevice);
    
    ToggleSubscriptionsOnFirmwareUpgraderEvents(subscribeNotUnsubscribe: true);
    
    var verdict = _firmwareUpgrader.BeginUpgrade(data: firmwareRawBytes, estimatedSwapTimeInMilliseconds: 50 * 1000); //milliseconds
    if (verdict == IFirmwareUpgrader.EFirmwareUpgradeVerdict.Success)
        return;
            
    var lastErrorMessage = _firmwareUpgrader.LastErrorMessage;
    
    CleanupFirmwareUpgrader();
    
    ToggleSubscriptionsOnFirmwareUpgraderEvents(subscribeNotUnsubscribe: false);
            
    App.DisplayAlert(title: "Error", message: $"Failed to Upgrade firmware '{verdict}'. Error message: {lastErrorMessage}");
    
    ToggleSubscriptionsOnFirmwareUpgraderEvents(subscribeNotUnsubscribe: false);
}

void ToggleSubscriptionsOnFirmwareUpgraderEvents(bool subscribeNotUnsubscribe)
{
    if (_firmwareUpgrader == null)
        return;
    
    if (subscribeNotUnsubscribe)
    {
        _firmwareUpgrader.Error += FirmwareUpgrader_Error;
        _firmwareUpgrader.Cancelled += FirmwareUpgrader_Cancelled;
        _firmwareUpgrader.StateChanged += FirmwareUpgrader_StateChanged;
        _firmwareUpgrader.FirmwareUploadProgressPercentageAndDataThroughputChanged += FirmwareUpgrader_FirmwareUploadProgressPercentageAndDataThroughputChanged;
        //firmwareUpgrader.BusyStateChanged += FirmwareUpgrader_BusyStateChanged;
    }
    else
    {
        _firmwareUpgrader.Error -= FirmwareUpgrader_Error;
        _firmwareUpgrader.Cancelled -= FirmwareUpgrader_Cancelled;
        _firmwareUpgrader.StateChanged -= FirmwareUpgrader_StateChanged;
        _firmwareUpgrader.FirmwareUploadProgressPercentageAndDataThroughputChanged -= FirmwareUpgrader_FirmwareUploadProgressPercentageAndDataThroughputChanged;
        //firmwareUpgrader.BusyStateChanged -= FirmwareUpgrader_BusyStateChanged;
    }
}

private void FirmwareUpgrader_Error(object sender, FirmwareUpgrader.Events.ErrorEventArgs ea)
{
    CleanupFirmwareUpgrader();

    App.DisplayAlert(title: "Upgrade Error", message: ea.ErrorMessage);
}

private void FirmwareUpgrader_Cancelled(object sender, CancelledEventArgs ea)
{
    ToggleSubscriptionsOnFirmwareUpgraderEvents(subscribeNotUnsubscribe: false);

    CleanupFirmwareUpgrader();

    App.DisplayAlert(title: "Info", message: "Upgrade Cancelled");
}

private void FirmwareUpgrader_StateChanged(object sender, FirmwareUpgrader.Events.StateChangedEventArgs ea)
{
    FirmwareUpgradeStage = ea.NewState.ToString();
    if (ea.NewState != IFirmwareUpgrader.EFirmwareUpgradeState.Complete)
        return;

    ToggleSubscriptionsOnFirmwareUpgraderEvents(subscribeNotUnsubscribe: false);

    App.DisplayAlert(title: "Upgrade Complete", message: "Firmware Upgrade Completed Successfully!");

    CleanupFirmwareUpgrader();
}

private void FirmwareUpgrader_FirmwareUploadProgressPercentageAndDataThroughputChanged(object sender, FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea)
{
    if (ea.ProgressPercentage < ProgressPercentage)
        return;

    // ProgressPercentage = ea.ProgressPercentage;
    // AverageThroughputInKilobytes = ea.AverageThroughput;
}

void CleanupFirmwareUpgrader()
{
    // ProgressPercentage = 0;
    // AverageThroughputInKilobytes = 0;
    
    _firmwareUpgrader?.Disconnect(); //vital to cleanup the connection otherwise the device will get locked up
    _firmwareUpgrader = null;
}
```

- To erase a specific firmware:

```c#
private Laerdal.McuMgr.FirmwareEraser.IFirmwareEraser _firmwareEraser;

private void EraseFirmware()
{
    var desiredBluetoothDevice = await Laerdal.Ble.Scanner.Instance.WaitForDeviceToAppearAsync(/*device id here*/); 

    _firmwareEraser = new Laerdal.McuMgr.FirmwareEraser.FirmwareEraser(desiredBluetoothDevice.BluetoothDevice);

    ToggleSubscriptionsOnFirmwareEraserEvents(subscribeNotUnsubscribe: true);

    _firmwareEraser.BeginErasure(imageIndex: 1); //this can either be 0 (for the first image which is active) or 1 (for the second image which is typically inactive)
}

private void ToggleSubscriptionsOnFirmwareEraserEvents(bool subscribeNotUnsubscribe)
{
    if (_firmwareEraser == null)
        return;

    if (subscribeNotUnsubscribe)
    {
        _firmwareEraser.Error += FirmwareEraser_Error;
        _firmwareEraser.StateChanged += FirmwareEraser_StateChanged;
        // _firmwareEraser.BusyStateChanged += FirmwareEraser_BusyStateChanged;
    }
    else
    {
        _firmwareEraser.Error -= FirmwareEraser_Error;
        _firmwareEraser.StateChanged -= FirmwareEraser_StateChanged;
        // _firmwareEraser.BusyStateChanged -= FirmwareEraser_BusyStateChanged;
    }
}

private void FirmwareEraser_StateChanged(object sender, StateChangedEventArgs ea)
{
    FirmwareErasureStage = ea.NewState.ToString();
    if (ea.NewState != IFirmwareEraser.EFirmwareErasureState.Complete)
        return;

    ToggleSubscriptionsOnFirmwareEraserEvents(subscribeNotUnsubscribe: false);

    CleanupFirmwareEraser();

    App.DisplayAlert(title: "Erasure Complete", message: "Firmware Erasure Completed Successfully!");
}

private void FirmwareEraser_Error(object sender, ErrorEventArgs ea)
{
    CleanupFirmwareEraser();

    App.DisplayAlert(title: "Image Erasure Error", message: ea.ErrorMessage);
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
    if (ea.NewState != IDeviceResetter.EDeviceResetterState.Complete)
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

            var invalidFiles = selectedFilesAndTheirRawBytes
                .Where(x => !ValidateFileForMassUploadingForDevice(x.Key))
                .ToArray();
            if (invalidFiles.Any())
            {
                App.DisplayAlert(
                    title: "Error",
                    message: $"Files '{string.Join(", ", invalidFiles)}' are not valid for the given device."
                );
                return;
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
        if (!EnsureNoOtherOperationIsInProgress())
            return;

        if (_massFileUploadSelectedFileNamesAndTheirRawBytes == null || !_massFileUploadSelectedFileNamesAndTheirRawBytes.Any())
        {
            App.DisplayAlert(title: "Forbidden", message: "No files specified for uploading!");
            return;
        }

        MassUploadResetUIToDefaultValues();

        try
        {
            await Device.ConnectAsync();
            if (!Device.IsConnected)
            {
                App.DisplayAlert(title: "Error", message: "Failed to connect to device!");
                return;
            }
            
            await Device.AuthenticateIfNeededAsync(); //00
            if (!Device.IsAuthenticated)
            {
                App.DisplayAlert(title: "Error", message: "Failed to authenticate to device!");
                return;
            }
            
            _massFileUploader = _platformSpecificMcumgrFactoryService.SpawnFileUploader(Device);

            ToggleSubscriptionsOnMassFileUploaderEvents(subscribeNotUnsubscribe: true);

            MassFileUploaderNumberOfFilesUploadedSuccessfully = 0;

            var remoteFilePathsAndTheirDataBytes = _massFileUploadSelectedFileNamesAndTheirRawBytes.ToDictionary(
                keySelector: x => $"{MassFileUploadRemoteTargetFolderPath.TrimEnd('/')}/{x.Key}", //dont use path.combine here   it would be a bad idea
                elementSelector: x => x.Value
            );
            
            await _massFileUploader.UploadAsync(
                maxRetriesPerUpload: MassFileUploadingMaxRetriesPerUpload,
                timeoutPerUploadInMs: 4 * 60 * 1_000, //4mins per upload
                sleepTimeBetweenRetriesInMs: MassFileUploadingSleepTimeBetweenRetriesInSecs * 1_000,
                remoteFilePathsAndTheirDataBytes: remoteFilePathsAndTheirDataBytes
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
        catch (TimeoutException) //order
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
        Console.Error.WriteLine($"** {nameof(MassFileUploader_StateChanged)}: OldState='{ea.OldState}' NewState='{ea.NewState}'");

        MassFileUploaderStage = ea.NewState.ToString();

        switch (ea.NewState)
        {
            case IFileUploader.EFileUploaderState.Idle:
                MassUploadResetUIToDefaultValues();
                return;
            
            case IFileUploader.EFileUploaderState.Error:
                MassFileUploaderNumberOfFailuresToUploadCurrentFile += 1;
                return;
            
            case IFileUploader.EFileUploaderState.Complete:
                MassFileUploaderNumberOfFilesUploadedSuccessfully += 1;
                return;
        }
    }

    private void MassFileUploader_FileUploadProgressPercentageAndDataThroughputChanged(object sender, FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea)
    {
        Console.Error.WriteLine($"** {nameof(MassFileUploader_FileUploadProgressPercentageAndDataThroughputChanged)}: File='{ea.RemoteFilePath}' MassFileUploadProgressPercentage='{ea.ProgressPercentage}' AverageThroughput='{ea.AverageThroughput}'");

        MassFileUploadProgressPercentage = ea.ProgressPercentage;
        MassFileUploadCurrentlyUploadedFile = Path.GetFileName(ea.RemoteFilePath);
        MassFileUploadAverageThroughputInKilobytes = ea.AverageThroughput;
    }
```

### 📱 iOS

Same as in Android with the only difference being that the constructors change a bit:

```c#
_fileUploader = new Laerdal.McuMgr.FileUploader.FileUploader(desiredBluetoothDevice.CbPeripheral);
_firmwareEraser   = new Laerdal.McuMgr.FirmwareEraser.FirmwareEraser(desiredBluetoothDevice.CbPeripheral);
_firmwareUpgrader = new Laerdal.McuMgr.FirmwareUpgrader.FirmwareUpgrader(desiredBluetoothDevice.CbPeripheral);

_deviceResetter = new Laerdal.McuMgr.DeviceResetter.DeviceResetter(desiredBluetoothDevice.CbPeripheral);
```

### 💻 Windows

Not supported yet.

### 🏗 IDE Setup / Generating Builds on Local-dev

     Forward Warning: If you want to build the nugets for both Android and iOS you'll have to use a Mac - if you
     use Windows you will only be able to build nugets for Android (since iOS requires XCode).

To build 'Laerdal.McuMgr' from source follow the instructions specified in:

### 1) Checkout

```bash
git   clone   git@github.com:Laerdal-Medical/scl-mcumgr.git    mcumgr.mst

# or for develop

git   clone   git@github.com:Laerdal-Medical/scl-mcumgr.git    --branch develop      mcumgr.dev
```

### 2) Make sure that Java11 is installed on your machine along with Gradle and Maven.

### 3) (optional) If you want to develop locally without pulling nugets from Azure make sure you add to your nuget sources the local filesystem-path to the folder 'Laerdal.McuMgr.Bindings.Output'

Same goes for the testbed-ui app. If you want to build it locally you'll have to add to nuget sources the local file-system path 'Laerdal.McuMgr.Output'.

### 4) On Mac set the MSBuild version to Mono's 15.0 in Rider's settings (/Library/Frameworks/Mono.framework/Versions/6.12.0/lib/mono/msbuild/15.0/bin/MSBuild.dll - MSBuild 17.0+ won't build on Mac)

     Note: You can grab the appropriate Mono release for MacOS from https://download.mono-project.com/archive/6.12.0/macos-10-universal/MonoFramework-MDK-6.12.0.182.macos10.xamarin.universal.pkg

If you are on Windows you can use the MSBuild ver.17 provided by Visual Studio (C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin)

### 5) On Mac make sure to install SDK 14.2+ (if you have multiple XCodes installed then make SDK 14.2+ the default by running 'sudo xcode-select -s /Applications/Xcode_XYZ.app/Contents/Developer').

### 6) On Windows you will also have to edit 'Java/LaerdalMcuMgrWrapperAndSampleApp/gradle.properties' and set the property 'org.gradle.java.home' to the folder-path of the JDK11.

### 7) On Windows you have to also make sure you have enabled in the OS (registry) 'Long Path Support' otherwise the build will fail due to extremely long paths.

### 8) Open 'Laerdal.McuMgr.sln' and build it.

You'll find the resulting nugets in the folders `Laerdal.McuMgr.Output/` and `Laerdal.McuMgr.Bindings.Output/`.

### Known issues

- Intercepting logs emitted by the underlying McuMgr libs is supported in iOS through the 'LogEmitted' family of events. 
  But the same family of events in Android is never triggered from the underlying McuMgr libs of Nordic (it's only triggered when we want to emit certain warnings ourselves) so logging
  in Android is very limited.

