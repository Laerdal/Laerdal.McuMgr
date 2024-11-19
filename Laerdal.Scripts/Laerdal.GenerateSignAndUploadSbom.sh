#!/bin/bash

# set -x

declare host_os=""
declare host_os_and_architecture=""

declare project_name=""
declare project_version=""

declare parent_project_name=""
declare parent_project_version=""

declare csproj_file_path=""
declare csproj_classifier=""
declare output_directory_path=""
declare output_sbom_file_name=""
declare sbom_signing_key_file_path=""
declare skip_installing_cyclonedx_cli_tool="no"
declare skip_installing_cyclonedx_dotnet_extension="no"

declare dependency_tracker_url=""
declare dependency_tracker_api_key_file_path=""

function parse_arguments() {

  while [[ $# -gt 0 ]]; do
    case $1 in

    --project-name)
      project_name="$2"
      shift
      ;;

    --project-version)
      project_version="$2"
      shift
      ;;

    --parent-project-name)
      parent_project_name="$2"
      shift
      ;;

    --parent-project-version)
      parent_project_version="$2"
      shift
      ;;

    --csproj-file-path)
      csproj_file_path="$2"
      shift
      ;;

    --csproj-classifier)
      csproj_classifier="$2"
      shift
      ;;

    --output-directory-path)
      output_directory_path="$2"
      shift
      ;;

    --output-sbom-file-name)
      output_sbom_file_name="$2"
      shift
      ;;

    --sbom-signing-key-file-path)
      sbom_signing_key_file_path="$2"
      shift
      ;;

    --skip-installing-cyclonedx-cli-tool)
      skip_installing_cyclonedx_cli_tool="yes"
      # shift # no need to shift here
      ;;

    --skip-installing-cyclonedx-dotnet-extension)
      skip_installing_cyclonedx_dotnet_extension="yes"
      # shift # no need to shift here
      ;;

    --dependency-tracker-url)
      dependency_tracker_url="$2"
      shift
      ;;

    --dependency-tracker-api-key-file-path)
      dependency_tracker_api_key_file_path="$2"
      shift
      ;;

    *)
      echo "Unknown option: $1"
      usage
      exit 1
      ;;
    esac

    shift
  done

  if [[ -z ${project_name} ]]; then
    echo "Specifying --project-name is mandatory!"
    usage
    exit 1
  fi

  if [[ -z ${project_version} ]]; then
    echo "Specifying --project-version is mandatory!"
    usage
    exit 1
  fi

  # if [[ -z ${parent_project_name} ]]; then         this is optional
  #      ...

  # if [[ -z ${parent_project_version} ]]; then         this is optional
  #      ...

  # if [[ -n ${parent_project_name} && -z ${parent_project_version} ]]; then # nah   better not to enforce this
  #   echo "Specifying --parent-project-version is mandatory when --parent-project-name has been used!"
  #   usage
  #   exit 1
  # fi

  if [[ -z ${csproj_file_path} ]]; then
    echo "Specifying --csproj-file-path is mandatory!"
    usage
    exit 1
  fi

  if [[ -z ${csproj_classifier} ]]; then
    echo "Specifying --csproj-classifier is mandatory!"
    usage
    exit 1
  fi

  if [[ -z ${output_directory_path} ]]; then
    echo "Specifying --output-directory-path is mandatory!"
    usage
    exit 1
  fi

  if [[ -z ${output_sbom_file_name} ]]; then
    echo "Specifying --output-sbom-file-name is mandatory!"
    usage
    exit 1
  fi

  if [[ -z ${sbom_signing_key_file_path} ]]; then
    echo "Specifying --sbom-signing-key-file-path is mandatory!"
    usage
    exit 1
  fi

  if [[ -z ${dependency_tracker_url} ]]; then
    echo "Specifying --dependency-tracker-url is mandatory!"
    usage
    exit 1
  fi

  if [[ -z ${dependency_tracker_api_key_file_path} ]]; then
    echo "Specifying --dependency-tracker-api-key-file-path is mandatory!"
    usage
    exit 1
  fi
}

function usage() {
  local -r script_name=$(basename "$0")

  echo "Usage: ${script_name}  --project-name  <name>   --project-version <version>  [--skip-installing-cyclonedx-cli-tool]  [--skip-installing-cyclonedx-dotnet-extension]  [--parent-project-name  <name>   --parent-project-version <version>]   --csproj-file-path <path>    --csproj-file-path <path>   --output-directory-path <path>  --output-sbom-file-name <name>   --sbom-signing-key-file-path <path>   --dependency-tracker-url <url>   --dependency-tracker-api-key-file-path <api_key>  "
}

function sniff_and_validate_host_os_and_architecture() {
  host_os="$(uname -s)"
  case "${host_os}" in
  Linux*) host_os="Linux" ;;
  MINGW*) host_os="Windows" ;;
  Darwin*) host_os="Mac" ;;
  CYGWIN*) host_os="Windows" ;;
  MSYS_NT*) host_os="Windows" ;;
  esac

  declare architecture=$(uname -m)
  case ${architecture} in
  i386) architecture="x86" ;;
  i686) architecture="x86" ;;
  x64) architecture="x64" ;; # shouldnt happen but just in case
  x86_64) architecture="x64" ;;

  armv7l) architecture="arm" ;;
  arm64) architecture="arm64" ;;
  aarch64) architecture="arm64" ;;
  arm) dpkg --print-architecture | grep -q "arm64" && architecture="arm64" || architecture="arm" ;;
  esac

  host_os_and_architecture="${host_os}-${architecture}" # e.g. Linux-x64

  if [[ ${host_os} == "Mac" ]] ||
    [[ ${host_os} == "Linux" ]] ||
    [[ ${host_os_and_architecture} == "Windows-x86" ]] ||
    [[ ${host_os_and_architecture} == "Windows-x64" ]] ||
    [[ ${host_os_and_architecture} == "Windows-arm" ]] ||
    [[ ${host_os_and_architecture} == "Windows-arm64" ]]; then
    return # host os is supported
  fi

  echo "Unsupported host OS '${host_os_and_architecture}' - don't know how to install the CycloneDX tool on this platform."
  exit 1
}

function install_dotnet_cyclonedx() {
  if [[ ${skip_installing_cyclonedx_dotnet_extension} == "yes" ]]; then
    return # the calling environment might have τηε cyclonedx extension for dotnet preinstalled
  fi

  echo
  echo "** Installing CycloneDX as a dotnet tool:"
  dotnet tool \
    install \
    --global CycloneDX
  declare exitCode=$?
  if [ $exitCode != 0 ]; then
    echo "Something went wrong with the CycloneDX tool for dotnet."
    exit 10
  fi

  echo
  echo "** CycloneDX:"
  which dotnet-CycloneDX && dotnet cyclonedx --version
  declare exitCode=$?
  if [ $exitCode != 0 ]; then
    echo "Something's wrong with 'dotnet-CycloneDX'."
    exit 12
  fi
}

function install_cyclonedx_standalone() { # we need to install the CycloneDX tool too in order to sign the artifacts
  if [[ ${skip_installing_cyclonedx_cli_tool} == "yes" ]]; then
    return # the calling environment might have cyclonedx preinstalled
  fi

  sniff_and_validate_host_os_and_architecture

  echo "** Installing cyclonedx cli tool for '${host_os}'"
  if [[ ${host_os} == "Mac" ]] || [[ ${host_os} == "Linux" ]]; then
    brew install cyclonedx/cyclonedx/cyclonedx-cli # both the macos and linux vmimages support brew so we can use it
    declare exitCode=$?
    if [ $exitCode != 0 ]; then
      echo "Failed to install 'cyclonedx'."
      exit 1
    fi

    return
  fi

  if [[ ${host_os_and_architecture} == "Windows-x86" ]]; then # windows does not support brew and chocolatey does not have a cyclonedx-cli package as of Q3 2024
    curl --output cyclonedx --url https://github.com/CycloneDX/cyclonedx-cli/releases/download/v0.27.1/cyclonedx-win-x86.exe &&
      chmod +x cyclonedx
    declare exitCode=$?
    if [ $exitCode != 0 ]; then
      echo "Failed to install 'cyclonedx'."
      exit 1
    fi

    return
  fi

  if [[ ${host_os_and_architecture} == "Windows-x64" ]]; then
    curl --output cyclonedx --url https://github.com/CycloneDX/cyclonedx-cli/releases/download/v0.27.1/cyclonedx-win-x64.exe &&
      chmod +x cyclonedx
    declare exitCode=$?
    if [ $exitCode != 0 ]; then
      echo "Failed to install 'cyclonedx'."
      exit 1
    fi

    return
  fi

  if [[ ${host_os_and_architecture} == "Windows-arm" ]]; then
    curl --output cyclonedx --url https://github.com/CycloneDX/cyclonedx-cli/releases/download/v0.27.1/cyclonedx-win-arm.exe &&
      chmod +x cyclonedx
    declare exitCode=$?
    if [ $exitCode != 0 ]; then
      echo "Failed to install 'cyclonedx'."
      exit 10
    fi

    return
  fi

  if [[ ${host_os_and_architecture} == "Windows-arm64" ]]; then
    curl --output cyclonedx --url https://github.com/CycloneDX/cyclonedx-cli/releases/download/v0.27.1/cyclonedx-win-arm64.exe &&
      chmod +x cyclonedx
    declare exitCode=$?
    if [ $exitCode != 0 ]; then
      echo "Failed to install 'cyclonedx'."
      exit 10
    fi

    return
  fi

  echo "Unsupported host OS '${host_os_and_architecture}' - cannot install 'cyclonedx-cli'."
  exit 1
}

function install_tools() {
  install_dotnet_cyclonedx     # order
  install_cyclonedx_standalone # order
}

function generate_sign_and_upload_sbom() {
  # set -x

  # GENERATE SBOM
  dotnet cyclonedx "${csproj_file_path}" \
    --exclude-dev \
    --include-project-references \
    \
    --output "${output_directory_path}" \
    --set-type "${csproj_classifier}" \
    --set-version "${project_version}" \
    \
    --filename "${output_sbom_file_name}"
  declare exitCode=$?
  if [ ${exitCode} != 0 ]; then
    echo "Failed to generate the SBOM!"
    exit 20
  fi

  # SIGN SBOM     todo  figure out why this doesnt actually sign anything on windows even though on macos it works as intended
  declare -r bom_file_path="${output_directory_path}/${output_sbom_file_name}"
  ./cyclonedx sign bom \
    "${bom_file_path}" \
    --key-file "${sbom_signing_key_file_path}"
  declare exitCode=$?
  if [ ${exitCode} != 0 ]; then
    echo "Singing the SBOM failed!"
    exit 30
  fi
  #  echo -e "\n\n"
  #  tail "${bom_file_path}"
  #  echo -e "\n\n"

  # UPLOAD SBOM
  declare optional_parent_project_name_parameter=""
  if [[ -n ${parent_project_name} ]]; then
    optional_parent_project_name_parameter="--form parentName=${parent_project_name}"
  fi

  declare optional_parent_project_version_parameter=""
  if [[ -n ${parent_project_version} ]]; then
    optional_parent_project_version_parameter="--form parentVersion=${parent_project_version}"
  fi

  declare -r http_response_code=$(
    curl "${dependency_tracker_url}" \
      --location \
      --request "POST" \
      \
      --header "Content-Type: multipart/form-data" \
      --header "X-API-Key: $(cat "${dependency_tracker_api_key_file_path}")" \
      \
      --form "bom=@${bom_file_path}" \
      --form "autoCreate=true" \
      \
      --form "projectName=${project_name}" \
      --form "projectVersion=${project_version}" \
      \
      ${optional_parent_project_name_parameter} \
      ${optional_parent_project_version_parameter} \
      \
      -w "%{http_code}"
  )
  declare exitCode=$?
  set +x

  echo "** Curl sbom-uploading HTTP Response Code: ${http_response_code}"

  if [ ${exitCode} != 0 ]; then
    echo "SBOM Uploading failed!"
    exit 40
  fi
}

function main() {
  parse_arguments "$@"          #           order
  install_tools                 #                  order
  generate_sign_and_upload_sbom #  order
}

main "$@"
