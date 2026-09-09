"""Deterministic, batched 3D scenery inspired by demo2; no collision geometry."""
import math
import random
import bpy
from mathutils import Vector

R = random.Random(1709)
batches = {}

def material(name, color, roughness=.85, metallic=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = roughness
    p.inputs['Metallic'].default_value = metallic
    return m

def face(mat, points, smooth=False):
    center = sum((Vector(p) for p in points), Vector()) / len(points)
    key = (mat.name, int(center.x // 12), int(center.y // 12))
    verts, faces, flags = batches.setdefault(key, ([], [], []))
    i = len(verts)
    verts.extend(points)
    faces.append(tuple(range(i, i+len(points))))
    flags.append(smooth)

def box(mat, center, size):
    x,y,z = center; a,b,c = (n*.5 for n in size)
    p = [(x+dx*a,y+dy*b,z+dz*c) for dx,dy,dz in
         [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    for f in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]: face(mat,[p[i] for i in f])

def tube(mat, a, b, r1, r2=None, sides=8):
    a,b=Vector(a),Vector(b); d=(b-a).normalized()
    u=d.cross(Vector((0,0,1)))
    if u.length < .01: u=d.cross(Vector((1,0,0)))
    u.normalize(); v=d.cross(u); r2=r1 if r2 is None else r2
    for i in range(sides):
        t=i*math.tau/sides; t2=(i+1)*math.tau/sides
        n=u*math.cos(t)+v*math.sin(t); n2=u*math.cos(t2)+v*math.sin(t2)
        face(mat,[a+n*r1,a+n2*r1,b+n2*r2,b+n*r2],True)

def leaf(mat, origin, direction, length, width, rise=.15, drop=.2):
    o=Vector(origin); d=Vector(direction).normalized(); side=Vector((-d.y,d.x,0)).normalized()
    steps=5
    for i in range(steps):
        t=i/steps; q=(i+1)/steps
        def p(t,s):
            return o+d*(length*t)+Vector((0,0,rise*math.sin(math.pi*t)-drop*t*t))+side*(s*width*math.sin(math.pi*t)**.65)
        face(mat,[p(t,-1),p(q,-1),p(q,0)+Vector((0,0,.018)),p(t,0)+Vector((0,0,.018))])
        face(mat,[p(t,0)+Vector((0,0,.018)),p(q,0)+Vector((0,0,.018)),p(q,1),p(t,1)])

def palm(x,y,height, greens, bark):
    lean=R.uniform(-.6,.6); last=Vector((x,y,0))
    for i in range(18):
        t=(i+1)/18; end=Vector((x+lean*t*t,y+.35*t*t,height*t))
        tube(bark,last,end,.20*(1-i/30),.20*(1-(i+1)/30),10)
        # Raised trunk rings catch the low sun.
        tube(bark,end-Vector((0,0,.026)),end,.205*(1-(i+1)/30),sides=10)
        last=end
    for f in range(13):
        angle=f*math.tau/13+R.uniform(-.15,.15)
        direction=Vector((math.cos(angle),math.sin(angle),0))
        side=Vector((-direction.y,direction.x,0))
        length=R.uniform(2.3,3.3); rise=R.uniform(.65,1.25); drop=R.uniform(.7,1.5)
        def spine(t): return last+direction*(length*t)+Vector((0,0,rise*math.sin(t*math.pi*.85)-drop*t*t))
        for k in range(7): tube(greens[1],spine(k/7),spine((k+1)/7),.021,.01,5)
        for k in range(1,15):
            t=k/16; pos=spine(t); l=.68*math.sin(math.pi*t)**.6+.08
            for sign in [-1,1]:
                leaf(R.choice(greens),pos,side*sign+direction*.4,l,.065,.07,.25)

def rock(mat, center, scale):
    # Beveled-looking irregular stones from three eight-sided rings.
    x,y,z=center; sx,sy,sz=scale
    rings=[]
    for h,r in [(-.7,.65),(-.3,1),(.45,.93),(.8,.60)]:
        rings.append([Vector((x+sx*r*math.cos(i*math.tau/8)*R.uniform(.91,1.09),y+sy*r*math.sin(i*math.tau/8)*R.uniform(.91,1.09),z+sz*h)) for i in range(8)])
    for j in range(3):
        for i in range(8): face(mat,[rings[j][i],rings[j][(i+1)%8],rings[j+1][(i+1)%8],rings[j+1][i]])
    face(mat,rings[-1])

def build():
    greens=[material('Foliage_Olive',(.18,.29,.035)),material('Foliage_Forest',(.055,.16,.025)),material('Foliage_Sunlit',(.32,.42,.07)),material('Foliage_Lime',(.24,.36,.035))]
    bark=material('Detail_Bark',(.24,.12,.045)); wood=material('Detail_Wood',(.20,.095,.037))
    plaster=material('Detail_Plaster',(.64,.45,.25)); dark=material('Detail_Shadow',(.035,.024,.014))
    stones=[material('Detail_Stone_'+str(i),c) for i,c in enumerate([(.29,.27,.20),(.38,.35,.27),(.22,.24,.18),(.43,.38,.28)])]
    roofs=[material('Detail_Terracotta_'+str(i),c) for i,c in enumerate([(.34,.10,.045),(.46,.16,.07),(.29,.075,.03),(.39,.12,.05)])]
    earth=material('Detail_Earth',(.40,.26,.105)); hill=material('Detail_Mountain',(.22,.30,.29))

    # Surrounding ground stays below the existing Unity playing surface.
    for side in [-1,1]: box(earth,(side*22,17,-.14),(26,100,.2))
    box(earth,(0,64,-.14),(75,52,.2))

    # Winding vegetation edges, never an opaque flat green rectangle.
    for i in range(14000):
        y=R.uniform(-10,48); side=R.choice([-1,1]); edge=2.35+.35*math.sin(y*.44)+.16*math.sin(y*1.4)
        x=side*R.uniform(edge,6 if R.random()<.8 else 15)
        h=R.uniform(.12,.38)
        for j in range(R.randint(4,7)):
            angle=R.random()*math.tau; d=Vector((math.cos(angle),math.sin(angle),0))
            base=Vector((x+R.uniform(-.08,.08),y+R.uniform(-.08,.08),.012))
            right=Vector((-d.y,d.x,0))*R.uniform(.024,.045)
            mid=base+d*h*.28+Vector((0,0,h*.68)); tip=base+d*h*.85+Vector((0,0,h*.80))
            mat=R.choice(greens)
            face(mat,[base-right,base+right,mid+right*.4,mid-right*.4])
            face(mat,[mid-right*.4,mid+right*.4,tip])
    # Pebbles scatter mostly toward the verges; the pit approaches stay clear.
    for i in range(700):
        x=R.uniform(-8,8); y=R.uniform(-9,42)
        if abs(x)<.85 or (abs(x)<2 and R.random()<.8): continue
        s=R.uniform(.025,.10)
        rock(R.choice(stones),(x,y,s*.2),(s*1.4,s,s*.65))

    # Textured masonry follows the left edge into the village.
    for row in range(3):
        for i in range(36):
            rock(R.choice(stones),(-4.6, -2+i*.8+(row%2)*.35,.17+row*.30),(.34,R.uniform(.35,.43),.19))

    # Kerala house: veranda, open dark windows and individual rounded roof tiles.
    box(plaster,(-7.2,9.8,1.6),(4.5,7.0,3.2))
    box(stones[1],(-4.85,9.8,.20),(1.5,7.4,.4))
    for y in [6.8,8.2,10.0,11.8,13.0]:
        box(wood,(-4.4,y,1.65),(.13,.13,2.9))
        box(dark,(-4.94,y,1.65),(.025,.7,1.15))
        for z in [1.1,2.20]: box(wood,(-4.88,y,z),(.07,.80,.07))
    box(dark,(-4.93,9.7,1.23),(.035,.85,2.05))
    for side in [-1,1]:
        for row in range(12):
            x=-7.2+side*(row+.5)*.245
            z=4.3-(row+.5)*.105
            for j in range(32):
                y=5.9+j*.25
                tube(R.choice(roofs),(x,y,z),(x+side*.26,y,z-.112),.155,.15,7)
    tube(roofs[1],(-7.2,5.7,4.33),(-7.2,14.1,4.33),.18,sides=12)
    for y in [7.2,12.0]: box(plaster,(-7.2,y,4.48),(.32,.40,.70))

    # Split rails and weathered benches.
    for x in range(-14,15,2): tube(wood,(x,39.5,0),(x+.08,39.5,1.4),.075,.055)
    for z in [.5,1.0]: tube(wood,(-14,39.5,z),(14,39.5,z),.065)
    for x,y in [(4.6,-2),(-4.0,.5),(6.1,13)]:
        for off in [-.19,0,.19]: box(wood,(x,y+off,.56),(1.9,.16,.13))
        for dx in [-.7,.7]: box(stones[0],(x+dx,y,.26),(.24,.42,.52))

    for x,y,h in [(-6,-6,7),(-8,2,8.5),(-10,15,9),(7,-3,8),(8,10,9),(6,26,8),(-6,28,9),(-10,37,10),(10,39,10),(0,45,11)]: palm(x,y,h,greens,bark)
    for i in range(20): palm(R.uniform(-30,30),R.uniform(45,68),R.uniform(7,13),greens,bark)
    # Large leafy foreground plants frame the lane without covering the marble.
    for i in range(55):
        x=R.choice([-1,1])*R.uniform(3.3,12); y=R.uniform(-9,39)
        for j in range(6):
            a=j*math.tau/6+R.random()*.3
            leaf(R.choice(greens),(x,y,.05),(math.cos(a),math.sin(a),0),R.uniform(.5,1.1),.13,.55,.22)

    # Continuous softly ridged heightfield, with overlapping asymmetric summits.
    peaks=[(-45,95,18,22,14),(-15,105,28,25,17),(12,97,20,22,15),(40,110,25,26,19)]
    def height(x,y):
        z=sum(h*math.exp(-((x-px)/sx)**2-((y-py)/sy)**2) for px,py,h,sx,sy in peaks)
        return max(-.2,z*(1+.045*math.sin(x*.47+y*.31)+.025*math.sin(x*.91-y*.43))-.3)
    for x in range(-85,85,2):
        for y in range(65,145,2):
            face(hill,[(x,y,height(x,y)),(x+2,y,height(x+2,y)),(x+2,y+2,height(x+2,y+2)),(x,y+2,height(x,y+2))],True)

    # Keep the supplied bicycle and cow as recognizable distant props.
    from environment import _add_bicycle, _add_cow_proxy
    col=bpy.data.collections.new('Village_Hero_Props'); bpy.context.scene.collection.children.link(col)
    _add_bicycle(col,material('Detail_Bicycle',(.025,.03,.025),.3,.7))
    _add_cow_proxy(col,material('Detail_Cow',(.52,.31,.14)),dark)

    for (name,x,y),(vertices,faces,flags) in batches.items():
        mesh=bpy.data.meshes.new(f'{name}_{x}_{y}')
        mesh.from_pydata(vertices,[],faces); mesh.update()
        obj=bpy.data.objects.new(mesh.name,mesh); bpy.context.scene.collection.objects.link(obj)
        mesh.materials.append(bpy.data.materials[name])
        for p,smooth in zip(mesh.polygons,flags): p.use_smooth=smooth
        # World-space UVs for bark/stone detail, plus a second directional axis.
        uv=mesh.uv_layers.new(name='UVMap')
        for poly in mesh.polygons:
            n=poly.normal
            for li in poly.loop_indices:
                p=mesh.vertices[mesh.loops[li].vertex_index].co
                uv.data[li].uv = (p.y,p.z) if abs(n.x)>.65 else ((p.x,p.z) if abs(n.y)>.65 else (p.x,p.y))
    print('Detailed village batches:',len(batches))
