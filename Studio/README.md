# RV Car Studio 3

The new version: a car editor for Re-Volt / RVGL with a modern interface and a 3D view you work in directly.
C#, .NET 10, Avalonia and OpenGL. Runs on Windows now, written so Linux and macOS can follow.

    dotnet run --project RvStudio.App
    dotnet run --project RvStudio.App -- -rvpath "D:\ReVolt"

## In the 3D view

Pick a car once: it stays locked until Change car (Ctrl+O). Click a part in the view or the Parts list to select it,
and use its handles: Move (W), Rotate (E), Scale (R). Rotate and Scale change the model itself, with all its LODs, and the
radius or length that goes with its size. Right drag or dragging on nothing turns the view, middle drag pans, the wheel
zooms. Arrow keys and PgUp/PgDn nudge, Shift for bigger steps. 1 2 3 4: 3D, top, front, side. F: frame the car.

Add a part, Replace, Remove and Hide are in the Parts list and on the selected part's page. Hide only changes the view.

Number boxes take typing, their arrows, Up and Down, or the wheel. Shift: bigger steps, Ctrl: finer.

The Corners buttons say what happens to the other corners:

| | |
|---|---|
| Single | only this one |
| Mirror L/R | the other side, mirrored |
| Front/Back | the other end, mirrored: changes the wheelbase |
| All four | all corners, mirrored both ways |

Crosses in the view are the centre of mass, weapon points, aerial, cameras and suspension mounts: they can be dragged too.

## Look

View, Theme (or the button at the end of the toolbar) has Dark, Light and Classic. Dark is what a new install starts with: black,
with the orange of the icon. Light is ghost white. Classic keeps the blue on slate it had before. The 3D view, the dialogs and
the small pictures follow, and the choice is remembered.

## Guide

The Guide tab (F1) shows the open car's numbers, worked out from the formulas of the original car physics and updated as you
edit: real top speed, time to top speed, balance, how far the springs sit down, bounce and damping, grip, turning, what to look
at. Try it shows what a change would do before you make it. There's a unit converter and a search through what each value does.

## Race

Race here (F7, or the Run tab) puts you against CPU cars on a built-in road: Oval, Park or Hairpins, with stock cars closest in
speed to yours, or all copies of your own. Every car uses the same physics as Drive here. The CPU driver reads the road's bends
and plans its speed; its corrections for sliding work as the game's do, from each car's own AI values, so you can watch them at
work, also on your own car (I lets the AI drive). The driver, road and walls are this editor's own, not the game's: AI nodes,
track polygons and materials aren't here, so times don't compare with RVGL.

## Shading and normals

Each polygon stores a colour per corner, so shading can look faceted or smooth on any model without changing it.
- *Tools > Auto shade* lights the car as a clay render and bakes it into those colours: soft light from the sky, darker
  where the car's own shape hides it, contact shadows where it meets the floor, and a soft light from above.
- *The whole car blocks light*: wheels shade the body and the body shades the wheels.
- *Controls*: Depth (how dark corners and shadows get), Brightness, and Surface (smooth with sharp edges kept, smooth,
  faceted, or the model's normals).
- It shows as you move them, and a full-quality bake takes a fraction of a second.

Normals are separate: the game takes reflections and its moving lights from them. A vertex holds one normal, so sharp edges need
two vertices in one place. Rewrite the normals offers smooth, by angle or by faces, and says how many vertices it took. Show has Vertex normals.

## Packs

RVGL stacks folders: the root, then the packs in `packs\default.txt`, later ones winning. Stock cars normally come from
`packs\game_files`, so a file in `cars\<stock car>` is ignored by the game. The Studio reads files the way RVGL does and
never writes into a read-only pack: a stock car's saved changes go to `packs\local`, on top. File has the way back.

## Installing a downloaded car

File, *Install a car from a zip or folder*, or drop the zip or folder on the window. It first shows what's inside: each car,
the folder it gets, and what's wrong (paths naming another folder, missing files, files that aren't part of a car, a folder
name that's taken). Cars go into the root's `cars` folder. Nothing that exists is replaced: when the name is taken, by a
stock car too, the car gets another folder and only the paths in its parameters.txt change. Entries that would land outside
the game folder are left out. *Undo the last install* deletes exactly the files that install made; files you changed since
are kept unless you say otherwise. The notes for that are in `.rvcarstudio\installs` in the game folder.

## Starting a car

**New car** asks what you're making. *Repaint*: new colours on a car that exists, opens on the Paint tab. *Remodel*: start
from a car and change its settings, parts or models. *Original car*: your own body, one wheel for all four (the right side
mirrored) and a texture. Models can be .prm or .obj, the texture .bmp, .png or .jpg. Wheels are placed, sizes fitted and a first hull made. The handling comes from the base car.

**Paint** shows hue, colour and brightness on the car as you move them, for all colours or only the ones you click.
Keep it as a new skin or over the texture. *Open the texture and watch it* opens your image editor, and each save there
shows here.

**UV and texture editor** (U, or Paint > *Open the UV and texture editor*) shows the texture with a part's faces laid
over it, and the car in the 3D view changes as you work.
- *Faces*: click, Shift, Ctrl, a box, or double click for a whole piece. Drag them to move, use the corners to size and
  the round handle to turn. You can also flip, snap to pixels, fit to the page, project from the side, top or front,
  *Unwrap by sides*, and pack the pieces.
- *Save the layout as a picture* gives a template to paint over in another editor.
- *Painting*: brush, an eraser that brings the original back, fill and colour pick, optionally kept inside the selected
  faces.
- *Generate*: plain colour, gradient, racing stripes, checks, carbon fibre, grunge, a number plate, panel lines along the
  faces, a colour per piece, light and shade. *Make a livery* does a whole paint job in one click.
- *Keeping it*: the painting is saved as a new skin or over the car's texture. The faces' places are saved with Save
  (Ctrl+S).

**Stock cars:** the first change offers a copy that keeps what you've done. The game's own files stay as they are.
**Shared files:** before changing a model or hull other cars use, or one outside the car's folder, it asks: your own copy,
change it for all, or cancel.

**Collision hull:** it follows the body when you move, rotate or scale it (a switch on the body's page turns that off).
*Show and edit the hull* lists its parts and spheres. Parts move, turn and scale as a whole, so they stay solid. Spheres
move and resize, can be added and removed, and with Mirror L/R the one on the other side follows.

## Other programs and other cars

- **.obj out and in** (File menu): *Export the selected part* or *the whole car* (each part where it stands, one object
  each, to model around), and *Import a model from .obj for the selected part*.
  - Scale and axes: 1 metre in the .obj is 100 game units, with Y up and the front towards -Z, as Blender's Re-Volt add-on
    uses. The texture comes along in a .mtl.
  - Import: polygons over 4 corners become triangles. Without normals the model is smooth; without colours it's white,
    ready for Auto shade. It's written as a .prm in the car's folder, named after the .obj.
  - Live link: an imported .obj stays watched. Save it again in Blender and the part changes here, so both can stay open.
- **Compare with another car** (Tools): every value that differs from another car, grouped (engine, steering, mass,
  wheels and grip, springs, AI, looks, menu and stats, files). Tick values, or a whole group, and *Take* them. It's one
  step that undo takes back.

## What it does for you

- Auto shade: a clay-render light baked into the vertex colours: sky, contact shadows, soft key light
- Make shadow: `shadow.bmp` from a top view, and the shadow table around the car
- Make carbox: a picture of the car with its name on white
- Make hull: a first `.hul` from the body (a hull made by hand fits better)
- Fit radius, skid width, spring and axle lengths to the models. Inertia from the shape. Place wheels on a new body. Symmetry
- Readme, zip, new car from another, rename a car's folder and its paths
- Skins: previews, and new ones from brightness, colour and hue
- A live check of the car, under the view

Saving changes only the lines you changed. Comments, `;)` lines, unknown sections and the checksum of stock files stay
as they are. The first version of each file stays next to it as `.bak`.

## RVGL additions it knows

`;)` lines, `TCARBOX`, `TSHADOW`, `SHADOWINDEX`, `SHADOWTABLE`, `SFXENGINE`, `SFXHONK`, `SFXSERVO`, `CPUSelectable`,
`Statistics`, `Flippable`, `Flying`, `ClothFx`, `WeaponOffset1`, `WeaponOffset2`, `Camber`, the `CAMATTACHED` section with
`FixedOffset` and `FixedLook`, spinner `Type`, `Trans` and `TransVel`, Super Pro rating, skins by suffix.

## Trying the car

**Test run** (F5) saves, starts RVGL in a window and goes into a race with your car, with the cars, laps and track you
chose in the Run tab. RVGL has no command for that, so it goes through the menus with key presses (Windows, for now), and only
while RVGL's window is in front. It has its own settings folder, so your profile and best times aren't touched, and it enters
the CARNIVAL code there so locked cars can be used. In dev mode it stays at the menu: Ctrl+R in the game reloads the car after you
save here, and Calc Car Stats gives the exact numbers.

**Drive here** (F6) is a simulation from the parameters, on a floor, bumps or a ramp. R restarts. **Automatic runs** report
acceleration, a hard turn and bumps. It's a new implementation of the behaviour of the original game's car physics in Arcade mode, with nothing fitted:
8 ms steps, the same tyre, spring and hull sphere impulses, spheres against polygons with the game's surfaces. Over 28 stock cars,
top speed is off by 0.6% on average and time to top speed by about 5%. RC Bandit was also timed against real RVGL on a flat level, by
its speedometer: acceleration, coasting and the steady full-lock circle (10 mph in both) agree within the speedometer's 1 mph steps.
Not here: AI nodes, pickups, weapons, the game's reposition timing, and track files. Other cars haven't been timed against RVGL,
so Test run stays the final check. Space flips the car when it's nearly upside down, as in the game.

## Other systems

Files are found whatever the case of their path, as Linux needs, and shaders are made for both OpenGL ES and desktop OpenGL.

    dotnet publish RvStudio.App -c Release -r linux-x64
    dotnet publish RvStudio.App -c Release -r osx-arm64

These build, but have not been run on those systems yet.

## Tests

Saving tests write: use a copy of the Re-Volt folder.

    dotnet run --project RvStudio.Tests -- read  "D:\ReVolt"     writes nothing: every car and model read and "saved" unchanged
    dotnet run --project RvStudio.Tests -- write <copy>          saving, new car, rename, tools
    dotnet run --project RvStudio.Tests -- fit   "D:\ReVolt"     the simulator against stock cars
    RvCarStudio -rvpath <copy> --auto <folder for pictures>      the real window: every car, tab, tool, mouse on the view, drive
    RvCarStudio -rvpath <folder> --look <folder for pictures>    only the looks: every page, dialog and menu in Dark and Light
