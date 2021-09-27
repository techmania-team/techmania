# TECHMANIA
An open source rhythm game, written in Unity, playable with or without a touchscreen.

[Download for Windows](https://github.com/techmania-team/techmania/releases)

Download for iOS/iPadOS (link pending)

[Trailer](https://www.youtube.com/watch?v=hcqb0Rwm1xY)

[Discord](https://discord.gg/K4Nf7AnAZt)

[Official website](https://techmania-team.herokuapp.com/)

## Licensing
All code and assets are released under the [MIT License](LICENSE), with the following exceptions:
* Sound effects in [TECHMANIA/Assets/Sfx](TECHMANIA/Assets/Sfx) are acquired from external resources, which use different licenses. Refer to [TECHMANIA/Assets/Sfx/Attributions.md](TECHMANIA/Assets/Sfx/Attributions.md) for details. Please note that some licenses prohibit commercial use.
* Some included tracks in the releases are under separate licenses:
  * f for fun is released under the [CC BY-NC-ND 4.0 License](https://creativecommons.org/licenses/by-nc-nd/4.0/).
  * Yin-Yang Specialist (MUG ver) is released under the [CC BY-NC-NA 4.0 License](https://creativecommons.org/licenses/by-nc-sa/4.0/).
  * v (Game Mix) is released under the [CC BY-NC-SA 4.0 License](https://creativecommons.org/licenses/by-nc-sa/4.0/).

## Roadmap and progress
Refer to the [Kanban](https://github.com/techmania-team/techmania/projects/1).

## Manual and documentation
Refer to the [documentation website](https://techmania-team.github.io/techmania-docs/).

## Platform
The game is designed for Windows PCs, with the Touch control scheme requiring a touchscreen monitor. Patterns using other control schemes are playable with a mouse and keyboard.

We also provide technical support for iOS/iPadOS, in that we are able to respond to bug reports and some feature requests. However please be aware of the following:

- The game's UI is designed for PC and may be difficult to navigate on a phone;
- The game's difficulty is tuned for PC and may prove too difficult for mobile players;
- Some features in the included editor require mouse and keyboard, and therefore are unavailble to mobile users.

[Builds for other platforms](#platform-specific-forks) also exist, but are not officially supported at the moment.

## Content policy
Per the MIT license, you are free to produce any Content with TECHMANIA, including but not limited to screenshots, videos and livestreams. Attributions are appreciated, but not required. However, please keep the following in mind:
* If your Content features 3rd party music, it may be subject to copyright claims and/or takedowns. You may not hold TECHMANIA responsible for the resulting losses.
* If your Content is publicly available and features any unofficial [skin](https://github.com/techmania-team/techmania-docs/blob/main/English/Skins.md), you must clearly state so in the description of your Content, to avoid potential confusion.
* If your Content is commercial, additional limitations apply:
  * Your Content cannot feature the official tracks f for fun, Yin-Yang Specialist (MUG ver) and v (Game Mix).
  * Your Content cannot feature the [Fever sound effect](TECHMANIA/Assets/Sfx/Fever.wav). You can swap the sound with one that allows commercial use, make a custom build, and produce Content from that build.

## Feedback
For technical issues, read the [contribution guidelines](CONTRIBUTING.md), then submit them to [Issues](https://github.com/techmania-team/techmania/issues).

For general discussions, head to [Discord](https://discord.gg/K4Nf7AnAZt).

## Making your own builds
Follow the standard building process:
* Install Unity, making sure your Unity version matches this project's. Check the project's Unity version at [ProjectVersion.txt](TECHMANIA/ProjectSettings/ProjectVersion.txt).
* Clone this repo, then open it from Unity.
* File - Build Settings
* Choose your target platform, then build.

Please note that the default skins are not part of the project, so you'll need to copy the `Skins` folder from an official release into your build folder, in order for your build to be playable.

If the build fails or produces a platform-specific bug, you can submit an issue, but we do not guarantee support.

## Platform-specific forks
* rogeraabbccdd's iOS builds: https://github.com/rogeraabbccdd/techmania/releases
* MoonLight's Android builds: https://github.com/yyj01004/techmania/releases
* fhalfkg's macOS builds: https://github.com/fhalfkg/techmania/releases
* samnyan's Android build on 0.2: https://github.com/samnyan/techmania/releases
