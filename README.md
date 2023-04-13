# BeatSaberMapRecommender

This mod will give you map recommendations based on Beat Saver tags and meta data similarities.

After selecting a map, a button will appear.
![recommendation button](https://i.imgur.com/xfGn6q1.png)

When clicking this button a menu will appear with the recommended maps.<br>

On the left side you get a list of maps recommended by the mod.<br>
Above this list there are multiple buttons to sort the list on different similarities.

On the right side you get an overview of the details of the selected map.<br>
Below you get the tags and the similarity score of the selected map.<br>

When you're happy with the selected map you can proceed to click play.

![recommendation menu](https://i.imgur.com/SchTl2I.png)

## Dependencies
This mod depends on the following Libraries that are available on [ModAssistant](https://github.com/Assistant/ModAssistant):
- BSML
- SiraUtil
- SongCore
- BeatSaverSharp

## Installation
1. Install the above dependencies.
2. Grab the latest plugin release from the [releases page](https://github.com/GianniKoch/BeatSaberMapRecommenderMod/releases).
3. Drop the .dll file in the Plugins folder of your Beat Saber installation.
4. Boot it up (or reboot).

## Developers
To build this project you will need to create a `BeatSaberMapRecommender/BeatSaberMapRecommender.csproj.user` file specifying where the game is located:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Change this path if necessary. Make sure it doesn't end with a backslash. -->
    <BeatSaberDir>C:\Program Files (x86)\Steam\steamapps\common\Beat Saber</BeatSaberDir>
  </PropertyGroup>
</Project>
```

## Credits
The projects below have been a guideline in making this mod.<br>
I started this mod from the [MorePrecisePlayerHeight](https://github.com/ErisApps/MorePrecisePlayerHeight) project by [Eris](https://github.com/ErisApps).<br>
I've used some implementations of the [Cherry](https://github.com/Auros/Cherry) project by [Auros](https://github.com/Auros/) to download and select maps.

