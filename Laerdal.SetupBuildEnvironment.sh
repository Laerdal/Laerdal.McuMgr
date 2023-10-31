#!/bin/bash

# this script is meant to be used only in our azure pipelines to setup the
# build environment for the xamarin bindings   its not meant to be used on localdev

# for macos13
# wget      https://github.com/macports/macports-base/releases/download/v2.8.1/MacPorts-2.8.1-13-Ventura.pkg
# sudo      installer    -verbose    -target /    -pkg MacPorts-2.8.1-13-Ventura.pkg

# for macos12
# wget   https://github.com/macports/macports-base/releases/download/v2.8.1/MacPorts-2.8.1-12-Monterey.pkg
# sudo   installer    -verbose    -target /    -pkg MacPorts-2.8.1-12-Monterey.pkg

# sudo    /opt/local/bin/port   install mono
# sudo  tar -xjf   /opt/local/var/macports/software/mono/mono-*.tbz2  -C /opt/local/var/macports/software/mono/
# sudo   sh -c   "echo   '\nexport PATH=\"/opt/local/var/macports/software/mono/opt/local/bin:\$PATH\"\n'   >> ~/.bash_profile"

# echo    "--------------------------------------------------"
# cat   ~/.bash_profile
# echo    "--------------------------------------------------"

# source  ~/.bash_profile
# ------------------- #

brew   install   --cask   objectivesharpie
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to install 'objectivesharpie'."
  exit 1
fi

brew   install   gradle
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to install 'gradle'."
  exit 1
fi

brew   install   openjdk@17
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to install 'openjdk@17'."
  exit 1
fi

# install a specific version of dotnet7 to ensure consistent results
curl -sSL "https://dot.net/v1/dotnet-install.sh" | bash /dev/stdin -Channel 7.0 -Version 7.0.402
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to install 'dotnet7'."
  exit 1
fi

echo
echo "** Dotnet CLI:"
which    dotnet   &&   dotnet   --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Something's wrong with 'dotnet' cli."
  exit 1
fi

#
# we do our best to explicitly version-pin our workloads so as to preemptively avoid problems that
# would be bound to crop up sooner or later by blindly auto-upgrading to bleeding-edge workloads
# 
# todo   unfortunately issuing a 'dotnet workload restore' on the root folder doesnt work as intended
# todo   on the azure pipelines and we need to figure out why
# todo
# todo   Users/runner/.dotnet/sdk/7.0.402/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets(1240,3): error MSB4019:
# todo   The imported project "/Users/runner/.dotnet/sdk/7.0.402/Sdks/Microsoft.NET.Sdk/16.0.1478/targets/Xamarin.Shared.Sdk.MultiTarget.targets"
# todo   was not found. Confirm that the expression in the Import declaration ";../16.0.1478/targets/Xamarin.Shared.Sdk.MultiTarget.targets"
# todo   is correct, and that the file exists on disk.
#
sudo    dotnet                                           \
             workload                                    \
             install                                     \
                 ios                                     \
                 android                                 \
                 maccatalyst                             \
                 --from-rollback-file=https://maui.blob.core.windows.net/metadata/rollbacks/7.0.96.json
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to restore dotnet workloads."
  exit 1
fi

cd "Laerdal.McuMgr.Bindings.iOS" || echo "##vso[task.logissue type=error]Failed to cd to Laerdal.McuMgr.Bindings.iOS" && exit 1
sudo         dotnet                                      \
             workload                                    \
             restore                                     \
                 --from-rollback-file=https://maui.blob.core.windows.net/metadata/rollbacks/7.0.96.json
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to restore dotnet workloads."
  exit 1
fi
cd - || exit 1

cd "Laerdal.McuMgr.Bindings.Android" || echo "##vso[task.logissue type=error]Failed to cd to Laerdal.McuMgr.Bindings.Android" && exit 1              
sudo    dotnet                                           \
             workload                                    \
             restore                                     \
                 --from-rollback-file=https://maui.blob.core.windows.net/metadata/rollbacks/7.0.96.json
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to restore dotnet workloads."
  exit 1
fi
cd - || exit 1

# this is handled by the build system
# echo  -e   '\norg.gradle.java.home=/usr/local/opt/openjdk@17/'   >>   "Laerdal.McuMgr.Bindings.Android.Native/gradle.properties"

echo
echo    "--------------------------------------------------"
cat    Laerdal.McuMgr.Bindings.Android.Native/gradle.properties
echo    "--------------------------------------------------"

# this is vital in order to select the ios 16.1+

echo "** XCode Installations:"

ls  -ld  /Applications/Xcode*

sudo   xcode-select   -s  /Applications/Xcode_14.2.app/Contents/Developer
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to apply 'xcode-select'."
  exit 1
fi
echo

echo "** Java Version:"
java               -version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'java'."
  exit 1
fi

echo
echo "** Javac Version:"
javac             -version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'javac'."
  exit 1
fi

echo
echo "** Gradle Version:"
gradle           --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'gradle'."
  exit 1
fi

echo
echo "** Sharpie Version:"
sharpie         --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'sharpie'."
  exit 1
fi

echo
echo "** XcodeBuild Version:"
xcodebuild   -version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'xcodebuild'."
  exit 1
fi

echo
echo "** Mono:"
which  mono  && mono  --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'mono'."
  exit 1
fi

echo
echo "** MSBuild:"
which   msbuild  && msbuild   --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'msbuild'."
  exit 1
fi

echo
echo "** Nuget:"
which  nuget  && nuget  --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'nuget'."
  exit 1
fi

echo
echo "** mtouch:"
/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mtouch  --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'mtouch'."
  exit 1
fi