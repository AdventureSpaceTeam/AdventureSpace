#!/usr/bin/env pwsh

param([string]$csvPath)

# Dumps Patreon's CSV download into a JSON file the game reads.

# Have to trim patron names because apparently Patreon doesn't which is quite ridiculous.
Get-content $csvPath | ConvertFrom-Csv -Delimiter "," | select @{l="Name";e={$_.Name.Trim()}},Tier | ConvertTo-Json -Compress
