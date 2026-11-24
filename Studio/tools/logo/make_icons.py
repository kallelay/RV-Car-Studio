"""The app icon from the logo: python make_icons.py LOGO.svg OUT.ico
Sizes 64 px and up are the logo itself; 48 px and down a simpler variant (all of it built, no wireframe or scan line,
bolder strokes), which stays readable that small. Also writes OUT.png (512 px) and LOGO.png (1024 px)."""
import os, re, subprocess, sys
from PIL import Image, ImageFilter
here = os.path.dirname(os.path.abspath(__file__))
logo, out = os.path.abspath(sys.argv[1]), os.path.abspath(sys.argv[2])
def render(svg_path, png_path):
    subprocess.run([sys.executable, "-I", os.path.join(here, "render.py"), svg_path, png_path], check=True, capture_output=True)
    return Image.open(png_path).convert("RGBA")
svg = open(logo, encoding="utf-8").read()
small = re.sub(r'<clipPath id="built"><rect [^>]*/>', '<clipPath id="built"><rect x="0" y="0" width="1024" height="1024"/>', svg)
small = re.sub(r'<clipPath id="mesh"><rect [^>]*/>', '<clipPath id="mesh"><rect x="0" y="0" width="0" height="0"/>', small)
small = small.replace('<g id="scanline">', '<g id="scanline" display="none">')
small = small.replace('stroke-dasharray="26 26"', 'stroke-dasharray="0 1000"').replace('stroke-width="13" stroke-linecap', 'stroke-width="30" stroke-linecap')
small = small.replace('stroke-dasharray="22 26"', 'stroke-dasharray="0 1000"')
small_svg = logo[:-4] + "_small.svg"
open(small_svg, "w", encoding="utf-8").write(small)
big = render(logo, logo[:-4] + ".png")
little = render(small_svg, logo[:-4] + "_small.png")
os.remove(small_svg)
sizes = [16, 24, 32, 48, 64, 128, 256]
frames = []
for n in sizes:
    f = (little if n <= 48 else big).resize((n, n), Image.LANCZOS)
    if n <= 48: f = f.filter(ImageFilter.UnsharpMask(radius=1, percent=50, threshold=0))
    frames.append(f)
frames[-1].save(out, format="ICO", sizes=[(n, n) for n in sizes], append_images=frames[:-1])
big.resize((512, 512), Image.LANCZOS).save(out[:-4] + ".png")
print(out)
