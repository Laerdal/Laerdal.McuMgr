#!/bin/bash

# https://cocoapods.org/pods/iOSDFULibrary   default values

function usage() {
  echo "usage: ./download.ios.carthage.sh [-v|--verbose] [-o|--output path]"
  echo "parameters:"
  echo "  -v | --verbose                          Enable verbose build details from msbuild and gradle tasks"
  echo "  -o | --output [path]                    Output path"
  echo "  -h | --help                             Prints this message"
}

function digest_cli_options() {
  while [ "$1" != "" ]; do
    case $1 in
    -o | --output)
      shift
      output_path=$1
      ;;
    --pod_author)
      shift
      pod_author=$1
      ;;
    --pod_name)
      shift
      pod_name=$1
      ;;
    --pod_version)
      shift
      pod_version=$1
      ;;
    -v | --verbose)
      verbose=1
      ;;
    -h | --help)
      usage
      exit
      ;;
    *)
      echo
      echo "### Wrong parameter: $1 ###"
      echo
      usage
      exit 1
      ;;
    esac
    shift
  done
}

function check_carthage() {
  if ! command -v carthage &>/dev/null; then
    echo "Carthage could not be found"
    echo
    echo "Run 'brew install carthage' to install it with homebrew"

    exit 1
  fi

  declare carthage_version="$(carthage version)"

  echo "Carthage version : $carthage_version"
  echo
}

function generate_xcframeworks_fat_libs() {
  declare output_folder="iOS_Carthage"
  declare temp_output_folder_for_carthage_raw_sourcecodes="$output_folder/.tmp"

  rm -rf "$temp_output_folder_for_carthage_raw_sourcecodes"
  mkdir -p "$temp_output_folder_for_carthage_raw_sourcecodes"

  echo
  echo "### DOWNLOAD IOS FRAMEWORK (via Carthage) ###"
  echo

#  declare pod_author="NordicSemiconductor"   #          we cant use this because the default branch fails to compile  
#  declare pod_name="IOS-nRF-Connect-Device-Manager" #   the swiftcbor dependency of it fails to compile   
#  declare pod_version="\"1.2.8\""

  declare pod_author="Laerdal"
  declare pod_name="IOS-nRF-Connect-Device-Manager" # this was branched off GriffinJBC/IOS-nRF-Connect-Device-Manager which contains some crucial fixes  
  declare pod_version="\"carthage-support\""

  echo "pod_author = $pod_author" # generates variables
  echo "pod_name = $pod_name"
  echo "pod_version = $pod_version"

  # github "GriffinJBC/IOS-nRF-Connect-Device-Manager" "carthage-support"
  echo "github \"$pod_author/$pod_name\" $pod_version" >"$temp_output_folder_for_carthage_raw_sourcecodes/Cartfile"

  echo
  echo "------------------------------[Cartfile]------------------------------"
  cat "$temp_output_folder_for_carthage_raw_sourcecodes/Cartfile"
  echo "----------------------------------------------------------------------"
  echo

  (cd "$temp_output_folder_for_carthage_raw_sourcecodes" && carthage update --use-xcframeworks --platform "iOS")
  if [ $? -ne 0 ]; then
    echo
    echo "** ERROR [download.ios.carthage.sh]: Carthage git-fetch + build failed for iOS"
    echo

    exit 1
  fi

  # tweak_source_code_files_for_exportable_symbols "$temp_output_folder_for_carthage_raw_sourcecodes"

  declare xcframeworks=$(find "$temp_output_folder_for_carthage_raw_sourcecodes" -iname "*.xcframework")
  for i in $xcframeworks; do
    generate_fat_lib "$i"
  done

  # rm -rf "$temp_output_folder_for_carthage_raw_sourcecodes"

  echo
  echo "$pod_version" >"$output_folder/version.txt"

  echo "Created :"
  for i in "$output_folder"/*; do
    echo "  - $i"
  done

  if [ -n "$output_path" ]; then
    echo
    echo "### COPY FILES TO OUTPUT ###"
    echo

    mkdir -p "$output_path"
    cp -a "$output_folder"/. "$output_path"

    echo "Copied files into '$output_path'"

    rm -rf "$output_folder"
  fi
}

function tweak_source_code_files_for_exportable_symbols() {
  declare temp_output_folder_for_carthage_raw_sourcecodes="$1"

  echo "** TWEAKING SOURCE CODE FILES FOR EXPORTABLE SYMBOLS"
  echo

  echo "** Tweaking '$temp_output_folder_for_carthage_raw_sourcecodes/Carthage/Checkouts/IOS-nRF-Connect-Device-Manager/Source/Managers/DFU/FirmwareUpgradeManager.swift'"
  echo

  echo "** Adding '@objc()' over 'FirmwareUpgradeManager' ..."
  sed -i.bak1 \
       's/public[ ]\{1,\}class[ ]\{1,\}FirmwareUpgradeManager[ ]*:/@objc("iOSFirmwareUpgradeManager")\npublic class FirmwareUpgradeManager :/' \
       "$temp_output_folder_for_carthage_raw_sourcecodes/Carthage/Checkouts/IOS-nRF-Connect-Device-Manager/Source/Managers/DFU/FirmwareUpgradeManager.swift"

  echo "** Adding '@objc()' over 'FirmwareUpgradeManager.init()' ..."
  sed -i.bak2 \
       's/public[ ]\{1,\}init[ ]*[(][ ]*transporter[ ]*:[ ]*McuMgrTransport[ ]*,/@objc()\n    public init(transporter: McuMgrTransport,/' \
       "$temp_output_folder_for_carthage_raw_sourcecodes/Carthage/Checkouts/IOS-nRF-Connect-Device-Manager/Source/Managers/DFU/FirmwareUpgradeManager.swift"

  echo "** Adding '@objc()' over 'FirmwareUpgradeDelegate' ..."
  sed -i.bak3 \
       's/public[ ]\{1,\}protocol[ ]\{1,\}FirmwareUpgradeDelegate/@objc("iOSFirmwareUpgradeDelegate")\npublic protocol FirmwareUpgradeDelegate/' \
       "$temp_output_folder_for_carthage_raw_sourcecodes/Carthage/Checkouts/IOS-nRF-Connect-Device-Manager/Source/Managers/DFU/FirmwareUpgradeManager.swift"
}

function generate_fat_lib() {
  local xcframework="$1"

  if [ ! -d "$xcframework" ]; then
    echo "Failed : generate_fat_lib takes one parameter : xcframework"
    exit 1
  fi

  local library_name="$(basename "$xcframework" .xcframework)"

  local iphoneos_directory="$xcframework/ios-arm64" # ios-arm64_armv7
  local iphoneos_framework="$iphoneos_directory/$library_name.framework"
  local iphoneos_framework_file="$iphoneos_framework/$library_name"

  local iphonesimulator_directory="$xcframework/ios-arm64_x86_64-simulator"
  local iphonesimulator_framework="$iphonesimulator_directory/$library_name.framework"
  local iphonesimulator_framework_file="$iphonesimulator_framework/$library_name"

  local fat_directory="$xcframework/ios-fat"

  cp -r "$iphoneos_directory" "$fat_directory"

  local fat_framework="$fat_directory/$library_name.framework"
  local fat_framework_file="$fat_framework/$library_name"
  local fat_framework_symbols="$fat_directory/dSYMs/$library_name.framework.dSYM"

  rm -rf "$fat_framework_file"

  echo
  if [ "$verbose" = "1" ]; then
    (cd "$(dirname "$iphoneos_framework_file")" && lipo -info "$(basename "$iphoneos_framework_file")")
    echo "+"
    (cd "$(dirname "$iphonesimulator_framework_file")" && lipo -info "$(basename "$iphonesimulator_framework_file")")
    echo "="
  fi

  lipo -remove arm64 -output "$iphonesimulator_framework_file" "$iphonesimulator_framework_file"
  lipo -create -output "$fat_framework_file" "$iphoneos_framework_file" "$iphonesimulator_framework_file"

  (cd "$(dirname "$fat_framework_file")" && lipo -info "$(basename "$fat_framework_file")")

  mkdir -p "$output_folder"

  cp -r "$fat_framework" "$output_folder/$library_name.framework"
  cp -r "$fat_framework_symbols" "$output_folder/$library_name.framework.dSYM"
}

function main() {
  digest_cli_options "$@"

  check_carthage

  generate_xcframeworks_fat_libs
}

main "$@"
