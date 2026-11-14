"""RV Car Studio's icon: a low RC car in side view, orange to coral on near-black, with a
blueprint grid behind it (the editing). Same family as the RVGL Track Editor's icon.
    python make_icon.py OUT.ico      (also writes OUT.png)"""
import sys
from PIL import Image, ImageDraw, ImageFilter

S = 1024


def bezier(p0, p1, p2, p3, n=40):
    out = []
    for i in range(n + 1):
        t = i / n
        u = 1 - t
        out.append((u**3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t**3 * p3[0],
                    u**3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t**3 * p3[1]))
    return out


def gradient(size, top, bottom, horizontal=False):
    g = Image.new("RGBA", (size, size))
    px = g.load()
    for i in range(size):
        t = i / (size - 1)
        c = tuple(round(top[k] + (bottom[k] - top[k]) * t) for k in range(4))
        for j in range(size):
            px[(i, j) if horizontal else (j, i)] = c
    return g


img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

# Tile: near-black with a slight lift at the top
tile_mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(tile_mask).rounded_rectangle((32, 32, S - 32, S - 32), radius=210, fill=255)
img.paste(gradient(S, (30, 30, 36, 255), (10, 10, 13, 255)), (0, 0), tile_mask)

# Blueprint grid, faint, only inside the tile
grid = Image.new("RGBA", (S, S), (0, 0, 0, 0))
gd = ImageDraw.Draw(grid)
for k in range(32 + 96, S - 32, 96):
    gd.line((k, 32, k, S - 32), fill=(255, 255, 255, 14), width=3)
    gd.line((32, k, S - 32, k), fill=(255, 255, 255, 14), width=3)
img.alpha_composite(Image.composite(grid, Image.new("RGBA", (S, S)), tile_mask))

# The body: one closed outline, nose at the right
body = []
body += bezier((150, 640), (150, 560), (200, 520), (290, 505))        # rear bumper up
body += bezier((290, 505), (360, 495), (380, 420), (450, 405))        # rear deck to cabin
body += bezier((450, 405), (540, 385), (640, 390), (700, 450))        # canopy
body += bezier((700, 450), (760, 500), (850, 500), (885, 545))        # bonnet
body += bezier((885, 545), (910, 580), (905, 630), (870, 645))        # nose
body += [(150, 645)]

shadow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
ImageDraw.Draw(shadow).ellipse((150, 700, 890, 770), fill=(0, 0, 0, 150))
img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(22)))

body_mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(body_mask).polygon(body, fill=255)
img.paste(gradient(S, (255, 168, 46, 255), (255, 72, 92, 255), horizontal=True), (0, 0), body_mask)

d = ImageDraw.Draw(img)
# Highlight along the top of the body
d.line(bezier((300, 512), (370, 500), (390, 430), (455, 418)) + bezier((455, 418), (540, 400), (630, 405), (690, 455)),
       fill=(255, 236, 200, 150), width=10, joint="curve")
# Window: dark glass in the canopy
glass = bezier((470, 430), (540, 412), (620, 415), (668, 462)) + [(470, 462)]
d.polygon(glass, fill=(16, 16, 20, 235))
# Rear wing
d.polygon([(170, 455), (300, 440), (300, 462), (176, 478)], fill=(255, 150, 60, 255))
d.rectangle((246, 462, 262, 515), fill=(255, 130, 70, 255))
# Side stripe
d.line([(330, 585), (820, 585)], fill=(255, 255, 255, 70), width=12)

# Wheels: dark tyres, light rims with a coral hub
for cx in (300, 745):
    cy, r = 655, 112
    d.ellipse((cx - r - 16, cy - r - 16, cx + r + 16, cy + r + 16), fill=(10, 10, 13, 255))  # arch gap
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(28, 28, 33, 255))
    rr = 62
    d.ellipse((cx - rr, cy - rr, cx + rr, cy + rr), fill=(225, 226, 232, 255))
    rh = 24
    d.ellipse((cx - rh, cy - rh, cx + rh, cy + rh), fill=(255, 92, 84, 255))

# Each size drawn down from the big one, sharpened a little so the small ones stay crisp
sizes = [16, 24, 32, 48, 64, 128, 256]
frames = []
for n in sizes:
    f = img.resize((n, n), Image.LANCZOS)
    if n <= 48:
        f = f.filter(ImageFilter.UnsharpMask(radius=1, percent=60, threshold=0))
    frames.append(f)
out = sys.argv[1]
frames[-1].save(out, format="ICO", sizes=[(n, n) for n in sizes], append_images=frames[:-1])
img.resize((512, 512), Image.LANCZOS).save(out[:-4] + ".png")
