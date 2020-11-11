```txt
______  _      _____
|  _  \| |    /  ___|
| | | || |    \ `--.
| | | || |     `--. \  Dynamic LED Strips
| |/ / | |____/\__/ /  Version 0.1.0
|___/  \_____/\____/   MIT License, (c) Lilian Gallon 2020

/***
  LED strips are alive! The color and the brightness change according to the activity of your computer!

  Improves your immersion in the game you're playing. Works great with the new Assassin's Creed Valhalla.
  No doubt that it will be awesome with Cyberpunk 2077 as well! Works with movies, yutube videos and
  anything your computer is streaming.
**/

/// Lightweight
  ./ Low processor usage
  ./ Low ram usage (~=30 MB)

/// Smart
  ./ Finds compatible bluetooth LE controllers automatically
  ./ It uses bluetooth to comunicate with LED controllers, but you can still use your game controller at
	 the same time
  ./ It does not react to the sound level directly, but rather to the bass level.

/// Customizable
  ./ Toggle features on and off (brightness / color)

/// Works with most of the controllers
  ./ Supports "LED BLE" controllers (ELK_BLEDOM)

/// Cross compatible
  ./ Windows 10
  ./ MacOS (you need to build it yourself)
  ./ Linux (you need to build it yourself)
```

## How to use it

Download the .zip file and extract it anywhere. Execute it (DynamicLedStrips.exe), then follow the instructions. First you may want to do an automatic scan to know which device to use. Then, you will be given an argument (a;b;c;d;e...) to run the program with the same settings. You can create a `.bat` file with that argument. It will look like this `./ble.exe a;b;c;d;..;`. (With a, b, c and so on being the settings). You will be given instructions while running the program anyway.

The v1.0.0 version (if it ever gets released) will be a software with a user-friendly interface. For now, you need to act like a hacker.. ;)

If you are a developper, you can even create your own launcher around it using command line arguments.

**Disclamer**

If the color variable is not set to NONE, you may get banned by intrusive anti-cheats (like Riot Vanguard) because it will read the main screen's pixels. I did not test it though (it may be safe). The program acts like it's taking screenshots. Works great for Survival Games. Rocket League is safe as well. If you get banned, don't blame me! Better be safe than sorry!

## For the devs:

**Works with Windows 10 > build 10240**

**Project setup:**
- Open .sln file with Visual Studio.

If, for some reason, VS does not downlaod the required packages, then:
- Right click on the project name > manage NuGet packages > Install Microsoft.Windows.SDK.Contracts (to use the Bluetooth LE SDK)
- Right click on the project name > manage NuGet packages > Install NAudio (to listen get the system sound level)
- Right click on the project name > manage NuGet packages > Install System.Drawing.Common (to change the LEDs color according to the screen average color)

**How to find your targetted device id:**
- Download and use the open source [Microsoft BLE Explorer](https://www.microsoft.com/en-us/p/bluetooth-le-explorer/9n0ztkf1qd98?activetab=pivot:overviewtab)
- Connect to the LEDs controller (maybe named ELK_BLEDOM  if you have the same as me)

If you have a different controller, nothing changes expect the thing that you will write. Here is an example with ELK-BLEDOM controllers (not everything is listed below):

*This data comes from the work of A.E.TECH accessible on [this repo](https://github.com/arduino12/ble_rgb_led_strip_controller), but I copied some of it just in case it gets deleted*

- `7e 00 01 brightness  00 00 00 00 ef` - `0-100   (0x00-0x64)`
- `7e 00 02 speed       00 00 00 00 ef` - `0-100   (0x00-0x64)`
- `7e 00 03 temperature 02 00 00 00 ef` - `128-138 (0x80-0x8a)`
- `7e 00 04 is_on       00 00 00 00 ef` - `0-1     (0x00-0x01)`
- `7e 00 03 effect      03 00 00 00 ef` - `see below`

**This is not in the repo linked above. It was reverse engineered by me (so it may be different for you)**

My controller:
- 7e0003**80**03000000ef red
- 7e0003**81**03000000ef blue
- 7e0003**82**03000000ef green
- 7e0003**83**03000000ef cyan
- 7e0003**84**03000000ef yellow
- 7e0003**85**03000000ef magenta
- 7e0003**86**03000000ef white
