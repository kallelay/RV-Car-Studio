# RV Car Studio 3

The new version: a car editor for Re-Volt / RVGL with a modern interface and a 3D view you work in directly.
C#, .NET 10, Avalonia and OpenGL. Runs on Windows now, written so Linux and macOS can follow.

    dotnet run --project RvStudio.App
    dotnet run --project RvStudio.App -- -rvpath "D:\ReVolt"

## In the 3D view

Click a part to select it, drag its arrows to move it. Right drag or dragging on nothing turns the view, middle drag
pans, the wheel zooms. Arrow keys and PgUp/PgDn nudge, Shift for bigger steps. 1 2 3 4: 3D, top, front, side. F: frame the car.

The Move buttons say what happens to the other corners:

| | |
|---|---|
| Single | only this one |
| Mirror L/R | the other side, mirrored |
| Front/Back | the other end, mirrored: changes the wheelbase |
| All four | all corners, mirrored both ways |

Crosses in the view are the centre of mass, weapon points, aerial, cameras and suspension mounts: they can be dragged too.

## What it does for you

- Auto shade: soft light baked into the vertex colours, with the shadows of the car's own shape
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

**Test run** (F5) saves and starts RVGL in a window with the car picked. It has its own settings folder, so your profile
and best times aren't touched. RVGL has no way to start a race from outside: pick Start Race. In dev mode, Ctrl+R in the game
reloads the car after you save here, and Calc Car Stats gives the exact numbers.

**Drive here** (F6) is a quick simulation from the parameters, on a floor, bumps or a ramp. **Automatic runs** report
acceleration, a hard turn and bumps. The game's car physics source isn't available, so this is not the game: forces come
from what each parameter means, and two constants are fitted to the stock cars. Over 28 stock cars, top speed is off by
4.5% on average and time to top speed by 14%. It's for feeling a change at once. For the truth, use Test run.

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
