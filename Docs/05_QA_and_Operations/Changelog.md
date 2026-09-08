# Pit Striker — Project Changelog

All notable changes to the Pit Striker project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

## [0.2.0] - 2026-09-08
### Added
* Completed Phase 1 (Unity Learning & Environment Setup) and Phase 2 (Physics Sandbox & Rolling Fairway).
* Procedural 24-sided round circular pit generator with seamless mesh colliders and numbered flags (`1`, `2`, `3`).
* Extended 40-meter sand fairway arena with solid wooden boundary rails.
* `MarbleController`: standardized mass (`1.0`), linear drag (`0.3`), angular drag (`0.8`), and out-of-bounds auto-recovery.
* `SwipeLaunchController`: drag-anywhere slingshot aiming, trajectory guide, and `Spacebar` test launch.
* `SmoothFollowCamera`: cinematic low-angle chase camera matching concept art.
* `PitZone`: sunken basin gravity funnel and goal capture detection.
* Verified URP materials: `M_Marble_Blue`, `M_Ground_Sand`, `M_Boundary_Wood`, `M_Pit_Dark`, and `M_Trajectory_Cyan`.

## [0.0.1] - 2026-09-08
### Added
* Completed Phase 0 Project Foundation and repository setup.
* Initialized multi-tier directory structure isolating Unity project from raw DCC art and audio.
* Established Git LFS tracking configuration for binary files in `.gitattributes`.
* Created comprehensive studio documentation suite in `Docs/` covering design, technical architecture, coding standards, physics, and operations.
* Scaffolding for `Assets/_Project/` Unity hierarchy with isolated submodules.
