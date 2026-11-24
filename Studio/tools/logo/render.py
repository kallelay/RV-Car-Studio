"""Renders an SVG logo (1024 x 1024) to a PNG with a clear background, using Edge headless: python render.py IN.svg OUT.png"""
import os, subprocess, sys, tempfile, time
src, out = os.path.abspath(sys.argv[1]), os.path.abspath(sys.argv[2])
edge = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
if os.path.exists(out): os.remove(out)
tmp = tempfile.mkdtemp()
subprocess.run([edge, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000",
                f"--user-data-dir={tmp}", "--window-size=1024,1024", f"--screenshot={out}", "file:///" + src.replace("\\", "/")],
               capture_output=True, timeout=60)
for _ in range(20):
    if os.path.exists(out): break
    time.sleep(0.5)
print(out if os.path.exists(out) else "FAILED")
