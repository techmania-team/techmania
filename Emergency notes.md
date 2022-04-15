Today is February 27, 2022. In case the current conflicts turn into a global one involving mushroom clouds and I don't live to complete this project, I leave the following notes to whoever picks it up.

# Internal idea list

Aside from the online kanban I have kept a private idea list in my OneNote, with the more nuanced stuff. Here is a copy of said list on the day of writing this file:

* Skin ideas
  * Add complete VFX for hold heads
  * Separate VFX for basic and chain
  * Texture tiling in trails and paths
* Don't show welcome mat when returning from editor
* Don't abort gameplay when BGA fails to load
* Sort skins by name as some platforms don't iterate folders in alphabetical order
* Add Noisysundae to developer list
* Game crashes when Discord is started with admin privileges
* Editor: mirror button
* Commits on sprite flipping:
  * https://github.com/Noisysundae/techmania-ns/commit/bedd46240146a2a9992a6641906faad1f08696f6
  * https://github.com/Noisysundae/techmania-ns/commit/175cef535b04e90c1441896cc025c3bc17310818

# Official track leads

Likewise I have kept a list of leads for new official tracks. I intend to pursue them in the order I acquired them.

* Hubaswift
  * A friend of Panda's
  * Two tracks to potentially use
  * https://www.youtube.com/watch?v=D60uYBUWihU
  * https://www.youtube.com/watch?v=XOBGjF61hDY
  * Contact via Panda
  * "wouldn't mind giving us the license for free, but would like a small fee if possible"
  * I like "Brain Station" Hubaswift - Brain Station
* himmel.tengoku
  * One of Kento's favorite composers
  * https://soundcloud.com/himmeltengoku
  * https://twitter.com/himmeltengoku
  * https://www.facebook.com/tianle.chenpan/
* SOTUI
  * Composer for various rhythm games
  * https://soundcloud.com/sotuiofficial
  * info@sotuiofficial.com
  * https://www.youtube.com/SOTUI
* Jehezukiel
  * Composer for various rhythm games including PIU
* MoAE
  * Kento found this person, accepting commissions
  * https://twitter.com/MoA_E_
* Enochii
  * Discord member who happened to be a composer
  * https://www.youtube.com/channel/UCjAmHPQh3XebaMZZ7AF3pmg

# Short- and long-term plans

* If 2.0 takes too long, finish up the following features and release 1.0.3:
  * Discord rich presence
    * Add an option to turn it off, for privacy purposes
    * Localization?
  * Sprite flipping
    * Possibly a new parameter in skins
    * Start from Noisysundae's commits in an earlier section
* UI Toolkit is currently unable to render sprites with additive shader, which many VFX skins rely on
  * Until Unity adds support for this, 2.0 will be blocked
    * Maybe migrate editor to UI Toolkit while waiting for this support? Because editor is currently planned to remain on UGUI
  * Unity lists "Blend Modes" as "Under Consideration" in its [roadmap](https://unity.com/roadmap/unity-platform/gameplay-ui-design). If by some miracle this is implemented, it can be an easy way out
* How themes are currently planned to work
  * A theme is an asset bundle.
  * Whenever entering play mode in Unity, there's a script to build everything in `Assets/UI` into `Assets/AssetBundles/default`; this bundle is then copied to the build folder when building. The game will load from `Assets/AssetBundles/default` when it detects that it's running within the Unity editor.
  * Theme developers will be told to clone the project and develop their theme by modifying the default one.
  * 2 files are required: `Assets/UI/MainTree.uxml`, containing the visual tree; `Assets/UI/MainScript.txt`, containing the initial script. The load screen will load these 2 files by hardcoded name. There will be an API for a script to load and execute another script.
  * Lua scripts are in `.txt` files because `.lua` is not recognized by Unity as a text asset, and thus will not be included in asset bundles.
  * `Assets/Scripts/Theme API` contains C# classes intended to be exposed to Lua scripts. There will be classes that wrap around Unity classes such as `Time`, UI classes such as `VisualElement`, and TECHMANIA classes such as `Options`.
