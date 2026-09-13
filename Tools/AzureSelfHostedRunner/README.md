# PitStriker Azure self-hosted Windows builder

The GitHub-hosted APK job fails because Unity license secrets are empty.
This VM is meant to run the `self-hosted (Windows PC)` job in
`.github/workflows/build-android-apk.yml`.

That job hard-requires:

```
C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe
labels: self-hosted, windows
```

## Recommended Azure size

- Windows Server 2022 or Windows 11 Pro
- 16 GB RAM minimum (32 GB better)
- 128 GB+ OS disk (Unity 6 + Android SDK/NDK is large)
- Open outbound 443 to GitHub and Unity
- Inbound 3389 only from your IP, not `0.0.0.0/0`

## On the VM (RDP as Administrator)

1. Copy `Setup-PitStrikerBuildRunner.ps1` onto the desktop.
2. Open PowerShell as Administrator:

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
cd $env:USERPROFILE\Desktop
.\Setup-PitStrikerBuildRunner.ps1
```

3. Sign in to Unity Hub and activate a license (Personal is fine).
4. Install editor **6000.6.0f1** with modules:
   - Android Build Support
   - OpenJDK
   - Android SDK & NDK Tools
   - Windows Build Support (IL2CPP)
5. Create a runner token (expires in ~1 hour):
   https://github.com/admin-xtinex/pitstricker/settings/actions/runners/new
6. Rerun:

```powershell
.\Setup-PitStrikerBuildRunner.ps1 -GitHubToken PASTE_TOKEN_HERE
```

7. Confirm the runner shows **Idle / Online** on the GitHub runners page.
8. Run the workflow:
   Actions → Build and Upload Android APK → Run workflow
   - Branch: `dev-isotrophic`
   - Runner type: `self-hosted (Windows PC)`

## What this machine cannot do from outside

Only TCP 3389 is open. SSH and WinRM are closed, so the runner cannot be
installed remotely without the RDP password. Do not paste that password into chat.
