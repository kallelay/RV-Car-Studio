# Third-party notices

RV Car Studio 3 is free software under the GNU General Public License, version 3 (`LICENSE.txt`). Its source code is
in the `RvCarStudio-<version>-source.zip` released with it. Project page: <https://github.com/kallelay/RV-Car-Studio>.

It uses the libraries below.

| Library | What for | Licence |
|---|---|---|
| [.NET runtime](https://github.com/dotnet/runtime) | Bundled in the exe of the portable build | MIT |
| [Avalonia](https://github.com/AvaloniaUI/Avalonia) 11.3 | The window and its controls | MIT |
| [Silk.NET](https://github.com/dotnet/Silk.NET) 2.23 (OpenGL bindings) | Calls into OpenGL | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) 2.88 and [Skia](https://skia.org/) (`libSkiaSharp.dll`) | 2D drawing, pictures | MIT; Skia: BSD 3-Clause |
| [HarfBuzzSharp](https://github.com/mono/SkiaSharp) and [HarfBuzz](https://github.com/harfbuzz/harfbuzz) (`libHarfBuzzSharp.dll`) | Text shaping | MIT; HarfBuzz: MIT-style "Old MIT" |
| [ANGLE](https://chromium.googlesource.com/angle/angle) (`av_libglesv2.dll`) | OpenGL ES over Direct3D | BSD 3-Clause |
| [Inter](https://github.com/rsms/inter) | The interface's typeface | SIL Open Font License 1.1 |
| [MicroCom](https://github.com/kekekeks/MicroCom), [Tmds.DBus.Protocol](https://github.com/tmds/Tmds.DBus) | Used by Avalonia | MIT |

## What is not included

No game data is shipped: no sounds, models, textures, cars or levels. The Studio reads them from your own RVGL folder.
Re-Volt is the property of its owners; RVGL is the work of the RVGL team. The car physics follow the original game's
published source as a description; none of that code is included.
