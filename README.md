<div align="center">
    <a href="https://github.com/deathkiller/jazz2"><img src="https://raw.githubusercontent.com/deathkiller/jazz2/master/Docs/Logo.gif" alt="Jazz² Resurrection" title="Jazz² Resurrection"></a>
</div>

<div align="center">
    Open-source <strong>Jazz Jackrabbit 2</strong> reimplementation
</div>

<div align="center">
  <sub>
    Brought to you by <a href="https://github.com/deathkiller">@deathkiller</a>
  </sub>
</div>
<hr/>


## Introduction
Jazz² Resurrection is reimplementation of the game **Jazz Jackrabbit 2** released in 1998. Supports various versions of the game (Shareware Demo, Holiday Hare '98, The Secret Files and Christmas Chronicles). Also, it partially supports some features of JJ2+ extension and MLLE. Further information can be found [here](http://deat.tk/jazz2/).

[![Build Status](https://img.shields.io/appveyor/ci/deathkiller/jazz2/master.svg?logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0iI2ZmZmZmZiIgZD0iTTI0IDIuNXYxOUwxOCAyNCAwIDE4LjV2LS41NjFsMTggMS41NDVWMHpNMSAxMy4xMTFMNC4zODUgMTAgMSA2Ljg4OWwxLjQxOC0uODI3TDUuODUzIDguNjUgMTIgM2wzIDEuNDU2djExLjA4OEwxMiAxN2wtNi4xNDctNS42NS0zLjQzNCAyLjU4OXpNNy42NDQgMTBMMTIgMTMuMjgzVjYuNzE3eiI+PC9wYXRoPjwvc3ZnPg==)](https://ci.appveyor.com/project/deathkiller/jazz2)
[![Latest Release](https://img.shields.io/github/release/deathkiller/jazz2.svg)](https://github.com/deathkiller/jazz2/releases)
[![Code Quality](https://img.shields.io/codacy/grade/7ef344d34def41a9b36e4a083f8b9542.svg)](https://www.codacy.com/app/deathkiller/jazz2)
[![License](https://img.shields.io/github/license/deathkiller/jazz2.svg)](https://github.com/deathkiller/jazz2/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/355651795390955520.svg?color=839ef7&label=chat&logo=discord&logoColor=ffffff&labelColor=586eb5)](https://discord.gg/Y7SBvkD)

This project uses parts of [Duality - A 2D GameDev Framework](https://www.duality2d.net/).


## Preview
<div align="center">
    <img src="https://raw.githubusercontent.com/deathkiller/jazz2/master/Docs/Screen2.gif" alt="Preview">
</div>

<div align="center"><a href="https://www.youtube.com/playlist?list=PLfrN-pyVL7k6n2VJF197F0yVOZq4EPTsP">:tv: Watch gameplay videos</a></div>


## Running the application
### Windows / Linux / macOS
* Download **Desktop** release (or build the solution and copy `Content` directory to `‹Game›/Content`)
* Run `‹Game›/Import.exe "Path to JJ2"` (or drag and drop JJ2 directory on `Import.exe`)
  * On Linux and macOS, you can run `mono Import.exe "Path to JJ2"`
* Run `‹Game›/Jazz2.exe`
  * On Linux and macOS, you can run `mono Jazz2.exe`

`‹Game›` *is path to Jazz² Resurrection. You can run* `Import.exe` *without parameters to show additional options.*

### Android
* Download **Desktop** and **Android** release (or build the solution and copy `Content` directory to `‹Game›/Content`)
* Run `‹Game›/Import.exe "Path to JJ2"` (or drag and drop JJ2 directory on `Import.exe`)
* Copy `‹Game›/Content` directory to `‹SDCard›/jazz2.android/Content` 
  * Create empty file `.nomedia` in `‹SDCard›/jazz2.android` to hide files from Android Gallery
* Install `Jazz2.apk` on Android device
* Run the application

*Requires device with Android 4.4 (or newer) and OpenGL ES 3.0 support.* `‹SDCard›` *could be internal or external storage.
The application tries to autodetect correct path. Also, you can use* `‹SDCard›/Android/Data/Jazz2.Android` *or* `‹SDCard›/Download/Jazz2.Android` *instead.*

### WebAssembly
* Go to [Jazz² Resurrection WebAssembly](http://deat.tk/jazz2/wasm/) to play **Shareware Demo**
  * Or build the solution and copy `Content` directory from **Desktop** release

*Requires Google Chrome 57 (or newer), Firefox 53 (or newer) or other browser supporting WebAssembly and WebGL.*


## Dependencies
### Windows
* .NET Framework 4.5.2 (or newer) / [.NET Core 3.0](https://dotnet.microsoft.com/download) `experimental`
* [OpenAL Soft](https://github.com/opentk/opentk-dependencies) (included in release)
  * Copy `x86/openal32.dll` to `‹Game›/Extensions/OpenALSoft.x86.dll`
  * Copy `x64/openal32.dll` to `‹Game›/Extensions/OpenALSoft.x64.dll`
* [libopenmpt](https://lib.openmpt.org/libopenmpt/download/) (included in release)
  * Copy `libopenmpt.dll` (*x86*, and its dependencies) to `‹Game›` directory

### Linux
* [Mono 5.0 (or newer)](http://www.mono-project.com/download/#download-lin) / [.NET Core 3.0](https://dotnet.microsoft.com/download) `experimental`
* OpenAL
  * Run `sudo apt install openal1` if it's missing
* [libopenmpt](https://lib.openmpt.org/libopenmpt/download/) (included in release)
  * Copy `libopenmpt.so` (*x86*, and its dependencies) to `‹Game›` directory

### macOS
* [Mono 5.0 (or newer)](http://www.mono-project.com/download/#download-mac) / [.NET Core 3.0](https://dotnet.microsoft.com/download) `experimental`
* OpenAL should be already installed by OS
* [libopenmpt](https://lib.openmpt.org/libopenmpt/)
  * Copy `libopenmpt.dylib` (*x86*, and its dependencies) to `‹Game›` directory

### Android
* Xamarin
* [OpenAL Soft](https://github.com/kcat/openal-soft) (included for *armeabi-v7a* and *x86*)
* [libopenmpt](https://lib.openmpt.org/libopenmpt/download/) (included for *armeabi-v7a* and *x86*)

### WebAssembly
* .NET Framework 4.5.2 (or newer) / Mono 5.0 (or newer)
* `Mono.WebAssembly.Sdk` (included as NuGet)
* [WebGL.NET](https://github.com/WaveEngine/WebGL.NET) (included)

## Building the solution
### Windows
* Open the solution in [Microsoft Visual Studio 2017](https://www.visualstudio.com/) (or newer) and build it
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`
* Copy dependencies to `/Jazz2/Bin/Debug/` or `/Jazz2/Bin/Release/`
* If you build Release configuration, you have to replace `Debug` with `Release` in paths above

### Linux
* Install [Mono 5.0 (or newer)](http://www.mono-project.com/download/#download-lin)
* Run `msbuild` in directory with the solution file (.sln):
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`
* Obtain and copy `libopenmpt.so` to `/Jazz2/Bin/Debug/libopenmpt.so` to enable music playback
* Then you can rebuild the solution only with `msbuild` command
* Use `msbuild /p:Configuration=Release` to build Release configuration, you have to replace `Debug` with `Release` in paths above

### macOS
* Install [Mono 5.0 (or newer)](http://www.mono-project.com/download/#download-mac)
* Open the solution in [Microsoft Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) and build it
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`
* Obtain and copy `libopenmpt.dylib` to `/Jazz2/Bin/Debug/libopenmpt.dylib` to enable music playback
* If you build Release configuration, you have to replace `Debug` with `Release` in paths above

***.NET Core 3.0** build can be compiled in a similar way. Use* `Jazz2.DotNetCore` *project for this. Errors about* `Jazz2.Android` *project can be ignored, if the Android build is not needed.*

### Android
* Install **Mobile development in .NET** for Microsoft Visual Studio 2017 (or newer)
* Open the solution and build `Jazz2.Android` project
* Dependencies are already included for common configurations

### WebAssembly
* Open the solution and build `Jazz2.Wasm` project
* Dependencies are already included for common configurations


## Extensions
### OpenGL ES 2.0 (Experimental)
Alternative OpenGL ES 2.0 backend can be built separately. It does not contain all features
that are available in default OpenGL 2.1 backend, but it should run faster on low-end configurations.
Don't use it if you have no reason to do so!

To use it, build `Extensions/Es20Backend` project. The library will be copied to
`/Jazz2/Bin/Debug/Extensions/Es20Backend.core.dll` automatically.
Then copy all files from `Content/_ES20` directory to `/Jazz2/Bin/Debug/Content` and replace them.

Also, you have to remove `/Jazz2/Bin/Debug/Extensions/GL21Backend.core.dll` file to disable default OpenGL 2.1 backend.


## License
This project is licensed under the terms of the [GNU General Public License v3.0](./LICENSE).