#!/bin/bash
# ==============================================================================
# Pit Striker — Google Cloud 1-Click VM Deploy Script
# Run this inside Google Cloud Shell (https://shell.cloud.google.com)
# ==============================================================================
set -e

PROJECT_ID=$(gcloud config get-value project 2>/dev/null)
if [ -z "$PROJECT_ID" ]; then
    echo "Please select or set your GCP project first: gcloud config set project <PROJECT_ID>"
    exit 1
fi

ZONE="us-central1-a"
INSTANCE_NAME="pitstriker-server-01"

echo "========================================================"
echo " Deploying Pit Striker Cloud Dedicated Server"
echo " Project:  $PROJECT_ID"
echo " Zone:     $ZONE"
echo " Instance: $INSTANCE_NAME (e2-micro Free Tier)"
echo "========================================================"

# 1. Open firewall port 7777
echo "[1/3] Configuring firewall for port 7777..."
gcloud compute firewall-rules create allow-pitstriker-ws \
    --allow tcp:7777 \
    --target-tags=pitstriker-server \
    --description="Allow WebSocket traffic on port 7777 for PitStriker multiplayer" \
    --quiet 2>/dev/null || echo "Firewall rule already exists."

# 2. Create the e2-micro VM (Eligible for Google Cloud Always Free Tier)
echo "[2/3] Creating Compute Engine VM..."
gcloud compute instances create "$INSTANCE_NAME" \
    --zone="$ZONE" \
    --machine-type="e2-micro" \
    --tags=pitstriker-server \
    --image-family=debian-12 \
    --image-project=debian-cloud \
    --metadata=startup-script='#!/bin/bash
apt-get update && apt-get install -y docker.io git
systemctl enable docker
systemctl start docker
mkdir -p /opt/pitstriker
cd /opt/pitstriker
git clone -b develop https://github.com/admin-xtinex/pitstricker.git .
docker compose -f Server/PitStrikerServer/docker-compose.yml up -d --build
' \
    --quiet 2>/dev/null || echo "VM already exists."

# 3. Retrieve External IP
echo "[3/3] Getting VM External IP address..."
EXTERNAL_IP=$(gcloud compute instances describe "$INSTANCE_NAME" --zone="$ZONE" --format='get(networkInterfaces[0].accessConfigs[0].natIP)')

echo ""
echo "========================================================"
echo " 🎉 Pit Striker Dedicated Server Deployed!"
echo " External IP: $EXTERNAL_IP"
echo " WebSocket URL: ws://$EXTERNAL_IP:7777"
echo ""
echo " Next step:"
echo " In your DNS manager for xtinex.com:"
echo " Set pitstriker.xtinex.com -> A Record -> $EXTERNAL_IP"
echo "========================================================"
