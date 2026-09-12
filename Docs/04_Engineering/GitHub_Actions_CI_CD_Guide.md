# GitHub Actions CI/CD & Firebase App Distribution Guide

This document explains how the automated Android APK build and distribution pipeline works for **Pit Striker**.

---

## 1. Pipeline Architecture

Every push to `develop` or `main` (or manual dispatch via the GitHub Actions tab) triggers the workflow:
[`.github/workflows/build-android-apk.yml`](file:///.github/workflows/build-android-apk.yml)

The pipeline performs the following steps:
1. **Checkout & Cache**: Clones the repo (with LFS) and restores the Unity `Library/` cache to keep build times fast (~5–7 min instead of 30+ min).
2. **GameCI Unity Builder**: Launches an official Unity 6 (`6000.6.0f1`) Android container to compile and package the game.
3. **Release Keystore Signing**: Signs the APK automatically using `pitstriker/Keystore/pitstriker.keystore` (Alias: `pitstriker`, Keystore Pass: `xtinex123`, Key Pass: `xtinex123`).
4. **GitHub Artifact Upload**: Uploads `PitStriker.apk` to GitHub Actions run artifacts (accessible and downloadable for 14 days under the run summary).
5. **Firebase App Distribution**: Distributes the signed APK directly to registered tester groups (e.g. `testers`) via Firebase App Tester.

---

## 2. Required GitHub Repository Secrets

Navigate to **GitHub Repository > Settings > Secrets and variables > Actions > New repository secret**:

### A. Unity License (One of the two options)

#### Option 1: Personal (Free) License
1. Go to **Actions** tab in GitHub, select **Request Unity License (.alf)** workflow, and click **Run workflow**.
2. Download the generated `.alf` file from the workflow artifacts.
3. Open [license.unity3d.com](https://license.unity3d.com) in your browser, upload the `.alf` file, and choose your free Personal license.
4. Download the generated `.ulf` license file.
5. Open the `.ulf` file in a text editor, copy all contents, and save it as secret `UNITY_LICENSE`.

#### Option 2: Unity Professional / Plus / Student
Set these three secrets:
- `UNITY_EMAIL`: Your Unity account email
- `UNITY_PASSWORD`: Your Unity account password
- `UNITY_SERIAL`: Your Unity license key

---

### B. Firebase App Distribution (Optional / Recommended)

To automatically distribute builds to Android devices without manual downloading:
1. Open the [Firebase Console](https://console.firebase.google.com/) and navigate to your project.
2. Go to **Release & Monitor > App Distribution**. Add your Android app (`com.xtinex.pitstriker`) and create a tester group (e.g. `testers`).
3. Add the following secrets to GitHub:
   - `FIREBASE_APP_ID`: Found in Firebase Project Settings (e.g. `1:1234567890:android:abcdef123456`).
   - `FIREBASE_SERVICE_ACCOUNT`: Content of a GCP Service Account JSON key with the **Firebase App Distribution Admin** role.
     *(Alternatively, generate a token using `firebase login:ci` and save it as `FIREBASE_TOKEN`).*
   - `FIREBASE_GROUPS`: *(Optional)* Name of tester group (defaults to `testers`).

> **Note**: If Firebase secrets are not configured yet, the build will still succeed and upload the APK as a downloadable GitHub Artifact.

---

### C. Keystore Customization (Optional)

The workflow already includes default values for the in-repository keystore (`pitstriker.keystore`). You only need these secrets if you want to override them:
- `ANDROID_KEYSTORE_PASS` (default: `xtinex123`)
- `ANDROID_KEYALIAS_NAME` (default: `pitstriker`)
- `ANDROID_KEYALIAS_PASS` (default: `xtinex123`)

---

## 3. Triggering a Build Manually

1. Go to your repository on GitHub.
2. Click **Actions** > **Build and Deploy Android APK**.
3. Click **Run workflow**, choose your branch (e.g., `develop`), and optionally enter custom release notes.
4. When finished, you will find:
   - Downloadable APK under **Artifacts** on the workflow run page.
   - Notification and instant download on Android via the **Firebase App Tester** app.
