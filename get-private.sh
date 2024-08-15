#!/usr/bin/sh
set -ue
git submodule add --force git@github.com:AdventureSpaceTeam/PrivateClient.git Content.Client/AdventurePrivate
git submodule add --force git@github.com:AdventureSpaceTeam/PrivateServer.git Content.Server/AdventurePrivate
git submodule add --force git@github.com:AdventureSpaceTeam/PrivateShared.git Content.Shared/AdventurePrivate
git restore --staged .gitmodules Content.Client/AdventurePrivate Content.Server/AdventurePrivate Content.Shared/AdventurePrivate
git restore .gitmodules
