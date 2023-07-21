#!/usr/bin/env bash

# Builds a fat library for a given xcode project (framework)
#
# Derived from https://github.com/xamcat/xamarin-binding-swift-framework/blob/master/Swift/Scripts/build.fat.sh#L3-L14

IOS_SDK_VERSION="${IOS_SDK_VERSION:-16.4}" # xcodebuild -showsdks

SWIFT_PROJECT_NAME="McuMgrBindingsiOS"
SWIFT_BUILD_PATH="./$SWIFT_PROJECT_NAME/build"
SWIFT_OUTPUT_PATH="./VendorFrameworks/swift-framework-proxy"
SWIFT_BUILD_SCHEME="McuMgrBindingsiOS"
SWIFT_PROJECT_PATH="./$SWIFT_PROJECT_NAME/$SWIFT_PROJECT_NAME.xcodeproj"
SWIFT_PACKAGES_PATH="./packages"
SWIFT_BUILD_CONFIGURATION="Release"

XAMARIN_BINDING_PATH="Xamarin/SwiftFrameworkProxy.Binding"

function print_macos_sdks() {
  xcodebuild -showsdks
}

function build() {
  echo "** Build iOS framework for simulator and device"

  echo "**** (Build 1/5) Cleanup any possible traces of previous builds"

  rm -Rf "$SWIFT_BUILD_PATH"
  rm -Rf "$SWIFT_PACKAGES_PATH"
  rm -Rf "$XAMARIN_BINDING_PATH"

  echo "**** (Build 2/5) Restore packages for 'iphoneos$IOS_SDK_VERSION'"

  xcodebuild \
    -sdk "iphoneos$IOS_SDK_VERSION" \
    -arch arm64 \
    -scheme "$SWIFT_BUILD_SCHEME" \
    -project "$SWIFT_PROJECT_PATH" \
    -configuration "$SWIFT_BUILD_CONFIGURATION" \
    -clonedSourcePackagesDirPath "$SWIFT_PACKAGES_PATH" \
    -resolvePackageDependencies

  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to download dependencies for 'iphoneos$IOS_SDK_VERSION'"
    exit 1
  fi

  echo "**** (Build 3/5) Build for 'iphoneos$IOS_SDK_VERSION'"

  # https://stackoverflow.com/a/74478244/863651
  xcodebuild \
    -sdk "iphoneos$IOS_SDK_VERSION" \
    -arch arm64 \
    -scheme "$SWIFT_BUILD_SCHEME" \
    -project "$SWIFT_PROJECT_PATH" \
    -configuration "$SWIFT_BUILD_CONFIGURATION" \
    -derivedDataPath "$SWIFT_BUILD_PATH" \
    -clonedSourcePackagesDirPath "$SWIFT_PACKAGES_PATH" \
    CODE_SIGN_IDENTITY="" \
    CODE_SIGNING_ALLOWED=NO \
    CODE_SIGNING_REQUIRED=NO

  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to build 'iphoneos$IOS_SDK_VERSION'"
    exit 1
  fi

  echo "**** (Build 4/5) Restore packages for 'iphonesimulator$IOS_SDK_VERSION'"

  xcodebuild \
    -sdk "iphonesimulator$IOS_SDK_VERSION" \
    -scheme "$SWIFT_BUILD_SCHEME" \
    -project "$SWIFT_PROJECT_PATH" \
    -configuration "$SWIFT_BUILD_CONFIGURATION" \
    -clonedSourcePackagesDirPath "$SWIFT_PACKAGES_PATH" \
    -resolvePackageDependencies

  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to download dependencies for 'iphonesimulator$IOS_SDK_VERSION'"
    exit 1
  fi

  echo "**** (Build 5/5) Build for 'iphonesimulator$IOS_SDK_VERSION'"

  # https://stackoverflow.com/a/74478244/863651
  # https://stackoverflow.com/a/64026089/863651
  xcodebuild \
    -sdk "iphonesimulator$IOS_SDK_VERSION" \
    -scheme "$SWIFT_BUILD_SCHEME" \
    -project "$SWIFT_PROJECT_PATH" \
    -configuration "$SWIFT_BUILD_CONFIGURATION" \
    -derivedDataPath "$SWIFT_BUILD_PATH" \
    -clonedSourcePackagesDirPath "$SWIFT_PACKAGES_PATH" \
    EXCLUDED_ARCHS="arm64" \
    CODE_SIGN_IDENTITY="" \
    CODE_SIGNING_ALLOWED=NO \
    CODE_SIGNING_REQUIRED=NO

  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to build 'iphonesimulator$IOS_SDK_VERSION'"
    exit 1
  fi
}

function create_fat_binaries() {
  echo "** Create fat binaries for Release-iphoneos and Release-iphonesimulator configuration"

  echo "**** (FatBinaries 1/10) Copy 'iphoneos' build as a fat framework"
  cp \
    -R \
    "$SWIFT_BUILD_PATH/Build/Products/Release-iphoneos" \
    "$SWIFT_BUILD_PATH/Release-fat"
  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to copy"
    exit 1
  fi

  echo "**** (FatBinaries 2/10) Combine modules from another build with the fat framework modules"
  cp \
    -R \
    "$SWIFT_BUILD_PATH/Build/Products/Release-iphonesimulator/$SWIFT_PROJECT_NAME.framework/Modules/$SWIFT_PROJECT_NAME.swiftmodule/" \
    "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework/Modules/$SWIFT_PROJECT_NAME.swiftmodule/"
  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to copy"
    exit 1
  fi

  echo "**** LISTING 'PRODUCTS' FILES"
  ls -lR "$SWIFT_BUILD_PATH/Build/Products/"

  echo "**** LISTING LIPO INPUT FILES"
  ls -lR "$SWIFT_BUILD_PATH/Build/Products/Release-iphoneos/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"
  ls -lR "$SWIFT_BUILD_PATH/Build/Products/Release-iphonesimulator/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"

  echo "**** (FatBinaries 3/10) Combine iphoneos + iphonesimulator configuration as fat libraries"
  lipo \
    -create \
    -output "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME" \
    "$SWIFT_BUILD_PATH/Build/Products/Release-iphoneos/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME" \
    "$SWIFT_BUILD_PATH/Build/Products/Release-iphonesimulator/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"
  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to combine configurations"
    exit 1
  fi

  echo "**** LISTING LIPO OUTPUT FILES"
  ls -lR "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"

  echo "**** (FatBinaries 4/10) Verify results"
  lipo \
    -info \
    "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"
  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to verify results"
    exit 1
  fi

  echo "**** (FatBinaries 5/10) Copy fat frameworks to the output folder"
  rm -Rf "$SWIFT_OUTPUT_PATH" &&
    mkdir -p "$SWIFT_OUTPUT_PATH" &&
    cp -Rf \
      "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework" \
      "$SWIFT_OUTPUT_PATH"
  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to copy fat frameworks"
    exit 1
  fi

  echo "**** (FatBinaries 6/10) Generating binding api definition and structs"
  sharpie \
    bind \
    --sdk="iphoneos$IOS_SDK_VERSION" \
    --scope="$SWIFT_OUTPUT_PATH/$SWIFT_PROJECT_NAME.framework/Headers/" \
    --output="$SWIFT_OUTPUT_PATH/XamarinApiDef" \
    --namespace="$SWIFT_PROJECT_NAME" \
    "$SWIFT_OUTPUT_PATH/$SWIFT_PROJECT_NAME.framework/Headers/$SWIFT_PROJECT_NAME-Swift.h"
  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to generate binding api definitions and structs"
    exit 1
  fi

  echo "**** (FatBinaries 7/10) Replace existing metadata with the updated ones"
  mkdir -p "$XAMARIN_BINDING_PATH/" &&
    cp \
      -Rf \
      "$SWIFT_OUTPUT_PATH/XamarinApiDef/." \
      "$XAMARIN_BINDING_PATH/"
  if [ $? -ne 0 ]; then
    echo "** [FAILED] Failed to replace existing metadata with the updated ones"
    exit 1
  fi

  echo "**** (FatBinaries 8/10) Print metadata files in their original form"

  echo
  echo "$XAMARIN_BINDING_PATH/ApiDefinitions.cs (original):"
  echo "==================================================="
  cat "$XAMARIN_BINDING_PATH/ApiDefinitions.cs"
  echo
  echo "===================================================="
  echo

  echo
  echo "$XAMARIN_BINDING_PATH/StructsAndEnums.cs (original):"
  echo "===================================================="
  cat "$XAMARIN_BINDING_PATH/StructsAndEnums.cs"
  echo
  echo "===================================================="
  echo

  echo "**** (FatBinaries 9/10) Replace NativeHandle -> IntPtr in the generated c# files"

  rm -f "$XAMARIN_BINDING_PATH"/*.bak || :

  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak "s/NativeHandle[ ]/IntPtr /gi" {} \;

  rm -f "$XAMARIN_BINDING_PATH"/*.bak || :

  # also need to get rid of stupid autogenerated [verify(...)] attributes which are intentionally placed there
  # by sharpie to force manual verification of the .cs files that have been autogenerated
  #
  # https://learn.microsoft.com/en-us/xamarin/cross-platform/macios/binding/objective-sharpie/platform/verify
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/\[Verify\s*\(.*\)\]//gi' {} \;

  rm -f "$XAMARIN_BINDING_PATH"/*.bak || :

  # [BaseType (typeof(NSObject), Name = "...")]  ->  [BaseType (typeof(NSObject))]
  #  find \
  #        "$XAMARIN_BINDING_PATH/" \
  #        -type f \
  #        -exec sed -i.bak 's/\[BaseType\s*\(.*_TtC17McuMgrBindingsiOS17IOSDeviceResetter.*\)\]/[BaseType (typeof(NSObject), Name = "IOSDeviceResetter")]/gi' {} \;
  #
  #  rm -f "$XAMARIN_BINDING_PATH"/*.bak || :

  # https://learn.microsoft.com/en-us/xamarin/ios/internals/registrar?force_isolation=true#new-registrar-required-changes-to-bindings
  #
  # adding [Protocol] to the 'interfaces' representing actual swift classes seems to be mandatory for the azure pipelines to generate a valid nuget
  # for ios if we omit adding this attribute then the nuget generated by the azure pipelines gets poisoned and it causes a very cryptic runtime error
  # so I'm not 100% sure why the [Protocol] attribute does away with the observed error but it does the trick of solving the problem somehow

  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSFileUploader/[Protocol] interface IOSFileUploader/gi' {} \;
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSFileDownloader/[Protocol] interface IOSFileDownloader/gi' {} \;
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSDeviceResetter/[Protocol] interface IOSDeviceResetter/gi' {} \;
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSFirmwareEraser/[Protocol] interface IOSFirmwareEraser/gi' {} \;
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSFirmwareInstaller/[Protocol] interface IOSFirmwareInstaller/gi' {} \;

  rm -f "$XAMARIN_BINDING_PATH"/*.bak || :

  # https://stackoverflow.com/a/49477937/863651   its vital to add [BaseType] to the interface otherwise compilation will fail
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForFileUploader/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForFileUploader/gi' {} \;
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForFileDownloader/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForFileDownloader/gi' {} \;
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForDeviceResetter/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForDeviceResetter/gi' {} \;
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForFirmwareEraser/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForFirmwareEraser/gi' {} \;
  find \
    "$XAMARIN_BINDING_PATH/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForFirmwareInstaller/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForFirmwareInstaller/gi' {} \;

  rm -f "$XAMARIN_BINDING_PATH"/*.bak || :

  echo "**** (FatBinaries 10/10) Print metadata files in their eventual form"

  echo
  echo "$XAMARIN_BINDING_PATH/ApiDefinitions.cs (eventual):"
  echo "==================================================="
  cat "$XAMARIN_BINDING_PATH/ApiDefinitions.cs"
  echo
  echo "===================================================="
  echo

  echo
  echo "$XAMARIN_BINDING_PATH/StructsAndEnums.cs (eventual):"
  echo "===================================================="
  cat "$XAMARIN_BINDING_PATH/StructsAndEnums.cs"
  echo
  echo "===================================================="
  echo
}

function main() {
  print_macos_sdks
  build
  create_fat_binaries

  echo "** Done!"
}

main "$@"
