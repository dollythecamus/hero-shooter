#!/usr/bin/env bash
# Headless Unity launcher for this host (NixOS).
#
# Two separate problems are solved here:
#  1. The editor binary links against libs NixOS does not expose globally — steam-run supplies an FHS
#     environment for the editor itself.
#  2. The editor spawns Editor/Data/Tools/UnityShaderCompiler as a child process, and that child cannot
#     find a 64-bit libtinfo or libstdc++ inside the FHS — without them every build aborts with
#     "Shader compiler initialization error ... Timed out". Both are prepended below.
#
# These two store paths can be invalidated by `nix-collect-garbage -d`; if the launcher stops working,
# re-derive them with:
#   ls -d /nix/store/*ncurses-*/lib/libtinfo.so.6 /nix/store/*gcc-*/lib/libstdc++.so.6
# (pick the 64-bit libtinfo: byte 4 of the file is 2, not 1).
#
# Usage: tools/unity-run.sh -batchmode -nographics -projectPath . \
#            -executeMethod HeroShooter.EditorTools.BuildScript.BuildLinux -logFile Logs/build.log
export LD_LIBRARY_PATH="/nix/store/62qhvy2m2lpqj7ca39j9wvp2kkcxwdcg-ncurses-6.6/lib:/nix/store/vhbhh61kn078jk7jqdj4vfvfx2bh9glb-gcc-14.3.0-lib/lib:${LD_LIBRARY_PATH}"
exec /home/chloe/Unity/Hub/Editor/6000.6.3f1/Editor/Unity "$@"
