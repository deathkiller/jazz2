# ![Jazz² Resurrection](https://github.com/deathkiller/jazz2/raw/master/Docs/Logo.gif)
Remake of game **Jazz Jackrabbit 2** from year 1998. Supports various versions of the game (Shareware Demo, Holiday Hare '98, The Secret Files and Christmas Chronicles). Also partially supports JJ2+ extension.


## Dependencies
### Windows
* .NET Framework 4.5.2 or newer
* [OpenALSoft](https://github.com/opentk/opentk-dependencies) - Copy `x86/openal32.dll` to `‹Game›/Extensions/OpenALSoft32.dll` and `x64/openal32.dll` to `‹Game›/Extensions/OpenALSoft64.dll`
* [libopenmpt](http://lib.openmpt.org/libopenmpt/) - Copy `libopenmpt.dll` to `‹Game›` directory

### Linux
* Mono
* [libopenmpt](http://lib.openmpt.org/libopenmpt/) - Copy `libopenmpt.so` to `‹Game›` directory

### Android
* Xamarin
* [libopenmpt](http://lib.openmpt.org/libopenmpt/)

Requires [Microsoft Visual Studio 2017](https://www.visualstudio.com/) (or equivalent Mono compiler) to build the solution.

## Running the application
* Build the solution
* Copy `Content` directory to `‹Game›/Content`
* Run `Import.exe "Path to JJ2"` (or drag and drop JJ2 directory on `Import.exe`)
* Run `Jazz2.exe`

## License
This software is licensed under the [GNU General Public License v3.0](./LICENSE).