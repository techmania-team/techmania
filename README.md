# TECHMANIA
An open source rhythm game for Windows, written in Unity, playable with or without a touchscreen.

Head to [Releases](https://github.com/techmania-team/techmania/releases) to download the released versions.

## Licensing
All code and assets are released under the [MIT License](LICENSE), with the following exceptions:
* Sound effects in [TECHMANIA/Assets/Sfx](TECHMANIA/Assets/Sfx) are acquired from external resources, which use different licenses. Refer to [TECHMANIA/Assets/Sfx/Attributions.md](TECHMANIA/Assets/Sfx/Attributions.md) for details. Please note that some licenses prohibit commercial use.
* Included tracks in the releases may be under separate licenses. Refer to the release notes on each release for details.

## Roadmap and progress
Refer to the [Kanban](https://github.com/techmania-team/techmania/projects/1).

## How to play
The game includes tutorials, but you can also find textual instructions in the [Wiki](https://github.com/techmania-team/techmania/wiki/How-to-play).

## How to edit
The included editor is hopefully self-explanatory, but you can find additional notes in the [Wiki](https://github.com/techmania-team/techmania/wiki/Additional-notes-on-the-editor).

## Platform
The current target is Windows PCs, with the Touch control scheme requiring a touchscreen monitor. Patterns using other control schemes are playable with a mouse and keyboard. Due to the associated cost and lack of hardware, I will not target Linux, macOS, Android or iOS/iPadOS.

The game may be ported to WebGL in the future so it can be played on any web-enabled device. However there are no concrete plans to make such a port at this moment.

## Feedback
For technical issues, read the [contribution guidelines](CONTRIBUTING.md), then submit them to [Issues](https://github.com/techmania-team/techmania/issues).

For general discussions, head to the [TECHMANIA subreddit](https://www.reddit.com/r/techmania).

## Making your own builds
While other OSes are not supported, it may be possible to build the project on these platforms. Follow the standard building process:
* Install Unity, making sure your Unity version matches this project's. Check the project's Unity version at [ProjectVersion.txt](TECHMANIA/ProjectSettings/ProjectVersion.txt).
* Clone this repo, then open it from Unity.
* File - Build Settings
* Choose your target platform, then build.

If the build fails or produces a platform-specific bug, you can submit an issue, but I do not guarantee support.

A running build on Android can be found on [samnyan's fork](https://github.com/samnyan/techmania/releases).
