"""Cross-sections of a binary FBX model, read without Unity.

Why: a vendor model placed by its bounding box can stand wrong (VI-8: the Victorian Interiors balustrade stood 0.32 m
too high and back to front, because its fascia hangs below its floor line). Before a model is wrapped in the kit, read
its parts and pivot here: the extent across its width and along its length in each band of height.

Usage: python -I tools/assets/fbx_sections.py <model.fbx> [<model.fbx> ...] [--bands N]

Units are the file's own (Victorian Interiors: cm, so 1.0 = 1 cm; Tripo: m). Unity negates X on import; the script
prints the file's axes as they are. The width axis is the shortest, the height axis the middle, the length the longest,
unless the file is taller than it is long; check the printed ranges.
"""
import struct
import sys
import zlib


def read_fbx(path):
    data = open(path, 'rb').read()
    if not data.startswith(b'Kaydara FBX Binary'):
        raise ValueError(f'{path}: not a binary FBX')
    version = struct.unpack_from('<I', data, 23)[0]
    wide = version >= 7500
    null = 25 if wide else 13

    def node(pos):
        if wide:
            end, count, _ = struct.unpack_from('<QQQ', data, pos)
            pos += 24
        else:
            end, count, _ = struct.unpack_from('<III', data, pos)
            pos += 12
        length = data[pos]
        pos += 1
        name = data[pos:pos + length].decode('latin1')
        pos += length
        props = []
        for _ in range(count):
            kind = chr(data[pos])
            pos += 1
            scalar = {'Y': ('<h', 2), 'C': ('<B', 1), 'I': ('<i', 4), 'F': ('<f', 4), 'D': ('<d', 8), 'L': ('<q', 8)}
            if kind in scalar:
                fmt, size = scalar[kind]
                props.append(struct.unpack_from(fmt, data, pos)[0])
                pos += size
            elif kind in 'SR':
                n = struct.unpack_from('<I', data, pos)[0]
                pos += 4
                props.append(data[pos:pos + n])
                pos += n
            elif kind in 'fdlib':
                n, encoding, size = struct.unpack_from('<III', data, pos)
                pos += 12
                raw = data[pos:pos + size]
                pos += size
                if encoding == 1:
                    raw = zlib.decompress(raw)
                fmt = {'f': 'f', 'd': 'd', 'l': 'q', 'i': 'i', 'b': 'B'}[kind]
                props.append(list(struct.unpack('<%d%s' % (n, fmt), raw)))
            else:
                raise ValueError(f'unknown property type {kind!r}')
        children = []
        while pos < end:
            if data[pos:pos + null] == b'\0' * null:
                pos += null
                break
            child, pos = node(pos)
            children.append(child)
        return (name, props, children), end

    nodes, pos = [], 27
    while pos < len(data) - 200:
        n, pos = node(pos)
        if n[0] == '':
            break
        nodes.append(n)
    return nodes


def find(nodes, name, out=None):
    out = [] if out is None else out
    for n in nodes:
        if n[0] == name:
            out.append(n)
        find(n[2], name, out)
    return out


def main(argv):
    bands = 30
    paths = []
    i = 0
    while i < len(argv):
        if argv[i] == '--bands':
            bands = int(argv[i + 1])
            i += 2
        else:
            paths.append(argv[i])
            i += 1

    for path in paths:
        nodes = read_fbx(path)
        print(path)
        for settings in find(nodes, 'GlobalSettings'):
            for p in find(settings[2], 'P'):
                key = p[1][0].decode()
                if key in ('UpAxis', 'UpAxisSign', 'FrontAxis', 'FrontAxisSign', 'CoordAxis', 'CoordAxisSign', 'UnitScaleFactor'):
                    print(f'  {key} = {p[1][-1]}')
        for geometry in find(nodes, 'Geometry'):
            vertices = [c for c in geometry[2] if c[0] == 'Vertices']
            if not vertices:
                continue
            v = vertices[0][1][0]
            pts = [(v[k], v[k + 1], v[k + 2]) for k in range(0, len(v), 3)]
            ranges = [max(p[a] for p in pts) - min(p[a] for p in pts) for a in range(3)]
            w, h, l = sorted(range(3), key=lambda a: ranges[a])
            label = geometry[1][1].split(b'\0')[0].decode('latin1', 'replace')
            print(f'  geometry {label}: {len(pts)} vertices; x {min(p[0] for p in pts):.3f}..{max(p[0] for p in pts):.3f}, '
                  f'y {min(p[1] for p in pts):.3f}..{max(p[1] for p in pts):.3f}, z {min(p[2] for p in pts):.3f}..{max(p[2] for p in pts):.3f}')
            print(f'  width {"xyz"[w]}, height {"xyz"[h]}, length {"xyz"[l]}')
            lo, hi = min(p[h] for p in pts), max(p[h] for p in pts)
            step = (hi - lo) / bands
            for b in range(bands):
                a0 = lo + b * step
                band = [p for p in pts if a0 <= p[h] < a0 + step or (b == bands - 1 and p[h] == hi)]
                if band:
                    print(f'    {"xyz"[h]} {a0:9.3f}..{a0 + step:9.3f}: {"xyz"[w]} {min(p[w] for p in band):9.3f}..{max(p[w] for p in band):9.3f}'
                          f'  {"xyz"[l]} {min(p[l] for p in band):9.3f}..{max(p[l] for p in band):9.3f}  ({len(band)})')


if __name__ == '__main__':
    main(sys.argv[1:])
