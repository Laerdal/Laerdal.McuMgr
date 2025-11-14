#!/bin/bash

# -e: abort script if one command fails
# -u: error if undefined variable used
# -x: log all commands
# -o pipefail: entire command fails if pipe fails. watch out for yes | ...
# -o history: record shell history

# set -xo pipefail -o history
# set -exo pipefail -o history

# This script is used to generate a version number for a release

######################
# LOGGING FUNCTIONS
######################
function error() {
    if [ "${TF_BUILD:=}" != "" ]; then
        echo "##vso[task.logissue type=error]$*"
    elif [ "${GITHUB_ACTIONS:=}" != "" ]; then
        echo "::error $*"
    else
        echo "ERROR : $*"
    fi
}

function debug() {
    if [ "${verbose}" != "true" ]; then
        return
    fi

    declare message=""
    if [ "${TF_BUILD:=}" != "" ]; then
        message="##vso[task.debug]$*"
    elif [ "${GITHUB_ACTIONS:=}" != "" ]; then
        message="::debug::$*"
    else
        message="DEBUG : $*"
    fi

    >&2 echo "${message}"
}

function info() {
    if [ "${verbose}" != "true" ]; then
        return
    fi

    >&2 echo "INFO : $*"
}

function warn() {
    if [ "${verbose}" != "true" ]; then
        return
    fi

    declare message=""
    if [ "${TF_BUILD:=}" != "" ]; then
        message="##vso[task.logissue type=warning]$*"
    elif [ "${GITHUB_ACTIONS:=}" != "" ]; then
        message="::warning $*"
    else
        message="WARNING : $*"
    fi

    >&2 echo "${message}"
}

######################
# DEFAULT VALUES
######################
verbose=""
remote_name="origin"

master_branch=""
develop_branch=""
major_version=""
minor_version=""
patch_version=""
revision_version=""
build_id=""
branch_name=""
commit_sha=""
output_txt=""
output_props=""

version_core=""
version_extension=""
version_full=""

temp_folder=".tmp"
branch_name_max_length=30

######################
# EXPLORING BRANCHES
######################

function escape_regex_string() {
    sed 's/[][\.|$(){}?+*^]/\\&/g' <<< "$*"
}

# Check if git branch -r contains develop
git fetch --all --quiet >/dev/null 2>&1 || true

if git branch -r | grep -q "^\s*$(escape_regex_string "$remote_name/develop")$"; then
    develop_branch="develop"
    info "develop_branch=$develop_branch (git branch -r)"
#else
#    error "Branch develop not found in remote repository"
#    exit 10
fi

# Check if git branch -r contains main or master
if git branch -r | grep -q "^\s*$(escape_regex_string "$remote_name/main")$"; then
    master_branch="main"
    info "master_branch=$master_branch (git branch -r)"
elif git branch -r | grep -q "^\s*$(escape_regex_string "$remote_name/master")$"; then
    master_branch="master"
    info "master_branch=$master_branch (git branch -r)"
else
    error "Branch main or master not found in remote repository"
    exit 11
fi

######################
# EXPLORING GITHUB VALUES
######################
if [ "${GITHUB_REF:=}" != "" ]; then
    branch_name=$(echo "$GITHUB_REF" | sed 's/refs\/heads\///g')
    info "branch_name=$branch_name (GITHUB_REF = $GITHUB_REF)"
else
    branch_name=$(git rev-parse --abbrev-ref HEAD)
    info "branch_name=$branch_name (git rev-parse --abbrev-ref HEAD)"
fi
# commit_sha
if [ "${GITHUB_SHA:=}" != "" ]; then
    commit_sha="$GITHUB_SHA"
    info "commit=$commit_sha (GITHUB_SHA = $GITHUB_SHA)"
else
    commit_sha=$(git rev-parse HEAD)
    info "commit=$commit_sha (git rev-parse HEAD)"
fi
# build_id
if [ "${GITHUB_RUN_NUMBER:=}" != "" ]; then
    build_id="$GITHUB_RUN_NUMBER"
    info "build_id=$build_id (GITHUB_RUN_NUMBER = $GITHUB_RUN_NUMBER)"
else
    build_id=0
    warn "build_id=$build_id (default)"
fi

######################
# USAGE
######################
function usage() {
    echo "usage: ./Laerdal.Version.sh [--verbose] [--remote name] [--master-branch master] [--develop-branch develop] [--major-version 1] [--minor-version 0] [--patch-version 0] [--revision-version 0] [--build-id 0] [--commit-sha HEAD] [--branch-name branchname] [--output-txt version.txt] [--output-props Laerdal.Version.props] [-h | --help]"
    echo "parameters:"
    echo "  --verbose                          Enables verbose output on stderr"
    echo "  --remote [name]                    Name of the remove git server (default is '$remote_name')"
    echo "  --master-branch [branch]           Name of the master branch (default is '$master_branch')"
    echo "  --develop-branch [branch]          Name of the develop branch (default is '$develop_branch')"
    echo "  --major-version [number]           Major version override"
    echo "  --minor-version [number]           Minor version override"
    echo "  --patch-version [number]           Patch version override"
    echo "  --revision-version [number]        Revision version override "
    echo "  --build-id [number]                Build id override (default is '$build_id')"
    echo "  --branch-name [branch]             Branch name override (default is '$branch_name' and note that the name will be trimmed to '$branch_name_max_length' characters)"
    echo "  --commit-sha [hash]                Commit hash override (default is '$commit_sha')"
    echo "  -o | --output-txt [filename]       Name of the output file"
    echo "  -p | --output-props [filename]     Name of the props file"
    echo "  -h | --help                        Prints this message"
}

function getCommitDate() {
    git show -s --format=%ct "$*"
}

######################
# OVERRIDING VALUES WITH ENVIRONMENT VARIABLES
######################

# if INPUTS_VERBOSE is set, then override the remote_name
if [ "$INPUTS_VERBOSE" != "" ]; then
    verbose="$INPUTS_VERBOSE"
    info "verbose=$verbose (INPUTS_VERBOSE)"
fi

# if INPUTS_REMOTE is set, then override the remote_name
if [ "$INPUTS_REMOTE" != "" ]; then
    remote_name="$INPUTS_REMOTE"
    info "remote_name=$remote_name (INPUTS_REMOTE)"
fi

# if INPUTS_MASTER_BRANCH is set, then override the master_branch
if [ "$INPUTS_MASTER_BRANCH" != "" ]; then
    master_branch="$INPUTS_MASTER_BRANCH"
    info "master_branch=$master_branch (INPUTS_MASTER_BRANCH)"
fi

# if INPUTS_DEVELOP_BRANCH is set, then override the develop_branch
if [ "$INPUTS_DEVELOP_BRANCH" != "" ]; then
    develop_branch="$INPUTS_DEVELOP_BRANCH"
    info "develop_branch=$develop_branch (INPUTS_DEVELOP_BRANCH)"
fi

# if INPUTS_MAJOR is set, then override the major
if [ "$INPUTS_MAJOR_VERSION" != "" ]; then
    major_version="$INPUTS_MAJOR_VERSION"
    info "major_version=$major_version (INPUTS_MAJOR_VERSION)"
fi

# if INPUTS_MINOR is set, then override the minor
if [ "$INPUTS_MINOR_VERSION" != "" ]; then
    minor_version="$INPUTS_MINOR_VERSION"
    info "minor_version=$minor_version (INPUTS_MINOR_VERSION)"
fi

# if INPUTS_PATCH is set, then override the patch
if [ "$INPUTS_PATCH_VERSION" != "" ]; then
    patch_version="$INPUTS_PATCH_VERSION"
    info "patch_version=$patch_version (INPUTS_PATCH_VERSION)"
fi

# if INPUTS_REVISION is set, then override the revision
if [ "$INPUTS_REVISION" != "" ]; then
    revision_version="$INPUTS_REVISION"
    info "revision_version=$revision_version (INPUTS_REVISION)"
fi

# if INPUTS_BUILD_ID is set, then override the build_id
if [ "$INPUTS_BUILD_ID" != "" ]; then
    build_id="$INPUTS_BUILD_ID"
    info "build_id=$build_id (INPUTS_BUILD_ID)"
fi

# if INPUTS_COMMIT is set, then override the commit
if [ "$INPUTS_COMMIT_SHA" != "" ]; then
    commit_sha="$INPUTS_COMMIT_SHA"
    info "commit_sha=$commit_sha (INPUTS_COMMIT_SHA)"
fi

# if INPUTS_BRANCH_NAME is set, then override the branch_name
if [ "$INPUTS_BRANCH_NAME" != "" ]; then
    branch_name="$INPUTS_BRANCH_NAME"
    info "branch_name=$branch_name (INPUTS_BRANCH_NAME)"
fi

# if INPUTS_OUTPUT_TXT is set, then override the output_txt
if [ "$INPUTS_OUTPUT_TXT" != "" ]; then
    output_txt="$INPUTS_OUTPUT_TXT"
    info "output_txt=$output_txt (INPUTS_OUTPUT_TXT)"
fi

# if INPUTS_OUTPUT_PROPS is set, then override the output_props
if [ "$INPUTS_OUTPUT_PROPS" != "" ]; then
    output_props="$INPUTS_OUTPUT_PROPS"
    info "output_props=$output_props (INPUTS_OUTPUT_PROPS)"
fi

######################
# PARSING PARAMETERS
######################
while [ "$1" != "" ]; do
    case $1 in
    --verbose)
        # shift    dont   no need
        verbose="true"
        ;;
    --remote)
        shift
        remote_name="$1"
        ;;
    --master-branch)
        shift
        master_branch="$1"
        ;;
    --develop-branch)
        shift
        develop_branch="$1"
        ;;
    --major-version)
        shift
        major_version="$1"
        ;;
    --minor-version)
        shift
        minor_version="$1"
        ;;
    --patch-version)
        shift
        patch_version="$1"
        ;;
    --revision-version)
        shift
        revision_version="$1"
        ;;
    --build-id)
        shift
        build_id="$1"
        ;;
    --branch-name)
        shift
        branch_name="$1"
        ;;
    --commit-sha)
        shift
        commit_sha="$1"
        ;;
    -h | --help)
        usage
        exit
        ;;
    -o | --output-txt)
        shift
        output_txt="$1"
        ;;
    -p | --output-props)
        shift
        output_props="$1"
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

if [[ "$major_version" == "" ]]; then
    echo
    echo "### No major version specified! ###"
    echo
    usage
    exit 2
fi

######################
# FETCHING KEY COMMIT SHA
######################

# First commit
first_commit=$(git rev-list --max-parents=0 "$commit_sha")
info "first_commit=$first_commit"

#head_develop_point
#develop_master_point
if [ "$branch_name" == "$master_branch" ]; then
    info "Will apply master branch process"
elif [ "$branch_name" == "$develop_branch" ]; then
    info "Will apply develop branch process"
    commit_date=$(getCommitDate "$commit_sha")
    debug "commit_date=$commit_date"
    develop_master_point=$(git rev-list "$remote_name/$master_branch..$commit_sha" --merges --before="$commit_date" --first-parent --max-count=1)
    if [ -z "$develop_master_point" ]; then # has never been merged
        develop_master_point=$(git merge-base "$remote_name/$master_branch" "$commit_sha" --fork-point)
    fi
    info "develop_master_point=$develop_master_point"
else
    info "Will apply feature branch process"
    commit_date=$(getCommitDate "$commit_sha")
    debug "commit_date=$commit_date"
    head_develop_point=$(git rev-list "$remote_name/$develop_branch..$commit_sha" --merges --before="$commit_date" --first-parent --max-count=1)
    if [ -z "$head_develop_point" ]; then # has never been merged
        head_develop_point=$(git merge-base "$remote_name/$develop_branch" "$commit_sha" --fork-point)
    fi
    info "head_develop_point=$head_develop_point"
    head_develop_date=$(getCommitDate "$head_develop_point")
    debug "head_develop_date=$head_develop_date"
    develop_master_point=$(git rev-list "$remote_name/$master_branch" --merges --before="$head_develop_date" --first-parent --max-count=1)
    if [ -z "$develop_master_point" ]; then # has never been merged
        develop_master_point=$(git merge-base "$remote_name/$master_branch" "$head_develop_point" --fork-point)
    fi
    info "develop_master_point=$develop_master_point"
fi


######################
# CHECKING VALUES ARE NOT EMPTY
######################

if [ "$branch_name" == "" ]; then
    error "Branch name is empty"
    exit 20
fi
if [ "$commit_sha" == "" ]; then
    error "Commit SHA is empty"
    exit 21
fi
if [ "$build_id" == "" ]; then
    error "Build ID is empty"
    exit 22
fi
if [ "$first_commit" == "" ]; then
    error "First commit is empty"
    exit 23
fi


######################
# FILLING VALUES
######################

# Suffix
# Replace / with - and _ with -
suffix=$(echo "$branch_name" | sed 's/\//-/g' | sed 's/_/-/g' | sed 's/\-*$//' | sed 's/^\-*//')

suffix=$(echo "${suffix}" | cut -b "1-${branch_name_max_length}" | sed 's/\-*$//' | sed 's/^\-*//') # trim the string to a reasonable length because if we don't the build system might run into errors later on due to large filepaths

info "suffix=$suffix"

# Minor
if [ "$minor_version" == "" ]; then
    if [ "$branch_name" == "$master_branch" ]; then
        minor_version=$(git rev-list "$first_commit..$commit_sha" --count --first-parent --ancestry-path)
    elif [ "$branch_name" == "$develop_branch" ]; then
        minor_version=$(git rev-list "$first_commit..$develop_master_point" --count --first-parent --ancestry-path)

        if [[ "$minor_version" -gt 0 ]]; then
            minor_version=$((minor_version + 1)) # vital to increment by one (to account for the merge-commit on the master branch) otherwise the non-master-tags will be lagging behind the latest master-tag in terms of version
        fi
    else
        minor_version=$(git rev-list "$first_commit..$develop_master_point" --count --first-parent --ancestry-path)

        if [[ "$minor_version" -gt 0 ]]; then
            minor_version=$((minor_version + 1)) # vital to increment by one (to account for the merge-commit on the master branch) otherwise the non-master-tags will be lagging behind the latest master-tag in terms of version
        fi
    fi
    info "minor_version=$minor_version"
else
    warn "Minor version override: $minor_version"
fi
if [ "$minor_version" == "" ]; then
    minor_version=0
fi

# Patch
if [ "$patch_version" == "" ]; then
    if [ "$branch_name" == "$master_branch" ]; then
        patch_version=0
    elif [ "$branch_name" == "$develop_branch" ]; then
        mkdir -p "$temp_folder"
        before_date=$(git show -s --format=%ct "$develop_master_point" --first-parent --ancestry-path)
        git rev-list "$first_commit..$commit_sha" --before="$before_date" > "$temp_folder/not.txt"
        git rev-list "$first_commit..$commit_sha" --first-parent --ancestry-path > "$temp_folder/all.txt"

        sort -o "$temp_folder/not.txt" "$temp_folder/not.txt"
        sort -o "$temp_folder/all.txt" "$temp_folder/all.txt"
        comm -13 "$temp_folder/not.txt" "$temp_folder/all.txt" > "$temp_folder/patch.txt"

        patch_version=$(sed -n '$=' $temp_folder/patch.txt)
        rm -rf "$temp_folder"
    else
        mkdir -p "$temp_folder"
        before_date=$(git show -s --format=%ct "$develop_master_point" --first-parent --ancestry-path)
        git rev-list "$first_commit..$head_develop_point" --before="$before_date" > "$temp_folder/not.txt"
        git rev-list "$first_commit..$head_develop_point" --first-parent --ancestry-path > "$temp_folder/all.txt"

        sort -o "$temp_folder/not.txt" "$temp_folder/not.txt"
        sort -o "$temp_folder/all.txt" "$temp_folder/all.txt"
        comm -13 "$temp_folder/not.txt" "$temp_folder/all.txt" > "$temp_folder/patch.txt"

        patch_version=$(sed -n '$=' $temp_folder/patch.txt)
        rm -rf "$temp_folder"
    fi
    info "patch_version=$patch_version"
else
    warn "Patch version override: $patch_version"
fi
if [ "$patch_version" == "" ]; then
    patch_version=0
fi

# Revision
if [ "$revision_version" == "" ]; then
    if [ "$branch_name" == "$master_branch" ]; then
        revision_version=0
    elif [ "$branch_name" == "$develop_branch" ]; then
        revision_version=0
    else
        revision_version=$(git rev-list "$head_develop_point..$commit_sha" --count)
    fi
    info "revision_version=$revision_version"
else
    warn "Revision version override: $revision_version"
fi
if [ "$revision_version" == "" ]; then
    revision_version=0
fi

# Version extension
if [ "$branch_name" == "$master_branch" ]; then
    version_extension=
elif [ "$branch_name" == "$develop_branch" ]; then
    version_extension=
else
    version_extension="-$suffix-$revision_version.$build_id"
fi
info "version_extension=$version_extension"

# Assembling versions
version_core=$major_version.$minor_version.$patch_version
info "version_core=$version_core"
version_full=$version_core$version_extension
info "version_full=$version_full"
version_assembly=$version_core.$revision_version
info "version_assembly=$version_assembly"

##############################
## Output
##############################

function setEnvironmentVariable() {
    if [ "${TF_BUILD:=}" != "" ]; then
        echo "##vso[task.setvariable variable=$1]$2"
        debug "Variable set : $1=$2"
    fi
    if [ "${GITHUB_ACTIONS:=}" != "" ]; then
        echo "$1=$2" >>"$GITHUB_ENV"
        debug "Variable set : $1=$2"
        echo "$1=$2" >>"$GITHUB_OUTPUT"
        debug "Output set : $1=$2"
    else
        export "$1"="$2"
        debug "Variable set : $1=$2"
    fi
}

setEnvironmentVariable "LAERDAL_VERSION_MAJOR" "$major_version"
setEnvironmentVariable "LAERDAL_VERSION_MINOR" "$minor_version"
setEnvironmentVariable "LAERDAL_VERSION_PATCH" "$patch_version"
setEnvironmentVariable "LAERDAL_VERSION_SUFFIX" "$suffix"
setEnvironmentVariable "LAERDAL_VERSION_REVISION" "$revision_version"
setEnvironmentVariable "LAERDAL_VERSION_BUILDID" "$build_id"
setEnvironmentVariable "LAERDAL_VERSION_CORE" "$version_core"
setEnvironmentVariable "LAERDAL_VERSION_EXTENSION" "$version_extension"
setEnvironmentVariable "LAERDAL_VERSION_FULL" "$version_full"
setEnvironmentVariable "LAERDAL_VERSION_ASSEMBLY" "$version_assembly"
setEnvironmentVariable "LAERDAL_VERSION_BRANCHNAME" "$branch_name"
setEnvironmentVariable "LAERDAL_VERSION_SCRIPTCALLED" true

if [ "$output_txt" != "" ]; then
    mkdir -p "$(dirname "$output_txt")" && touch "$output_txt"
    {
        echo "LAERDAL_VERSION_MAJOR=$major_version"
        echo "LAERDAL_VERSION_MINOR=$minor_version"
        echo "LAERDAL_VERSION_PATCH=$patch_version"
        echo "LAERDAL_VERSION_SUFFIX=$suffix"
        echo "LAERDAL_VERSION_REVISION=$revision_version"
        echo "LAERDAL_VERSION_BUILDID=$build_id"
        echo "LAERDAL_VERSION_CORE=$version_core"
        echo "LAERDAL_VERSION_EXTENSION=$version_extension"
        echo "LAERDAL_VERSION_FULL=$version_full"
        echo "LAERDAL_VERSION_ASSEMBLY=$version_assembly"
        echo "LAERDAL_VERSION_BRANCHNAME=$branch_name"
    } >"$output_txt"
    info "Generated $output_txt"
    setEnvironmentVariable "LAERDAL_VERSION_OUTPUTTXT" "$output_txt"
fi

if [ "$output_props" != "" ]; then
    mkdir -p "$(dirname "$output_props")" && touch "$output_props"
    {
        echo "<Project>"
        echo "    <PropertyGroup>"
        echo "        <Laerdal_Version_Major>$major_version</Laerdal_Version_Major>"
        echo "        <Laerdal_Version_Minor>$minor_version</Laerdal_Version_Minor>"
        echo "        <Laerdal_Version_Patch>$patch_version</Laerdal_Version_Patch>"
        echo "        <Laerdal_Version_Suffix>$suffix</Laerdal_Version_Suffix>"
        echo "        <Laerdal_Version_Revision>$revision_version</Laerdal_Version_Revision>"
        echo "        <Laerdal_Version_BuildId>$build_id</Laerdal_Version_BuildId>"
        echo "        <Laerdal_Version_Core>$version_core</Laerdal_Version_Core>"
        echo "        <Laerdal_Version_Extension>$version_extension</Laerdal_Version_Extension>"
        echo "        <Laerdal_Version_Full>$version_full</Laerdal_Version_Full>"
        echo "        <Laerdal_Version_Assembly>$version_assembly</Laerdal_Version_Assembly>"
        echo "        <Laerdal_Version_BranchName>$branch_name</Laerdal_Version_BranchName>"
        echo "        <Laerdal_Version_ScriptCalled>true</Laerdal_Version_ScriptCalled>"
        echo "    </PropertyGroup>"
        echo "</Project>"
    } >"$output_props"
    echo "Generated $output_props"
    setEnvironmentVariable "LAERDAL_VERSION_OUTPUTPROPS" "$output_props"
fi

if [ "${GITHUB_STEP_SUMMARY}" != "" ]; then    
    echo "# $version_full" >> "$GITHUB_STEP_SUMMARY"
    echo "<details><summary>Expand for details</summary>" >> "$GITHUB_STEP_SUMMARY"
    echo "" >> "$GITHUB_STEP_SUMMARY"
    echo "| Variable | Value |" >> "$GITHUB_STEP_SUMMARY"
    echo "|----------|-------|" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_MAJOR | $major_version |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_MINOR | $minor_version |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_PATCH | $patch_version |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_SUFFIX | $suffix |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_REVISION | $revision_version |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_BUILDID | $build_id |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_CORE | $version_core |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_EXTENSION | $version_extension |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_FULL | $version_full |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_ASSEMBLY | $version_assembly |" >> "$GITHUB_STEP_SUMMARY"
    echo "| LAERDAL_VERSION_BRANCHNAME | $branch_name |" >> "$GITHUB_STEP_SUMMARY"
    echo "" >> "$GITHUB_STEP_SUMMARY"
    echo "</details>" >>"$GITHUB_STEP_SUMMARY"
fi

echo "$version_full"

exit 0
