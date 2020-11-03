```txt
______  _      _____
|  _  \| |    /  ___|
| | | || |    \ `--.
| | | || |     `--. \  Dynamic LED Strips
| |/ / | |____/\__/ /  Version 0.1.0
|___/  \_____/\____/   MIT License, (c) Lilian Gallon 2020

/***
  LED strips are alive! The color and the brightness change according to the activity of
    your computer!

  Playing Doom? The LEDs will flash and turn red when killing monsters.
  Watching a movie? The LEDs color will change according to the scene you're looking at and
    will flash according to the audio.
**/

/// Lightweight 
  ./ Low processor usage
  ./ Low ram usage (~=30 MB)
  ./ Refresh rate customizable (WIP)
  ./ Area used by color picker customizable (WIP)

/// Smart
  ./ Finds bluetooth controllers automatically (WIP)
  ./ It uses bluetooth to comunicate with LED controllers, but you can still use your game controller in the same time!

/// Customizable 
  ./ Toggle features on and off (brightness / color) (WIP)
  ./ Audio sensibility customizable (WIP)
  
/// Works with most of the controllers
  ./ Supports "LED BLE" controllers (ELK_BLEDOM)

/// Cross compatible
  ./ Windows 10
  ./ MacOS
  ./ Linux
```

## Goals:

- [x] Change intensity of the light according to the sound played by the default output device
- [x] Change the LEDs color according to the screen main color
- [x] Smooth brightness variation
- [x] Option for brightness to change according to the bass instead of the audio level
- [ ] Refresh rate customizable
- [ ] Ask for the user to select the device, the service, and the characteristic
- [ ] Allow the user to change the sound level sensibility
- [ ] Allow the user to map the colors by himself (the same device may have different color mappings)
- [ ] Allow the user to save that by giving them command line arguments to run the program - V 1.0.0 goal
- [ ] Allow the user to start this app in the foreground on windows startup
- [ ] Cool UWP UI
- [ ] Automatic configuration (make it work on any Bluetooth LE LED controller) - yes, it's possible

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

- 7e0003**80**03000000ef red
- 7e0003**81**03000000ef blue
- 7e0003**82**03000000ef green
- 7e0003**83**03000000ef cyan
- 7e0003**84**03000000ef yellow
- 7e0003**85**03000000ef magenta
- 7e0003**86**03000000ef white
