# RV Car Studio

A car editor for [Re-Volt](https://re-volt.gitlab.io/rvgl-docs/) / RVGL: browse your cars, see them in 3D,
edit their parameters, check them and pack them for sharing. VB.NET, Windows Forms, OpenTK.
By Kallel A.Y (theKDL). GPL v3.

## Building

Open `RvCarStudio.sln` in Visual Studio, or:

    MSBuild RvCarStudio.sln /p:Configuration=Debug

It targets .NET Framework 4.8. OpenTK and QuickFont are in `RvCarStudio/lib`.

## Using it

The first start asks for your Re-Volt folder (the one holding `cars`). It's kept in `%APPDATA%\RVCS\SETTINGS`.

    RvCarStudio.exe -rvpath "D:\ReVolt"      use this folder for this run only
    RvCarStudio.exe -config                  ask for the folder again

**Saving** changes only the lines of `parameters.txt` whose value you changed. Comments, `;)` RVGL lines,
unknown sections and the checksum at the end of stock files stay as they are. The first version of a file is
kept next to it as `parameters.txt.bak` (models and textures too, when shaded or recoloured).

**Modes** of the editor: Main, Texture, Handling, Frontend, Body, Wheels, Suspensions, Spinner, Aerial, AI, Camera,
Collision (links the hull file), Carbox, Shade, Shadow, Misc, Readme (generator), Packing (zip), About, Console.

The search box of the car list takes names, several separated by commas, and `#stock` or `#custom`.

## 3D view

Left drag turns around the car, right drag moves, the wheel zooms. Models are drawn with their texture and vertex
colours, solid polygons first, then see-through ones. Springs and axles go from their mount to the wheel and stretch
by distance / Length, pins hang from the mount without stretching.

Not drawn: environment reflections, the shadow, and the aerial's wobble.

## Tests

`RvCarStudio.Tests` runs on **a copy** of a Re-Volt folder:

    RvCarStudio.Tests core <copy>                  file round trips, checker, new car, rename, zip, readme
    RvCarStudio.Tests gui  <copy> <shots> [cars]   the real windows: every car, mode and button, with screenshots
    RvCarStudio.Tests real <Re-Volt folder>        nothing is written: is every car read and "saved" unchanged?
