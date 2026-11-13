"""Concept C: the Terraces. A city on three steps between a quay and a plateau.

The island is a tilted block: high cliffs along the north, a plateau behind them, and the ground falling south in
natural benches to a sheltered shore behind a rocky hook. The city climbs three of them: the quay and Lower Veil on the
water, the Penny and the Sleepy Quarter on the middle bench, the cathedral and the villas on the upper one. Scarps
between the benches are gardens, steps and ramps. The river leaves the plateau in a ravine west of the town, falls from
bench to bench, and meets the sea at the quay's end, where the works stand.
"""
from islandgen import District

T = 26.565

quay = District(
    "Lower Veil", (640, 650), 180 - T, cols=[45, 40, 45, 40], rows=[40], border=("street", "street", "quay", "street"),
    level=1.9, apron=6, family="warehouse", wealth="poor",
    cells={(2, 0): {"kind": "square", "singles": [("Soot & Bell", 3, 3, 15, 15, 12, "town"), ("Harrow & Tincture", 27, 3, 10, 10, 15, "shop")]},
           (3, 0): {"kind": "built", "family": "low"}},
    label="Lower Veil and Fishlip Quay")

centre = District(
    "The Penny", (500, 440), 0, cols=[40, 52, 40], rows=[45, 40], level=10.0, apron=14, family="shop", wealth="rich",
    cells={(1, 0): {"kind": "market", "singles": [("Stone Grandad", 22, 18, 8, 8, 12, "church")]},
           (1, 1): {"kind": "square", "singles": [("Town Hall", 11, 4, 30, 15, 19, "civic"), ("Clock tower", 3, 4, 5, 5, 30, "civic"), ("Guild hall", 11, 23, 30, 15, 15, "civic")]},
           (0, 1): {"kind": "square", "singles": [("Assize and Wardens", 5, 4, 30, 15, 15, "civic")]},
           (2, 1): {"kind": "built", "family": "town", "wealth": "mid"}},
    label="The Penny")

sleepy = District(
    "Sleepy Quarter", (486, 552), 135, cols=[45, 45], rows=[40, 40], level=10.0, apron=14, family="town", wealth="mid",
    cells={(1, 1): {"kind": "park"}, (0, 0): {"kind": "built", "family": "tenement", "wealth": "poor"}},
    label="Sleepy Quarter")

close = District(
    "Parsons' End", (590, 270), T, cols=[70, 40], rows=[50, 30], border=("lane",) * 4, sx=["lane"], sy=["lane"],
    level=19.0, apron=16, family="low", wealth="poor",
    cells={(0, 0): {"kind": "park", "singles": [("Cathedral", 8, 12, 46, 22, 24, "church"), ("Cathedral tower", 54, 16, 12, 14, 44, "church")]},
           (1, 0): {"kind": "cemetery", "singles": [("Old chapel", 14, 36, 13, 10, 9, "church")]},
           (0, 1): {"kind": "park", "singles": [("Faction's house", 6, 8, 15, 15, 13, "villa"), ("Brothers' House", 36, 6, 28, 18, 12, "church")]}},
    label="Parsons' End")

rise = District(
    "Salt Rise", (380, 330), 0, cols=[45, 45], rows=[45], level=19.0, apron=16, family="villa", wealth="rich",
    cells={(0, 0): {"kind": "square", "wealth": "rich", "singles": [("Villa", 4, 4, 15, 15, 13, "villa"), ("Villa", 26, 4, 15, 15, 13, "villa"), ("Villa", 4, 26, 15, 15, 13, "villa")]},
           (1, 0): {"kind": "square", "wealth": "rich", "singles": [("Governor's House", 7, 4, 30, 15, 15, "civic"), ("Villa", 4, 26, 15, 15, 13, "villa")]}},
    label="Salt Rise")

works = District(
    "The Reek", (236, 738), 0, cols=[45, 40], rows=[40, 35], border=("works", "lane", "works", "lane"), sx=["works"], sy=["lane"],
    level=2.0, apron=8, family="low", wealth="poor",
    cells={(0, 0): {"kind": "works"}, (1, 0): {"kind": "works", "h": 16.0}, (0, 1): {"kind": "works", "boxes": [(4, 4, 34, 31)], "h": 11.0}},
    label="The Reek")

CONCEPT = dict(
    key="C", name="The Terraces: a city on three steps under a plateau", seed=303,
    base=3.0, base_reach=150.0, lift_reach=90.0, spurs=0.14, gully=2.6, forest=0.10,
    coast=[
        (180, 110, "c", 34), (320, 70, "c", 40), (440, 84, "c", 40), (480, 122, "c", 30), (540, 80, "c", 42), (680, 96, "c", 38),
        (780, 140, "c", 30), (850, 210, "r"), (900, 300, "g"), (950, 430, "b"), (940, 560, "b"), (900, 660, "g"),
        (866, 740, "r"), (836, 806, "c", 10), (770, 852, "c", 9), (704, 848, "c", 8), (700, 800, "r"), (716, 730, "r"),
        (694, 672, "g"), (668, 640, "g"),
        (648, 649, "q"), (538, 704, "q"), (438, 754, "q"), (386, 800, "q"), (362, 850, "q"),
        (300, 864, "g"), (236, 866, "g"), (140, 884, "b"), (64, 826, "b"), (110, 760, "b"), (96, 650, "r"), (60, 520, "c", 22),
        (100, 420, "c", 26), (150, 330, "r"), (110, 240, "c", 30), (120, 160, "c", 34),
    ],
    ridges=[
        [(160, 170, 36, 170), (330, 150, 44, 190), (520, 150, 46, 190), (700, 180, 40, 180), (840, 260, 26, 150)],
        [(330, 340, 20, 150), (520, 330, 21, 150), (700, 350, 19, 150)],
        [(380, 500, 11, 120), (560, 500, 11, 120), (700, 520, 10, 110)],
        [(130, 470, 24, 120), (150, 580, 18, 110)],
        [(840, 700, 9, 70), (800, 800, 11, 70), (730, 836, 8, 50)],
    ],
    rivers=[dict(name="Veil", side=0.42, pts=[
        (300, 130, 46.0, 3, 14, 0.4), (310, 230, 38.0, 4, 20, 0.5), (296, 330, 28.0, 5, 24, 0.5), (270, 430, 19.0, 6, 30, 0.6),
        (268, 530, 10.0, 8, 40, 0.8), (300, 620, 4.0, 10, 50, 1.0), (350, 700, 0.4, 14, 60, 1.2), (395, 762, -1.6, 26, 70, 1.8),
        (424, 802, -2.8, 34, 80, 2.8)])],
    rocks=[(560, 840, 14, 5.0), (585, 860, 8, 3.0), (770, 840, 9, 4.0), (50, 560, 9, 4.0), (450, 30, 9, 5.0), (700, 60, 8, 4.0)],
    stamps=[
        dict(name="Layered Rocks 15", at=(200, 200), size=300, rot=60, amp=8.0, mask=False),
        dict(name="Meadows 12", at=(820, 420), size=360, rot=10, amp=4.0),
        dict(name="Rough Cliffs 18", at=(520, 110), size=300, rot=180, amp=9.0),
        dict(name="Rock Shapes 22", at=(740, 800), size=110, rot=40, amp=6.0, stage="post", mask=False),
    ],
    districts=[quay, centre, sleepy, close, rise, works],
    pads=[
        dict(poly=[(412, 652), (492, 572), (600, 574), (602, 586), (420, 678)], level=6.0, apron=10, kind="park", name="Hanging Gardens"),
        dict(poly=[(548, 428), (664, 428), (560, 376)], level=14.5, apron=10, kind="park", name="Drowsy Steps"),
    ],
    roads=[
        dict(name="Coal Ramp", cls="street", smooth=False, pts=[(606, 594), (650, 584), (690, 556)]),
        dict(name="Weir Ramp", cls="street", smooth=False, pts=[(470, 662), (430, 650), (400, 644)]),
        dict(name="Lenten Street", cls="side", smooth=False, pts=[(500, 440), (520, 404), (552, 366)]),
        dict(name="Rise Road", cls="street", smooth=False, pts=[(520, 440), (506, 402)]),
        dict(name="Plateau Road", cls="road", pts=[(440, 330), (430, 250), (380, 180), (380, 96)]),
        dict(name="East Road", cls="road", pts=[(680, 500), (740, 484), (800, 474), (880, 470), (936, 462)]),
        dict(name="Hook Lane", cls="road", pts=[(672, 638), (720, 690), (770, 780), (730, 834)]),
        dict(name="West Road", cls="road", pts=[(236, 790), (180, 806), (118, 824)]),
    ],
    bridges=[dict(name="Grubby Bridge", a=(430, 758), b=(364, 770), w=14.0)],
    piers=[dict(a=(600, 672), b=(612, 700), w=8.0), dict(a=(500, 722), b=(512, 750), w=8.0)],
    walls=[[(330, 400), (372, 318), (520, 262), (600, 250), (726, 326), (728, 440), (690, 570)]],
    points=[dict(at=(716, 842), kind="lighthouse", name="The One-Eye"), dict(at=(446, 290), kind="gate", name="Creaking Gate")],
    hamlets=[dict(name="Crooked Hamlet", at=(800, 480), angle=10, n=8, pitch=22.0)],
    farmland=[[(740, 400), (880, 380), (920, 480), (860, 560), (740, 540)], [(760, 250), (860, 290), (880, 360), (770, 350)]],
    forest_bias=[(150, 440, 150, 0.5), (300, 200, 150, 0.4), (600, 170, 170, 0.35), (820, 620, 100, 0.3), (820, 450, 120, -0.6)],
    labels=[("The Hook", 760, 760, 9), ("The Plateau", 520, 170, 10), ("River Veil", 250, 480, 8), ("East Fields", 830, 430, 8),
            ("West Wood", 140, 480, 8), ("Sandy Reach", 150, 800, 8), ("North Cliffs", 520, 44, 8)],
    views=[dict(name="from_the_south", eye=(560.0, 1240.0, 300.0), look=(520.0, 440.0, 8.0), lens=32.0),
           dict(name="up_the_steps", eye=(640.0, 830.0, 70.0), look=(540.0, 430.0, 14.0), lens=28.0)],
)
