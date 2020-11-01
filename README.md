# BLE-Controller
Change the brightness of LEDs using Bluetooth LE according to the music played on your computer

## Goals:

- [ ] Change intensity of the light according to the sound played by the default output device
- [ ] Ask for the user to select the device, the service, and the characteristic
- [ ] Allow the user to save that by giving them command line arguments to run the program
- [ ] Allow the user to start this app in the foreground on windows startup
- [ ] Cool UWP UI

## For the devs:

**Project setup:**
- Open .sln file with Visual Studio.
- Right click on the project name > manage NuGet packages > Install Microsoft.Windows.SDK.Contracts (to use the Bluetooth LE SDK)

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

| effect | description|
| - | - |
| `0x80` | r (red) |
| `0x81` | g (green) |
| `0x82` | b (blue) |
| `0x83` | y (yellow) |
| `0x84` | c (cyan) |
| `0x85` | m (magenta) |
| `0x86` | w (white) |
| `0x87` | jump_rgb |
| `0x88` | jump_rgbycmw |
| `0x89` | gradient_rgb |
| `0x8a` | gradient_rgbycmw |
| `0x8b` | gradient_r |
| `0x8c` | gradient_g |
| `0x8d` | gradient_b |
| `0x8e` | gradient_y |
| `0x8f` | gradient_c |
| `0x90` | gradient_m |
| `0x91` | gradient_w |
| `0x92` | gradient_rg |
| `0x93` | gradient_rb |
| `0x94` | gradient_gb |
| `0x95` | blink_rgbycmw |
| `0x96` | blink_r |
| `0x97` | blink_g |
| `0x98` | blink_b |
| `0x99` | blink_y |
| `0x9a` | blink_c |
| `0x9b` | blink_m |
| `0x9c` | blink_w |