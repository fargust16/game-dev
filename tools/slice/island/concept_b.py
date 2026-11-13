"""Concept B: the Valley. A river city between two ridges, on both banks.

The island is long from the north-west to the south-east, and a valley runs its length between a high eastern ridge
and a lower western one. The river comes down it to a tidal reach and an estuary behind a sand spit. The city stands
at the head of the tide, astride the river: the Penny on the east bank above the weir, the Sleepy Quarter facing it on
the west bank, the quay below the Beck's mouth and the works across the water from it. Every district is turned to its
own reach of the river, so the river's bends are the city's angles.
"""
from islandgen import District

T = 26.565

quay = District(
    "Lower Veil", (694, 738), 225, cols=[45, 40], rows=[40, 40], border=("street", "street", "quay", "street"),
    level=1.9, apron=6, family="tenement", wealth="poor",
    cells={(0, 0): {"kind": "built", "family": "warehouse"}, (1, 0): {"kind": "built", "family": "warehouse"},
           (1, 1): {"kind": "square", "singles": [("Soot & Bell", 3, 3, 15, 15, 12, "town"), ("Harrow & Tincture", 27, 3, 10, 10, 15, "shop")]}},
    label="Lower Veil and Fishlip Quay")

centre = District(
    "The Penny", (518, 554), 270 - T, cols=[40, 52], rows=[45, 40], border=("street", "street", "emb", "street"),
    level=4.6, apron=18, family="shop", wealth="rich",
    cells={(1, 1): {"kind": "market", "singles": [("Stone Grandad", 22, 16, 8, 8, 12, "church")]},
           (1, 0): {"kind": "square", "singles": [("Town Hall", 11, 6, 30, 15, 19, "civic"), ("Clock tower", 3, 6, 5, 5, 30, "civic"), ("Guild hall", 11, 26, 30, 15, 15, "civic")]},
           (0, 1): {"kind": "square", "singles": [("Assize and Wardens", 5, 5, 30, 15, 15, "civic")]}},
    label="The Penny")

close = District(
    "Parsons' End", (716, 455), 0, cols=[70, 40], rows=[50, 30], border=("lane",) * 4, sx=["lane"], sy=["lane"],
    level=14.0, tilt=(0.025, -0.02), apron=24, family="low", wealth="poor",
    cells={(0, 0): {"kind": "park", "singles": [("Cathedral", 8, 12, 46, 22, 24, "church"), ("Cathedral tower", 54, 16, 12, 14, 44, "church")]},
           (1, 0): {"kind": "cemetery", "singles": [("Old chapel", 14, 36, 13, 10, 9, "church")]},
           (0, 1): {"kind": "park", "singles": [("Faction's house", 6, 8, 15, 15, 13, "villa"), ("Brothers' House", 36, 6, 28, 18, 12, "church")]}},
    label="Parsons' End")

sleepy = District(
    "Sleepy Quarter", (406, 440), 90 - T, cols=[45, 45], rows=[40, 40], border=("street", "street", "emb", "street"),
    level=6.0, tilt=(0.0, 0.03), apron=20, family="town", wealth="mid",
    cells={(0, 1): {"kind": "square", "wealth": "rich", "singles": [("Governor's House", 7, 4, 30, 15, 15, "civic"), ("Villa", 4, 23, 15, 15, 13, "villa"), ("Villa", 26, 23, 15, 15, 13, "villa")]},
           (1, 1): {"kind": "park"}},
    label="Sleepy Quarter")

works = District(
    "The Reek", (548, 648), 45, cols=[45, 40], rows=[40, 35], border=("works", "lane", "works", "lane"), sx=["works"], sy=["lane"],
    level=2.0, apron=8, family="low", wealth="poor",
    cells={(0, 0): {"kind": "works"}, (1, 0): {"kind": "works", "h": 16.0}, (0, 1): {"kind": "works", "boxes": [(4, 4, 34, 31)], "h": 11.0}},
    label="The Reek")

CONCEPT = dict(
    key="B", name="The Valley: a river city between two ridges", seed=202,
    base=3.0, base_reach=160.0, lift_reach=100.0, spurs=0.18, gully=2.4, forest=0.12,
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
        [(600, 160, 24, 110), (720, 230, 40, 140), (810, 330, 44, 150), (884, 450, 34, 120), (892, 580, 20, 100)],
        [(250, 250, 24, 110), (285, 360, 28, 115), (270, 470, 18, 100), (300, 590, 22, 110), (370, 700, 16, 100), (450, 800, 10, 90)],
    ],
    hills=[(420, 190, 10, 100), (150, 590, 18, 90), (140, 170, 20, 100)],
    rivers=[
        dict(name="Veil", side=0.30, pts=[
            (250, 170, 32.0, 3, 16, 0.4), (300, 260, 22.0, 5, 30, 0.5), (370, 340, 14.0, 8, 50, 0.7), (430, 430, 8.0, 11, 70, 0.9),
            (470, 520, 3.2, 14, 80, 1.2), (510, 580, -1.2, 26, 90, 1.6), (590, 660, -2.4, 36, 100, 2.4), (690, 760, -3.0, 46, 110, 3.0)]),
        dict(name="the Beck", side=0.36, pts=[
            (720, 380, 24.0, 2, 12, 0.3), (670, 440, 14.0, 3, 16, 0.4), (640, 510, 7.0, 4, 20, 0.5), (600, 560, 2.2, 5, 24, 0.6),
            (568, 606, -1.0, 7, 26, 0.9), (556, 624, -1.6, 8, 28, 1.4)]),
    ],
    rocks=[(360, 40, 10, 4.0), (300, 36, 7, 3.0), (952, 500, 9, 5.0), (936, 380, 8, 4.0), (230, 800, 10, 4.0)],
    stamps=[
        dict(name="Meadows 28", at=(400, 330), size=400, rot=30, amp=5.0),
        dict(name="Layered Rocks 15", at=(800, 400), size=320, rot=100, amp=10.0, mask=False),
        dict(name="Rough Cliffs 18", at=(330, 110), size=260, rot=0, amp=10.0),
        dict(name="Talus Rock Clusters 19", at=(240, 790), size=170, rot=40, amp=7.0, stage="post"),
    ],
    districts=[quay, centre, close, sleepy, works],
    pads=[
        dict(poly=[(588, 612), (612, 636), (644, 604), (630, 572), (608, 572)], level=2.4, kind="square", name="Bawling Wedge"),
        dict(poly=[(722, 566), (792, 636), (850, 566)], level=8.5, apron=18, kind="park", name="Bishop's Park"),
    ],
    roads=[
        dict(name="Vale Road", cls="road", pts=[(400, 436), (380, 380), (345, 310), (300, 236), (240, 262), (200, 322)]),
        dict(name="Salt Gullet", cls="main", smooth=False, pts=[(572, 530), (600, 584)]),
        dict(name="Lenten Street", cls="side", smooth=False, pts=[(606, 450), (660, 470), (716, 486)]),
        dict(name="Spit Lane", cls="road", pts=[(700, 726), (740, 740), (800, 790), (828, 850), (822, 886)]),
        dict(name="South Road", cls="road", pts=[(558, 812), (540, 870), (520, 928)]),
        dict(name="West track", cls="track", pts=[(355, 607), (300, 628), (240, 600), (196, 540), (184, 476)]),
        dict(name="Ridge track", cls="track", pts=[(856, 500), (872, 440)]),
    ],
    bridges=[dict(name="Last Farthing Bridge", a=(486, 499), b=(444, 517), w=14.0), dict(name="Grubby Bridge", a=(598, 630), b=(554, 670), w=14.0),
             dict(name="Beck Bridge", a=(578, 544), b=(594, 574), w=22.0)],
    piers=[dict(a=(650, 696), b=(634, 712), w=7.0)],
    walls=[[(404, 432), (296, 486), (354, 612), (440, 700)], [(452, 424), (506, 356), (590, 348), (668, 396), (712, 440)]],
    points=[dict(at=(822, 890), kind="lighthouse", name="The One-Eye"), dict(at=(402, 438), kind="gate", name="Creaking Gate")],
    hamlets=[dict(name="Crooked Hamlet", at=(300, 236), angle=55, n=8, pitch=22.0)],
    farmland=[[(300, 256), (372, 300), (400, 400), (340, 410), (286, 330)], [(420, 300), (520, 330), (470, 400), (425, 380)]],
    marsh=[[(680, 790), (740, 850), (760, 925), (700, 905), (672, 840)], [(780, 800), (800, 830), (800, 870), (784, 850)]],
    forest_bias=[(780, 370, 170, 0.5), (230, 420, 150, 0.45), (420, 790, 120, 0.3), (430, 170, 130, 0.3)],
    labels=[("Veil Water", 760, 850, 10), ("East Ridge", 800, 330, 10), ("West Ridge", 215, 400, 10), ("River Veil", 350, 340, 8),
            ("the Beck", 668, 420, 7), ("Sand Tongue", 850, 830, 8), ("Long Strand", 580, 960, 8), ("Kettle Bay", 100, 390, 8)],
    views=[dict(name="up_the_valley", eye=(1020.0, 1160.0, 330.0), look=(500.0, 480.0, 0.0), lens=32.0),
           dict(name="over_the_river", eye=(700.0, 820.0, 90.0), look=(520.0, 520.0, 6.0), lens=28.0)],
)
