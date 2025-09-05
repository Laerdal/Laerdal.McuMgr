#!/bin/bash

declare -r DOTNET_TARGET_WORKLOAD_VERSION="${1}"

declare -r NUGET_FEED_URL="${2}"
declare -r NUGET_FEED_USERNAME="${3}"
declare -r NUGET_FEED_ACCESSTOKEN="${4}"

declare -r ARTIFACTS_FOLDER_PATH="${5}"

declare -r SHOULD_RESTORE_WORKLOADS="${6:-true}"  # this is a boolean parameter that defaults to true

if [ -z "${DOTNET_TARGET_WORKLOAD_VERSION}" ]; then
  echo "##vso[task.logissue type=error]Missing 'DOTNET_TARGET_WORKLOAD_VERSION' which was expected to be parameter #1."
  exit 1
fi

if [ -z "${NUGET_FEED_URL}" ]; then
  echo "##vso[task.logissue type=error]Missing 'NUGET_FEED_URL' which was expected to be parameter #2."
  exit 2
fi

if [ -z "${NUGET_FEED_USERNAME}" ]; then
  echo "##vso[task.logissue type=error]Missing 'NUGET_FEED_USERNAME' which was expected to be parameter #3."
  exit 3
fi

if [ -z "${NUGET_FEED_ACCESSTOKEN}" ]; then
  echo "##vso[task.logissue type=error]Missing 'NUGET_FEED_ACCESSTOKEN' which was expected to be parameter #4."
  exit 4
fi

if [ -z "${ARTIFACTS_FOLDER_PATH}" ]; then
  echo "##vso[task.logissue type=error]Missing 'ARTIFACTS_FOLDER_PATH' which was expected to be parameter #5."
  exit 5
fi

brew   install   --cask   objectivesharpie
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to install 'objectivesharpie'."
  exit 10
fi

brew   reinstall     gradle@7
#declare exitCode=$?
#if [ $exitCode != 0 ]; then  # intentionally disabled because the installation though successful returns a non-zero exit code for some reason and we need to figure out why
#  echo "##vso[task.logissue type=error]Failed to install 'gradle'."
#  exit 20
#fi

# in github ci gradle@7 is installed under    /opt/homebrew/opt/gradle@7/bin
# (but in azure devops it is installed under   /usr/local/opt/gradle@7/bin)

if [[ ! -d "/opt/homebrew/opt/gradle@7/bin" ]];then
  echo "##vso[task.logissue type=error]Gradle doesn't appear to have been installed under '/opt/homebrew/opt/gradle@7/bin/' as expected - cannot proceed"
  exit 25
fi

echo 'export PATH="/opt/homebrew/opt/gradle@7/bin:$PATH"' >> /Users/runner/.zprofile
echo 'export PATH="/opt/homebrew/opt/gradle@7/bin:$PATH"' >> /Users/runner/.bash_profile
source /Users/runner/.bash_profile

brew  install  --cask   microsoft-openjdk@17  # brew   install   openjdk@17   this installs the temurin flavour of openjdk which is not that great  
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to install java through 'openjdk@17'."
  exit 30
fi

if [[ ! -d "/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home" ]];then
  echo "##vso[task.logissue type=error]Java doesn't appear to have been installed under '/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home' as expected - cannot proceed"
  exit 35
fi

echo 'export PATH="/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home/bin:$PATH"' >> /Users/runner/.zprofile
echo 'export JAVA_HOME="/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home"     ' >> /Users/runner/.zprofile

echo 'export PATH="/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home/bin:$PATH"' >> /Users/runner/.bash_profile
echo 'export JAVA_HOME="/Library/Java/JavaVirtualMachines/microsoft-17.jdk/Contents/Home"     ' >> /Users/runner/.bash_profile
source /Users/runner/.bash_profile

echo "** Path now is :"
echo
echo "$PATH"

# we enforce this via global.json
# curl -sSL "https://dot.net/v1/dotnet-install.sh" | bash /dev/stdin -Channel 8.0 -Version 8.0.405
# declare exitCode=$?
# if [ $exitCode != 0 ]; then
#   echo "##vso[task.logissue type=error]Failed to install 'dotnet8'."
#   exit 40
# fi

echo
echo "** Dotnet CLI:"
which    dotnet   &&   dotnet   --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Something's wrong with 'dotnet' cli."
  exit 50
fi

#
# we do our best to explicitly version-pin our workloads so as to preemptively avoid problems that
# would be bound to crop up sooner or later by blindly auto-upgrading to bleeding-edge workloads
# 
# also note that issuing a 'dotnet workload restore' doesnt work reliably and this is why resorted
# to being so explicit about the workloads we need 
#

if [ "${SHOULD_RESTORE_WORKLOADS}" != "true" ]; then
  echo "** Skipping dotnet workload restoration per 'SHOULD_RESTORE_WORKLOADS=${SHOULD_RESTORE_WORKLOADS}'!=true (meaning we had a happy cache-hit in this run)."

else

  echo "** Restoring dotnet-workloads ver. '${DOTNET_TARGET_WORKLOAD_VERSION}' because it seems we had a cache-miss in this run ..."
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
                   maui-maccatalyst    --version "${DOTNET_TARGET_WORKLOAD_VERSION}"
  declare exitCode=$?
  if [ $exitCode != 0 ]; then
    echo "##vso[task.logissue type=error]Failed to restore dotnet workloads."
    exit 60
  fi
  
  cd "Laerdal.McuMgr.Bindings.iOS"
  declare exitCode=$?
  if [ $exitCode != 0 ]; then
    echo "##vso[task.logissue type=error]Failed to cd to Laerdal.McuMgr.Bindings.iOS"
    exit 65
  fi

  sudo         dotnet                                      \
               workload                                    \
               restore  --version "${DOTNET_TARGET_WORKLOAD_VERSION}"
  declare exitCode=$?
  if [ $exitCode != 0 ]; then
    echo "##vso[task.logissue type=error]Failed to restore dotnet workloads."
    exit 70
  fi
  cd - || exit 71
fi

cd "Laerdal.McuMgr.Bindings.Android"
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to cd to Laerdal.McuMgr.Bindings.Android"
  exit 75
fi

sudo    dotnet                                           \
             workload                                    \
             restore
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to restore dotnet workloads."
  exit 80
fi
cd - || exit 85

echo
echo    "--------------------------------------------------"
cat    Laerdal.McuMgr.Bindings.Android.Native/gradle.properties
echo    "--------------------------------------------------"

# this is vital in order to select ios 16.1+

echo "** XCode Installations:"

ls  -ld  /Applications/Xcode* || exit 90

#sudo   xcode-select   -s   /Applications/Xcode.app/Contents/Developer
#declare exitCode=$?
#if [ $exitCode != 0 ]; then
#  echo "##vso[task.logissue type=error]Failed to apply 'xcode-select'."
#  exit 90
#fi
#echo

echo
echo "** XCode SDKs:"
xcodebuild -showsdks
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to list XCode SDKs."
  exit 93
fi

echo
echo "** XCode SDKs from Sharpie's point of view:"
sharpie xcode -sdks
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to list XCode SDKs from Sharpie's point of view."
  exit 95
fi


echo "** Default-Java Location:"
which    java
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'java'."
  exit 100
fi

echo "** Java Version:"
java               -version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'java'."
  exit 105
fi

echo
echo "** Javac Version:"
javac             -version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'javac'."
  exit 110
fi

echo
echo "** Gradle Location and Version:"
which gradle    &&    gradle           --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'gradle'."
  exit 120
fi


echo
echo "** Sharpie Version:"
sharpie         --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'sharpie'."
  exit 130
fi

echo
echo "** XcodeBuild Version:"
xcodebuild   -version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'xcodebuild'."
  exit 140
fi


echo
echo "** Adding 'Artifacts' Folder as a Nuget Source (dotnet):"
mkdir -p "${ARTIFACTS_FOLDER_PATH}"   &&   dotnet   nuget   add   source   "${ARTIFACTS_FOLDER_PATH}"   --name "LocalArtifacts"
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to add 'Artifacts' folder as a nuget source."
  exit 170
fi

echo
echo "** Adding 'Laerdal Nuget Feed' as a Nuget Source:"
# keep this after workload-restoration   otherwise we will run into problems    note that the 'store-password-in-clear-text'
# is necessary for azure pipelines   once we move fully over to github actions we can remove this parameter completely
dotnet   nuget   add                                     \
    source      "${NUGET_FEED_URL}"                      \
    --name      "LaerdalMedical"                         \
    --username  "${NUGET_FEED_USERNAME}"                 \
    --password  "${NUGET_FEED_ACCESSTOKEN}"              \
    --store-password-in-clear-text
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to add 'Laerdal Nuget Feed' as a nuget source."
  exit 180
fi

dotnet nuget list source
