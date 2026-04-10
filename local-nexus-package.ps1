# Used to build Nexus package locally

dotnet build -c Release
$env:BUILD_DIR = "$PSScriptRoot/CustomTranslation/thunderstore/temp/plugins"
.\package-nexus.ps1