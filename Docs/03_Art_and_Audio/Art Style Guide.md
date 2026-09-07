# Pit Striker — Art Style Guide

## 1. Visual Direction: Stylized Realism
*Pit Striker* adopts a **tactile, stylized-realistic** aesthetic reminiscent of high-end physical tabletop games (e.g. polished glass marbles on miniature textured sandboxes).

* **Shapes:** Clean, rounded silhouettes. Avoid noisy geometry that interferes with physics trajectory reads.
* **Lighting:** Soft directional sunlight, subtle ambient occlusion, warm contact shadows under marbles.
* **Palette:** Earthy base neutrals (warm sands, terracotta clays, rich loam soils) contrasted with high-saturation gemstone marbles (Cobalt Blue, Emerald Green, Ruby Red, Amber Gold).

## 2. Marble Material Standards (URP Lit)
* **Smoothness:** `0.85 – 0.95` (Crisp specular highlights).
* **Metallic:** `0.0` for traditional glass/swirl marbles; `0.9` for special brass/metallic ball bearings.
* **Normal Map:** Subtle micro-scratches (`0.1` strength) to provide authentic physical texture under light.

## 3. Poly Count & Mobile Limits
* **Arena Model:** < 5,000 triangles total.
* **Marble Sphere:** 250 – 400 triangles (subdivided octahedron/icosphere).
* **Draw Calls Target:** < 40 draw calls per frame on screen.
