#!/usr/bin/env bash

# set -x # echo on for debugging 

# Builds a fat library for a given xcode project (framework)
#
# Derived from https://github.com/xamcat/xamarin-binding-swift-framework/blob/master/Swift/Scripts/build.fat.sh#L3-L14
#
# Note that all parameters passed to xcodebuild must be in the form of -parameter value instead of --parameter

declare XCODE_IDE_DEV_FOLDERPATH="${XCODE_IDE_DEV_FOLDERPATH:-}"

declare XCODEBUILD_TARGET_SDK="${XCODEBUILD_TARGET_SDK:-iphoneos}"
declare XCODEBUILD_TARGET_SDK_VERSION="${XCODEBUILD_TARGET_SDK_VERSION}" # xcodebuild -showsdks

declare XCODEBUILD_MIN_SUPPORTED_IOS_SDK_VERSION="${XCODEBUILD_MIN_SUPPORTED_IOS_SDK_VERSION:-14.5}" # the minimum supported iOS version for the McuMgrBindingsiOS framework
declare XCODEBUILD_MIN_SUPPORTED_MACCATALYST_SDK_VERSION="${XCODEBUILD_MIN_SUPPORTED_MACCATALYST_SDK_VERSION:-14.6}" # the minimum supported MacCatalyst version for the McuMgrBindingsiOS framework

if [ "${XCODEBUILD_TARGET_SDK}" == "iphoneos" ] && [ -z "${XCODEBUILD_TARGET_SDK_VERSION}" ]; then # ios
  XCODEBUILD_TARGET_SDK_VERSION="18.1" # requires xcode 16.1

elif [ "${XCODEBUILD_TARGET_SDK}" == "macosx" ] && [ -z "${XCODEBUILD_TARGET_SDK_VERSION}" ]; then # maccatalyst
  XCODEBUILD_TARGET_SDK_VERSION="15.1" # requires xcode 16.1
fi

declare SWIFT_BUILD_CONFIGURATION="${SWIFT_BUILD_CONFIGURATION:-Release}" 

declare SUPPORTS_MACCATALYST="${SUPPORTS_MACCATALYST:-NO}"
declare XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY="${XCODEBUILD_TARGET_SDK}${XCODEBUILD_TARGET_SDK_VERSION}"

declare SWIFT_OUTPUT_PATH="${SWIFT_OUTPUT_PATH:-./VendorFrameworks/swift-framework-proxy/}"

declare SWIFT_PROJECT_NAME="McuMgrBindingsiOS"
declare SWIFT_BUILD_PATH="./${SWIFT_PROJECT_NAME}/build"
declare SWIFT_BUILD_SCHEME="McuMgrBindingsiOS"
declare SWIFT_PROJECT_PATH="./McuMgrBindingsiOS/${SWIFT_PROJECT_NAME}.xcodeproj"
declare SWIFT_PACKAGES_PATH="./packages"

declare OUTPUT_FOLDER_POSTFIX=""
if [ "${XCODEBUILD_TARGET_SDK}" == "macosx" ]; then
  OUTPUT_FOLDER_POSTFIX="-maccatalyst" # special case for mac catalyst
else
  OUTPUT_FOLDER_POSTFIX="-${XCODEBUILD_TARGET_SDK}"
fi

declare OUTPUT_FOLDER_NAME="${SWIFT_BUILD_CONFIGURATION}${OUTPUT_FOLDER_POSTFIX}" #        Release-iphoneos or Release-maccatalyst       note that we intentionally *omitted* the sdk-version 
declare OUTPUT_SHARPIE_HEADER_FILES_PATH="SharpieOutput/SwiftFrameworkProxy.Binding"  #    from the folder name contains the resulting files ApiDefinitions.cs and StructsAndEnums.cs  

function print_setup() {
  echo "** xcode path    : '$( "xcode-select" --print-path  )'"
  echo "** xcode version : '$( "xcodebuild"   -version      )'"
  echo "** xcode sdks    :" 
  xcodebuild -showsdks
  echo "** xcode sdks visible to sharpie   :" 
  sharpie   xcode  -sdks

  echo
  echo "** SWIFT_BUILD_PATH            : '${SWIFT_BUILD_PATH}'            "
  echo "** SWIFT_OUTPUT_PATH           : '${SWIFT_OUTPUT_PATH}'           "
  echo "** SWIFT_BUILD_SCHEME          : '${SWIFT_BUILD_SCHEME}'          "
  echo "** SWIFT_PROJECT_NAME          : '${SWIFT_PROJECT_NAME}'          "
  echo "** SWIFT_PROJECT_PATH          : '${SWIFT_PROJECT_PATH}'          "
  echo "** SWIFT_PACKAGES_PATH         : '${SWIFT_PACKAGES_PATH}'         "
  echo "** SWIFT_BUILD_CONFIGURATION   : '${SWIFT_BUILD_CONFIGURATION}'   "
  echo
  echo "** OUTPUT_FOLDER_NAME                : '${OUTPUT_FOLDER_NAME}'                "
  echo "** OUTPUT_SHARPIE_HEADER_FILES_PATH  : '${OUTPUT_SHARPIE_HEADER_FILES_PATH}'  "
  echo
  echo "** SUPPORTS_MACCATALYST                       : '${SUPPORTS_MACCATALYST}'     "
  echo
  echo "** XCODE_IDE_DEV_FOLDERPATH                   : '${XCODE_IDE_DEV_FOLDERPATH:-(No path specified so the system-wide default xcode currently in effect will be used)}'"
  echo
  echo "** XCODEBUILD_TARGET_SDK                      : '${XCODEBUILD_TARGET_SDK}'                      "
  echo "** XCODEBUILD_TARGET_SDK_VERSION              : '${XCODEBUILD_TARGET_SDK_VERSION:-(No specific version specified so the latest version will be used)}'"
  echo "** XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY  : '${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}'  "
  echo
  echo "** XCODEBUILD_MIN_SUPPORTED_IOS_SDK_VERSION         : '${XCODEBUILD_MIN_SUPPORTED_IOS_SDK_VERSION}'          "
  echo "** XCODEBUILD_MIN_SUPPORTED_MACCATALYST_SDK_VERSION : '${XCODEBUILD_MIN_SUPPORTED_MACCATALYST_SDK_VERSION}'  " 
  echo
}

function set_system_wide_default_xcode_ide() {
  declare -r currentXcodeDevPath=$( "xcode-select" --print-path )
  if [ "${XCODE_IDE_DEV_FOLDERPATH}" != "" ] && [ "${currentXcodeDevPath}" != "${XCODE_IDE_DEV_FOLDERPATH}" ]; then
      echo "** Setting Xcode IDE path to '${XCODE_IDE_DEV_FOLDERPATH}' - remember to manually revert it back to '${currentXcodeDevPath}' after the build is done!"      
      sudo xcode-select --switch "${XCODE_IDE_DEV_FOLDERPATH}"
      local exitCode=$?

      if [ ${exitCode} -ne 0 ]; then
        echo "** [FAILED] Failed to set xcode-select to '${XCODE_IDE_DEV_FOLDERPATH}'"
        exit 1
      fi
  fi
}

function build() {
  echo "** Building '${OUTPUT_FOLDER_NAME}' framework for device ..."

  echo "**** (Build 1/3) Cleanup any possible traces of previous builds"

  rm -Rf "${SWIFT_BUILD_PATH}"
  rm -Rf "${SWIFT_PACKAGES_PATH}"
  rm -Rf "${OUTPUT_SHARPIE_HEADER_FILES_PATH}"

  echo "**** (Build 2/3) Restore packages for '${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}'" # @formatter:off

  xcodebuild                                                                                   \
                              -sdk "${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}"              \
                             -arch "arm64"                                                     \
                           -scheme "${SWIFT_BUILD_SCHEME}"                                     \
                          -project "${SWIFT_PROJECT_PATH}"                                     \
                    -configuration "${SWIFT_BUILD_CONFIGURATION}"                              \
       -resolvePackageDependencies                                                             \
      -clonedSourcePackagesDirPath "${SWIFT_PACKAGES_PATH}"                                    \
                CODE_SIGN_IDENTITY=""                                                          \
              CODE_SIGNING_ALLOWED="NO"                                                        \
              SUPPORTS_MACCATALYST="${SUPPORTS_MACCATALYST}"                                   \
             CODE_SIGNING_REQUIRED="NO"                                                        \
          MACOSX_DEPLOYMENT_TARGET="${XCODEBUILD_MIN_SUPPORTED_MACCATALYST_SDK_VERSION}"       \
        IPHONEOS_DEPLOYMENT_TARGET="${XCODEBUILD_MIN_SUPPORTED_IOS_SDK_VERSION}" # @formatter:on
  local exitCode=$?

  if [ ${exitCode} -ne 0 ]; then
    echo "** [FAILED] Failed to download dependencies for '${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}'"
    exit 1
  fi

  echo "**** (Build 3/3) Build for '${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}'" # https://stackoverflow.com/a/74478244/863651  @formatter:off

  xcodebuild                                                                                   \
                              -sdk "${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}"              \
                             -arch "arm64"                                                     \
                           -scheme "${SWIFT_BUILD_SCHEME}"                                     \
                          -project "${SWIFT_PROJECT_PATH}"                                     \
                    -configuration "${SWIFT_BUILD_CONFIGURATION}"                              \
                  -derivedDataPath "${SWIFT_BUILD_PATH}"                                       \
      -clonedSourcePackagesDirPath "${SWIFT_PACKAGES_PATH}"                                    \
                CODE_SIGN_IDENTITY=""                                                          \
              CODE_SIGNING_ALLOWED="NO"                                                        \
              SUPPORTS_MACCATALYST="${SUPPORTS_MACCATALYST}"                                   \
             CODE_SIGNING_REQUIRED="NO"                                                        \
          MACOSX_DEPLOYMENT_TARGET="${XCODEBUILD_MIN_SUPPORTED_MACCATALYST_SDK_VERSION}"       \
        IPHONEOS_DEPLOYMENT_TARGET="${XCODEBUILD_MIN_SUPPORTED_IOS_SDK_VERSION}" # @formatter:on
  local exitCode=$?

  if [ ${exitCode} -ne 0 ]; then
    echo "** [FAILED] Failed to build '${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}'"
    exit 1
  fi
}

function create_fat_binaries() {
  echo "** Create fat binaries for '${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}-${SWIFT_BUILD_CONFIGURATION}'"

  echo "**** (FatBinaries 1/8) Copy '${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}' build as a fat framework"
  cp \
    -R \
    "${SWIFT_BUILD_PATH}/Build/Products/${OUTPUT_FOLDER_NAME}" \
    "${SWIFT_BUILD_PATH}/fat"
  local exitCode=$?

  if [ ${exitCode} -ne 0 ]; then
    echo "** [FAILED] Failed to copy"
    exit 1
  fi

  echo "**** LISTING 'PRODUCTS' FILES"
  ls -lR "${SWIFT_BUILD_PATH}/Build/Products/"

  echo "**** LISTING LIPO INPUT FILES"
  ls -lR "${SWIFT_BUILD_PATH}/Build/Products/${OUTPUT_FOLDER_NAME}/${SWIFT_PROJECT_NAME}.framework/${SWIFT_PROJECT_NAME}"

  echo "**** (FatBinaries 2/8) Turn artifacts in '${OUTPUT_FOLDER_NAME}' into fat libraries"
  lipo \
    -create \
    -output "${SWIFT_BUILD_PATH}/fat/${SWIFT_PROJECT_NAME}.framework/${SWIFT_PROJECT_NAME}" \
    "${SWIFT_BUILD_PATH}/Build/Products/${OUTPUT_FOLDER_NAME}/${SWIFT_PROJECT_NAME}.framework/${SWIFT_PROJECT_NAME}"
  local exitCode=$?

  if [ ${exitCode} -ne 0 ]; then
    echo "** [FAILED] Failed to combine configurations"
    exit 1
  fi

  echo "**** LISTING LIPO OUTPUT FILES"
  ls -lR "${SWIFT_BUILD_PATH}/fat/${SWIFT_PROJECT_NAME}.framework/${SWIFT_PROJECT_NAME}"

  echo "**** (FatBinaries 3/8) Verify results"
  lipo \
    -info \
    "${SWIFT_BUILD_PATH}/fat/${SWIFT_PROJECT_NAME}.framework/${SWIFT_PROJECT_NAME}"
  local exitCode=$?

  if [ ${exitCode} -ne 0 ]; then
    echo "** [FAILED] Failed to verify results"
    exit 1
  fi

  echo "**** (FatBinaries 4/8) Copy fat frameworks to the output folder"
  rm -Rf "${SWIFT_OUTPUT_PATH}" &&
    mkdir -p "${SWIFT_OUTPUT_PATH}" &&
    cp -Rf \
      "${SWIFT_BUILD_PATH}/fat/${SWIFT_PROJECT_NAME}.framework" \
      "${SWIFT_OUTPUT_PATH}"
  local exitCode=$?

  if [ ${exitCode} -ne 0 ]; then
    echo "** [FAILED] Failed to copy fat frameworks"
    exit 1
  fi

  echo "**** (FatBinaries 5/8) Generating binding api definition and structs"
  set -x
  sharpie \
    bind \
    -sdk "${XCODEBUILD_TARGET_SDK_WITH_VERSION_IF_ANY}" \
    -scope "${SWIFT_OUTPUT_PATH}/${SWIFT_PROJECT_NAME}.framework/Headers/" \
    -output "${OUTPUT_SHARPIE_HEADER_FILES_PATH}" \
    -namespace "${SWIFT_PROJECT_NAME}" \
    "${SWIFT_OUTPUT_PATH}/${SWIFT_PROJECT_NAME}.framework/Headers/${SWIFT_PROJECT_NAME}-Swift.h" \
    -clang -arch arm64 # vital   needed for mac-catalyst
  local exitCode=$?
  set +x

  if [ ${exitCode} -ne 0 ]; then
    echo "** [FAILED] Failed to generate binding api definitions and structs"
    exit 1
  fi

  echo "**** (FatBinaries 6/8) Print metadata files in their original form"

  echo
  echo "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/ApiDefinitions.cs (original):"
  echo "==================================================="
  cat "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/ApiDefinitions.cs"
  echo
  echo "===================================================="
  echo

  echo
  echo "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/StructsAndEnums.cs (original):"
  echo "===================================================="
  cat "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/StructsAndEnums.cs"
  echo
  echo "===================================================="
  echo

  echo "**** (FatBinaries 7/8) Replace NativeHandle -> IntPtr in the generated c# files"

  rm -f "${OUTPUT_SHARPIE_HEADER_FILES_PATH}"/*.bak || :

  # starting from net8 sharpie seems to generate a file that is missing the using CoreBluetooth; directive from the top of the file so we have to add it ourselves
  sed -i.bak '1s/^/using CoreBluetooth;\n/' "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/ApiDefinitions.cs"

  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak "s/NativeHandle[ ]/IntPtr /gi" {} \;

  rm -f "${OUTPUT_SHARPIE_HEADER_FILES_PATH}"/*.bak || :

  # also need to get rid of stupid autogenerated [verify(...)] attributes which are intentionally placed there
  # by sharpie to force manual verification of the .cs files that have been autogenerated
  #
  # https://learn.microsoft.com/en-us/xamarin/cross-platform/macios/binding/objective-sharpie/platform/verify
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/\[Verify\s*\(.*\)\]//gi' {} \;

  rm -f "${OUTPUT_SHARPIE_HEADER_FILES_PATH}"/*.bak || :

  # [BaseType (typeof(NSObject), Name = "...")]  ->  [BaseType (typeof(NSObject))]
  #  find \
  #        "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
  #        -type f \
  #        -exec sed -i.bak 's/\[BaseType\s*\(.*_TtC17McuMgrBindingsiOS17IOSDeviceResetter.*\)\]/[BaseType (typeof(NSObject), Name = "IOSDeviceResetter")]/gi' {} \;
  #
  #  rm -f "${OUTPUT_SHARPIE_HEADER_FILES_PATH}"/*.bak || :

  # https://learn.microsoft.com/en-us/xamarin/ios/internals/registrar?force_isolation=true#new-registrar-required-changes-to-bindings
  #
  # adding [Protocol] to the 'interfaces' representing actual swift classes seems to be mandatory for the azure pipelines to generate a valid nuget
  # for ios if we omit adding this attribute then the nuget generated by the azure pipelines gets poisoned and it causes a very cryptic runtime error
  # so I'm not 100% sure why the [Protocol] attribute does away with the observed error but it does the trick of solving the problem somehow

  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSFileUploader/[Protocol] interface IOSFileUploader/gi' {} \;
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSFileDownloader/[Protocol] interface IOSFileDownloader/gi' {} \;
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSDeviceResetter/[Protocol] interface IOSDeviceResetter/gi' {} \;
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSFirmwareEraser/[Protocol] interface IOSFirmwareEraser/gi' {} \;
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSFirmwareInstaller/[Protocol] interface IOSFirmwareInstaller/gi' {} \;

  rm -f "${OUTPUT_SHARPIE_HEADER_FILES_PATH}"/*.bak || :

  # https://stackoverflow.com/a/49477937/863651   its vital to add [BaseType] to the interface otherwise compilation will fail
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForFileUploader/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForFileUploader/gi' {} \;
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForFileDownloader/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForFileDownloader/gi' {} \;
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForDeviceResetter/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForDeviceResetter/gi' {} \;
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForFirmwareEraser/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForFirmwareEraser/gi' {} \;
  find \
    "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
    -type f \
    -exec sed -i.bak 's/interface IOSListenerForFirmwareInstaller/[BaseType(typeof(NSObject))] [Model] interface IOSListenerForFirmwareInstaller/gi' {} \;

  # some plain methods unfortunately get autoprojected into properties by sharpie so we need to fix that    
  find \
        "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/" \
        -type f \
        -exec sed -i.bak 's/bool TryInvalidateCachedTransport { get; }/bool TryInvalidateCachedTransport();/gi' {} \;

  rm -f "${OUTPUT_SHARPIE_HEADER_FILES_PATH}"/*.bak || :

  echo "**** (FatBinaries 8/8) Print metadata files in their eventual form"

  echo
  echo "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/ApiDefinitions.cs (eventual):"
  echo "==================================================="
  cat "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/ApiDefinitions.cs"
  echo
  echo "===================================================="
  echo

  echo
  echo "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/StructsAndEnums.cs (eventual):"
  echo "===================================================="
  cat "${OUTPUT_SHARPIE_HEADER_FILES_PATH}/StructsAndEnums.cs"
  echo
  echo "===================================================="
  echo
}

function main() {
  set_system_wide_default_xcode_ide # order
  print_setup #                  order
  build #                             order
  create_fat_binaries #               order

  echo "** Done!"
}

main "$@"
