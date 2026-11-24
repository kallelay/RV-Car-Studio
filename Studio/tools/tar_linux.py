"""tar_linux.py FOLDER OUT.tar.gz: the folder as a .tar.gz whose program can be run (Windows has no such mark to keep)."""
import os
import sys
import tarfile

folder, out = os.path.abspath(sys.argv[1]), sys.argv[2]


def mark(t):
    t.uid = t.gid = 0
    t.uname = t.gname = "root"
    t.mode = 0o755 if t.isdir() or os.path.basename(t.name) == "RvCarStudio" else 0o644
    return t


with tarfile.open(out, "w:gz", compresslevel=6) as tar:
    tar.add(folder, arcname=os.path.basename(folder), filter=mark)
print(f"{os.path.getsize(out):,}  {out}")
