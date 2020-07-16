# TECHMANIA
An attempt at an open-source clone of a certain dead rhythm game. I believe this is legal; but I will do whatever I can to avoid any legal issues just in case, including not mentioning the name of the original game I'm cloning.

## What I aim to eventually accomplish

TECHMANIA will be a touchscreen-based rhythm game. Stationary notes appear in 1 of 4 lanes, and a few scanlines scan back and forth across the screen. Touch the notes when a scanline passes their center.

Due to the associated cost, and a lack of hardware, I will not target Android or iOS/iPadOS at this moment. The current target is touchscreen-enabled Windows PCs, including but not limited to Surfaces and Surface Studios. If you have a regular PC, you can add a touchscreen to it by connecting a touchscreen monitor. You can also connect an Android tablet of iPad and run the project from Unity using Unity Remote.

The game will include a pattern editor, but I do not have any plan to build a platform for pattern distribution at the moment.

## Control schemes

A touchscreen is not strictly required because the game will support 3 control schemes.

* Touchscreen: touch the notes
* Keyboard/gamepad: each lane is associated with a row of keys or specific buttons on a gamepad, press keys/buttons to hit notes
* Keyboard + mouse: click some kinds of notes with the mouse cursor, press any key for other kinds of notes

## Notes from failed experiments

* InputSystem

It looks promising because it allows hooking up events and provides timestamps on all input events, neither of which the legacy Input system supports.

I thought with it I could get microsecond granularity on input timestamp, but experiments show that, at least on keyboard and mouse, timestamps are still multiples of 0.016s apart, and that looks like frame granularity to me. I don't have a reason to switch to InputSystem at this moment, and I may have to bear with frame granularity for a while.
* UIElements

It looks promising because it defines UI with a flavor of XML and CSS, allowing elements across scenes to share styles, something very painful to maintain on the legacy UI system.

However as of Unity 2019.4.3f1, UIElements only supports Editor UI, not runtime in-game UI. I may look into it again when runtime support is added.