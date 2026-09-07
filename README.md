# Pit Striker

> **Physics-based Multiplayer Marble Strategy Game for Android**  
> Built with Unity 6 LTS | C#

---

## 🎯 Overview

**Pit Striker** is a tactical, physics-based marble game where players compete to sink marbles into designated ground pits using precise swipe trajectories, strategic bank shots, and tactical collisions.

* **Platform:** Android (Mobile First)
* **Engine:** Unity 6 LTS (Universal Render Pipeline - URP)
* **Status:** Phase 0 — Project Foundation & Architecture Established
* **Target Audience:** Casual-competitive mobile gamers, turn-based tabletop enthusiasts

---

## 📁 Repository Structure

This repository follows a strict multi-tier studio hierarchy separating raw digital content creation (DCC) assets from engine runtime assets:

* `Docs/` — Game design, technical architecture, physics specifications, and operations checklists.
* `Art/` — Layered Photoshop, Blender, and concept art sources (tracked via Git LFS, isolated from Unity).
* `Audio/` — High-fidelity audio masters and DAW sessions.
* `Builds/` — Android test builds (`.apk` / `.aab`).
* `Unity/` — Clean Unity 6 LTS project root.
  * `Unity/Assets/_Project/` — Isolated custom project code, prefabs, scenes, and shaders.
  * `Unity/Assets/ThirdParty/` — Quarantined third-party packages and plugins.

---

## 📚 Documentation Index

All architectural and design specifications live inside [`Docs/`](file:///c:/Users/Tisan/Documents/pitstricker/Docs):

1. **Vision & Design:**
   * [Vision & Core Pillars](file:///c:/Users/Tisan/Documents/pitstricker/Docs/01_Vision_and_Design/Vision.md)
   * [Master Roadmap](file:///c:/Users/Tisan/Documents/pitstricker/Docs/01_Vision_and_Design/Roadmap.md)
   * [Game Design Document (GDD)](file:///c:/Users/Tisan/Documents/pitstricker/Docs/01_Vision_and_Design/Game%20Design%20Document.md)
2. **Mechanics & Physics:**
   * [Physics Design](file:///c:/Users/Tisan/Documents/pitstricker/Docs/02_Gameplay_Mechanics/Physics%20Design.md)
   * [Camera System](file:///c:/Users/Tisan/Documents/pitstricker/Docs/02_Gameplay_Mechanics/Camera%20Design.md)
   * [Touch Controls](file:///c:/Users/Tisan/Documents/pitstricker/Docs/02_Gameplay_Mechanics/Controls.md)
   * [Rules & Scoring](file:///c:/Users/Tisan/Documents/pitstricker/Docs/02_Gameplay_Mechanics/Rules%20and%20Scoring.md)
3. **Engineering & Standards:**
   * [Technical Architecture](file:///c:/Users/Tisan/Documents/pitstricker/Docs/04_Engineering/Technical%20Architecture.md)
   * [Coding Standards & Conventions](file:///c:/Users/Tisan/Documents/pitstricker/Docs/04_Engineering/Coding%20Standards.md)
   * [Android Optimization Guide](file:///c:/Users/Tisan/Documents/pitstricker/Docs/04_Engineering/Android%20Optimization.md)
4. **Operations & QA:**
   * [Testing Checklist](file:///c:/Users/Tisan/Documents/pitstricker/Docs/05_QA_and_Operations/Testing%20Checklist.md)
   * [Play Store Compliance Checklist](file:///c:/Users/Tisan/Documents/pitstricker/Docs/05_QA_and_Operations/Play%20Store%20Checklist.md)

---

## 🛠️ Getting Started for Developers

1. **Prerequisites:**
   * Unity 6 LTS (6000.0.x or newer) with Android Build Support & OpenJDK installed.
   * Git + Git LFS (`git lfs install`).
   * Visual Studio 2022 / JetBrains Rider / VS Code.
2. **Clone Repository:**
   ```bash
   git clone https://github.com/admin-xtinex/pitstricker.git
   git lfs pull
   ```
3. **Open Project:**
   * In Unity Hub, click **Add project from disk** and select the [`Unity/`](file:///c:/Users/Tisan/Documents/pitstricker/Unity) folder.
