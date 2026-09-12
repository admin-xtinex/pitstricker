# ==============================================================================
# Pit Striker — Google Cloud Deployment Script (PowerShell)
# ==============================================================================
param(
    [string]$ProjectId = $env:GCP_PROJECT_ID,
    [string]$Region = "us-central1",
    [string]$Zone = "us-central1-a",
    [string]$InstanceName = "pitstriker-server-01",
    [string]$MachineType = "e2-micro"
)

$ErrorActionPreference = "Stop"

if (-not $ProjectId) {
    $ProjectId = "pitstriker-prod"
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " Deploying Pit Striker Cloud Dedicated Server to GCP" -ForegroundColor Cyan
Write-Host " Project:  $ProjectId"
Write-Host " Region:   $Region"
Write-Host " Instance: $InstanceName ($MachineType)"
Write-Host "========================================================" -ForegroundColor Cyan

if (-not (Get-Command gcloud -ErrorAction SilentlyContinue)) {
    Write-Error "Google Cloud SDK (gcloud) is not installed on this machine. Download from https://cloud.google.com/sdk/docs/install"
    return
}

gcloud config set project $ProjectId

Write-Host "Configuring firewall rule for port 7777..." -ForegroundColor Yellow
try {
    gcloud compute firewall-rules create allow-pitstriker-ws --allow tcp:7777 --target-tags=pitstriker-server --description="Allow WebSocket traffic on port 7777 for PitStriker multiplayer"
} catch {
    Write-Host "Firewall rule already exists." -ForegroundColor Gray
}

Write-Host "Provisioning Compute Engine VM with Docker..." -ForegroundColor Yellow
$startupScript = @'
#!/bin/bash
apt-get update && apt-get install -y docker.io git
systemctl enable docker
systemctl start docker
mkdir -p /opt/pitstriker
cd /opt/pitstriker
if [ ! -d ".git" ]; then
  git clone https://github.com/admin-xtinex/pitstricker.git .
else
  git pull origin main
fi
docker compose -f Server/PitStrikerServer/docker-compose.yml up -d --build
'@

gcloud compute instances create $InstanceName `
    --zone=$Zone `
    --machine-type=$MachineType `
    --tags=pitstriker-server,http-server `
    --image-family=debian-12 `
    --image-project=debian-cloud `
    --metadata=startup-script="$startupScript"

$externalIp = (gcloud compute instances describe $InstanceName --zone=$Zone --format='get(networkInterfaces[0].accessConfigs[0].natIP)').Trim()

Write-Host "========================================================" -ForegroundColor Green
Write-Host " ★ Deployment Successful! ★" -ForegroundColor Green
Write-Host " Server WebSocket Endpoint: ws://$($externalIp):7777" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Green
