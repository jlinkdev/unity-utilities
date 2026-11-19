"""Regenerate three rigged meshes from the baked MPFB anatomical base."""
import heapq
import json
import math
from pathlib import Path
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector

HERE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
sys.path.insert(0, str(HERE))
from mesh_io import activate, export, normalize_skin, save


def replace_head(mesh, body, sides, twist):
    # Cut below the chin, through one closed neck loop. The profile preserves
    # the tilted underside of the jaw and a forward chin in side view.
    cut = 1.489
    result = bmesh.ops.bisect_plane(mesh, geom=list(mesh.verts) + list(mesh.edges) + list(mesh.faces),
                                    plane_co=(0, 0, cut), plane_no=(0, 0, 1), clear_outer=True)
    border = [v for v in result['geom_cut'] if isinstance(v, bmesh.types.BMVert) and v.is_boundary]
    center_y = sum(v.co.y for v in border) / len(border)
    def angle(v):
        return math.atan2(v.co.x, center_y - v.co.y) % math.tau
    border.sort(key=angle)
    assert len(border) >= 8 and all(mesh.edges.get((v, border[i - 1])) for i, v in enumerate(border))
    weights = mesh.verts.layers.deform.active
    neck, head = (body.vertex_groups[name].index for name in ('neck_01', 'head'))
    last, last_angles = border, [angle(v) for v in border]
    # z, half width, depth radius, y centre, jaw slope, head influence
    profile = [(1.508,.048,.055,-.038,.006,.22), (1.550,.056,.086,-.057,.032,.85),
               (1.622,.076,.098,-.052,.006,1), (1.681,.068,.086,-.048,0,1),
               (1.714,.043,.056,-.049,0,1)]
    for row, (z, width, depth, cy, slope, influence) in enumerate(profile):
        phase = (0.5 + twist * math.sin(row * 1.8)) * math.tau / sides
        angles = sorted((i * math.tau / sides + phase) % math.tau for i in range(sides))
        ring = []
        for t in angles:
            diagonal = (.028 if row == 2 else .009 if row == 3 else 0) * math.sin(t)
            v = mesh.verts.new((width * math.sin(t), cy - depth * math.cos(t), z - slope * math.cos(t) + diagonal))
            v[weights][head] = influence
            if influence < 1:
                v[weights][neck] = 1 - influence
            ring.append(v)
        # Zipper rings by azimuth, including the loop across the 2π boundary.
        i = j = 0
        while i < len(last) or j < sides:
            a = last_angles[i + 1] if i + 1 < len(last) else last_angles[0] + math.tau
            b = angles[j + 1] if j + 1 < sides else angles[0] + math.tau
            p, q = last[i % len(last)], ring[j % sides]
            if j == sides or (i < len(last) and a <= b):
                i += 1
                mesh.faces.new((p, last[i % len(last)], q))
            else:
                j += 1
                mesh.faces.new((p, ring[j % sides], q))
        last, last_angles = ring, angles
    apex = mesh.verts.new((0, -.047, 1.729))
    apex[weights][head] = 1
    for i, v in enumerate(last):
        mesh.faces.new((v, last[(i + 1) % sides], apex))
    bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))


def simplify_forefoot(mesh, body, sign):
    # Replace individual toes with a continuous forefoot, retaining the ball
    # joint for toe-off. Start behind the clefts, on one closed cross-section.
    foot = [v for v in mesh.verts if v.co.x * sign > 0 and v.co.z < .18]
    selected = set(foot)
    edges = [e for e in mesh.edges if all(v in selected for v in e.verts)]
    faces = [f for f in mesh.faces if all(v in selected for v in f.verts)]
    result = bmesh.ops.bisect_plane(mesh, geom=foot + edges + faces,
                                    plane_co=(0,-.123,0), plane_no=(0,-1,0), clear_outer=True)
    cut = [v for v in result['geom_cut'] if isinstance(v, bmesh.types.BMVert) and v.is_boundary]
    # Follow topology around the outline, including a nearly flat sole.
    outline = [min(cut, key=lambda v: (v.co.x, v.co.z))]
    remaining = set(cut) - set(outline)
    while remaining:
        choices = [e.other_vert(outline[-1]) for e in outline[-1].link_edges if e.is_boundary
                   and e.other_vert(outline[-1]) in remaining]
        v = min(choices, key=lambda p: (p.co.z, p.co.x))
        outline.append(v)
        remaining.remove(v)
    center = (max(v.co.x for v in cut) + min(v.co.x for v in cut)) / 2
    layer = mesh.verts.layers.deform.active
    side = 'l' if sign > 0 else 'r'
    ball, ankle = [body.vertex_groups[n + side].index for n in ('ball_', 'foot_')]
    previous = outline
    for y, width, height, toe_weight in ((-.16, .97,.75,.55),(-.201,.72,.46,1),(-.216,.39,.31,1)):
        ring = []
        for v in outline:
            dx = v.co.x-center
            point = mesh.verts.new((center+dx*width, y+.21*sign*dx, .003+(v.co.z-.003)*height))
            point[layer][ball] = toe_weight
            if toe_weight < 1:
                point[layer][ankle] = 1 - toe_weight
            ring.append(point)
        for j in range(len(ring)):
            mesh.faces.new((previous[j-1],previous[j],ring[j],ring[j-1]))
        previous = ring
    mesh.faces.new(previous)
    bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))


def reduce_body(body, cfg):
    mesh = bmesh.new()
    mesh.from_mesh(body.data)
    # Relax anatomical detail more strongly on the trunk and feet than on
    # the limbs. The head is replaced after relaxation to keep its profile.
    trunk = [v for v in mesh.verts if abs(v.co.x) < .20 and .78 < v.co.z < 1.45]
    feet = [v for v in mesh.verts if v.co.z < .065]
    for _ in range(cfg['smooth_passes']):
        bmesh.ops.smooth_vert(mesh, verts=trunk + feet, factor=.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    if cfg['torso_planes']:
        shape_trunk(trunk, cfg['torso_planes'], cfg['relief_meters'], cfg.get('waist_inset_meters', 0))
    replace_head(mesh, body, cfg['head_sides'], .32 if cfg['relief_meters'] else .14)
    for sign in (1,-1):
        simplify_forefoot(mesh, body, sign)
    bmesh.ops.triangulate(mesh, faces=list(mesh.faces))
    ratio = cfg['triangles'] / len(mesh.faces)
    mesh.to_mesh(body.data)
    mesh.free()
    # Collapse is cheaper on calm surfaces. Protect knuckles and bend zones
    # by applying additional collapse cost in their vicinity.
    joint_positions = [np.array(b.head_local) for b in body.parent.data.bones
                       if b.name.startswith(('lowerarm','hand','calf','foot','upperarm','thigh'))]
    mask = body.vertex_groups.new(name="Reduction mask")
    for v in body.data.vertices:
        d = min(np.linalg.norm(np.array(v.co) - p) for p in joint_positions)
        cost = max(0, 1 - d / .075)
        if abs(v.co.x) > .45:
            cost = max(cost, .6)
        # Blender scales collapse cost by the inverse of this group's weight.
        weight = 0 if v.co.z >= 1.489 - .0001 else 1 - .88 * cost
        mask.add([v.index], weight, 'REPLACE')
    modifier = body.modifiers.new("Curvature reduction", 'DECIMATE')
    modifier.ratio = min(1, ratio)
    modifier.vertex_group = mask.name
    modifier.vertex_group_factor = .75
    bpy.ops.object.modifier_move_to_index(modifier=modifier.name, index=0)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    body.vertex_groups.remove(body.vertex_groups["Reduction mask"])


def shape_trunk(vertices, sides, relief, waist_inset):
    # Sampled outlines along the pelvis, lower ribs, sternum and clavicles.
    # Project onto a polygonal cross section. A shared front plane extends
    # across the sternum, and the side planes connect the rib cage to the hips.
    levels = [.83, .92, 1.075, 1.25, 1.33, 1.42]
    widths = [.169, .160, .141, .169, .179, .167]
    fronts = [-.111, -.124, -.124, -.143, -.123, -.077]
    backs = [.080, .095, .061, .080, .088, .070]
    for v in vertices:
        x, y, z = v.co
        strength = np.clip((z-.83)/.08, 0, 1) * np.clip((1.42-z)/.055, 0, 1)
        width = np.interp(z, levels, widths)
        strength *= np.clip((width+.016-abs(x))/.023, 0, 1)
        front, back = np.interp(z, levels, fronts), np.interp(z, levels, backs)
        cy, depth = (front+back)*.5, (back-front)*.5
        twist = relief * 65 * math.sin((z-.82)*12)
        angles = [twist + math.tau*i/sides for i in range(sides)]
        # The support planes are tangent to a unit circle. Their intersections
        # can project beyond its sides, especially on a rotated hexagon.
        corner = math.pi / sides
        lateral_extent = max(math.sin(t + corner) for t in angles) / math.cos(corner)
        waist = np.interp(z, [.90, 1.005, 1.085, 1.16, 1.25], [0, .50, 1, .65, 0])
        half_width = (width - waist_inset * waist) / lateral_extent
        u, w = x / half_width, (y-cy) / depth
        support = max(u*math.sin(t)+w*math.cos(t) for t in angles)
        if support > .45:
            scale = 1 + strength * (1/support - 1)
            v.co.x = x * scale
            v.co.y = cy + (y-cy) * scale


def planarize(body, cfg):
    """Merge connected triangles by normal and span, then fit shared vertices
    to their incident patch planes. One vertex serves both sides of a crease.
    """
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()
    bm.normal_update()
    faces = list(bm.faces)
    count = len(faces)
    parent = list(range(count))
    adjacent = [set(n.index for e in f.edges for n in e.link_faces if n != f) for f in faces]
    areas = np.array([f.calc_area() for f in faces])
    normals = np.array([f.normal[:] for f in faces])
    centers = np.array([f.calc_center_median()[:] for f in faces])
    lo = np.array([np.min([v.co[:] for v in f.verts], axis=0) for f in faces])
    hi = np.array([np.max([v.co[:] for v in f.verts], axis=0) for f in faces])
    queue = []
    version = [0] * count
    limit = math.cos(math.radians(cfg['patch_angle']))
    def enqueue(i, j):
        alignment = np.dot(normals[i], normals[j])
        span = np.linalg.norm(np.maximum(hi[i], hi[j]) - np.minimum(lo[i], lo[j]))
        if alignment >= limit and span < 2 * cfg['patch_radius']:
            heapq.heappush(queue, (1 - alignment + .16 * span, i, j, version[i], version[j]))
    for i, neighbors in enumerate(adjacent):
        for j in neighbors:
            if i < j:
                enqueue(i, j)
    while queue:
        _, i, j, vi, vj = heapq.heappop(queue)
        if vi != version[i] or vj != version[j]:
            continue
        parent[j] = i
        version[i] += 1
        version[j] += 1
        ai, aj = areas[i], areas[j]
        normal = ai * normals[i] + aj * normals[j]
        normals[i] = normal / np.linalg.norm(normal)
        centers[i] = (ai * centers[i] + aj * centers[j]) / (ai + aj)
        areas[i] += aj
        lo[i], hi[i] = np.minimum(lo[i], lo[j]), np.maximum(hi[i], hi[j])
        adjacent[i] = (adjacent[i] | adjacent[j]) - {i, j}
        for k in adjacent[i]:
            adjacent[k].discard(j)
            adjacent[k].add(i)
            enqueue(i, k)
    def root(i):
        while parent[i] != i:
            i = parent[i]
        return i
    rng = np.random.default_rng(cfg['seed'])
    relief = cfg['relief_meters'] * rng.uniform(-1, 1, count)
    for v in bm.verts:
        p = np.array(v.co)
        if p[2] >= 1.50 or p[2] < .009:
            continue
        ids = sorted({root(f.index) for f in v.link_faces})
        n = normals[ids]
        residual = np.einsum('ij,ij->i', n, centers[ids] - p) + relief[ids]
        # Regularization preserves tangential spacing at nearly parallel seams.
        delta = np.linalg.solve(n.T @ n + .08 * np.eye(3), n.T @ residual)
        edge_length = min(e.calc_length() for e in v.link_edges)
        bound = min(.012, .24 * edge_length)
        v.co = p + delta * min(1, bound / max(np.linalg.norm(delta), 1e-12))
    bm.to_mesh(body.data)
    bm.free()


def refine_hip_bend(body, blend_height):
    """Route the front hip planes around the bend at their original density."""
    if not math.isfinite(blend_height) or blend_height <= 0:
        raise ValueError("hip_blend_meters must be finite and positive when enabled")

    def influence(p):
        radius = ((p.x / .17) ** 2 + ((p.z - .858) / .13) ** 2) ** .5
        return min(1, max(0, (1 - radius) * 3)) * min(1, max(0, (.03 - p.y) / .05))

    bm = bmesh.new()
    bm.from_mesh(body.data)
    # A diagonal joining left and right thigh vertices shears when just one
    # leg lifts. Turn it toward the midline if its opposite corners lie closer
    # to the sagittal plane. This keeps the two large panels on separate thighs.
    for edge in list(bm.edges):
        a, b = edge.verts
        if not edge.is_manifold or influence((a.co + b.co) * .5) < .6:
            continue
        if min(a.co.x, b.co.x) > -.02 or max(a.co.x, b.co.x) < .02:
            continue
        c, d = [next(v for v in f.verts if v not in edge.verts) for f in edge.link_faces]
        if abs(c.co.x) + abs(d.co.x) < abs(a.co.x) + abs(b.co.x):
            bmesh.utils.edge_rotate(edge, False)
    region = [(v, influence(v.co)) for v in bm.verts if influence(v.co) > 0]
    moved = []
    for v, amount in region:
        adjacent = [e.other_vert(v).co for e in v.link_edges]
        mean = sum(adjacent, Vector()) / len(adjacent)
        moved.append(v.co.lerp(mean, .32 * amount))
    for (v, _), point in zip(region, moved):
        v.co = point
    weights = bm.verts.layers.deform.active
    pelvis, left, right = [body.vertex_groups[n].index for n in ('pelvis','thigh_l','thigh_r')]
    for v, amount in region:
        x, y, z = v.co
        # Bring the central fold nearer the midline so the large planes on
        # either side have a comparable span in alternate knee lifts.
        centrality = max(0, min(1, (.072 - abs(x)) / .04))
        v.co.x *= 1 - .65 * amount * centrality
        x = v.co.x
        # Recede the prominent central ledge into the lower abdomen.
        center = math.exp(-(x / .075)**2 - ((z - .875) / .061)**2)
        v.co.y += .008 * amount * center
        # A broad transition through flexion distributes compression over the
        # upper thigh. The inner saddle shares rotation across the midline.
        leg = float(np.clip((.993 - z) / blend_height, 0, 1))
        side = float(np.clip(.5 + x / .105, 0, 1))
        side = side * side * (3 - 2 * side)
        current = v[weights]
        for group in list(current.keys()):
            current[group] *= 1 - amount
        for group, value in ((pelvis, 1-leg), (left, leg*side), (right, leg*(1-side))):
            current[group] = current.get(group, 0) + amount * value
    bm.to_mesh(body.data)
    bm.free()


def generate():
    bpy.ops.wm.open_mainfile(filepath=str(HERE / "Anatomical Base.blend"))
    base = bpy.data.objects["Anatomical Base"]
    original = base.parent
    configs = json.loads((HERE / "treatments.json").read_text())
    for index, (name, cfg) in enumerate(configs.items(), start=1):
        rig = original.copy()
        rig.data = original.data.copy()
        bpy.context.collection.objects.link(rig)
        rig.name = name + " Rig"
        body = base.copy()
        body.data = base.data.copy()
        bpy.context.collection.objects.link(body)
        body.name = name
        body.parent = rig
        body.modifiers[0].object = rig
        activate(body)
        reduce_body(body, cfg)
        if cfg['patch_angle']:
            planarize(body, cfg)
        if cfg.get('hip_blend_meters'):
            refine_hip_bend(body, cfg['hip_blend_meters'])
        for f in body.data.polygons:
            f.use_smooth = False
        normalize_skin(body, rig)
        bm = bmesh.new()
        bm.from_mesh(body.data)
        if any(not e.is_manifold for e in bm.edges) or any(f.calc_area() < 1e-12 for f in bm.faces):
            raise ValueError(f"{name} has open edges or degenerate faces.")
        bm.free()
        export(body, rig)
        print(name, len(body.data.polygons), "faces", len(body.data.vertices), "vertices")
        rig.location.x = index * 1.45
    activate(base)
    for obj in bpy.context.scene.objects:
        if obj.type == 'ARMATURE':
            obj.hide_set(True)
    save(HERE / "Humanoid Studies.blend")


if __name__ == "__main__":
    generate()
