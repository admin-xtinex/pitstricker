import bpy
print("=== COLLECTIONS ===")
for c in bpy.data.collections:
    print(f"COL|{c.name}|objs={len(c.objects)}")
print("=== OBJECTS ===")
for o in bpy.data.objects:
    dims = tuple(round(x, 3) for x in o.dimensions)
    polys = len(o.data.polygons) if getattr(o.data, "polygons", None) is not None else 0
    print(f"OBJ|{o.name}|{o.type}|dims={dims}|tris~{polys}|hide={o.hide_viewport}")
print("=== MATERIALS ===")
for m in list(bpy.data.materials)[:40]:
    print(f"MAT|{m.name}")
print("DONE")