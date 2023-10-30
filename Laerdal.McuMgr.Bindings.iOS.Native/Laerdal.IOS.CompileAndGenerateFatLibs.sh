#!/usr/bin/env bash

# set -x # echo on for debugging 

# Builds a fat library for a given xcode project (framework)
#
# Derived from https://github.com/xamcat/xamarin-binding-swift-framework/blob/master/Swift/Scripts/build.fat.sh#L3-L14
#
# Note that all parameters passed to xcodebuild must be in the form of -parameter value instead of --parameter

declare IOS_SDK_VERSION="${IOS_SDK_VERSION:-17.0}" # xcodebuild -showsdks
declare XCODEBUILD_TARGET_MAIN_SDK="${XCODEBUILD_TARGET_MAIN_SDK:-iphoneos${IOS_SDK_VERSION}}"

declare SWIFT_PROJECT_NAME="McuMgrBindingsiOS"
declare SWIFT_BUILD_PATH="./$SWIFT_PROJECT_NAME/build"
declare SWIFT_OUTPUT_PATH="./VendorFrameworks/swift-framework-proxy"
declare SWIFT_BUILD_SCHEME="McuMgrBindingsiOS"
declare SWIFT_PROJECT_PATH="./$SWIFT_PROJECT_NAME/$SWIFT_PROJECT_NAME.xcodeproj"
declare SWIFT_PACKAGES_PATH="./packages"
declare SWIFT_BUILD_CONFIGURATION="Release"

declare OUTPUT_FOLDER_NAME="$XCODEBUILD_TARGET_MAIN_SDK-$SWIFT_BUILD_CONFIGURATION" 
declare XAMARIN_BINDING_PATH="Xamarin/SwiftFrameworkProxy.Binding"

function print_macos_sdks() {
  echo "** xcode path    : '$( "xcode-select" -p       )'"
  echo "** xcode version : '$( "xcodebuild"   -version )'"
  echo "** xcode sdks    :" 
  xcodebuild -showsdks

  echo
  echo "** SWIFT_BUILD_PATH            : '$SWIFT_BUILD_PATH'            "
  echo "** SWIFT_OUTPUT_PATH           : '$SWIFT_OUTPUT_PATH'           "
  echo "** SWIFT_BUILD_SCHEME          : '$SWIFT_BUILD_SCHEME'          "
  echo "** SWIFT_PROJECT_NAME          : '$SWIFT_PROJECT_NAME'          "
  echo "** SWIFT_PROJECT_PATH          : '$SWIFT_PROJECT_PATH'          "
  echo "** SWIFT_PACKAGES_PATH         : '$SWIFT_PACKAGES_PATH'         "
  echo "** SWIFT_BUILD_CONFIGURATION   : '$SWIFT_BUILD_CONFIGURATION'   "
  echo
  echo "** OUTPUT_FOLDER_NAME          : '$OUTPUT_FOLDER_NAME'          "
  echo "** XAMARIN_BINDING_PATH        : '$XAMARIN_BINDING_PATH'        "
  echo "** XCODEBUILD_TARGET_MAIN_SDK  : '$XCODEBUILD_TARGET_MAIN_SDK'  "
  echo
}

function build() {
  echo "** Build $OUTPUT_FOLDER_NAME framework for device"

  echo "**** (Build 1/3) Cleanup any possible traces of previous builds"

  rm -Rf "$SWIFT_BUILD_PATH"
  rm -Rf "$SWIFT_PACKAGES_PATH"
  rm -Rf "$XAMARIN_BINDING_PATH"

  echo "**** (Build 2/3) Restore packages for '$XCODEBUILD_TARGET_MAIN_SDK'"

  xcodebuild \
    -sdk "$XCODEBUILD_TARGET_MAIN_SDK" \
    -arch "arm64" \
    -scheme "$SWIFT_BUILD_SCHEME" \
    -project "$SWIFT_PROJECT_PATH" \
    -configuration "$SWIFT_BUILD_CONFIGURATION" \
    -clonedSourcePackagesDirPath "$SWIFT_PACKAGES_PATH" \
    -resolvePackageDependencies
  local exitCode=$?

  if [ $exitCode -ne 0 ]; then
    echo "** [FAILED] Failed to download dependencies for '$XCODEBUILD_TARGET_MAIN_SDK'"
    exit 1
  fi

  echo "**** (Build 3/3) Build for '$XCODEBUILD_TARGET_MAIN_SDK'"

  # https://stackoverflow.com/a/74478244/863651
  xcodebuild \
    -sdk "$XCODEBUILD_TARGET_MAIN_SDK" \
    -arch "arm64" \
    -scheme "$SWIFT_BUILD_SCHEME" \
    -project "$SWIFT_PROJECT_PATH" \
    -configuration "$SWIFT_BUILD_CONFIGURATION" \
    -derivedDataPath "$SWIFT_BUILD_PATH" \
    -clonedSourcePackagesDirPath "$SWIFT_PACKAGES_PATH" \
    CODE_SIGN_IDENTITY="" \
    CODE_SIGNING_ALLOWED=NO \
    CODE_SIGNING_REQUIRED=NO
  local exitCode=$?

  if [ $exitCode -ne 0 ]; then
    echo "** [FAILED] Failed to build '$XCODEBUILD_TARGET_MAIN_SDK'"
    exit 1
  fi
}

function create_fat_binaries() {
  echo "** Create fat binaries for '$XCODEBUILD_TARGET_MAIN_SDK-$SWIFT_BUILD_CONFIGURATION'"

  echo "**** (FatBinaries 1/9) Copy '$XCODEBUILD_TARGET_MAIN_SDK' build as a fat framework"
  cp \
    -R \
    "$SWIFT_BUILD_PATH/Build/Products/$OUTPUT_FOLDER_NAME" \
    "$SWIFT_BUILD_PATH/Release-fat"
  local exitCode=$?

  if [ $exitCode -ne 0 ]; then
    echo "** [FAILED] Failed to copy"
    exit 1
  fi

  echo "**** LISTING 'PRODUCTS' FILES"
  ls -lR "$SWIFT_BUILD_PATH/Build/Products/"

  echo "**** LISTING LIPO INPUT FILES"
  ls -lR "$SWIFT_BUILD_PATH/Build/Products/$OUTPUT_FOLDER_NAME/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"

  echo "**** (FatBinaries 2/9) Turn artifacts in '$OUTPUT_FOLDER_NAME' into fat libraries"
  lipo \
    -create \
    -output "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME" \
    "$SWIFT_BUILD_PATH/Build/Products/$OUTPUT_FOLDER_NAME/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"
  local exitCode=$?

  if [ $exitCode -ne 0 ]; then
    echo "** [FAILED] Failed to combine configurations"
    exit 1
  fi

  echo "**** LISTING LIPO OUTPUT FILES"
  ls -lR "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"

  echo "**** (FatBinaries 3/9) Verify results"
  lipo \
    -info \
    "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework/$SWIFT_PROJECT_NAME"
  local exitCode=$?

  if [ $exitCode -ne 0 ]; then
    echo "** [FAILED] Failed to verify results"
    exit 1
  fi

  echo "**** (FatBinaries 4/9) Copy fat frameworks to the output folder"
  rm -Rf "$SWIFT_OUTPUT_PATH" &&
    mkdir -p "$SWIFT_OUTPUT_PATH" &&
    cp -Rf \
      "$SWIFT_BUILD_PATH/Release-fat/$SWIFT_PROJECT_NAME.framework" \
      "$SWIFT_OUTPUT_PATH"
  local exitCode=$?

  if [ $exitCode -ne 0 ]; then
    echo "** [FAILED] Failed to copy fat frameworks"
    exit 1
  fi

  echo "**** (FatBinaries 5/9) Generating binding api definition and structs"
  sharpie \
    bind \
    -sdk "$XCODEBUILD_TARGET_MAIN_SDK" \
    -scope "$SWIFT_OUTPUT_PATH/$SWIFT_PROJECT_NAME.framework/Headers/" \
    -output "$SWIFT_OUTPUT_PATH/XamarinApiDef" \
    -namespace "$SWIFT_PROJECT_NAME" \
    "$SWIFT_OUTPUT_PATH/$SWIFT_PROJECT_NAME.framework/Headers/$SWIFT_PROJECT_NAME-Swift.h"
  local exitCode=$?

  if [ $exitCode -ne 0 ]; then
    echo "** [FAILED] Failed to generate binding api definitions and structs"
    exit 1
  fi

  echo "**** (FatBinaries 6/9) Replace existing metadata with the updated ones"
  mkdir -p "$XAMARIN_BINDING_PATH/" &&
    cp \
      -Rf \
      "$SWIFT_OUTPUT_PATH/XamarinApiDef/." \
      "$XAMARIN_BINDING_PATH/"
  local exitCode=$?

  if [ $exitCode -ne 0 ]; then
    echo "** [FAILED] Failed to replace existing metadata with the updated ones"
    exit 1
  fi

  echo "**** (FatBinaries 7/9) Print metadata files in their original form"

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

  echo "**** (FatBinaries 8/9) Replace NativeHandle -> IntPtr in the generated c# files"

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

  echo "**** (FatBinaries 9/9) Print metadata files in their eventual form"

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
