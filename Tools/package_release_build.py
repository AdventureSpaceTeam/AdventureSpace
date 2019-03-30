#!/usr/bin/env python3
# Packages a full release build that can be unzipped and you'll have your SS14 client or server.

import os
import shutil
import subprocess
import sys
import zipfile
import argparse

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

p = os.path.join

SHARED_IGNORED_RESOURCES = {
    "ss13model.7z",
    "ResourcePack.zip",
    "buildResourcePack.py",
    "CONTENT_GOES_HERE",
    ".gitignore"
}
CLIENT_IGNORED_RESOURCES = {
    "Maps",
    "emotes.xml",
    "Groups"
}
SERVER_IGNORED_RESOURCES = {
    "Textures",
    "Fonts",
    "Audio",
    "Scenes",
    "Nano",
    "Shaders",
}

def main():
    global GODOT
    parser = argparse.ArgumentParser(
        description="Packages the SS14 content repo for release on all platforms.")
    parser.add_argument("--platform",
                        "-p",
                        action="store",
                        choices=["windows", "mac", "linux"],
                        nargs="*",
                        help="Which platform to build for. If not provided, all platforms will be built")

    args = parser.parse_args()
    platforms = args.platform
    if not platforms:
        platforms = ["windows", "mac", "linux"]

    if os.path.exists("release"):
        print(Fore.BLUE + Style.DIM +
              "Cleaning old release packages (release/)..." + Style.RESET_ALL)
        shutil.rmtree("release")

    os.mkdir("release")

    copy_godot_scenes()

    if "windows" in platforms:
        wipe_bin()
        build_windows()

    if "linux" in platforms:
        wipe_bin()
        build_linux()

    if "mac" in platforms:
        wipe_bin()
        build_macos()


def wipe_bin():
    print(Fore.BLUE + Style.DIM +
          "Clearing old build artifacts (if any)..." + Style.RESET_ALL)
    if os.path.exists(p("RobustToolbox", "bin")):
        shutil.rmtree(p("RobustToolbox", "bin"))

    if os.path.exists("bin"):
        shutil.rmtree("bin")


def build_windows():
    # Run a full build.
    print(Fore.GREEN + "Building project for Windows x64..." + Style.RESET_ALL)
    subprocess.run(["msbuild",
                    "SpaceStation14Content.sln",
                    "/m",
                    "/p:Configuration=Release",
                    "/p:Platform=x64",
                    "/nologo",
                    "/v:m",
                    "/p:TargetOS=Windows",
                    "/t:Rebuild"
                    ], check=True)

    print(Fore.GREEN + "Packaging Windows x64 client..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "SS14.Client_Windows_x64.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_dir_into_zip(p("RobustToolbox", "bin", "Client"), "", client_zip)
    copy_resources("Resources", client_zip, server=False)
    copy_content_assemblies(p("Resources", "Assemblies"), client_zip, server=False)
    # Cool we're done.
    client_zip.close()

    print(Fore.GREEN + "Packaging Windows x64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_Windows_x64.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    copy_content_assemblies(p("Resources", "Assemblies"), server_zip, server=True)
    server_zip.close()


def build_macos():
    print(Fore.GREEN + "Building project for macOS x64..." + Style.RESET_ALL)
    subprocess.run(["msbuild",
                    "SpaceStation14Content.sln",
                    "/m",
                    "/p:Configuration=Release",
                    "/p:Platform=x64",
                    "/nologo",
                    "/v:m",
                    "/p:TargetOS=MacOS",
                    "/t:Rebuild"
                    ], check=True)

    print(Fore.GREEN + "Packaging macOS x64 client..." + Style.RESET_ALL)
    # Client has to go in an app bundle.
    client_zip = zipfile.ZipFile(p("release", "SS14.Client_macOS_x64.zip"), "a",
                                 compression=zipfile.ZIP_DEFLATED)

    contents = p("Space Station 14.app", "Contents", "Resources")
    copy_dir_into_zip(p("BuildFiles", "Mac", "Space Station 14.app"), "Space Station 14.app", client_zip)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Client"), contents, client_zip)

    copy_resources(p(contents, "Resources"), client_zip, server=False)
    copy_content_assemblies(p(contents, "Resources", "Assemblies"), client_zip, server=False)
    client_zip.close()

    print(Fore.GREEN + "Packaging macOS x64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_macOS_x64.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    copy_content_assemblies(p("Resources", "Assemblies"), server_zip, server=True)
    server_zip.close()


def build_linux():
    # Run a full build.
    print(Fore.GREEN + "Building project for Linux x64..." + Style.RESET_ALL)
    subprocess.run(["msbuild",
                    "SpaceStation14Content.sln",
                    "/m",
                    "/p:Configuration=Release",
                    "/p:Platform=x64",
                    "/nologo",
                    "/v:m",
                    "/p:TargetOS=Linux",
                    "/t:Rebuild"
                    ], check=True)

    print(Fore.GREEN + "Packaging Linux x64 client..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "SS14.Client_Linux_x64.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_dir_into_zip(p("RobustToolbox", "bin", "Client"), "", client_zip)
    copy_resources("Resources", client_zip, server=False)
    copy_content_assemblies(p("Resources", "Assemblies"), client_zip, server=False)
    # Cool we're done.
    client_zip.close()

    print(Fore.GREEN + "Packaging Linux x64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_Linux_x64.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    copy_content_assemblies(p("Resources", "Assemblies"), server_zip, server=True)
    server_zip.close()


def copy_resources(target, zipf, server):
    # Content repo goes FIRST so that it won't override engine files as that's forbidden.
    do_resource_copy(target, "Resources", zipf, server)
    do_resource_copy(target, p("RobustToolbox", "Resources"), zipf, server)


def do_resource_copy(target, source, zipf, server):
    for filename in os.listdir(source):
        if filename in SHARED_IGNORED_RESOURCES \
                or filename in (SERVER_IGNORED_RESOURCES if server else CLIENT_IGNORED_RESOURCES):
            continue

        # Get rid of Godot .import files, thanks.
        if filename.endswith(".import"):
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


def copy_content_assemblies(target, zipf, server):
    if server:
        source_dir = p("bin", "Content.Server")
        files = ["Content.Shared.dll", "Content.Server.dll"]
    else:
        source_dir = p("bin", "Content.Client")
        files = ["Content.Shared.dll", "Content.Client.dll"]

    # Write assemblies dir if necessary.
    if not zip_entry_exists(zipf, target):
        zipf.write(".", target)

    for x in files:
        zipf.write(p(source_dir, x), p(target, x))


def copy_godot_scenes():
    target_dir = p("RobustToolbox", "SS14.Client.Godot", "Scenes")
    from_dir = p("Resources", "Scenes")
    for path in os.listdir(from_dir):
        if path.startswith("."):
            continue

        frompath = p(from_dir, path)
        targetpath = p(target_dir, path)
        if os.path.exists(targetpath):
            if os.path.isfile(targetpath):
                os.remove(targetpath)

            elif os.path.isdir(targetpath):
                shutil.rmtree(targetpath)

            else:
                print("So, what the hell is {} exactly and how did it get there?"
                      .format(targetpath))
                exit(1)

        copy_dir_or_file(frompath, targetpath)


def copy_dir_or_file(src, dst):
    """
    Just something from src to dst. If src is a dir it gets copied recursively.
    """

    if os.path.isfile(src):
        shutil.copy2(src, dst)

    elif os.path.isdir(src):
        shutil.copytree(src, dst)

    else:
        raise IOError("{} is neither file nor directory. Can't copy.".format(src))


if __name__ == '__main__':
    main()
