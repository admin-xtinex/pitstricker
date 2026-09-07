# Pit Striker — Android Performance & Optimization Guide

## 1. Frame Rate & Timesteps
* Target: Stable **60 FPS** on mid-range Android hardware.
* Timestep Configuration (`Project Settings > Time`):
  * `Fixed Timestep`: `0.02` (50Hz) or `0.0166` (60Hz) to prevent stutter during physics calculations.
  * `Maximum Allowed Timestep`: `0.05` (prevents physics spiral of death during temporary CPU hiccups).

## 2. Rendering & Shaders (Universal Render Pipeline - URP)
* **Shading Model:** Use `URP/Simple Lit` or optimized `URP/Lit` without complex multi-pass lighting.
* **Shadows:** Single directional light with Hard Shadows or Soft Shadows at 1024 resolution. Max shadow distance: 15 meters.
* **Batching:** Enable **SRP Batcher** and **Static Batching** on arena ground and boundary walls.

## 3. Textures & Compression
* Texture compression standard: **ASTC (Adaptive Scalable Texture Compression)**.
  * Normal Maps: ASTC 4x4 or 5x5.
  * Albedo / Base Textures: ASTC 6x6.
* Texture sizes capped at 1024x1024 for arena surfaces, 512x512 for individual marbles.

## 4. Mobile Memory & Thermal Throttling
* Target Memory Footprint: < 250 MB total RAM usage.
* Zero per-frame Garbage Collector allocations to avoid hitching on weak mobile CPUs.
