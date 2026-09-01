$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$appProject = Join-Path $projectRoot "src\ModuKeymapStudio\ModuKeymapStudio.csproj"
$dist = Join-Path $projectRoot "dist"

if (Test-Path -LiteralPath $dist) {
    Remove-Item -LiteralPath $dist -Recurse -Force
}

dotnet restore $appProject --runtime win-x64 --configfile (Join-Path $projectRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) {
    throw "Restore failed with exit code $LASTEXITCODE"
}

dotnet publish $appProject --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false --output $dist --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Publishing failed with exit code $LASTEXITCODE"
}

$executable = Join-Path $dist "ModuKeymapStudio.exe"
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Published executable was not found: $executable"
}

$process = Start-Process -FilePath $executable -ArgumentList "--smoke-test" -PassThru -Wait -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    throw "Packaged executable smoke test failed with exit code $($process.ExitCode)"
}

Write-Host "Published: $executable"
