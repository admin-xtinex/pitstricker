# GitHub Actions Android Build & Artifact Delivery Guide

This pipeline automatically builds, signs, and packages the Android APK for **Pit Striker** in GitHub Actions, completely removing the need to build locally.

---

## 1. How It Works

Every push to `develop` or `main` (or clicking **Run workflow** manually under the GitHub **Actions** tab) runs:
[`.github/workflows/build-android-apk.yml`](file:///.github/workflows/build-android-apk.yml)

The workflow:
1. **Clones Repository & Restores Cache**: Restores the Unity `Library/` cache to compile in ~5 minutes instead of 30+.
2. **Builds Android Player**: Uses GameCI (`game-ci/unity-builder@v4`) with Unity 6 (`6000.6.0f1`).
3. **Signs with Keystore**: Automatically signs using the in-repo keystore (`pitstriker/Keystore/pitstriker.keystore` with alias `pitstriker` and password `xtinex123`).
4. **Uploads Downloadable Artifact**: Publishes `PitStriker.apk` to GitHub Actions run artifacts, available for direct 1-click download from your browser for 14 days.

---

## 2. Setting Up the One-Time GitHub Secret

Because Unity batchmode requires license activation on GitHub runners, set **one** secret in your GitHub repository:

1. Open **[license.unity3d.com](https://license.unity3d.com)** in your browser and sign in with your Unity account.
2. Upload the activation file that has already been generated in your repo:
   [`Unity_v6000.6.0f1.alf`](file:///Unity_v6000.6.0f1.alf)
3. Select **Unity Personal Edition** (free) and choose **I don't use Unity in a professional capacity**.
4. Click **Download license file** (`Unity_lic.ulf`).
5. Open `Unity_lic.ulf` in Notepad and copy all of its text.
6. In GitHub, go to:
   **Repository Settings > Secrets and variables > Actions > New repository secret**
   - Name: `UNITY_LICENSE`
   - Value: Paste the `.ulf` XML contents.
   - Click **Add secret**.

*(If you have a Unity Plus/Pro subscription instead, you can simply add `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL` as secrets).*

---

## 3. How to Download Built APKs

1. Go to your GitHub repo and click the **Actions** tab.
2. Click on the latest run of **Build and Upload Android APK**.
3. Under the **Artifacts** section at the bottom of the summary page, click **PitStriker-Android-build-XXX**.
4. A `.zip` file containing `PitStriker.apk` will download immediately to your PC/phone!
