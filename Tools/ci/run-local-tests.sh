#!/usr/bin/env bash
# Run Core simulation tests locally without Unity.
# Requires: dotnet SDK 8.0+
#   sudo pacman -S dotnet-sdk
set -euo pipefail
cd "$(dirname "$0")/../../Tests.Local"
dotnet test --verbosity normal
