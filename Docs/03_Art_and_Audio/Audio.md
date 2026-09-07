# Pit Striker — Audio Design Specification

## 1. Soundscapes & Atmosphere
* **Acoustic Clarity:** Because marble collisions occur frequently, clips must be clean, short, and recorded with high dynamic range.
* **Pitch Variation:** Every collision sound must randomize pitch within `[0.92, 1.08]` to avoid repetitive "machine gun" sound fatigue.
* **Volume Velocity Scaling:** Impact volume scales logarithmically with relative collision velocity.

## 2. Core SFX Asset Manifest

| Cue Name | Trigger Event | Description |
| :--- | :--- | :--- |
| `SFX_Marble_Hit_Glass_01..05` | Marble strikes marble | Sharp resonant glass-on-glass clack. |
| `SFX_Marble_Hit_Wood_01..03` | Marble strikes boundary rail | Dull, dense wooden thud. |
| `SFX_Marble_Roll_Loop` | Marble velocity > 0.1 m/s | Low rumble rolling sound modulated by surface roughness. |
| `SFX_Marble_Sink_Pit_01..03` | Marble enters pit trigger | Hollow satisfying thud dropping into cup. |
| `SFX_UI_Button_Click` | Menu interaction | Crisp wooden or stone tile tap. |
| `SFX_Turn_Bell` | Turn handover | Gentle chiming tone. |
| `SFX_Victory_Fanfare` | Final score reached | Short uplifting acoustic guitar/xylophone riff. |

## 3. Compression & Memory Settings
* **SFX:** Load into memory as Decompress on Load (small `.wav` files < 200KB).
* **Music / Ambient Stems:** Compressed in Vorbis format, Stream from Disc.
