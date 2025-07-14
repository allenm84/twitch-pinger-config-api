#!/bin/bash

pushd .
cd /home/james/api/twitch-pinger-config-api/bin
dotnet twitch-pinger-config-api.dll
popd
