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

      Note: The library doesn't support Windows/UWP yet.



## ❗️ Salient Points

- **For the firmware-upgrade to actually persist through the rebooting of the device it's absolutely vital to set the upgrade mode to 'Test & Confirm'. If you set it to just 'Test' then the effects of the firmware-upgrade will only last up to the next reboot and the the device will revert back to its previous firmware image.**

- **Make sure to explicitly un-bond any app (including the NRF apps!) from the devices you are trying to upgrade. Any device in the vicinity that's still bonded will cause problems
in case you try to perform a firmware-upgrade on the desired device.**

- **Make sure to clean up after your apps when using the firmware-upgrader, device-resetter or firmware-eraser. Calling .Disconnect() is vital to avoid leaving behind latent connections
to the device.**

- **At the time of this writing the generated nugets target iOS 16.2.**



### 🚀 Using the Nugets in your Projects

Add the following Nuget packages. If you're dealing in Xamarin then you'll have to add these Nugets to ALL of your projects in your Xamarin solution (not just the Core/Forms/Shared ones):

       Laerdal.McuMgr
       Laerdal.McuMgr.Bindings

Make sure to always get the latest versions of the above packages.



### 🤖 Android

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
    catch (FirmwareInstallationErroredOutImageSwapTimeoutException) //order
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

private void FirmwareInstaller_StateChanged(object sender, StateChangedEventArgs ea)
{
    FirmwareInstallationStage = ea.NewState.ToString();

    if (ea.NewState != IFirmwareInstaller.EFirmwareInstallationState.Complete)
        return;

    ToggleSubscriptionsOnFirmwareInstallerEvents(subscribeNotUnsubscribe: false);

    App.DisplayAlert(title: "Installation Complete", message: "Firmware Installation Completed Successfully!");

    CleanupFirmwareInstaller();
}

private void FirmwareInstaller_FirmwareUploadProgressPercentageAndDataThroughputChanged(object sender, FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea)
{
    if (ea.ProgressPercentage < FirmwareUploadProgressPercentage)
        return;

    FirmwareUploadProgressPercentage = ea.ProgressPercentage;
    FirmwareUploadAverageThroughputInKilobytes = ea.AverageThroughput;
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
_firmwareUpgrader = new Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller(desiredBluetoothDevice.CbPeripheral);

_deviceResetter = new Laerdal.McuMgr.DeviceResetter.DeviceResetter(desiredBluetoothDevice.CbPeripheral);
```



### 💻 Windows / UWP

Not supported yet.



### 🏗 IDE Setup / Generating Builds on Local-dev


    Note#1 There's an azure-pipelines.yml file which you can use as a template to integrate the build in your azure pipelines. With said .yml the generated nugets will work on both Android and iOS.
_

    Note#2 To build full-blown nugets that work both on iOS and Android you must use MacOS as your build-machine with XCode 14.3+ and JDK11 installed - have a look at the .yml file to see how you
    can install java easily using 'brew'.

_

    Note#3 If you build on Windows the build system will work but the generated nugets *will only work on Android* but they will error out on iOS considering that the 'iOS part' of the build gets skipped in Windows.

To build the nugets from source follow these instructions:

### 1) Checkout

```bash
git   clone   git@github.com:Laerdal-Medical/scl-mcumgr.git    mcumgr.mst

# or for develop

git   clone   git@github.com:Laerdal-Medical/scl-mcumgr.git    --branch develop      mcumgr.dev
```

### 2) Make sure that Java11 is installed on your machine along with Gradle and Maven.

### 3) (optional) If you want to develop locally without pulling nugets from Azure make sure you add to your nuget sources the local filesystem-path to the folder 'Artifacts'

Same goes for the testbed-ui app. If you want to build it locally you'll have to add to nuget sources the local file-system path 'Artifacts'.

### 4) On Mac set the MSBuild version to Mono's 15.0 in Rider's settings (/Library/Frameworks/Mono.framework/Versions/6.12.0/lib/mono/msbuild/15.0/bin/MSBuild.dll - MSBuild 17.0+ won't build on Mac)

     Note: You can grab the appropriate Mono release for MacOS from https://download.mono-project.com/archive/6.12.0/macos-10-universal/MonoFramework-MDK-6.12.0.182.macos10.xamarin.universal.pkg

If you are on Windows you can use the MSBuild ver.17 provided by Visual Studio (C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin)

### 5) On Mac make sure to install XCode 14.3+ (if you have multiple XCodes installed then make SDK 14.3+ the default by running 'sudo xcode-select -s /Applications/Xcode_XYZ.app/Contents/Developer').

### 6) On Windows you have to also make sure you have enabled in the OS (registry) 'Long Path Support' otherwise the build will fail due to extremely long paths.

### 7) Open 'Laerdal.McuMgr.sln' and build it.

You'll find the resulting nugets in the folder `Artifacts/`.

    Note: For software development you might want to consider bumping the version of Laerdal.McuMgr.Bindings first and building just that project
    and then bumping the package version of Laerdal.McuMgr.Bindings inside Laerdal.McuMgr.csproj and then building Laerdal.McuMgr.csproj.

    If you don't follow these steps then any changes you make in Laerdal.McuMgr.Bindings won't be picked up by Laerdal.McuMgr because it will still
    use the cached nuget package of Laerdal.McuMgr.Bindings.

    To make this process a bit easier you can use the following script at the top level directory (on branches other than 'main' or 'develop' to keep yourself on the safe side):

    # on macos
    msbuild                                                           \
         Laerdal.McuMgr.Builder.csproj                                \
         '"/p:Laerdal_Version_Full=1.0.x.0"'

    # on windows powershell
    & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"       ^
            Laerdal.McuMgr.Builder.csproj    ^
            /p:Laerdal_Version_Full=1.0.x.0

    Make sure to +1 the 'x' number each time in the scriptlet above before running it.


### Known issues

- Intercepting logs emitted by the underlying McuMgr libs is supported in iOS through the 'LogEmitted' family of events. 
  But the same family of events in Android is never triggered from the underlying McuMgr libs of Nordic (it's only triggered when we want to emit certain warnings ourselves) so logging
  in Android is very limited.


### Lead Maintainers

- [Kyriakos Sidiropoulos (@dsidirop)](https://github.com/dsidirop)

- [Francois Raminosona (@framinosona)](https://github.com/framinosona)


### Credits

Special thanks goes to:

- [Francois Raminosona](https://www.linkedin.com/in/francois-raminosona/) for his insights and guidance on the entire spectrum of Xamarin development and underlying build system. This project
  would have been impossible to bring to fruition in such a short period of time without Francois' know-how.  

- [Geir-Inge T.](https://www.linkedin.com/in/geir-inge-t-68749629) for his immense contributions in terms of field-testing the library and providing invaluable feedback and insights.
