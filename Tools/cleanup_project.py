"""Archive and plan removal of obsolete imports and development captures.

GUID references and literal asset paths in retained project files protect assets.
Deletion is performed separately by PowerShell using the verified manifest.
"""
from pathlib import Path
import re, json, zipfile, datetime

root = Path(__file__).resolve().parents[1]
assets = root / 'pitstricker/Assets'
candidates = set()
def add(relative):
    p = root / relative
    if p.is_dir():
        candidates.update(f for f in p.rglob('*') if f.is_file())
    elif p.is_file():
        candidates.add(p)
    meta = Path(str(p) + '.meta')
    if meta.is_file(): candidates.add(meta)

for relative in [
    'pitstricker/Assets/TEST',
    'pitstricker/Assets/ThirdParty/CanyonDesert',
    'pitstricker/Assets/ThirdParty/DeadTrees',
    'pitstricker/Assets/ThirdParty/CrashBash/Scene',
    'pitstricker/Assets/ThirdParty/CrashBash/New Terrain.asset',
    'pitstricker/Assets/_Project/Art/Environments/Beach',
    'pitstricker/Assets/vill.unity',
    'pitstricker/Assets/Scenes/SampleScene.unity',
    'pitstricker/Assets/_Project/Scripts/Core/Editor/AutoArenaBuilder.cs',
    'pitstricker/Assets/_Project/Scripts/Core/Editor/UnityExecProbe.cs',
    'pitstricker/Assets/_Project/Scripts/Core/Editor/ExportPitViewSnapshot.cs',
    'Art/Generated/VillageIntegration/AndroidRepairBackup',
    'Art/Generated/VillageIntegration/BeforeReferencePolish',
    'Tools/Blender/__pycache__',
    'Tools/fix_marble_collision.py',
]: add(relative)
for pattern in ['screenshot_*.png', 'emulator_*.png', '*.log']:
    for p in (root / 'pitstricker').glob(pattern): add(p.relative_to(root))
for p in (root / 'Art/Generated').glob('pit_striker_beach*'): add(p.relative_to(root))
for p in (root / 'Art').rglob('*.blend1'): add(p.relative_to(root))
for p in (root / 'Art/Generated/VillageIntegration').iterdir():
    if p.is_file() and p.suffix in ['.log', '.txt', '.backup', '.png', '.json']:
        add(p.relative_to(root))

# Protect anything referenced by retained serialized assets, settings or code.
files = list(assets.rglob('*')) + list((root / 'pitstricker/ProjectSettings').rglob('*'))
textfiles = {p: p.read_text(encoding='utf-8', errors='ignore') for p in files
             if p.is_file() and p.suffix in ['.meta', '.unity', '.asset', '.prefab', '.mat', '.cs', '.json']}
protected = set()
while True:
    retained = '\n'.join(t for p, t in textfiles.items() if p not in candidates)
    referenced = set(re.findall(r'guid:\s*([0-9a-f]{32})', retained))
    keep = set()
    for p in candidates:
        if not p.is_relative_to(assets) or p.suffix == '.meta': continue
        meta = Path(str(p) + '.meta')
        match = re.search(r'^guid:\s*([0-9a-f]{32})', textfiles.get(meta, ''), re.M)
        assetpath = p.relative_to(root / 'pitstricker').as_posix()
        if (match and match[1] in referenced) or assetpath in retained:
            keep.update([p, meta])
    keep &= candidates
    if not keep: break
    protected.update(keep)
    candidates -= keep

stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
archive = root.parent / 'PitStriker-Cleanup-Backups' / ('cleanup-' + stamp + '.zip')
archive.parent.mkdir(exist_ok=True)
ordered = sorted(candidates)
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=1) as z:
    for p in ordered: z.write(p, p.relative_to(root).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
manifest = {'archive': str(archive), 'files': [str(p) for p in ordered],
            'bytes': sum(p.stat().st_size for p in ordered),
            'protected': [str(p.relative_to(root)) for p in sorted(protected)]}
out = root / 'pitstricker/Library/cleanup-manifest.json'
out.write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(json.dumps({'archive': str(archive), 'files': len(ordered), 'MB': round(manifest['bytes']/1e6, 1), 'protected': manifest['protected']}, indent=2))
