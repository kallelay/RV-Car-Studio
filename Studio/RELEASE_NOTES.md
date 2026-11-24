RV Car Studio 3 is a new car editor for Re-Volt / RVGL, written again from the start: you work on the car directly in the 3D view.

## Download

- **Windows (64-bit): `RvCarStudio-3.0.1-win-x64.zip`**. Unzip it anywhere and run `RvCarStudio.exe`. Nothing to install.
- **Linux (64-bit): `RvCarStudio-3.0.1-linux-x64.tar.gz`**. `tar -xzf` it and run `./RvCarStudio`. Tested on Ubuntu 24.04.
- `RvCarStudio-3.0.1-source.zip`: the source they were built from. `SHA256SUMS.txt`: checksums.

## New in 3.0.1

- A Linux version. File names with the game's backslashes are read right there, so installing cars and exporting .obj work too.
- Full documentation with pictures, in the download: Help > Documentation.

It asks for your RVGL folder the first time. Your own folder's `cars` are used; the game's own files are never overwritten: changes to a stock car go to `packs\local`.

## What's in it

- **Pro and Easy.** Pro shows everything, RVGL's own values too. Easy has the layout of the old RV Car Studio: Edit Menu, Render Settings, tips.
- **Editing in the 3D view**: move, turn and size parts with handles; edit the vertices of models and of the collision hull; with undo.
- **New car**: a repaint, a remodel, or your own body, wheel and texture (`.prm` or `.obj`). Tours walk you through each.
- **Paint**: hue, colour swaps and skins seen live on the car, and a UV and texture editor.
- **Shading**: light and contact shadows baked into the model, its normals, its shadow.
- **Collision hull**: spheres fitted to the body's outline, convex parts, all editable.
- **Carbox maker**: the car cut out of the view, the stock look, the name in any direction, kerning and size.
- **Handling**: how it drives, worked out as you edit; take the handling of another car; drive or race it right there, or in RVGL with one key.
- **Package**: a check of the car, its readme and a zip ready to share.
- Cars from a zip or folder installed safely, with undo.

## Good to know

- It isn't signed, so Windows may ask before it runs the first time.
- On Linux, Test run starts RVGL but you pick the car and the track yourself, and gamepads don't work in Drive here. The carbox name uses another heavy font when Impact isn't installed.
- macOS: it builds from source, but hasn't been tested on a Mac.
- The hull's fit is a good start, not a replacement for a test race: Test run (F5) is the final check.

GPL v3. Re-Volt is the property of its owners; RVGL is the work of the RVGL team. No game data is included.
