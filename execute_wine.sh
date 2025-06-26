#!/bin/sh
cd LC3-Simu-WinForms
wine dotnet clean
wine dotnet restore
wine dotnet build --no-restore
wine dotnet run --no-restore
cd ..
