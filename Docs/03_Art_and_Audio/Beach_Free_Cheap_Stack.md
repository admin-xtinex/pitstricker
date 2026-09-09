# Pit Striker — Free / Cheap Beach Graphics Stack

**Honest bar:** Free stack gets a strong stylized tropical look + glassier marbles + sand juice.  
It will **not** match the photoreal concept overnight. It *will* look far better than greybox and stays Android-friendly.

## Free stack (₹0)

### Environment
1. **Yughues Free Palm Trees** (Asset Store)  
   https://assetstore.unity.com/packages/3d/vegetation/trees/yughues-free-palm-trees-13540  
   - Convert materials with Unity’s URP Render Pipeline Converter if needed

2. **Palm Tree Pack Free** (Asset Store, URP listed)  
   https://assetstore.unity.com/packages/3d/vegetation/trees/palm-tree-pack-free-214483

3. **Poly Pizza** — boat, lighthouse, hut, rocks, shells (CC/check each license)  
   https://poly.pizza/search/beach  
   https://poly.pizza/search/palm  
   https://poly.pizza/search/lighthouse

4. **Quaternius** nature / docks (CC0)  
   https://quaternius.com/  
   - Grab rocks, docks, nature pieces; not full photoreal beach but great fillers

5. **Our FBX** (already generated)  
   `Assets/_Project/Art/Environments/Beach/pit_striker_beach.fbx`  
   - Fairway scale + temporary props until free models replace them

### Marbles / glass
6. **GitHub — Unity URP Glass Shader** (MIT)  
   https://github.com/omid3098/Unity-URP-GlassShader  
   - Enable Opaque Texture on URP asset  
   - Tint Blue/Red/Green/Amber; high smoothness; optional swirl texture later

7. Optional stronger free-ish glass:  
   https://github.com/Youssef-Afella/UnityURP-FakeRealGlass (Unity 6+)

### Sand / juice (free DIY)
8. Build simple ParticleSystems (no pack):  
   - Soft sand burst on marble land / strike / pit sink  
   - Hook into existing `VFXManager`  
9. URP Lit sand material: warm albedo + tiling normal (free normals from ambientCG / Poly Haven — CC0)

### Sky / water
10. URP default Procedural Sky or free skybox from Asset Store “free skybox”  
11. Simple ocean: plane + URP Lit transparent blue, or Unity’s sample water if available — keep one plane only

## Cheap upgrades (if budget allows later)
| Item | Why | Approx |
|---|---|---|
| EZ Glass URP | Better mobile glass than DIY | ~$5 |
| Ultimate Glass Marbles | Swirl hero marbles | ~$15 |
| Sand VFX URP | Instant juice | paid |
| KEKOS / Hideout | Full beach kit | paid |

## Install order
1. Import free palms → `Assets/ThirdParty/Free/Palms/`  
2. Drop Poly Pizza / Quaternius FBX → `Assets/ThirdParty/Free/BeachProps/`  
3. Add GitHub glass shader → make 4 marble materials  
4. Keep physics colliders; swap **renderers only**  
5. Place ocean beside fairway; scatter palms off the 2.8 m play strip  
6. Add sand particle prefabs to VFXManager events

## Expectation check
- Free path ≈ stylized tropical mobile prototype  
- Concept image ≈ later paid packs or custom art  
- Gameplay stays frozen while we dress the arena