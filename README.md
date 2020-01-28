# koala-bot
A bot for iLoveBacons, using an advance permission system. 
* It allows for different roles to have moderative permissions over different channels using the permission engine.
* It allows for starwatch moderation
* It has some fun things too.

## Code Quality
While the permission engine is mostly high quality code, the starwatch and relating components were hacked together last moment to get it done. Please expect to see some terrible code as at the time of writing the modules, I couldn't be bothered anymore and was writing to get it complete.

## Permissions
This bot is using an experimental Node-Base permission engine that I have designed to manage command controls. Its a bit slapped together and works for the most part. Treat it like Bukkit's permissions.

## Requirements
* MySQL
* Redis
* Dotnet Core 2.0
* D#+ Nightly Branch (Via SlimGet)[https://nuget.emzi0767.com/api/v3/index.json]

