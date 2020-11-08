## Version 0.1.0

**Configuration**
- The brightness changes according to the audio:
	- BASS_LEVEL (default): the brightness changes according to the bass currently played
	- SOUND_LEVEL: the brightness changes according to the sound level currently played (volume)
	- NONE: the brightness does not change
- The brightness variation can be smoothen:
	- NONE (default): no smoothing
	- DYNAMIC: dynamic smooth (the smoothing value changes according to the level difference)
	- VALUE: you specify the value to smoothen the brightness variation. Instead of going from 10% to 50% bightness intensity if the sound volume goes from 10% to 50%, it goes to 10% + {smoothingValue}.
- The color changes according to the color of your screen:
	- COLOR_AVG (default): takes the average of the colors of your screen (in the center)
	- NONE: the color won't change
- You can configure everything by hand, or you can use the default config
- You can run the program with a command line that contains all the information needed to connect to the device and to set the configuration

**Device detection**
- You can scan for compatible devices, then you have to select the one that you want

**Details**
- You can still use your gaming controller while using the program, it won't interact with it
- It works with full screen app

**Disclamer**
- If the color variable is not set to NONE, you may get banned by intrusive anti-cheats (like Riot Vanguard). I did not test it though. The program acts like it's taking screenshots. Works great for Survival Games. Rocket League is safe as well. If you get banned, don't blame me!