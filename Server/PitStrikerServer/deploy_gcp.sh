#!/usr/bin/env bash
# ==============================================================================
# Pit Striker — Google Cloud Deployment Script (Compute Engine / Cloud Run)
# ==============================================================================
set -euo pipefail

PROJECT_ID="${GCP_PROJECT_ID:-pitstriker-prod}"
REGION="${GCP_REGION:-us-central1}"
ZONE="${GCP_ZONE:-us-central1-a}"
INSTANCE_NAME="pitstriker-server-01"
MACHINE_TYPE="e2-micro" # Free Tier eligible machine type in us-central1/us-west1/us-east1

echo "========================================================"
echo " Deploying Pit Striker Cloud Dedicated Server to GCP"
echo " Project:  ${PROJECT_ID}"
echo " Region:   ${REGION}"
echo " Instance: ${INSTANCE_NAME} (${MACHINE_TYPE})"
echo "========================================================"

# Check for gcloud CLI
if ! command -v gcloud &> /dev/null; then
    echo "ERROR: Google Cloud SDK (gcloud) is not installed."
    echo "Install from: https://cloud.google.com/sdk/docs/install"
    exit 1
fi

gcloud config set project "${PROJECT_ID}"

# 1. Create Firewall rule for port 7777 if not present
echo "Ensuring firewall rule for port 7777 exists..."
gcloud compute firewall-rules create allow-pitstriker-ws \
    --allow tcp:7777 \
    --target-tags=pitstriker-server \
    --description="Allow WebSocket traffic on port 7777 for PitStriker multiplayer" \
    || true

# 2. Deploy or update VM with startup script
echo "Creating/Updating Compute Engine VM..."
gcloud compute instances create "${INSTANCE_NAME}" \
    --zone="${ZONE}" \
    --machine-type="${MACHINE_TYPE}" \
    --tags=pitstriker-server,http-server \
    --image-family=debian-12 \
    --image-project=debian-cloud \
    --metadata=startup-script='#!/bin/bash
apt-get update && apt-get install -y docker.io git
systemctl enable docker
systemctl start docker
mkdir -p /opt/pitstriker
cd /opt/pitstriker
# Clone or pull latest repository
if [ ! -d ".git" ]; then
  git clone https://github.com/admin-xtinex/pitstricker.git .
else
  git pull origin main
fi
docker compose -f Server/PitStrikerServer/docker-compose.yml up -d --build
' || true

# 3. Retrieve External IP
EXTERNAL_IP=$(gcloud compute instances describe "${INSTANCE_NAME}" --zone="${ZONE}" --format='get(networkInterfaces[0].accessConfigs[0].natIP)')

echo "========================================================"
echo " ★ Deployment Successful! ★"
echo " Server WebSocket Endpoint: ws://${EXTERNAL_IP}:7777"
echo " Configure this IP in Unity NetworkConfig or ServerConfig."
echo "========================================================"
