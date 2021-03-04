#!/usr/bin/env python3
# Packages a full release build that can be unzipped and you'll have your SS14 client or server.

import os
import shutil
import subprocess
import sys
import zipfile
import argparse

from typing import List, Optional

try:
    from colorama import init, Fore, Style
    init()

except ImportError:
    # Just give an empty string for everything, no colored logging.
    class ColorDummy(object):
        def __getattr__(self, name):
            return ""

    Fore = ColorDummy()
    Style = ColorDummy()

class PlatformReg:
    def __init__(self, rid: str, target_os: str):
        self.rid = rid
        self.target_os = target_os

p = os.path.join

PLATFORMS = [
    PlatformReg("win-x64", "Windows"),
    PlatformReg("linux-x64", "Linux"),
    PlatformReg("linux-arm64", "Linux"),
    PlatformReg("osx-x64", "MacOS"),
]

PLATFORM_RIDS = {x.rid for x in PLATFORMS}

SHARED_IGNORED_RESOURCES = {
    ".gitignore",
    ".directory",
    ".DS_Store"
}

SERVER_IGNORED_RESOURCES = {
    "Textures",
    "Fonts",
    "Audio",
    "Shaders",
}

# Assembly names to copy from content.
# PDBs are included if available, .dll/.pdb appended automatically.
SERVER_CONTENT_ASSEMBLIES = [
    "Content.Server.Database",
    "Content.Server",
    "Content.Shared"
]

# Extra assemblies to copy on the server, with a startswith
SERVER_EXTRA_ASSEMBLIES = [
    "Npgsql.",
    "Microsoft",
]

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Packages the SS14 content repo for release on all platforms.")
    parser.add_argument("--platform",
                        "-p",
                        action="store",
                        choices=PLATFORM_RIDS,
                        nargs="*",
                        help="Which platform to build for. If not provided, all platforms will be built")

    parser.add_argument("--skip-build",
                        action="store_true",
                        help=argparse.SUPPRESS)

    args = parser.parse_args()
    platforms = args.platform
    skip_build = args.skip_build

    if not platforms:
        platforms = PLATFORM_RIDS

    if os.path.exists("release"):
        print(Fore.BLUE + Style.DIM +
              "Cleaning old release packages (release/)..." + Style.RESET_ALL)
        shutil.rmtree("release")

    os.mkdir("release")

    # Good variable naming right here.
    for platform in PLATFORMS:
        if platform.rid not in platforms:
            continue

        build_platform(platform, skip_build)


def wipe_bin():
    print(Fore.BLUE + Style.DIM +
          "Clearing old build artifacts (if any)..." + Style.RESET_ALL)
    if os.path.exists(p("RobustToolbox", "bin")):
        shutil.rmtree(p("RobustToolbox", "bin"))

    if os.path.exists("bin"):
        shutil.rmtree("bin")


def build_platform(platform: PlatformReg, skip_build: bool) -> None:
    print(Fore.GREEN + f"Building project for {platform.rid}..." + Style.RESET_ALL)

    if not skip_build:
        subprocess.run([
            "dotnet",
            "build",
            p("Content.Server", "Content.Server.csproj"),
            "-c", "Release",
            "--nologo",
            "/v:m",
            f"/p:TargetOS={platform.target_os}",
            "/t:Rebuild",
            "/p:FullRelease=True",
            "/m"
        ], check=True)

        publish_client_server(platform.rid, platform.target_os)

    print(Fore.GREEN + "Packaging {platform.rid} server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", f"SS14.Server_{platform.rid}.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server", platform.rid, "publish"), "", server_zip)
    copy_resources(p("Resources"), server_zip)
    copy_content_assemblies(p("Resources", "Assemblies"), server_zip)
    server_zip.close()


def publish_client_server(runtime: str, target_os: str) -> None:
    # Runs dotnet publish on client and server.
    base = [
        "dotnet", "publish",
        "--runtime", runtime,
        "--no-self-contained",
        "-c", "Release",
        f"/p:TargetOS={target_os}",
        "/p:FullRelease=True",
        "/m"
    ]

    subprocess.run(base + ["RobustToolbox/Robust.Server/Robust.Server.csproj"], check=True)


def copy_resources(target, zipf):
    # Content repo goes FIRST so that it won't override engine files as that's forbidden.
    ignore_set = SHARED_IGNORED_RESOURCES.union(SERVER_IGNORED_RESOURCES)

    do_resource_copy(target, "Resources", zipf, ignore_set)
    do_resource_copy(target, p("RobustToolbox", "Resources"), zipf, ignore_set)


def do_resource_copy(target, source, zipf, ignore_set):
    for filename in os.listdir(source):
        if filename in ignore_set:
            continue

        path = p(source, filename)
        target_path = p(target, filename)
        if os.path.isdir(path):
            copy_dir_into_zip(path, target_path, zipf)

        else:
            zipf.write(path, target_path)


def zip_entry_exists(zipf, name):
    try:
        # Trick ZipInfo into sanitizing the name for us so this awful module stops spewing warnings.
        zinfo = zipfile.ZipInfo.from_file("Resources", name)
        zipf.getinfo(zinfo.filename)
    except KeyError:
        return False
    return True


def copy_dir_into_zip(directory, basepath, zipf):
    if basepath and not zip_entry_exists(zipf, basepath):
        zipf.write(directory, basepath)

    for root, _, files in os.walk(directory):
        relpath = os.path.relpath(root, directory)
        if relpath != "." and not zip_entry_exists(zipf, p(basepath, relpath)):
            zipf.write(root, p(basepath, relpath))

        for filename in files:
            zippath = p(basepath, relpath, filename)
            filepath = p(root, filename)

            message = "{dim}{diskroot}{sep}{zipfile}{dim} -> {ziproot}{sep}{zipfile}".format(
                sep=os.sep + Style.NORMAL,
                dim=Style.DIM,
                diskroot=directory,
                ziproot=zipf.filename,
                zipfile=os.path.normpath(zippath))

            print(Fore.CYAN + message + Style.RESET_ALL)
            zipf.write(filepath, zippath)


def copy_content_assemblies(target, zipf):
    files = []
    source_dir = p("bin", "Content.Server")
    base_assemblies = SERVER_CONTENT_ASSEMBLIES

    # Additional assemblies that need to be copied such as EFCore.
    for filename in os.listdir(source_dir):
        for extra_assembly_start in SERVER_EXTRA_ASSEMBLIES:
            if filename.startswith(extra_assembly_start):
                files.append(filename)
                break

    # Include content assemblies.
    for asm in base_assemblies:
        files.append(asm + ".dll")
        # If PDB available, include it aswell.
        pdb_path = asm + ".pdb"
        if os.path.exists(p(source_dir, pdb_path)):
            files.append(pdb_path)

    # Write assemblies dir if necessary.
    if not zip_entry_exists(zipf, target):
        zipf.write(".", target)

    for x in files:
        zipf.write(p(source_dir, x), p(target, x))


if __name__ == '__main__':
    main()
