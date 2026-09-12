# Pit Striker — Google Cloud Deployment Guide

This guide describes how to deploy `PitStrikerServer` (.NET 10 standalone dedicated multiplayer server) to Google Cloud Platform (GCP).

---

## 1. Prerequisites

1. Google Cloud Account with an active project (`gcp_project_id`).
2. Google Cloud SDK (`gcloud`) installed locally or Google Cloud Shell.
3. Docker installed (for container builds).

---

## 2. Option A: Google Compute Engine (e2-micro Free-Tier Eligible)

The `e2-micro` VM (1 GB RAM, 0.25-2 vCPU) is eligible for Google Cloud Free Tier in US regions (`us-central1`, `us-west1`, `us-east1`). `PitStrikerServer` uses less than 35 MB of RAM and ~0.5% CPU when idle, making it ideal for this tier.

### Automated Deployment Script
Run the automated deployment script from the project root:

**Linux / macOS:**
```bash
export GCP_PROJECT_ID="your-project-id"
export GCP_REGION="us-central1"
chmod +x Server/PitStrikerServer/deploy_gcp.sh
./Server/PitStrikerServer/deploy_gcp.sh
```

**Windows PowerShell:**
```powershell
$env:GCP_PROJECT_ID = "your-project-id"
.\Server\PitStrikerServer\deploy_gcp.ps1
```

### What the script does:
1. Creates firewall rule `allow-pitstriker-ws` allowing TCP traffic on port `7777`.
2. Provisions a Debian 12 `e2-micro` VM tagged `pitstriker-server`.
3. Installs Docker and starts the server via `docker compose -f Server/PitStrikerServer/docker-compose.yml up -d --build`.
4. Returns the external IP address: `ws://<EXTERNAL_IP>:7777`.

---

## 3. Option B: Local Development / Testing

To run the dedicated server locally on your development machine:

```bash
cd Server/PitStrikerServer
dotnet run -- --port 7777
```

Output:
```text
=================================================
★ PitStriker Cloud Server (.NET 10) Online ★
  Listening on: port 7777
  Environment:  Development
  Protocol Ver: 1
=================================================
```

Android emulators connect via `ws://10.0.2.2:7777` (standard Android emulator loopback alias) or local LAN IP `ws://192.168.x.x:7777`.

---

## 4. Configuring Unity Client

In Unity:
1. Open the inspector or scriptable config.
2. Set `_serverUrl` on `CloudNetworkClient` to your cloud endpoint:
   - Development: `ws://127.0.0.1:7777`
   - Android Emulator: `ws://10.0.2.2:7777`
   - Google Cloud Staging/Production: `ws://<EXTERNAL-IP>:7777`
