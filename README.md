# TECHMANIA
An open source rhythm game for Windows, written in Unity, playable with or without a touchscreen.

[Announcement trailer](https://www.youtube.com/watch?v=hcqb0Rwm1xY)

[Subreddit](https://www.reddit.com/r/TechMania/)

[Discord](https://discord.gg/K4Nf7AnAZt)

[Official website](https://techmania-team.herokuapp.com/)

Head to [Releases](https://github.com/techmania-team/techmania/releases) to download the released versions.

## Licensing
All code and assets are released under the [MIT License](LICENSE), with the following exceptions:
* Sound effects in [TECHMANIA/Assets/Sfx](TECHMANIA/Assets/Sfx) are acquired from external resources, which use different licenses. Refer to [TECHMANIA/Assets/Sfx/Attributions.md](TECHMANIA/Assets/Sfx/Attributions.md) for details. Please note that some licenses prohibit commercial use.
* Some included tracks in the releases are under separate licenses:
  * f for fun is released under the [CC BY-NC-ND 4.0 License](https://creativecommons.org/licenses/by-nc-nd/4.0/).
  * Yin-Yang Specialist (MUG ver) is released under the [CC BY-NC-NA 4.0 License](https://creativecommons.org/licenses/by-nc-sa/4.0/).

## Roadmap and progress
Refer to the [Kanban](https://github.com/techmania-team/techmania/projects/1).

## Manual and documentation
Refer to the [documentation website](https://techmania-team.github.io/techmania-docs/).

## Platform
The current target is Windows PCs, with the Touch control scheme requiring a touchscreen monitor. Patterns using other control schemes are playable with a mouse and keyboard. Due to the associated cost and lack of hardware, we do not officially support Linux, macOS, Android or iOS/iPadOS.

## Content policy
Per the MIT license, you are free to produce any Content with TECHMANIA, including but not limited to screenshots, videos and livestreams. Attributions are appreciated, but not required. However, please keep the following in mind:
* If your Content features 3rd party music, it may be subject to copyright claims and/or takedowns. You may not hold TECHMANIA responsible for the resulting losses.
* If your Content is publicly available and features any unofficial [skin](https://github.com/techmania-team/techmania-docs/blob/main/English/Skins.md), you must clearly state so in the description of your Content, to avoid potential confusion.
* If your Content is commercial, additional limitations apply:
  * Your Content cannot feature the official tracks f for fun and Yin-Yang Specialist (MUG ver).
  * Your Content cannot feature the [Fever sound effect](TECHMANIA/Assets/Sfx/Fever.wav). You can swap the sound with one that allows commercial use, make a custom build, and produce Content from that build.

## Feedback
For technical issues, read the [contribution guidelines](CONTRIBUTING.md), then submit them to [Issues](https://github.com/techmania-team/techmania/issues).

For general discussions, head to the [TECHMANIA subreddit](https://www.reddit.com/r/techmania) or [Discord](https://discord.gg/K4Nf7AnAZt).

## Making your own builds
While other OSes are not supported, it may be possible to build the project on these platforms. Follow the standard building process:
* Install Unity, making sure your Unity version matches this project's. Check the project's Unity version at [ProjectVersion.txt](TECHMANIA/ProjectSettings/ProjectVersion.txt).
* Clone this repo, then open it from Unity.
* File - Build Settings
* Choose your target platform, then build.

Note that the default skins are not part of the project, so you'll need to copy the `Skins` folder from an official release into your build folder, in order for your build to be playable.

If the build fails or produces a platform-specific bug, you can submit an issue, but I do not guarantee support.

There are a few unofficial builds available:
* fhalfkg's macOS builds: https://github.com/fhalfkg/techmania/releases
* rogeraabbccdd's iOS builds: https://github.com/rogeraabbccdd/techmania/releases
* MoonLight's Android builds:
  * 0.7: https://drive.google.com/file/d/11jgs4E46cm6swlt6CN4j7kkwjljSiDdj/view?usp=sharing
  * 0.6: https://drive.google.com/file/d/18S81J4U3DN5BNEHQe4b5vxKH6YoCmYe2/view?usp=sharing/
* samnyan's Android build on 0.2: https://github.com/samnyan/techmania/releases
