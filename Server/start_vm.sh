#!/bin/bash
# ==============================================================================
# Pit Striker — Google Cloud 1-Click VM Deploy Script with STATIC IP
# Run this inside Google Cloud Shell (https://shell.cloud.google.com)
# ==============================================================================
set -e

PROJECT_ID=$(gcloud config get-value project 2>/dev/null)
if [ -z "$PROJECT_ID" ]; then
    echo "Please select or set your GCP project first: gcloud config set project <PROJECT_ID>"
    exit 1
fi

REGION="us-central1"
ZONE="us-central1-a"
INSTANCE_NAME="pitstriker-server-01"
IP_NAME="pitstriker-static-ip"

echo "========================================================"
echo " Deploying Pit Striker Cloud Dedicated Server"
echo " Project:     $PROJECT_ID"
echo " Region/Zone: $REGION / $ZONE"
echo " Machine:     $INSTANCE_NAME (e2-micro Free Tier)"
echo " IP Mode:     Reserved Static External IP"
echo "========================================================"

# 1. Open firewall port 7777
echo "[1/4] Configuring firewall for port 7777..."
gcloud compute firewall-rules create allow-pitstriker-ws \
    --allow tcp:7777 \
    --target-tags=pitstriker-server \
    --description="Allow WebSocket traffic on port 7777 for PitStriker multiplayer" \
    --quiet 2>/dev/null || echo "Firewall rule already exists."

# 2. Reserve Static External IP
echo "[2/4] Reserving Static External IP ($IP_NAME)..."
gcloud compute addresses create "$IP_NAME" \
    --region="$REGION" \
    --description="Static IP for PitStriker Game Server" \
    --quiet 2>/dev/null || echo "Static IP reservation already exists."

STATIC_IP=$(gcloud compute addresses describe "$IP_NAME" --region="$REGION" --format='value(address)')
echo "Reserved Static IP: $STATIC_IP"

# 3. Create the e2-micro VM with the static IP
echo "[3/4] Creating Compute Engine VM with Static IP..."
gcloud compute instances create "$INSTANCE_NAME" \
    --zone="$ZONE" \
    --machine-type="e2-micro" \
    --address="$STATIC_IP" \
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
    --quiet 2>/dev/null || echo "VM already exists or running."

# 4. Final Verification
echo "[4/4] Verifying VM network configuration..."
EXTERNAL_IP=$(gcloud compute instances describe "$INSTANCE_NAME" --zone="$ZONE" --format='get(networkInterfaces[0].accessConfigs[0].natIP)' 2>/dev/null || echo "$STATIC_IP")

echo ""
echo "========================================================"
echo " 🎉 Pit Striker Dedicated Server Deployed!"
echo " STATIC PUBLIC IP : $STATIC_IP"
echo " WebSocket URL    : ws://$STATIC_IP:7777"
echo ""
echo " DNS Setup (In your DNS manager for xtinex.com):"
echo "   Type   : A"
echo "   Name   : pitstriker"
echo "   Value  : $STATIC_IP"
echo "========================================================"
