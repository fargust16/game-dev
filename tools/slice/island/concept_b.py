"""Concept B, the Valley: a river city between two ridges, on both banks. Chosen by the owner on 2026-11-14.

The island is long from the north-west to the south-east, and a valley runs its length between a high eastern ridge
and a lower western one. The Veil is tidal for its whole length (owner, 2026-11-14: a river on its own falling surface
only if it is cheap, and it is not): a creek that begins at Veil Head, a mere under the hills fed by stony brooks, widens
past the hamlet and the fields, runs through the city and opens into Veil Water behind a sand spit. One sheet of water
at Sea Level serves it all. The city stands astride it: the Penny on the east bank between the river and the Beck, the
Sleepy Quarter facing it, the quay below the Beck's mouth and the works across the water. Each district is turned to
its own reach, so the river's bends are the city's angles. Where neither water nor the ridge closes the city, a wall
does, with a gate on each road (owner, 2026-11-14).
"""
import math

from islandgen import District

T = 26.565

quay = District(
    "Lower Veil", (696, 736), 225, cols=[45, 40], rows=[40, 40], border=("street", "street", "quay", "street"),
    level=1.9, apron=6, family="tenement", wealth="poor",
    cells={(0, 0): {"kind": "built", "family": "warehouse"}, (1, 0): {"kind": "built", "family": "warehouse"},
           (1, 1): {"kind": "square", "singles": [("Soot & Bell", 3, 3, 15, 15, 12, "town"), ("Harrow & Tincture", 27, 3, 10, 10, 15, "shop")]}},
    label="Lower Veil and Fishlip Quay")

centre = District(
    "The Penny", (518, 554), 270 - T, cols=[40, 52, 40], rows=[45, 40], border=("street", "street", "emb", "street"),
    level=4.6, apron=18, family="shop", wealth="rich",
    cells={(1, 1): {"kind": "market", "singles": [("Stone Grandad", 22, 16, 8, 8, 12, "church")]},
           (1, 0): {"kind": "square", "singles": [("Town Hall", 11, 6, 30, 15, 19, "civic"), ("Clock tower", 3, 6, 5, 5, 30, "civic"), ("Guild hall", 11, 26, 30, 15, 15, "civic")]},
           (0, 1): {"kind": "square", "singles": [("Assize and Wardens", 5, 5, 30, 15, 15, "civic")]},
           (2, 1): {"kind": "built", "family": "town", "wealth": "mid"}},
    label="The Penny")

close = District(
    "Parsons' End", (716, 455), 0, cols=[70, 40], rows=[50, 30], border=("lane",) * 4, sx=["lane"], sy=["lane"],
    level=12.0, tilt=(0.03, -0.02), apron=30, family="low", wealth="poor",
    cells={(0, 0): {"kind": "park", "singles": [("Cathedral", 8, 12, 46, 22, 24, "church"), ("Cathedral tower", 54, 16, 12, 14, 44, "church")]},
           (1, 0): {"kind": "cemetery", "singles": [("Old chapel", 14, 36, 13, 10, 9, "church")]},
           (0, 1): {"kind": "park", "singles": [("Faction's house", 6, 8, 15, 15, 13, "villa"), ("Brothers' House", 36, 6, 28, 18, 12, "church")]}},
    label="Parsons' End")

sleepy = District(
    "Sleepy Quarter", (384, 394), 90 - T, cols=[40, 45, 45], rows=[40, 40], border=("street", "street", "emb", "street"),
    level=4.6, tilt=(0.0, 0.03), apron=24, family="town", wealth="mid",
    cells={(0, 1): {"kind": "square", "wealth": "rich", "singles": [("Governor's House", 5, 4, 30, 15, 15, "civic"), ("Villa", 3, 23, 15, 15, 13, "villa"), ("Villa", 22, 23, 15, 15, 13, "villa")]},
           (2, 1): {"kind": "park"}, (0, 0): {"kind": "built", "family": "town", "wealth": "rich"}},
    label="Sleepy Quarter")

works = District(
    "The Reek", (548, 648), 45, cols=[45, 40], rows=[40, 35], border=("works", "lane", "works", "lane"), sx=["works"], sy=["lane"],
    level=2.0, apron=8, family="low", wealth="poor",
    cells={(0, 0): {"kind": "works"}, (1, 0): {"kind": "works", "h": 16.0}, (0, 1): {"kind": "works", "boxes": [(4, 4, 34, 31)], "h": 11.0}},
    label="The Reek")

mere = [(254 + 15 * math.cos(a), 178 + 12 * math.sin(a)) for a in [k * math.pi / 5 for k in range(10)]]

CONCEPT = dict(
    key="B", name="The Valley: a river city between two ridges", seed=202,
    base=3.0, base_reach=160.0, lift_reach=100.0, spurs=0.20, gully=2.6, forest=0.12,
    coast=[
        (150, 96, "c", 22), (250, 70, "c", 22), (340, 112, "r"), (410, 146, "g"), (500, 112, "r"), (600, 90, "c", 24),
        (720, 140, "c", 28), (830, 230, "c", 28), (900, 340, "c", 24), (872, 424, "r"), (930, 500, "c", 20), (936, 600, "r"),
        (900, 680, "g"), (850, 740, "b"), (846, 820, "b"), (822, 896, "b"),
        (800, 880, "m"), (796, 820, "m"), (770, 790, "g"), (722, 762, "g"), (700, 744, "q"),
        (668, 776, "q"), (690, 830, "m"), (716, 890, "m"), (742, 936, "g"), (640, 952, "b"), (520, 942, "b"),
        (420, 912, "r"), (310, 880, "c", 14), (230, 800, "c", 18), (200, 700, "r"), (120, 640, "c", 16), (90, 540, "r"),
        (170, 470, "g"), (212, 400, "b"), (182, 320, "b"), (100, 280, "r"), (80, 190, "c", 20),
    ],
    ridges=[
        [(600, 160, 24, 110), (720, 230, 40, 140), (820, 320, 44, 140), (900, 440, 34, 88), (900, 580, 20, 78)],
        [(250, 250, 24, 110), (262, 350, 26, 110), (250, 470, 14, 90), (290, 600, 22, 110), (370, 700, 16, 100), (450, 800, 10, 90)],
    ],
    hills=[(420, 190, 10, 100), (150, 590, 18, 90), (140, 170, 20, 100), (500, 250, 9, 80), (600, 840, 6, 70)],
    rivers=[
        dict(name="Veil", side=0.30, pts=[
            # 1.8 m of water and more everywhere: deep enough to swim (10.1: from 1.3 m), and for the water's own colour
            (256, 180, -1.8, 6, 40, 1.8), (292, 222, -1.9, 6, 44, 1.9), (296, 268, -2.0, 7, 50, 2.0), (340, 300, -2.1, 8, 56, 2.1),
            (366, 348, -2.2, 10, 62, 2.2), (430, 430, -2.4, 12, 70, 2.4),
            (470, 520, -2.6, 16, 80, 2.6), (510, 580, -2.8, 26, 90, 2.8), (590, 660, -3.0, 36, 100, 3.0), (690, 760, -3.2, 46, 110, 3.2)]),
        dict(name="the Beck", side=0.40, pts=[
            (650, 490, -1.6, 5, 22, 1.6), (640, 512, -1.7, 6, 24, 1.7), (600, 560, -1.8, 7, 26, 1.8), (568, 606, -1.9, 8, 28, 1.9),
            (556, 624, -2.0, 9, 30, 2.0)]),
        dict(name="Beck gully", dry=True, side=0.45, pts=[
            (724, 376, 22.0, 2.5, 10, 0.2), (690, 410, 14.0, 2.5, 12, 0.2), (664, 452, 6.0, 3, 14, 0.2), (652, 484, 0.6, 3, 16, 0.2)]),
        dict(name="West brook", dry=True, side=0.45, pts=[
            (206, 246, 18.0, 2.5, 10, 0.2), (232, 206, 7.0, 2.5, 12, 0.2), (250, 188, 0.5, 3, 14, 0.2)]),
        dict(name="North brook", dry=True, side=0.45, pts=[
            (306, 120, 15.0, 2.5, 10, 0.2), (276, 150, 6.0, 2.5, 12, 0.2), (258, 172, 0.5, 3, 14, 0.2)]),
    ],
    basins=[dict(poly=mere, depth=2.0)],
    rocks=[(360, 40, 10, 4.0), (300, 36, 7, 3.0), (952, 500, 9, 5.0), (936, 380, 8, 4.0), (230, 800, 10, 4.0), (60, 560, 8, 3.5)],
    stamps=[
        dict(name="Meadows 28", at=(400, 300), size=420, rot=30, amp=5.0),
        dict(name="Tuscany Rolling Hills 05", at=(470, 850), size=380, rot=25, amp=4.0),
        dict(name="Layered Rocks 15", at=(800, 400), size=320, rot=100, amp=10.0, mask=False),
        dict(name="Rough Cliffs 18", at=(200, 150), size=260, rot=200, amp=9.0),
        dict(name="Talus Rock Clusters 19", at=(240, 790), size=170, rot=40, amp=7.0, stage="post"),
        dict(name="Rock Shapes 22", at=(904, 440), size=120, rot=10, amp=7.0, stage="post", mask=False),
    ],
    districts=[quay, centre, close, sleepy, works],
    pads=[
        dict(poly=[(588, 612), (612, 636), (644, 604), (630, 572), (608, 572)], level=2.4, kind="square", name="Bawling Wedge"),
        dict(poly=[(722, 566), (792, 636), (850, 566)], level=8.5, apron=18, kind="park", name="Bishop's Park"),
    ],
    roads=[
        dict(name="Vale Road", cls="road", pts=[(378, 386), (326, 336), (268, 294), (236, 250), (210, 270), (200, 322)]),
        dict(name="East Lane", cls="road", pts=[(452, 372), (430, 320), (390, 278), (340, 236), (300, 200)]),
        dict(name="Head path", cls="track", pts=[(258, 214), (240, 246)]),
        dict(name="Salt Gullet", cls="main", smooth=False, pts=[(572, 530), (600, 584)]),
        dict(name="Lenten Street", cls="side", smooth=False, pts=[(606, 450), (660, 470), (716, 486)]),
        dict(name="Spit Lane", cls="road", pts=[(700, 726), (740, 740), (800, 790), (828, 850), (822, 886)]),
        dict(name="South Road", cls="road", pts=[(558, 812), (540, 870), (520, 928)]),
        dict(name="West track", cls="track", pts=[(318, 524), (280, 560), (236, 580), (196, 540), (184, 476)]),
        dict(name="Ridge track", cls="track", pts=[(856, 500), (872, 440)]),
    ],
    bridges=[dict(name="Last Farthing Bridge", a=(486, 499), b=(444, 517), w=14.0), dict(name="Grubby Bridge", a=(598, 630), b=(554, 670), w=14.0),
             dict(name="Beck Bridge", a=(578, 544), b=(594, 574), w=22.0), dict(name="Lenten Bridge", a=(650, 466), b=(668, 474), w=17.0),
             dict(name="Head footbridge", a=(296, 202), b=(258, 214), w=3.0)],
    piers=[dict(a=(650, 696), b=(634, 712), w=7.0)],
    walls=[[(396, 374), (266, 438), (346, 622), (458, 730), (552, 824), (640, 746)],
           [(432, 378), (548, 322), (640, 396), (716, 440)],
           [(858, 560), (820, 680), (742, 754)]],
    points=[dict(at=(822, 890), kind="lighthouse", name="The One-Eye"), dict(at=(374, 384), kind="gate", name="Creaking Gate"),
            dict(at=(306, 530), kind="gate", name="West Gate"), dict(at=(548, 820), kind="gate", name="South Gate"),
            dict(at=(470, 360), kind="gate", name="Farthing Gate"), dict(at=(750, 746), kind="gate", name="Salt Gate")],
    hamlets=[dict(name="Crooked Hamlet", at=(268, 294), angle=227, n=8, pitch=22.0)],
    farmland=[[(300, 304), (352, 340), (372, 378), (330, 380), (290, 340)], [(390, 270), (450, 300), (470, 350), (430, 360), (380, 300)]],
    marsh=[[(680, 790), (740, 850), (760, 925), (700, 905), (672, 840)], [(780, 800), (800, 830), (800, 870), (784, 850)],
           [(236, 160), (280, 166), (292, 212), (248, 218)]],
    forest_bias=[(780, 370, 170, 0.5), (230, 420, 150, 0.45), (420, 790, 120, 0.3), (430, 170, 130, 0.3), (350, 320, 90, -0.5)],
    labels=[("Veil Water", 760, 850, 10), ("East Ridge", 800, 330, 10), ("West Ridge", 240, 400, 10), ("River Veil", 350, 350, 8),
            ("the Beck", 630, 540, 7), ("Sand Tongue", 850, 830, 8), ("Long Strand", 580, 960, 8), ("Kettle Bay", 150, 400, 8),
            ("Veil Head", 250, 160, 7)],
    views=[dict(name="up_the_valley", eye=(1020.0, 1160.0, 330.0), look=(500.0, 480.0, 0.0), lens=32.0),
           dict(name="over_the_river", eye=(700.0, 820.0, 90.0), look=(520.0, 520.0, 6.0), lens=28.0)],
)
