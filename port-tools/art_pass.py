"""Art pass 2026-09-24 (5.18 + 5.19): Auga-style sprites for the panels that still showed vanilla art.

Draws at 4x and downsamples (anti-aliasing), in Auga's palette and shape language (chamfered corners, thin #706457
ornament lines, #B98A12 gold accents, the AugaPanelBase gradient). Writes PNGs + Unity .meta files (sprite, 9-slice
border, assetBundleName augaassets) into AugaUnity/Assets/Sprites/ArtPass. Re-run to regenerate; GUIDs are fixed.

    python port-tools/art_pass.py [--preview out.png]
"""
import math
import os
import re
import sys

from PIL import Image, ImageDraw

S = 4  # supersampling
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "AugaUnity", "Assets", "Sprites", "ArtPass")
TEMPLATE_META = os.path.join(HERE, "..", "AugaUnity", "Assets", "Sprites", "TextBackdrop.png.meta")

ORNAMENT = (0x70, 0x64, 0x57)
GOLD = (0xB9, 0x8A, 0x12)
BRIGHT_GOLD = (0xFF, 0xBF, 0x1B)
DARK = (0x18, 0x14, 0x10)
# AugaPanelBase corner colours (UL, UR, LR, LL)
PANEL = [(0.125, 0.102, 0.082), (0.282, 0.235, 0.188), (0.322, 0.267, 0.216), (0.180, 0.153, 0.125)]

# name -> (guid, 9-slice border L,B,R,T)
SPRITES = {
    "AugaListRow": ("a7a55e0b1c2d4e3fa0b1c2d3e4f50601", (12, 12, 12, 12)),
    "AugaListRowSelected": ("a7a55e0b1c2d4e3fa0b1c2d3e4f50602", (12, 12, 12, 12)),
    "AugaToastPlate": ("a7a55e0b1c2d4e3fa0b1c2d3e4f50603", (64, 0, 64, 0)),
    "AugaRadialCenter": ("a7a55e0b1c2d4e3fa0b1c2d3e4f50604", (0, 0, 0, 0)),
    "AugaPortraitRing": ("a7a55e0b1c2d4e3fa0b1c2d3e4f50605", (0, 0, 0, 0)),
}


def rgba(c, a=1.0):
    if isinstance(c[0], float):
        c = tuple(int(round(v * 255)) for v in c)
    return (c[0], c[1], c[2], int(round(a * 255)))


def chamfer_poly(x0, y0, x1, y1, c):
    return [(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c), (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)]


def mask_of(size, draw_fn):
    m = Image.new("L", size, 0)
    draw_fn(ImageDraw.Draw(m))
    return m


def gradient(size, corners, alpha):
    """Bilinear 4-corner gradient like Auga's UIGradient (UL, UR, LR, LL)."""
    w, h = size
    small = Image.new("RGBA", (2, 2))
    ul, ur, lr, ll = [rgba(c, alpha) for c in corners]
    small.putpixel((0, 0), ul)
    small.putpixel((1, 0), ur)
    small.putpixel((1, 1), lr)
    small.putpixel((0, 1), ll)
    return small.resize((w, h), Image.BILINEAR)


def finish(img, w, h):
    return img.resize((w, h), Image.LANCZOS)


def list_row(selected):
    w, h = 128, 48
    W, H, c = w * S, h * S, 5 * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body = mask_of((W, H), lambda d: d.polygon(chamfer_poly(0, 0, W - 1, H - 1, c), fill=255))
    fill = Image.new("RGBA", (W, H), rgba((0x2A, 0x21, 0x18), 0.88) if selected else rgba(DARK, 0.72))
    img.paste(fill, (0, 0), body)
    d = ImageDraw.Draw(img)
    stroke = 2 * S if selected else 1 * S
    colour = rgba(GOLD, 1.0) if selected else rgba(ORNAMENT, 0.85)
    inset = stroke // 2
    d.line(chamfer_poly(inset, inset, W - 1 - inset, H - 1 - inset, c) + [(inset + c, inset)], fill=colour, width=stroke, joint="curve")
    if not selected:
        # faint top highlight, like light catching a bevel
        d.line([(c + 2 * S, 3 * S), (W - c - 2 * S, 3 * S)], fill=rgba((0xEA, 0xE1, 0xD9), 0.10), width=S)
    return finish(img, w, h)


def diamond(d, cx, cy, r, colour):
    d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=colour)


def toast_plate():
    w, h = 400, 110
    W, H, c = w * S, h * S, 14 * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body = mask_of((W, H), lambda d: d.polygon(chamfer_poly(0, 6 * S, W - 1, H - 1 - 6 * S, c), fill=255))
    img.paste(gradient((W, H), PANEL, 0.96), (0, 0), body)
    d = ImageDraw.Draw(img)
    # ornament frame
    d.line(chamfer_poly(S, 6 * S + S, W - 1 - S, H - 1 - 6 * S - S, c) + [(S + c, 7 * S)], fill=rgba(ORNAMENT), width=2 * S, joint="curve")
    # gold rules top and bottom, fading toward the ends, broken by a centre diamond
    for y in (12 * S, H - 12 * S):
        for x in range(26 * S, W - 26 * S, S):
            t = abs((x - W / 2) / (W / 2 - 26 * S))
            a = max(0.0, 0.9 - 0.9 * t ** 1.6)
            if abs(x - W / 2) < 10 * S:
                continue
            d.line([(x, y), (x + S, y)], fill=rgba(GOLD, a), width=S)
        diamond(d, W // 2, y, 4 * S, rgba(GOLD))
    # side diamonds: the notch Auga uses on panel edges
    for cx in (9 * S, W - 1 - 9 * S):
        diamond(d, cx, H // 2, 9 * S, rgba(DARK))
        diamond(d, cx, H // 2, 7 * S, rgba(GOLD))
        diamond(d, cx, H // 2, 3 * S, rgba(BRIGHT_GOLD))
    return finish(img, w, h)


def radial_center():
    w = h = 256
    W = H = w * S
    cx = cy = W / 2
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    r_out = W / 2 - 10 * S
    # radial gradient disc
    disc = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = disc.load()
    inner = (0.20, 0.17, 0.13)
    outer = (0.094, 0.078, 0.063)
    step = S  # compute on a coarse grid, fine enough after downsampling
    for y in range(0, H, step):
        for x in range(0, W, step):
            dist = math.hypot(x + step / 2 - cx, y + step / 2 - cy) / r_out
            if dist > 1.0:
                continue
            t = dist ** 1.4
            col = tuple(inner[i] * (1 - t) + outer[i] * t for i in range(3))
            value = rgba(col, 0.93)
            for yy in range(y, min(y + step, H)):
                for xx in range(x, min(x + step, W)):
                    px[xx, yy] = value
    img.alpha_composite(disc)
    d = ImageDraw.Draw(img)
    d.ellipse([cx - r_out, cy - r_out, cx + r_out, cy + r_out], outline=rgba(ORNAMENT), width=3 * S)
    r_in = r_out - 12 * S
    d.ellipse([cx - r_in, cy - r_in, cx + r_in, cy + r_in], outline=rgba(GOLD, 0.8), width=S)
    for ang in (0, 90, 180, 270):
        a = math.radians(ang)
        x = cx + math.cos(a) * r_out
        y = cy + math.sin(a) * r_out
        diamond(d, x, y, 8 * S, rgba(DARK))
        diamond(d, x, y, 6 * S, rgba(GOLD))
    return finish(img, w, h)


def portrait_ring():
    w = h = 128
    W = H = w * S
    cx = cy = W / 2
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    r = W / 2 - 8 * S
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=rgba(ORNAMENT), width=4 * S)
    r2 = r - 6 * S
    d.ellipse([cx - r2, cy - r2, cx + r2, cy + r2], outline=rgba(GOLD, 0.85), width=int(1.5 * S))
    diamond(d, cx, cy + r, 9 * S, rgba(DARK))
    diamond(d, cx, cy + r, 7 * S, rgba(GOLD))
    diamond(d, cx, cy + r, 3 * S, rgba(BRIGHT_GOLD))
    return finish(img, w, h)


def write_meta(path, guid, border):
    with open(TEMPLATE_META, encoding="utf-8") as f:
        meta = f.read()
    meta = meta.replace("guid: 56ea2911ddf6c5240a218f1b9b69663c", "guid: " + guid, 1)
    l, b, r, t = border
    meta = re.sub(r"spriteBorder: \{x: [0-9.]+, y: [0-9.]+, z: [0-9.]+, w: [0-9.]+\}", f"spriteBorder: {{x: {l}, y: {b}, z: {r}, w: {t}}}", meta, count=1)
    meta = re.sub(r"assetBundleName: .*", "assetBundleName: augaassets", meta, count=1)
    if "assetBundleName:" not in meta:
        raise SystemExit("template meta has no assetBundleName line")
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)


def main():
    os.makedirs(OUT, exist_ok=True)
    folder_meta = OUT + ".meta"
    if not os.path.exists(folder_meta):
        with open(folder_meta, "w", encoding="utf-8", newline="\n") as f:
            f.write("fileFormatVersion: 2\nguid: a7a55e0b1c2d4e3fa0b1c2d3e4f50600\nfolderAsset: yes\nDefaultImporter:\n"
                    "  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    images = {
        "AugaListRow": list_row(False),
        "AugaListRowSelected": list_row(True),
        "AugaToastPlate": toast_plate(),
        "AugaRadialCenter": radial_center(),
        "AugaPortraitRing": portrait_ring(),
    }
    for name, img in images.items():
        path = os.path.join(OUT, name + ".png")
        img.save(path)
        guid, border = SPRITES[name]
        write_meta(path, guid, border)
        print(f"{name}: {img.size} border {border}")

    if "--preview" in sys.argv:
        out = sys.argv[sys.argv.index("--preview") + 1]
        pad = 16
        W = sum(i.width for i in images.values()) + pad * (len(images) + 1)
        H = max(i.height for i in images.values()) + 2 * pad
        sheet = Image.new("RGBA", (W, H), (70, 90, 60, 255))  # meadow green, like the game behind the UI
        x = pad
        for img in images.values():
            sheet.alpha_composite(img, (x, pad))
            x += img.width + pad
        sheet.save(out)
        print("preview:", out)


if __name__ == "__main__":
    main()
