$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $argumentList = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '"'
    $process = Start-Process powershell.exe -Verb RunAs -ArgumentList $argumentList -Wait -PassThru
    exit $process.ExitCode
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$publishPath = Join-Path $repositoryRoot 'publish/LunchFate'
$executablePath = Join-Path $publishPath 'LunchFate.Blazor.exe'
$serviceName = 'LunchFate'

if (-not (Test-Path -LiteralPath $executablePath)) {
    throw "Published executable not found: $executablePath"
}

$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$credential = Get-Credential -UserName $currentUser -Message 'Enter the current Windows account password for LocalDB access.'

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    if ($existingService.Status -ne 'Stopped') {
        Stop-Service -Name $serviceName -Force
    }

    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 1
}

$serviceBinaryPath = '"{0}"' -f $executablePath
New-Service -Name $serviceName -BinaryPathName $serviceBinaryPath -DisplayName 'LunchFate' -Description 'LunchFate web service' -StartupType Automatic -Credential $credential | Out-Null

sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
sc.exe failureflag $serviceName 1 | Out-Null
Start-Service -Name $serviceName

Write-Host 'LunchFate service installed and started.'
