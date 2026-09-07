# Pit Striker — Release Build Checklist

*Use this checklist before distributing internal APK test builds or production candidates.*

- [ ] Version number incremented in `Project Settings > Player` (Bundle Version & Version Code).
- [ ] Debug logging (`Debug.Log`) disabled or stripped via conditional compiler tags.
- [ ] Development Build checkbox unchecked in Build Settings.
- [ ] Scripting Backend set to **IL2CPP**.
- [ ] Target Architectures: **ARM64** and **ARMv7** checked.
- [ ] App Bundle (`.aab`) export selected for Google Play distribution.
- [ ] Release signing configured using the official release Keystore (never share passwords).
- [ ] Git commit tagged with release version (e.g. `git tag -a v1.0.0 -m "Release v1.0.0"`).
