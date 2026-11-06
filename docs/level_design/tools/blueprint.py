#!/usr/bin/env python3
"""
Topdown blueprint for the level-design assignment: "Patio de Subestacao" (22_Map_Substation).

Single source of truth for the level's blockmesh. Emits:
  layout.json      - geometry, routes, POIs (the file the Unity/ProBuilder builder will read)
  metrics.json     - measured route lengths, loop perimeters, exposure indices
  blueprint.svg    - plan view (zones, props, spawn, POIs, breadcrumbs)
  circulation.svg  - same plan, dimmed, with the three loops + the crossing axes

Coordinates: metres, X east, Z north, origin at the centre of the yard.
Run:  python3 blueprint.py            (writes all four)
"""

import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.dirname(HERE)          # docs/level_design

WALK = 6.0
SPRINT = WALK * 1.6                  # PlayerMotor: walkSpeed 6, sprintMultiplier 1.6
JUMP = 1.4
DECK_Y = 4.0
CHOKE_W = 2.5
GATE_W = 5.0


# --------------------------------------------------------------------------- geometry
def rect(x0, z0, x1, z1, h, name, kind):
    return {"name": name, "x0": min(x0, x1), "z0": min(z0, z1),
            "x1": max(x0, x1), "z1": max(z0, z1), "h": h, "kind": kind}


def build_layout():
    solids, decks, connectors, pads = [], [], [], []

    # perimeter: 1 m thick, 5 m tall -> nothing is visible or reachable from outside
    solids += [
        rect(-22, -22, 22, -21, 5, "Wall_S", "wall"),
        rect(-22, 21, 22, 22, 5, "Wall_N", "wall"),
        rect(-22, -21, -21, 21, 5, "Wall_W", "wall"),
        rect(21, -21, 22, 21, 5, "Wall_E", "wall"),
    ]

    # gallery inner wall: 1 m thick, 3.0 m tall, four openings
    #   N/S = 5 m gates, E/W = 2.5 m chokepoints
    gx, gz = GATE_W / 2, CHOKE_W / 2
    solids += [
        rect(-15, -15, -gx, -14, 3.0, "RingWall_S1", "ring"),
        rect(gx, -15, 15, -14, 3.0, "RingWall_S2", "ring"),
        rect(-15, 14, -gx, 15, 3.0, "RingWall_N1", "ring"),
        rect(gx, 14, 15, 15, 3.0, "RingWall_N2", "ring"),
        rect(-15, -14, -14, -gz, 3.0, "RingWall_W1", "ring"),
        rect(-15, gz, -14, 14, 3.0, "RingWall_W2", "ring"),
        rect(14, -14, 15, -gz, 3.0, "RingWall_E1", "ring"),
        rect(14, gz, 15, 14, 3.0, "RingWall_E2", "ring"),
    ]

    # the inner ring's centre of mass: one transformer bank inside a blast wall
    solids.append(rect(-3, -3, 3, 3, 3, "TransformerBank", "mass"))

    # two containment alcoves in the yard lanes (single exit, cover from the catwalk)
    solids += [
        rect(-10, 11, -9, 14, 3.0, "Alcove_N_W", "alcove"),
        rect(-8, 11, -7, 14, 3.0, "Alcove_N_E", "alcove"),
        rect(7, -14, 8, -11, 3.0, "Alcove_S_W", "alcove"),
        rect(9, -14, 10, -11, 3.0, "Alcove_S_E", "alcove"),
    ]

    # circuit-breaker cabinets: the yard's only chest-high cover
    # (placed clear of the ramp, the stair and the two alcoves; two of them sit under the catwalk)
    for i, (cx, cz, w, d) in enumerate([
            (-5.5, 11.5, 2.0, 1.2), (5.5, 11.5, 2.0, 1.2),
            (5.5, -11.5, 2.0, 1.2), (-12.0, -11.5, 2.0, 1.2),
            (12.0, -6.0, 1.2, 2.0), (-12.0, 6.0, 1.2, 2.0)]):
        solids.append(rect(cx - w / 2, cz - d / 2, cx + w / 2, cz + d / 2, 1.8, "Breaker_%d" % i, "cover"))

    # busbar masts on the ring wall's corners: landmarks, visible from every loop
    for sx in (-1, 1):
        for sz in (-1, 1):
            solids.append(rect(14.5 * sx - 0.6, 14.5 * sz - 0.6, 14.5 * sx + 0.6,
                               14.5 * sz + 0.6, 8.0, "Mast_%s%s" % ("N" if sz > 0 else "S",
                                                                     "E" if sx > 0 else "W"), "mast"))

    # the corner mast: breaks the elevated loop. 8.5 m tall, only Scissors crosses it.
    solids.append(rect(-12.5, 9.5, -9.5, 12.5, 8.5, "CornerMast", "mast"))

    # concrete pad, flush with the yard: visual only, no step for the character controller
    pads.append(rect(-7, -7, 7, 7, 0.0, "Pad", "pad"))

    # elevated catwalk, 3 m wide, 0.3 m slab at 4.0 m
    decks += [
        rect(-9.5, 9.5, 12.5, 12.5, DECK_Y, "Deck_N", "deck"),
        rect(-12.5, -12.5, -9.5, 9.5, DECK_Y, "Deck_W", "deck"),
    ]

    # north access: a ramp, 4.0 m over 8 m (26.6 deg, under the 45 deg slope limit)
    connectors.append({"name": "Ramp_N", "kind": "ramp", "x0": 9.5, "z0": 1.5, "x1": 12.5, "z1": 9.5,
                       "low": [10.0, 1.5], "high": [10.0, 9.5], "rise": DECK_Y})
    # west access: a stair, 16 x 0.25 m risers (0.3 m step offset), 0.4 m treads
    connectors.append({"name": "Stair_W", "kind": "stair", "x0": -9.5, "z0": -12.5, "x1": -3.1, "z1": -9.5,
                       "low": [-3.1, -11.0], "high": [-9.5, -11.0], "rise": DECK_Y,
                       "steps": 16, "riser": 0.25, "tread": 0.4})

    return {"name": "Patio de Subestacao", "scene": "22_Map_Substation", "unit": "m",
            "footprint": {"w": 44, "d": 44},
            "levels": {"yard": 0.0, "deck": DECK_Y, "bank": 3.0, "mast": 8.5},
            "solids": solids, "pads": pads, "decks": decks, "connectors": connectors}


# --------------------------------------------------------------------------- routes
def build_routes():
    """Waypoints avoid the solids; every leg is checked in check_routes()."""
    R = {}

    # critical path: spawn -> S gate -> pad edge -> the inner ring
    R["CP1_principal"] = {"kind": "critical", "label": "Critical path",
                          "pts": [[0, -18], [0, -14.5], [0, -9], [0, -7], [0, -4.5]]}

    # the three loops, drawn as their centre lines
    def square(half):
        return [[-half, -half], [half, -half], [half, half], [-half, half], [-half, -half]]
    R["L1_inner"] = {"kind": "loop", "label": "Anel interno (tanque)", "pts": square(5.0), "closed": True}
    R["L2_middle"] = {"kind": "loop", "label": "Anel medio (patio)", "pts": square(9.0), "closed": True}
    R["L3_outer"] = {"kind": "loop", "label": "Anel externo (galeria)", "pts": square(18.0), "closed": True}

    # secondary: the gallery detour between the two gates (the long, covered way)
    R["S1_gallery_detour"] = {"kind": "secondary", "label": "Desvio pela galeria",
                              "pts": [[0, -14.5], [0, -18], [-18, -18], [-18, 0], [-14.5, 0],
                                      [-18, 0], [-18, 18], [0, 18], [0, 14.5]]}

    # vertical loop: north arm -> corner mast -> west arm
    R["V1_elevated"] = {"kind": "elevated", "label": "Loop elevado (Scissors)",
                        "pts": [[10.0, 11.0], [-9.5, 11.0], [-11.0, 11.0], [-11.0, 9.5], [-11.0, -11.0]]}

    # the two access connectors, on the ground
    R["A1_ramp"] = {"kind": "access", "label": "Acesso rampa", "pts": [[10.0, 1.5], [10.0, 9.5]]}
    R["A2_stair"] = {"kind": "access", "label": "Acesso escada", "pts": [[-3.1, -11.0], [-9.5, -11.0]]}

    # breadcrumbs: painted floor markings from the spawn to the pad
    R["B1_breadcrumbs"] = {"kind": "breadcrumb", "label": "Breadcrumbs",
                           "pts": [[0, -20.5], [0, -16], [0, -14.5], [0, -11], [0, -7.5]]}
    return R


# --------------------------------------------------------------------------- metrics
def seg_hits_rect(p, q, r, pad=0.0):
    """Does segment p-q intersect the axis-aligned rect r (expanded by pad)?"""
    x0, z0, x1, z1 = r["x0"] - pad, r["z0"] - pad, r["x1"] + pad, r["z1"] + pad
    if x0 <= p[0] <= x1 and z0 <= p[1] <= z1:
        return True
    if x0 <= q[0] <= x1 and z0 <= q[1] <= z1:
        return True
    dx, dz = q[0] - p[0], q[1] - p[1]
    t0, t1 = 0.0, 1.0
    for d, lo, hi, o in ((dx, x0, x1, p[0]), (dz, z0, z1, p[1])):
        if abs(d) < 1e-9:
            if o < lo or o > hi:
                return False
            continue
        a, b = (lo - o) / d, (hi - o) / d
        if a > b:
            a, b = b, a
        t0, t1 = max(t0, a), min(t1, b)
        if t0 > t1:
            return False
    return True


def entry_t(p, q, r):
    """Parameter t in [0,1] where segment p->q first enters rect r, or None."""
    x0, z0, x1, z1 = r["x0"], r["z0"], r["x1"], r["z1"]
    dx, dz = q[0] - p[0], q[1] - p[1]
    t0, t1 = 0.0, 1.0
    for d, lo, hi, o in ((dx, x0, x1, p[0]), (dz, z0, z1, p[1])):
        if abs(d) < 1e-9:
            if o < lo or o > hi:
                return None
            continue
        a, b = (lo - o) / d, (hi - o) / d
        if a > b:
            a, b = b, a
        t0, t1 = max(t0, a), min(t1, b)
        if t0 > t1:
            return None
    return t0


def blocked(p, q, solids, pad=0.0):
    """2.5D visibility: a rect blocks only where the ray is still below its height.
    p and q are (x, z, y). A 4 m catwalk slab therefore never blocks a ground sightline,
    while a 3 m wall does."""
    for s in solids:
        r = s if not pad else {"x0": s["x0"] - pad, "z0": s["z0"] - pad,
                               "x1": s["x1"] + pad, "z1": s["z1"] + pad}
        t = entry_t(p, q, r)
        if t is None:
            continue
        if p[2] + (q[2] - p[2]) * t < s["h"]:
            return True
    return False


def polyline_len(pts, closed=False):
    total = 0.0
    for i in range(len(pts) - 1):
        total += math.dist(pts[i], pts[i + 1])
    if closed:
        total += math.dist(pts[-1], pts[0])
    return total


def sample(pts, step=0.5, closed=False):
    out = []
    seq = pts + [pts[0]] if closed else pts
    for i in range(len(seq) - 1):
        a, b = seq[i], seq[i + 1]
        n = max(1, int(math.dist(a, b) / step))
        for k in range(n):
            t = k / n
            out.append([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t])
    return out


EYE = 1.6          # eye height used for every ground sample and probe

# probe points: what the level can be seen from, with the height of the observer
PROBES = [
    (0.0, -14.5, EYE), (0.0, 14.5, EYE), (-14.5, 0.0, EYE), (14.5, 0.0, EYE),   # gates + chokepoints
    (0.0, -18.0, EYE), (0.0, 18.0, EYE), (-18.0, 0.0, EYE), (18.0, 0.0, EYE),   # gallery mid-sides
    (10.0, 11.0, DECK_Y + EYE), (-11.0, -11.0, DECK_Y + EYE),                   # catwalk ends
    (-11.0, 11.0, 8.5 + EYE),                                                   # corner mast top
    (0.0, 0.0, 3.0),                                                            # bank roof
    (0.0, 10.5, EYE), (0.0, -10.5, EYE), (10.5, 0.0, EYE), (-10.5, 0.0, EYE),   # mid-lane points
]

CROSSFIRE_N = 4    # "under crossfire" = seen from this many probes at once


def metrics(layout, routes):
    solids = layout["solids"]
    out = {"probe_count": len(PROBES), "eye_height": EYE, "routes": {}, "loops": {}}

    def exposure(pts, y, closed):
        """(mean share of probes that see the route, share of the route seen from >= CROSSFIRE_N)"""
        samples = sample(pts, 0.5, closed)
        total, cross = 0.0, 0
        for s in samples:
            p = (s[0], s[1], y)
            seen = sum(0 if blocked(p, q, solids) else 1 for q in PROBES)
            total += seen / len(PROBES)
            cross += 1 if seen >= CROSSFIRE_N else 0
        return total / max(1, len(samples)), cross / max(1, len(samples))

    for name, r in routes.items():
        pts = r["pts"]
        length = polyline_len(pts, r.get("closed", False))
        y = DECK_Y + EYE if r["kind"] == "elevated" else EYE
        expo, cross = exposure(pts, y, r.get("closed", False))
        out["routes"][name] = {
            "label": r["label"], "kind": r["kind"], "length_m": round(length, 1),
            "exposure": round(expo, 3), "crossfire": round(cross, 3),
            "walk_s": round(length / WALK, 1), "sprint_s": round(length / SPRINT, 1),
        }
        if r["kind"] == "loop":
            out["loops"][name] = {"perimeter_m": round(length, 1),
                                  "walk_s": round(length / WALK, 1),
                                  "sprint_s": round(length / SPRINT, 1),
                                  "exposure": round(expo, 3), "crossfire": round(cross, 3)}

    # redundancy: N gate -> S gate, the short exposed way vs the long covered way
    short = [[0, 14.5], [0, 3.5], [4.0, 3.5], [4.0, -3.5], [0, -3.5], [0, -14.5]]
    long_ = [[0, 14.5], [0, 18], [18, 18], [18, 0], [14.5, 0], [9.0, 0],
             [9.0, -11.0], [0, -11.0], [0, -14.5]]
    for label, pts in (("gate_to_gate_short", short), ("gate_to_gate_long", long_)):
        e, c = exposure(pts, EYE, False)
        out["routes"][label] = {"length_m": round(polyline_len(pts), 1),
                                "exposure": round(e, 3), "crossfire": round(c, 3),
                                "walk_s": round(polyline_len(pts) / WALK, 1),
                                "sprint_s": round(polyline_len(pts) / SPRINT, 1)}
    out["redundancy_ratio"] = round(out["routes"]["gate_to_gate_long"]["length_m"] /
                                    out["routes"]["gate_to_gate_short"]["length_m"], 2)
    return out


def check_routes(layout, routes):
    """Every ground route leg must be walkable: no leg crosses a solid >= 1.8 m."""
    problems = []
    for name, r in routes.items():
        if r["kind"] in ("elevated",):
            continue
        pts = r["pts"] + ([r["pts"][0]] if r.get("closed") else [])
        for a, b in zip(pts, pts[1:]):
            for s in layout["solids"]:
                if s["h"] >= 1.8 and seg_hits_rect(a, b, s, pad=-0.05):
                    problems.append("%s: leg %s->%s crosses %s" % (name, a, b, s["name"]))
    return problems


# --------------------------------------------------------------------------- svg
S = 26.0          # px per metre
M = 96.0          # margin
W_PX = 44 * S + 2 * M
BAND_Y = M + 44 * S + 22      # the sheet band starts below the plan
BAND_H = 200
H_PX = BAND_Y + BAND_H + 12

COL = {
    "bg": "#ffffff",
    "grid": "#f0f2f4", "grid_major": "#e2e6ea",
    "wall": "#2b3036", "wall_edge": "#12161a",          # perimeter, 5 m
    "ring": "#959da4", "ring_edge": "#5a6167",          # gallery wall, 3 m
    "mass": "#b7bdc3", "mass_edge": "#4b5257",          # transformer bank, 3 m
    "cover": "#d5dade", "alcove": "#c6ccd1",            # 1.8 m / 3 m props
    "deck": "#ffffff", "deck_edge": "#2b3036",          # 4 m catwalk
    "ramp": "#e8ebee", "ramp_edge": "#5a6167",
    "pad": "#fbfbfc", "pad_edge": "#a7aeb4",
    "text": "#12161a", "dim": "#525a61",
    "critical": "#c62828", "elevated": "#6a1b9a", "access": "#00695c",
    "inner": "#e08600", "middle": "#2e7d32", "outer": "#1565c0",
    "breadcrumb": "#8e959c", "spawn": "#1b5e20", "enemy": "#22262a", "poi": "#12161a",
}


def px(x, z):
    return (x + 22) * S + M, (22 - z) * S + M


def svg_rect(r, fill, stroke, op=1.0, dash=None, sw=1.0):
    x, y = px(r["x0"], r["z1"])
    w, h = (r["x1"] - r["x0"]) * S, (r["z1"] - r["z0"]) * S
    d = ' stroke-dasharray="%s"' % dash if dash else ""
    return ('<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" fill="%s" stroke="%s" '
            'stroke-width="%.1f" opacity="%.2f"%s/>' % (x, y, w, h, fill, stroke, sw, op, d))


def svg_line(pts, stroke, sw=1.4, dash=None, op=1.0, cap="round"):
    d = " ".join("%.1f,%.1f" % px(*p) for p in pts)
    da = ' stroke-dasharray="%s"' % dash if dash else ""
    return ('<polyline points="%s" fill="none" stroke="%s" stroke-width="%.1f" '
            'stroke-linecap="%s" stroke-linejoin="round" opacity="%.2f"%s/>' % (d, stroke, sw, cap, op, da))


def svg_text(x, z, text, size=12, fill=None, anchor="start", weight="normal", dx=0, dy=0, op=1.0):
    tx, ty = px(x, z)
    return ('<text x="%.1f" y="%.1f" font-family="DejaVu Sans Mono, monospace" font-size="%.1f" '
            'fill="%s" text-anchor="%s" font-weight="%s" opacity="%.2f" '
            'stroke="#ffffff" stroke-width="3.6" paint-order="stroke" '
            'stroke-linejoin="round">%s</text>'
            % (tx + dx, ty + dy, size, fill or COL["text"], anchor, weight, op,
               text.replace("&", "&amp;").replace("<", "&lt;")))


def txt_px(x, y, text, size=12, fill=None, weight="normal", anchor="start"):
    return ('<text x="%.1f" y="%.1f" font-family="DejaVu Sans Mono, monospace" font-size="%.1f" '
            'fill="%s" text-anchor="%s" font-weight="%s">%s</text>'
            % (x, y, size, fill or COL["text"], anchor, weight,
               text.replace("&", "&amp;").replace("<", "&lt;")))


def line_px(x1, y1, x2, y2, color, sw=1.0, dash=None):
    d = ' stroke-dasharray="%s"' % dash if dash else ""
    return ('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="%s" stroke-width="%.1f"%s/>'
            % (x1, y1, x2, y2, color, sw, d))


def arrow(p, q, color, sw=3.0, op=1.0, head=9.0):
    x1, y1 = px(*p)
    x2, y2 = px(*q)
    ang = math.atan2(y2 - y1, x2 - x1)
    hx, hy = x2 - head * math.cos(ang), y2 - head * math.sin(ang)
    left = (hx - head * 0.55 * math.sin(ang), hy + head * 0.55 * math.cos(ang))
    right = (hx + head * 0.55 * math.sin(ang), hy - head * 0.55 * math.cos(ang))
    return ('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="%s" stroke-width="%.1f" '
            'opacity="%.2f" stroke-linecap="round"/>'
            '<polygon points="%.1f,%.1f %.1f,%.1f %.1f,%.1f" fill="%s" opacity="%.2f"/>'
            % (x1, y1, hx, hy, color, sw, op, x2, y2, left[0], left[1], right[0], right[1], color, op))


def frame(title, subtitle):
    out = ['<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d" viewBox="0 0 %d %d">'
           % (W_PX, H_PX, W_PX, H_PX),
           '<rect x="0" y="0" width="%d" height="%d" fill="%s"/>' % (W_PX, H_PX, COL["bg"]),
           '<defs><pattern id="hatch" width="11" height="11" patternUnits="userSpaceOnUse" '
           'patternTransform="rotate(45)"><rect width="11" height="11" fill="#ffffff"/>'
           '<line x1="0" y1="0" x2="0" y2="11" stroke="#9aa1a7" stroke-width="1.5"/>'
           '</pattern>'
           '<pattern id="mhatch" width="7" height="7" patternUnits="userSpaceOnUse" '
           'patternTransform="rotate(45)"><rect width="7" height="7" fill="#7d858c"/>'
           '<line x1="0" y1="0" x2="0" y2="7" stroke="#4b5257" stroke-width="1.2"/>'
           '</pattern></defs>']
    # grid: 2 m minor, 10 m major
    for g in range(-22, 23, 2):
        major = g % 10 == 0
        c = COL["grid_major"] if major else COL["grid"]
        x0, y0 = px(g, -22)
        x1, y1 = px(g, 22)
        out.append(line_px(x0, y0, x1, y1, c, 1.2 if major else 0.8))
        x0, y0 = px(-22, g)
        x1, y1 = px(22, g)
        out.append(line_px(x0, y0, x1, y1, c, 1.2 if major else 0.8))
    out.append(txt_px(M, 44, title, 23, COL["text"], "bold"))
    out.append(txt_px(M, 68, subtitle, 13.5, COL["dim"]))
    return out


def plan_geometry(layout, op=1.0):
    out = []
    for p in layout["pads"]:
        out.append(svg_rect(p, COL["pad"], COL["pad_edge"], op, dash="9 6", sw=1.6))
    for c in layout["connectors"]:
        out.append(svg_rect(c, COL["ramp"], COL["ramp_edge"], op, sw=1.6))
        lo, hi = c["low"], c["high"]
        mid = [(lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2]
        q = [lo[0] + (hi[0] - lo[0]) * 0.92, lo[1] + (hi[1] - lo[1]) * 0.92]
        out.append(arrow(mid, q, COL["access"], 2.2, op * 0.9, 9))
    for d in layout["decks"]:
        out.append(svg_rect(d, "url(#hatch)", COL["deck_edge"], min(1.0, op), sw=2.0))
    style = {"wall": (COL["wall"], COL["wall_edge"], 1.6),
             "ring": (COL["ring"], COL["ring_edge"], 1.4),
             "mass": (COL["mass"], COL["mass_edge"], 1.6),
             "cover": (COL["cover"], COL["ring_edge"], 1.4),
             "alcove": (COL["alcove"], COL["ring_edge"], 1.4),
             "mast": ("url(#mhatch)", "#3f454a", 1.8)}
    for s in layout["solids"]:
        f, st, sw = style[s["kind"]]
        out.append(svg_rect(s, f, st, op, sw=sw))
    return out


def legend_px(items, x, y, spacing=26.0, size=12.5):
    out = []
    for i, (color, dash, label) in enumerate(items):
        yy = y + i * spacing
        out.append(line_px(x, yy, x + 42, yy, color, 3.4, dash))
        out.append(txt_px(x + 54, yy + 4.4, label, size, COL["text"]))
    return out


def marker(x, z, text, filled=True, r=13.0, size=12.5):
    cx, cy = px(x, z)
    if filled:
        return ('<circle cx="%.1f" cy="%.1f" r="%.1f" fill="%s" stroke="#ffffff" stroke-width="1.6"/>'
                % (cx, cy, r, COL["enemy"])) + \
               txt_px(cx, cy + 4.6, text, size, "#ffffff", "bold", "middle")
    return ('<circle cx="%.1f" cy="%.1f" r="%.1f" fill="#ffffff" stroke="%s" stroke-width="2.2"/>'
            % (cx, cy, r, COL["poi"])) + txt_px(cx, cy + 4.6, text, size, COL["poi"], "bold", "middle")


def draw_plan(layout, routes, met):
    out = frame("PATIO DE SUBESTACAO  -  blockmesh",
                "Hero Shooter  |  44 x 44 m  |  1 quadrado de grade = 2 m  |  ProBuilder")
    out += plan_geometry(layout)

    # openings: arrows point inward, width written at the opening
    out.append(arrow([0, -17.6], [0, -15.4], COL["critical"], 3.0))
    out.append(arrow([0, 17.6], [0, 15.4], COL["critical"], 3.0))
    out.append(arrow([-17.6, 0], [-15.4, 0], COL["critical"], 3.0))
    out.append(arrow([17.6, 0], [15.4, 0], COL["critical"], 3.0))
    out.append(svg_text(0, 16.4, "PORTAO N  5 m", 12.5, COL["critical"], anchor="middle"))
    out.append(svg_text(0, -16.4, "PORTAO S  5 m", 12.5, COL["critical"], anchor="middle"))
    out.append(svg_text(11.2, -2.0, "GARGALO 2.5 m", 12, COL["critical"], anchor="middle"))
    out.append(svg_text(-11.2, -2.0, "GARGALO 2.5 m", 12, COL["critical"], anchor="middle"))

    # breadcrumbs + spawn
    out.append(svg_line(routes["B1_breadcrumbs"]["pts"], COL["breadcrumb"], 3.0, "5 7", 0.9))
    sx, sy = px(0, -18)
    out.append('<circle cx="%.1f" cy="%.1f" r="13" fill="#ffffff" stroke="%s" stroke-width="2.6"/>' % (sx, sy, COL["spawn"]))
    out.append('<circle cx="%.1f" cy="%.1f" r="5" fill="%s"/>' % (sx, sy, COL["spawn"]))
    out.append(svg_text(0, -19.9, "SPAWN", 12, COL["spawn"], anchor="middle", weight="bold"))

    # enemies as authored
    for (ex, ez, kind) in [(10.0, 11.0, "R"), (-11.0, -8.0, "R"), (-10.5, 0.0, "M"), (10.5, -2.0, "M")]:
        out.append(marker(ex, ez, kind))

    # POIs
    for (x, z, n) in [(-11.0, 11.6, "1"), (10.0, 11.0, "2"), (2.1, 2.1, "3"),
                      (-8.5, 12.6, "4"), (8.5, -12.6, "5")]:
        out.append(marker(x, z, n, filled=False))

    # zone labels
    out.append(svg_text(0, 18.0, "GALERIA DE CABOS  6 m", 13, COL["dim"], anchor="middle"))
    out.append(svg_text(-18.0, 7.0, "GALERIA", 13, COL["dim"], anchor="middle"))
    out.append(svg_text(18.0, -7.0, "GALERIA", 13, COL["dim"], anchor="middle"))
    out.append(svg_text(5.6, 8.3, "PATIO 7 m", 12.5, COL["dim"], anchor="middle"))
    out.append(svg_text(-9.2, -4.3, "PATIO 7 m", 12.5, COL["dim"], anchor="middle"))
    out.append(svg_text(0, 1.1, "TRANSFORMADOR", 12, COL["mass_edge"], anchor="middle", weight="bold"))
    out.append(svg_text(0, -1.3, "6 x 6 x 3 m", 12, COL["mass_edge"], anchor="middle"))
    out.append(svg_text(11.0, 6.1, "RAMPA", 11.5, COL["access"], anchor="middle"))
    out.append(svg_text(11.0, 4.4, "4 m", 11.5, COL["access"], anchor="middle"))
    out.append(svg_text(-6.3, -10.4, "ESCADA", 11.5, COL["access"], anchor="middle"))
    out.append(svg_text(-6.3, -12.1, "4 m", 11.5, COL["access"], anchor="middle"))
    out.append(svg_text(-3.0, 11.0, "DECK 4 m", 12, COL["deck_edge"], anchor="middle", weight="bold"))
    out.append(svg_text(-11.0, -5.4, "DECK", 12, COL["deck_edge"], anchor="middle", weight="bold"))
    out.append(svg_text(-11.0, -7.0, "4 m", 12, COL["deck_edge"], anchor="middle", weight="bold"))
    out.append(svg_text(-11.0, 10.0, "8.5 m", 11.5, "#ffffff", anchor="middle"))

    # ---- band below the plan
    by = BAND_Y
    out.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%d" fill="#f7f8f9" stroke="%s" stroke-width="1.4"/>'
               % (M - 8, by, 44 * S + 16, BAND_H, COL["grid_major"]))
    out.append(txt_px(M, by + 30, "PATIO DE SUBESTACAO   |   22_Map_Substation   |   CIRCULATION", 15, COL["text"], "bold"))
    out.append(txt_px(M, by + 60,
                      "aneis  40 / 72 / 144 m      vao curto 37 m (54% exposto)  vs  vao coberto 72 m (40%)  =  x1.95",
                      13, COL["text"]))
    out.append(txt_px(M, by + 86,
                      "POIs  1 pilar de canto   2 deck NE   3 topo do banco   4/5 nichos      R = ranged   M = melee",
                      13, COL["dim"]))
    sy = by + 128
    out.append(line_px(M, sy, M + 10 * S, sy, COL["text"], 2.4))
    for k in range(0, 11, 2):
        out.append(line_px(M + k * S, sy - 7, M + k * S, sy + 7, COL["text"], 1.6))
    out.append(txt_px(M + 10 * S + 14, sy + 5, "10 m", 12.5, COL["dim"]))
    out.append(txt_px(M, sy + 26, "1 quadrado de grade = 2 m", 11.5, COL["dim"]))
    nx = M + 44 * S - 30
    out.append('<polygon points="%.1f,%.1f %.1f,%.1f %.1f,%.1f" fill="%s"/>'
               % (nx, sy - 34, nx - 10, sy - 2, nx + 10, sy - 2, COL["text"]))
    out.append(txt_px(nx, sy + 20, "N", 14, COL["text"], "bold", "middle"))
    out.append("</svg>")
    return "\n".join(out)


def draw_circulation(layout, routes, met):
    out = frame("PATIO DE SUBESTACAO  -  CIRCULATION",
                "tres aneis aninhados  |  dois gargalos  |  um loop elevado quebrado por um pilar")
    out += plan_geometry(layout, op=0.30)

    loop_col = {"L1_inner": COL["inner"], "L2_middle": COL["middle"], "L3_outer": COL["outer"]}
    for name in ("L1_inner", "L2_middle", "L3_outer"):
        r = routes[name]
        out.append(svg_line(r["pts"], loop_col[name], 3.6, None, 0.95))
        pts = r["pts"]
        for i in range(4):
            a, b = pts[i], pts[i + 1]
            mid = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2]
            nxt = [(a[0] * 0.32 + b[0] * 0.68), (a[1] * 0.32 + b[1] * 0.68)]
            out.append(arrow(mid, nxt, loop_col[name], 2.6, 0.95, 10))
        lab = r["label"] + "  %.0f m" % met["loops"][name]["perimeter_m"]
        out.append(svg_text(pts[0][0], pts[0][1], lab, 12.5, loop_col[name],
                            anchor="middle", weight="bold", dy=-13))

    # crossing axes = the critical path
    for a, b in (((0, 13.6), (0, 3.0)), ((0, -13.6), (0, -3.0)),
                 ((13.6, 0), (3.0, 0)), ((-13.6, 0), (-3.0, 0))):
        out.append(arrow(a, b, COL["critical"], 3.2, 0.95))
    out.append(svg_text(0, 16.2, "PORTAO 5 m", 12, COL["critical"], anchor="middle"))
    out.append(svg_text(0, -16.2, "PORTAO 5 m", 12, COL["critical"], anchor="middle"))
    out.append(svg_text(11.6, -1.9, "GARGALO 2.5 m", 11.5, COL["critical"], anchor="middle"))
    out.append(svg_text(-11.6, -1.9, "GARGALO 2.5 m", 11.5, COL["critical"], anchor="middle"))

    # elevated loop: two arms, broken at the pillar
    out.append(svg_line([[12.5, 11.0], [-9.5, 11.0]], COL["elevated"], 3.6, None, 1.0))
    out.append(svg_line([[-11.0, 9.5], [-11.0, -12.5]], COL["elevated"], 3.6, None, 1.0))
    out.append(arrow([4.0, 11.0], [-5.0, 11.0], COL["elevated"], 2.8, 1.0, 10))
    out.append(arrow([-11.0, 2.0], [-11.0, -7.0], COL["elevated"], 2.8, 1.0, 10))
    cmx, cmy = px(-11.0, 11.0)
    out.append('<circle cx="%.1f" cy="%.1f" r="17" fill="none" stroke="%s" stroke-width="2.6" '
               'stroke-dasharray="5 4"/>' % (cmx, cmy, COL["elevated"]))
    out.append(svg_text(-11.0, 6.4, "PILAR 8.5 m", 12.5, COL["elevated"], anchor="middle", weight="bold"))
    out.append(svg_text(-11.0, 5.4, "o loop quebra aqui", 11.5, COL["elevated"], anchor="middle"))
    out.append(svg_text(-11.0, 4.4, "so Scissors fecha", 11.5, COL["elevated"], anchor="middle"))

    out.append(svg_text(0, -20.6, "37 m exposto (curto)", 11.5, COL["dim"], anchor="middle"))

    # ---- band: legend (left) + measured table (right) + scale
    by = BAND_Y
    out.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%d" fill="#f7f8f9" stroke="%s" stroke-width="1.4"/>'
               % (M - 8, by, 44 * S + 16, BAND_H, COL["grid_major"]))
    out += legend_px([(COL["critical"], None, "eixos de travessia (portoes e gargalos)"),
                      (COL["inner"], None, "anel interno 40 m - sem cobertura"),
                      (COL["middle"], None, "anel medio 72 m - cabines de cobertura"),
                      (COL["outer"], None, "anel externo 144 m - o unico abrigado"),
                      (COL["elevated"], None, "loop elevado 4 m - quebrado pelo pilar"),
                      (COL["access"], None, "acessos: rampa (N) e escada (W)")],
                     M, by + 34, 26.0, 12.5)
    tx = M + 25 * S
    out.append(txt_px(tx, by + 34, "MEDIDO   (2.5D, 16 pontos de visada, olho 1.6 m)", 13, COL["text"], "bold"))
    out.append(txt_px(tx, by + 62, "%-9s %7s %7s %8s %7s %10s"
                      % ("anel", "perim", "walk", "sprint", "expo", "crossfire"), 12, COL["dim"]))
    rows = [("interno", met["loops"]["L1_inner"]), ("medio", met["loops"]["L2_middle"]),
            ("externo", met["loops"]["L3_outer"])]
    for i, (lab, m) in enumerate(rows):
        out.append(txt_px(tx, by + 86 + i * 22,
                          "%-9s %5.0f m %5.1f s %6.1f s %6.0f%% %8.0f%%"
                          % (lab, m["perimeter_m"], m["walk_s"], m["sprint_s"],
                             m["exposure"] * 100, m["crossfire"] * 100), 12, COL["text"]))
    out.append(txt_px(tx, by + 86 + 3 * 22, "vao curto    %3.0f m   expo %2.0f%%" % (
        met["routes"]["gate_to_gate_short"]["length_m"],
        met["routes"]["gate_to_gate_short"]["exposure"] * 100), 12, COL["text"]))
    out.append(txt_px(tx, by + 86 + 4 * 22, "vao coberto  %3.0f m   expo %2.0f%%   (x%.2f)" % (
        met["routes"]["gate_to_gate_long"]["length_m"],
        met["routes"]["gate_to_gate_long"]["exposure"] * 100,
        met["redundancy_ratio"]), 12, COL["text"]))
    sy2 = by + 176
    out.append(line_px(M, sy2, M + 10 * S, sy2, COL["text"], 2.4))
    for k in range(0, 11, 2):
        out.append(line_px(M + k * S, sy2 - 7, M + k * S, sy2 + 7, COL["text"], 1.6))
    out.append(txt_px(M + 10 * S + 14, sy2 + 5, "10 m", 12.5, COL["dim"]))
    nx = M + 44 * S - 30
    out.append('<polygon points="%.1f,%.1f %.1f,%.1f %.1f,%.1f" fill="%s"/>'
               % (nx, sy2 - 32, nx - 10, sy2, nx + 10, sy2, COL["text"]))
    out.append(txt_px(nx, sy2 + 22, "N", 14, COL["text"], "bold", "middle"))
    out.append("</svg>")
    return "\n".join(out)


# --------------------------------------------------------------------------- ascii
def ascii_plan(layout):
    n = 44
    g = [["." for _ in range(n)] for _ in range(n)]
    for p in layout["pads"]:
        for z in range(int(p["z0"] + 22), int(p["z1"] + 22)):
            for x in range(int(p["x0"] + 22), int(p["x1"] + 22)):
                g[z][x] = " "
    glyph = {"wall": "#", "ring": "=", "mass": "B", "cover": "c", "alcove": "a", "mast": "M"}
    for s in layout["solids"]:
        ch = glyph[s["kind"]]
        for z in range(int(math.floor(s["z0"] + 22)), int(math.ceil(s["z1"] + 22))):
            for x in range(int(math.floor(s["x0"] + 22)), int(math.ceil(s["x1"] + 22))):
                if 0 <= z < n and 0 <= x < n:
                    g[z][x] = ch
    for d in layout["decks"]:
        for z in range(int(math.floor(d["z0"] + 22)), int(math.ceil(d["z1"] + 22))):
            for x in range(int(math.floor(d["x0"] + 22)), int(math.ceil(d["x1"] + 22))):
                if 0 <= z < n and 0 <= x < n and g[z][x] in ". ":
                    g[z][x] = "D"
    for c in layout["connectors"]:
        for z in range(int(math.floor(c["z0"] + 22)), int(math.ceil(c["z1"] + 22))):
            for x in range(int(math.floor(c["x0"] + 22)), int(math.ceil(c["x1"] + 22))):
                if 0 <= z < n and 0 <= x < n and g[z][x] in ". ":
                    g[z][x] = "R" if c["kind"] == "ramp" else "S"
    g[int(-18 + 22)][22] = "@"
    lines = ["   " + "".join(str((x - 22) % 10) for x in range(n))]
    for z in range(n - 1, -1, -1):
        lines.append("%3d" % (z - 22) + "".join(g[z]))
    return "\n".join(lines)


# --------------------------------------------------------------------------- main
def main():
    layout = build_layout()
    routes = build_routes()
    problems = check_routes(layout, routes)
    met = metrics(layout, routes)

    layout_out = dict(layout)
    layout_out["routes"] = routes
    layout_out["spawn"] = {"x": 0, "z": -18, "yaw": 0}
    layout_out["enemies"] = [
        {"prefab": "ranged", "x": 10.0, "z": 11.0, "y": DECK_Y, "on": "Deck_N"},
        {"prefab": "ranged", "x": -11.0, "z": -8.0, "y": DECK_Y, "on": "Deck_W"},
        {"prefab": "melee", "x": -10.5, "z": 0.0, "y": 0.0, "on": "yard"},
        {"prefab": "melee", "x": 10.5, "z": -2.0, "y": 0.0, "on": "yard"},
    ]
    layout_out["pois"] = [
        {"id": 1, "name": "Pilar de canto", "x": -11.0, "z": 11.0, "y": 8.5,
         "note": "unico ponto que fecha o loop elevado; so Scissors (wall climb)"},
        {"id": 2, "name": "Ponta NE do deck", "x": 10.0, "z": 11.0, "y": DECK_Y,
         "note": "vantagem sobre metade norte do patio"},
        {"id": 3, "name": "Topo do banco", "x": 0.0, "z": 0.0, "y": 3.0,
         "note": "ancora visual e cobertura central"},
        {"id": 4, "name": "Nicho N", "x": -8.5, "z": 12.6, "y": 0.0,
         "note": "bolsao de contencao: cobertura do deck, uma saida"},
        {"id": 5, "name": "Nicho S", "x": 8.5, "z": -12.6, "y": 0.0,
         "note": "bolsao de contencao: cobertura do deck, uma saida"},
    ]

    with open(os.path.join(OUT, "layout.json"), "w") as f:
        json.dump(layout_out, f, indent=1)
    with open(os.path.join(OUT, "metrics.json"), "w") as f:
        json.dump(met, f, indent=1)
    with open(os.path.join(OUT, "blueprint.svg"), "w") as f:
        f.write(draw_plan(layout, routes, met))
    with open(os.path.join(OUT, "circulation.svg"), "w") as f:
        f.write(draw_circulation(layout, routes, met))

    print(ascii_plan(layout))
    print()
    if problems:
        print("!! ROUTE CHECK FAILED")
        for p in problems:
            print("   ", p)
    else:
        print("route check: every ground leg is clear of solids >= 1.8 m")
    print()
    for k, v in met["loops"].items():
        print("  %-10s %5.0f m  walk %4.1f s  expo %2.0f%%  crossfire %2.0f%%"
              % (k, v["perimeter_m"], v["walk_s"], v["exposure"] * 100, v["crossfire"] * 100))
    print("short gate->gate: %.1f m expo %.0f%%" % (met["routes"]["gate_to_gate_short"]["length_m"],
                                                    met["routes"]["gate_to_gate_short"]["exposure"] * 100))
    print("long  gate->gate: %.1f m expo %.0f%%  ratio x%.2f" % (met["routes"]["gate_to_gate_long"]["length_m"],
                                                                 met["routes"]["gate_to_gate_long"]["exposure"] * 100,
                                                                 met["redundancy_ratio"]))
    print("wrote layout.json, metrics.json, blueprint.svg, circulation.svg ->", OUT)


if __name__ == "__main__":
    main()
