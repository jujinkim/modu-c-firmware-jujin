$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$testProject = Join-Path $projectRoot "tests\ModuKeymapStudio.Tests\ModuKeymapStudio.Tests.csproj"

dotnet restore $testProject --configfile (Join-Path $projectRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) {
    throw "Keymap Studio restore failed with exit code $LASTEXITCODE"
}

dotnet run --project $testProject --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Keymap Studio tests failed with exit code $LASTEXITCODE"
}
