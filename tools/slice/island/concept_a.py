"""Concept A: the Horseshoe. A harbour city round the head of a natural bay.

A horseshoe bay opens to the south between two headlands. The city stands on its north shore, the quay along the
water, the Penny on the first rise behind it and the cathedral on a knoll above; the river comes down a vale from the
Spine, the island's western ridge, passes the city's west side and enters the bay beside the quay, with the works over
its mouth. Each district is a rectangular frame turned to what it stands by: the quay to the shore, the Penny square
to the compass, the close to its knoll, the Sleepy Quarter to the slope. The wedges between them are greens and squares.
"""
from islandgen import District

T = 26.565  # the turn of a 1 in 2 line, which the Combat Grid reads as regularly as 45 (Logs/GridAngle/report_angles.txt)

harbour = District(
    "Lower Veil", (680, 550), 180 - T, cols=[45, 40, 45], rows=[40, 40], border=("street", "street", "quay", "street"),
    level=1.9, apron=6, family="tenement", wealth="poor",
    cells={(0, 0): {"kind": "built", "family": "warehouse"}, (1, 0): {"kind": "built", "family": "warehouse"},
           (1, 1): {"kind": "square", "singles": [("Soot & Bell", 3, 3, 15, 15, 12, "town"), ("Harrow & Tincture", 27, 3, 10, 10, 15, "shop")]},
           (2, 1): {"kind": "built", "family": "low"}},
    label="Lower Veil and Fishlip Quay")

centre = District(
    "The Penny", (470, 310), 0, cols=[40, 52, 40], rows=[45, 40], level=4.4, apron=20, family="shop", wealth="rich",
    cells={(1, 0): {"kind": "market", "singles": [("Stone Grandad", 22, 18, 8, 8, 12, "church")]},
           (1, 1): {"kind": "square", "singles": [("Town Hall", 11, 4, 30, 15, 19, "civic"), ("Clock tower", 3, 4, 5, 5, 30, "civic"), ("Guild hall", 11, 23, 30, 15, 15, "civic")]},
           (0, 1): {"kind": "square", "singles": [("Assize and Wardens", 5, 4, 30, 15, 15, "civic")]},
           (2, 1): {"kind": "built", "family": "town", "wealth": "mid"}},
    label="The Penny")

close = District(
    "Parsons' End", (540, 140), T, cols=[70, 40], rows=[50, 30], border=("lane",) * 4, sx=["lane"], sy=["lane"],
    level=11.0, tilt=(0.0, -0.02), apron=24, family="low", wealth="poor",
    cells={(0, 0): {"kind": "park", "singles": [("Cathedral", 8, 12, 46, 22, 24, "church"), ("Cathedral tower", 54, 16, 12, 14, 44, "church")]},
           (1, 0): {"kind": "cemetery", "singles": [("Old chapel", 14, 36, 13, 10, 9, "church")]},
           (0, 1): {"kind": "park", "singles": [("Faction's house", 6, 8, 15, 15, 13, "villa"), ("Brothers' House", 36, 6, 28, 18, 12, "church")]}},
    label="Parsons' End")

sleepy = District(
    "Sleepy Quarter", (772, 318), 45, cols=[45, 45], rows=[40, 40], level=7.0, tilt=(0.0, -0.015), apron=20, family="town", wealth="mid",
    cells={(0, 0): {"kind": "square", "wealth": "rich", "singles": [("Governor's House", 7, 4, 30, 15, 15, "civic"), ("Villa", 4, 23, 15, 15, 13, "villa"), ("Villa", 26, 23, 15, 15, 13, "villa")]},
           (1, 1): {"kind": "park"}},
    label="Sleepy Quarter")

works = District(
    "The Reek", (350, 660), 0, cols=[45, 40], rows=[40, 35], border=("works", "lane", "works", "lane"), sx=["works"], sy=["lane"],
    level=2.0, apron=8, family="low", wealth="poor",
    cells={(0, 0): {"kind": "works"}, (1, 0): {"kind": "works", "h": 16.0},
           (0, 1): {"kind": "works", "boxes": [(4, 4, 34, 31)], "h": 11.0}},
    label="The Reek")

CONCEPT = dict(
    key="A", name="The Horseshoe: a harbour city round a natural bay", seed=101,
    base=3.2, base_reach=170.0, lift_reach=110.0, spurs=0.16, gully=2.2, forest=0.10,
    coast=[
        (500, 88, "c", 15), (600, 70, "c", 14), (700, 100, "r"), (780, 160, "g"), (845, 222, "c", 16), (902, 300, "c", 18),
        (932, 400, "r"), (916, 500, "b"), (940, 620, "g"), (932, 742, "r"), (884, 852, "c", 14), (802, 902, "c", 12),
        (742, 868, "r"), (772, 800, "g"), (800, 700, "b"), (790, 606, "b"), (742, 552, "g"),
        (690, 548, "q"), (602, 592, "q"), (514, 636, "q"), (492, 668, "q"), (478, 715, "q"), (477, 770, "q"),
        (490, 822, "m"), (522, 870, "r"), (470, 930, "c", 14), (380, 946, "c", 16), (292, 902, "r"), (232, 822, "g"),
        (192, 722, "b"), (172, 602, "b"), (202, 492, "b"), (162, 402, "r"), (102, 332, "c", 24), (72, 232, "c", 28),
        (112, 142, "c", 24), (202, 102, "r"), (296, 128, "g"), (372, 160, "b"), (446, 140, "g"),
    ],
    ridges=[
        [(120, 225, 28, 120), (215, 285, 46, 150), (300, 350, 40, 150), (348, 440, 27, 130), (345, 540, 16, 110), (300, 620, 9, 90)],
        [(640, 112, 18, 100), (760, 190, 22, 110), (850, 290, 22, 110), (890, 400, 12, 90)],
        [(250, 760, 16, 100), (330, 850, 23, 120), (430, 890, 19, 100)],
        [(850, 640, 10, 90), (860, 760, 20, 110), (850, 840, 14, 80)],
    ],
    hills=[(575, 215, 10, 110), (500, 840, 8, 60)],
    rivers=[dict(name="Veil", side=0.30, pts=[
        (330, 250, 28.0, 3, 16, 0.4), (372, 300, 19.0, 4, 28, 0.5), (402, 376, 11.0, 7, 50, 0.7),
        (424, 466, 5.5, 10, 70, 0.9), (442, 556, 1.8, 14, 80, 1.2), (470, 625, -1.4, 26, 90, 1.6), (500, 664, -2.8, 40, 100, 2.8)])],
    rocks=[(902, 876, 12, 4.5), (918, 850, 7, 3.0), (60, 200, 12, 5.0), (52, 250, 7, 3.5), (270, 930, 10, 4.0),
           (640, 880, 8, 2.5), (560, 52, 9, 4.0)],
    stamps=[
        dict(name="Meadows 12", at=(380, 300), size=380, rot=20, amp=6.0),
        dict(name="Layered Rocks 15", at=(150, 230), size=300, rot=30, amp=10.0, mask=False),
        dict(name="Rock Shapes 22", at=(880, 830), size=120, rot=40, amp=6.0, stage="post", mask=False),
        dict(name="Talus Rock Clusters 19", at=(330, 880), size=180, rot=80, amp=7.0, stage="post"),
    ],
    districts=[harbour, centre, close, sleepy, works],
    pads=[
        dict(poly=[(482, 436), (618, 436), (618, 446), (482, 514)], level=3.4, kind="park", name="Weir Green"),
        dict(poly=[(540, 436), (585, 436), (585, 462), (540, 484)], level=3.4, kind="square", name="Bawling Wedge"),
        dict(poly=[(656, 335), (684, 400), (766, 482), (692, 536), (640, 446), (656, 432)], level=4.6, kind="park", name="Bishop's Park"),
    ],
    roads=[
        dict(name="Vale Road", cls="road", pts=[(474, 316), (444, 250), (424, 220), (396, 196), (372, 176)]),
        dict(name="Salt Gullet", cls="main", smooth=False, pts=[(561, 431), (561, 474)]),
        dict(name="Smoked Street", cls="side", smooth=False, pts=[(476, 431), (469, 500), (468, 522)]),
        dict(name="Lenten Street", cls="side", smooth=False, pts=[(560, 310), (580, 286)]),
        dict(name="Widows' Row", cls="main", smooth=False, pts=[(650, 371), (694, 402)]),
        dict(name="Tar Row", cls="street", smooth=False, pts=[(775, 487), (722, 520), (688, 542)]),
        dict(name="South Road", cls="road", pts=[(350, 700), (300, 690), (240, 650), (205, 600)]),
        dict(name="Headland Lane", cls="road", pts=[(779, 489), (826, 560), (850, 660), (862, 760), (872, 842)]),
        dict(name="Downs track", cls="track", pts=[(665, 203), (740, 215), (800, 262)]),
    ],
    bridges=[dict(name="Grubby Bridge", a=(512, 630), b=(474, 660), w=14.0)],
    piers=[dict(a=(640, 575), b=(655, 605), w=8.0), dict(a=(570, 610), b=(585, 640), w=8.0)],
    walls=[[(410, 330), (436, 262), (476, 206), (522, 126), (606, 118), (684, 184), (762, 290), (868, 394), (926, 432)]],
    points=[dict(at=(878, 852), kind="lighthouse", name="The One-Eye"), dict(at=(444, 250), kind="gate", name="Creaking Gate")],
    hamlets=[dict(name="Crooked Hamlet", at=(428, 222), angle=222, n=8, pitch=22.0)],
    farmland=[[(340, 236), (420, 200), (440, 262), (404, 330), (366, 296)], [(240, 640), (320, 660), (330, 720), (260, 730)]],
    marsh=[[(480, 790), (520, 800), (530, 850), (495, 850)]],
    forest_bias=[(230, 330, 170, 0.5), (780, 220, 150, 0.4), (330, 840, 140, 0.4), (860, 720, 120, 0.3), (200, 600, 90, -0.5)],
    labels=[("The Horseshoe", 640, 720, 11), ("The Spine", 230, 300, 10), ("North Downs", 770, 180, 9), ("River Veil", 400, 420, 8),
            ("Gull Head", 880, 760, 8), ("Southward Head", 360, 900, 8), ("Long Strand", 215, 610, 8), ("Kettle Cove", 372, 140, 8),
            ("Cold Head", 110, 240, 8)],
    views=[dict(name="from_the_south", eye=(700.0, 1250.0, 330.0), look=(540.0, 470.0, 0.0), lens=32.0),
           dict(name="over_the_harbour", eye=(640.0, 800.0, 80.0), look=(560.0, 420.0, 6.0), lens=28.0)],
)
