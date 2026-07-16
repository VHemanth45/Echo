param(
    [string]$Endpoint = "https://integrate.api.nvidia.com/v1/chat/completions",
    [string]$Model = "nvidia/nemotron-3-super-120b-a12b",
    [string]$Url = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

$secureKey = Read-Host "Paste your NVIDIA NIM API key" -AsSecureString
$plainKey = [System.Net.NetworkCredential]::new("", $secureKey).Password.Trim()
if ($plainKey.StartsWith("Bearer ", [System.StringComparison]::OrdinalIgnoreCase)) {
    $plainKey = $plainKey.Substring(7).Trim()
}

if ([string]::IsNullOrWhiteSpace($plainKey)) {
    throw "NVIDIA NIM API key is required."
}

$existing = Get-Process -Name Echo.Gateway -ErrorAction SilentlyContinue
if ($existing) {
    $existing | Stop-Process
}

$env:Nim__Endpoint = $Endpoint
$env:Nim__ApiKey = $plainKey
$env:Nim__Model = $Model
$env:DOTNET_CLI_HOME = $repoRoot

Write-Host "Starting Echo gateway with NIM model '$Model' at $Url"
dotnet run --project "$repoRoot\src\Echo.Gateway" --urls $Url
