param(
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$projectPath = Join-Path $repositoryRoot 'src/LunchFate.Blazor/LunchFate.Blazor.csproj'
$outputPath = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $repositoryRoot 'publish/LunchFate'
} elseif ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory
} else {
    Join-Path (Get-Location) $OutputDirectory
}
$outputPath = [System.IO.Path]::GetFullPath($outputPath)

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output $outputPath `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    -p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish 失败，退出码：$LASTEXITCODE"
}

Write-Host "LunchFate 已发布到：$outputPath"
