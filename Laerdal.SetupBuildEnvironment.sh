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
  exit 10
fi

brew   reinstall     gradle@7
#declare exitCode=$?
#if [ $exitCode != 0 ]; then  # intentionally disabled because the installation though successful returns a non-zero exit code for some reason and we need to figure out why
#  echo "##vso[task.logissue type=error]Failed to install 'gradle'."
#  exit 20
#fi

# in github ci gradle@7 is installed under    /opt/homebrew/opt/gradle@7/bin
# but in azure devops it is installed under   /usr/local/opt/gradle@7/bin
echo 'export PATH="/usr/local/opt/gradle@7/bin:/opt/homebrew/opt/gradle@7/bin:$PATH"' >> /Users/runner/.zprofile
echo 'export PATH="/usr/local/opt/gradle@7/bin:/opt/homebrew/opt/gradle@7/bin:$PATH"' >> /Users/runner/.bash_profile
source /Users/runner/.bash_profile

brew   install   openjdk@17
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to install 'openjdk@17'."
  exit 30
fi

# install a specific version of dotnet8 to ensure consistent results
curl -sSL "https://dot.net/v1/dotnet-install.sh" | bash /dev/stdin -Channel 8.0 -Version 8.0.100
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to install 'dotnet8'."
  exit 40
fi

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
# todo   unfortunately issuing a 'dotnet workload restore' on the root folder doesnt work as intended
# todo   on the azure pipelines and we need to figure out why
#
sudo    dotnet                                           \
             workload                                    \
             install                                     \
                 ios                                     \
                 android                                 \
                 maccatalyst
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
             restore

declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to restore dotnet workloads."
  exit 70
fi
cd - || exit 71

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

sudo   xcode-select   -s   /Applications/Xcode_14.3.app/Contents/Developer  # todo  experiment with /Applications/Xcode_15.2.app and see if it works
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to apply 'xcode-select'."
  exit 95
fi
echo

echo "** Java Version:"
java               -version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'java'."
  exit 100
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
echo "** Mono:"
which  mono  &&  mono  --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'mono'."
  exit 150
fi

echo
echo "** MSBuild:"
which   msbuild  &&  msbuild   --version
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'msbuild'."
  exit 160
fi

echo
echo "** Nuget:"
which  nuget  &&  nuget  help
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to find 'nuget'."
  exit 170
fi

echo
echo "** Adding 'Artifacts' Folder as a Nuget Source:"
dotnet   nuget   add   source   "Artifacts"   --name "LocalArtifacts"
declare exitCode=$?
if [ $exitCode != 0 ]; then
  echo "##vso[task.logissue type=error]Failed to add 'Artifacts' folder as a nuget source."
  exit 170
fi
dotnet nuget list source

#echo
#echo "** mtouch:"
#/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mtouch  --version
#declare exitCode=$?
#if [ $exitCode != 0 ]; then
#  echo "##vso[task.logissue type=error]Failed to find 'mtouch'."
#  exit 180
#fi