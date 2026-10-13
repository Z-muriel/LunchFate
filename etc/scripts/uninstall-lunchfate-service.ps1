#requires -RunAsAdministrator

param(
    [string]$ServiceName = 'LunchFate'
)

$ErrorActionPreference = 'Stop'
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "Windows 服务 '$ServiceName' 不存在。"
    exit 0
}

if ($service.Status -ne 'Stopped') {
    Stop-Service -Name $ServiceName -Force
}

sc.exe delete $ServiceName | Out-Null
Write-Host "Windows 服务 '$ServiceName' 已卸载。"
