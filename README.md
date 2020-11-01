# BLE-Controller
Change the brightness of LEDs using Bluetooth LE according to the music played on your computer.

**Works with Windows 10 > build 10240**

## Goals:

- [x] Change intensity of the light according to the sound played by the default output device
- [x] Change the LEDs color according to the screen main color
- [ ] Ask for the user to select the device, the service, and the characteristic
- [ ] Allow the user to save that by giving them command line arguments to run the program
- [ ] Allow the user to start this app in the foreground on windows startup
- [ ] Cool UWP UI

## For the devs:

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