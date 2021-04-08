# TECHMANIA
An open source rhythm game for Windows, written in Unity, playable with or without a touchscreen.

[Announcement trailer](https://www.youtube.com/watch?v=hcqb0Rwm1xY)

[Subreddit](https://www.reddit.com/r/TechMania/)

[Discord](https://discord.gg/K4Nf7AnAZt)

[Official website](https://techmania-team.github.io/#/)

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
Refer to the [documentation repo](https://github.com/techmania-team/techmania-docs/).

## Platform
The current target is Windows PCs, with the Touch control scheme requiring a touchscreen monitor. Patterns using other control schemes are playable with a mouse and keyboard. Due to the associated cost and lack of hardware, I will not target Linux, macOS, Android or iOS/iPadOS.

The game may be ported to WebGL in the future so it can be played on any web-enabled device. However there are no concrete plans to make such a port at this moment.

## Feedback
For technical issues, read the [contribution guidelines](CONTRIBUTING.md), then submit them to [Issues](https://github.com/techmania-team/techmania/issues).

For general discussions, head to the [TECHMANIA subreddit](https://www.reddit.com/r/techmania) or [Discord](https://discord.gg/K4Nf7AnAZt).

## Making your own builds
While other OSes are not supported, it may be possible to build the project on these platforms. Follow the standard building process:
* Install Unity, making sure your Unity version matches this project's. Check the project's Unity version at [ProjectVersion.txt](TECHMANIA/ProjectSettings/ProjectVersion.txt).
* Clone this repo, then open it from Unity.
* File - Build Settings
* Choose your target platform, then build.

If the build fails or produces a platform-specific bug, you can submit an issue, but I do not guarantee support.

A running build on Android can be found on [samnyan's fork](https://github.com/samnyan/techmania/releases).
