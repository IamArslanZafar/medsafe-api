param(
    [string]$Configuration = "Release",
    [string]$ProjectPath = (Join-Path $PSScriptRoot "MedSafe.Api/MedSafe.Api.csproj"),
    [string]$PublishDir = (Join-Path $PSScriptRoot "MedSafe.Api/publish"),
    [string]$SiteName = "medsafe-001-site1",
    [string]$ComputerName = "https://win8100.site4now.net:8172/msdeploy.axd?site=medsafe-001-site1",
    [string]$UserName = "medsafe-001",
    [string]$Password = $env:WEBDEPLOY_PASSWORD,
    [string]$ApiUrl = "https://medsafe-001-site1.etempurl.com/"
)

# Set the WEBDEPLOY_PASSWORD environment variable or pass -Password before running.
if (-not $Password) {
    throw "Web Deploy password not set. Set `$env:WEBDEPLOY_PASSWORD or pass -Password."
}

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logDir = Join-Path $PSScriptRoot "logs"
$logPath = Join-Path $logDir "deploy-$timestamp.log"

if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
    Write-Host $line
    Add-Content -Path $logPath -Value $line
}

if (-not (Test-Path $ProjectPath)) {
    throw "Project file not found: $ProjectPath"
}

if (-not (Test-Path $PublishDir)) {
    New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null
}

try {
    Write-Log "Starting deployment"
    Write-Log "Configuration: $Configuration"
    Write-Log "Project: $ProjectPath"
    Write-Log "Publish dir: $PublishDir"
    Write-Log "Site: $SiteName"

    Write-Log "Publishing application (ASPNETCORE_ENVIRONMENT=Production)..."
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    & dotnet publish $ProjectPath -c $Configuration -o $PublishDir --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    Write-Log "Publishing completed successfully"

    # Inject the ASPNETCORE_ENVIRONMENT into the IIS web.config so the live
    # server always loads appsettings.Production.json on start-up.
    $webConfigPath = Join-Path $PublishDir "web.config"
    if (Test-Path $webConfigPath) {
        $xml = [xml](Get-Content $webConfigPath)
        $aspNetCore = $xml.SelectSingleNode("//aspNetCore")
        if ($aspNetCore -ne $null) {
            $envVars = $aspNetCore.SelectSingleNode("environmentVariables")
            if ($envVars -eq $null) {
                $envVars = $xml.CreateElement("environmentVariables")
                $aspNetCore.AppendChild($envVars) | Out-Null
            }
            # Remove any existing ASPNETCORE_ENVIRONMENT entry to avoid duplicates
            $existing = $envVars.SelectSingleNode("environmentVariable[@name='ASPNETCORE_ENVIRONMENT']")
            if ($existing -ne $null) { $envVars.RemoveChild($existing) | Out-Null }

            $envVar = $xml.CreateElement("environmentVariable")
            $envVar.SetAttribute("name", "ASPNETCORE_ENVIRONMENT")
            $envVar.SetAttribute("value", "Production")
            $envVars.AppendChild($envVar) | Out-Null
            $xml.Save($webConfigPath)
            Write-Log "Injected ASPNETCORE_ENVIRONMENT=Production into web.config"
        } else {
            Write-Log "WARNING: aspNetCore element not found in web.config — skipping env injection"
        }
    } else {
        Write-Log "WARNING: web.config not found in publish dir — skipping env injection"
    }

    $msdeploy = "C:/Program Files/IIS/Microsoft Web Deploy V3/msdeploy.exe"
    if (-not (Test-Path $msdeploy)) {
        throw "MSDeploy not found at $msdeploy"
    }

    Write-Log "Starting Web Deploy sync..."
    & $msdeploy -verb:sync `
        -source:contentPath="$PublishDir" `
        -dest:contentPath="$SiteName",computerName="$ComputerName",userName="$UserName",password="$Password",authType="Basic" `
        -allowUntrusted `
        -enableRule:AppOffline `
        -enableRule:DoNotDeleteRule

    if ($LASTEXITCODE -ne 0) {
        throw "Web Deploy failed with exit code $LASTEXITCODE"
    }

    Write-Log "Web Deploy completed successfully"

    Write-Log "Checking deployed endpoint..."
    try {
        $response = Invoke-WebRequest -Uri $ApiUrl -Method Get -UseBasicParsing -TimeoutSec 20
        Write-Log "Endpoint status: $($response.StatusCode)"
        Write-Log "Endpoint returned content length: $($response.Content.Length)"
    }
    catch {
        Write-Log "Endpoint check failed: $($_.Exception.Message)"
    }

    Write-Log "Deployment finished"
}
finally {
    Write-Log "Script completed"
}
