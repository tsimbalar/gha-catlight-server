#!/bin/sh
dotnet restore src/GhaCatlightServer
dotnet build src/GhaCatlightServer

dotnet restore tests/GhaCatlightServer.Tests
dotnet build tests/GhaCatlightServer.Tests
dotnet test tests/GhaCatlightServer.Tests
