#!/bin/bash
dotnet publish -c Release -r linux-x64 --self-contained=true -p:PublishSingleFile=false -p:GenerateRuntimeConfigurationFiles=true -o /opt/data/traveler-management Traveler.DiscordBot.csproj
