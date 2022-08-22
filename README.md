# EclypsesPresentation
A presentation that I made for a company I was interning for, and I decided to put it up on Github! This is a VR controlled OWI Robotic Edge Arm using the StereoKit library with an arduino. 
I have attached a circuit diagram of the setup made by a professional electrician (thanks Dad) if anyone wants to recreate it. Don't be afraid to ask questions if you are stuck or confused! Questions regarding StereoKit can be answered on their discord.

Description:

The general idea of this project is to create a control panel in VR, with buttons that send information along a usb cable to an Arduino or some microcontroller. Once the Arduino receives a signal, it outputs some digital pin, which triggers a relay, which moves a part of the robotic arm. I do not have specific instructions to exactly recreate this scenario, but I may make that in the future. In the meantime, I have commented the code as best as I can, and have included an electrical diagram of the setup.

Links in the program.cs file!

I would make the license MIT, but EmGu requires that I make the license GNU GPLv3.

On the circuit diagram, each motor has an 'up' and 'down' relay, the LED has an 'up' relay. Up corresponds to +, down with -. 
