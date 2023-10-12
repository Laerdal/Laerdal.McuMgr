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
brew   install   gradle
brew   install   java11

# install a specific version of dotnet7 to ensure consistent results
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -Channel 7.0 -Version 7.0.402

echo
echo "** Dotnet CLI:"
which    dotnet
dotnet   --version

#
# we do our best to explicitly version-pin our workloads so as to preemptively avoid problems that
# would be bound to crop up sooner or later by blindly autoupgrading to bleeding-edge workloads
#
# todo   once we migrate every csproj to netX we can simply issue 'workload restore' on the root folder
# 
( cd "Laerdal.McuMgr.Bindings.Android"                   \
  && sudo    dotnet                                      \
             workload                                    \
             restore                                     \
                 --from-rollback-file=https://maui.blob.core.windows.net/metadata/rollbacks/7.0.96.json )

sudo    dotnet                                      \
             workload                               \
             install                                \
                ios                                 \
                maui-ios                            \
                --from-rollback-file=https://maui.blob.core.windows.net/metadata/rollbacks/7.0.96.json 

# this is handled by the build system
# echo  -e   '\norg.gradle.java.home=/usr/local/opt/openjdk@11/'   >>   "Laerdal.McuMgr.Bindings.Android.Native/gradle.properties"

echo
echo    "--------------------------------------------------"
cat    Laerdal.McuMgr.Bindings.Android.Native/gradle.properties
echo    "--------------------------------------------------"

# this is vital in order to select the ios 16.1+

echo "** XCode Installations:"

ls  -ld  /Applications/Xcode*

sudo   xcode-select   -s  /Applications/Xcode_14.2.app/Contents/Developer
echo

echo "** Java Version:"
java               -version

echo
echo "** Javac Version:"
javac             -version

echo
echo "** Gradle Version:"
gradle           --version

echo
echo "** Sharpie Version:"
sharpie         --version

echo
echo "** XcodeBuild Version:"
xcodebuild   -version

echo
echo "** Mono:"
which       mono
mono        --version

echo
echo "** MSBuild:"
which       msbuild
msbuild   --version

echo
echo "** Nuget:"
which       nuget
nuget        --version

echo
echo "** mtouch:"
/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mtouch  --version
