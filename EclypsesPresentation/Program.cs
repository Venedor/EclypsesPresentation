//THIS PROGRAM WILL NOT COMPILE WITHOUT EDITS! YOU MUST FIRST EDIT THE PATHS OF THE BUTTON MODELS AND BACKGROUND: THERE ARE THREE PLACES WHERE THIS OCCURS!
//Hello everyone, this is a cross disciplinary proof of concept robotic arm + vr headset project that I made for a presentation, but decided to post it to GitHub. 
//This code works as of 8/17/2022 on Visual Studio 2022 on Windows 10. 
//The following libraries were installed with the NuGet package manager (Project -> Manage NuGet packages):
//      • Emgu.CV v4.5.5.4823                   | A .NET wrapper to the OpenCV library, used to capture webcam video. https://www.emgu.com/wiki/index.php/Main_Page
//      • Emgu.CV.Bitmap v4.5.5.4823            | Used to convert webcam video into bitmaps, which were used to display video into the VR environment. 
//      • Emgu.CV.runtime.windows v4.5.5.4823   | I dont remember, I think it is needed to run Emgu on the .NET Core project.
//      • StereoKit v0.3.6                      | Fantastic and lightweight C# library for VR, made more for tools and less games. If you want games, look at Unity. https://stereokit.net/
//      • System.IO.Ports v6.0.0                | Windows specific library for controlling serial ports, used to establish connection with arduino. 
//DESCRIPTION:
//This program uses StereoKit to create a VR environment that allows you to connect to plugged in ARDUINO and send character information to it. On the arduino side, these characters can be translated to pin outputs. 
//This program is a proof of concept of the library and the idea. 
//DISCLAIMER: 
//I am not a programming, I just know how to code. The difference is that a programmer knows what they are doing. What you may see here may offend anyone with the most basic understanding of proper code techniques. 
using StereoKit;

using System;
using System.IO.Ports;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;


namespace EclypsesPresentation
{
    internal class Program
    {
        //I use classes for pretty much any storage relating thing, I dont know if that is what you are supposed to do.
        //ARObject holds information about the camera and its display in VR. Most of it is from StereoKit.
        public class ARObject
        {   
            public Vec3 pos;                    //Vec3 pos is the x,y,z coordinates of where the object will appear in VR. All units are in meters by default. 
            public Vec2 size;                   //Vec2 size is the x,y lengths of how large the object will be.
            public Quat rot;                    //Quat rot is the rotation, and after 20 hours I still dont fully understand it. Read more about it here: https://stereokit.net/Pages/StereoKit/Quat/Quat.html
            public Pose pose;                   //Pose pose is essentially the position and the rotation in one variable. 
            public Tex tex;                     //Tex tex is the texture of the object. We will take a frame from the webcam and put that frame information here to be displayed. I took this process from https://github.com/mbucchia/OpenXR-Window-Manager/blob/main/Window-Manager/Program.cs#L205
            public Sprite sprite;               //We take the information from tex and put it into sprite for final display.
            public VideoCapture videoCapture;   //Part of the EmGu package, used to capture video. 
        }
        //RoboticPoint is used to store information relating to the robotics, including the control board positioning. 
        public class RoboticPoint       //Arduino 
        {
            public bool conState;       //The state of connection between the computer and the arduino.
            public SerialPort port;     //Windows System.IO.Ports, controls the settings for the serial ports.
            public string selectedPort; //A string that stores which port you have selected.
            public String[] ports;      //A string that stores all ports.

            public Vec3 txtVec;         //A vector that stores button text/the button labels. Each button label is set, displayed, and overwritten by the next button, which is then displayed, overwritted, etc. 
            public Vec3 bVec;           //Button Vector, a vec3 that decribes the position of the button. Each button is actually a handle with a custom model for pressed and unpressed. I explain this more on line 187ish.
            public Quat bRot;           //Button Rotation, defines the rotation of the button (facing out towards the user).
            public float scale;         //Button Scale, the original models are 4 meters wide, so I have to scale it down a lot.
            public Matrix txt;          //The matrix used for txt is Matrix.TRS, which defines the transformation/position, the rotation, and the scale.
            public Pose winPose;        //Pose is position and rotation, which is what we are defining for the window where the buttons will sit.
            public Vec2 size;           //Size of the window.
            public Model bP;            //The button Pressed model.
            public Model bU;            //The button Unpressed model.
            public Pose bPose;          //The position of each button. This variable gets reused by each button. 
        }
        //ControlBoard stores information related to that small control board which is used to initialize the connection to the ardunio. 
        public class ControlBoard
        {
            //Control Board (cB)
            public Vec3 cBPosition;     //Position of control board
            public Quat cBOrientation;  //Orientation of control board
            public Pose controlBoard;   //Pose of control board, position + orientation
            public Vec3 errorPosition;  //Position of error board
            public Quat errorRotation;  //Orientation of error board
            public Pose errorPose;      //Pose of error board, position + orientation
            public bool initClicked;    //Tracks if the initializeConnection button is clicked.
            public bool error;          //Tracks if an error has occured.
            public Exception ex;        //Keeps storage of error if one has occured.
            public bool decClicked;     //tracks if the decoupledConnection button is clicked. 
        }
        //I dont remember what static means, and at this point I'm too afraid to ask. 
        //Main function where pretty much everything is. It is loosly divided into two main sections:
        //  1. Setup and Initialization, StereoKit settings are definind, variables and default positions are initialized, models are loaded.
        //  2. Main Loop (SK.Run(()). StereoKit has a main loop that displays every frame every time it is run. This allows for low level controlling of objects and allows the user to precisely define what they want on screen. 
        static void Main(string[] args)
        {
            ////SETUP AND INITIALIZATION////
            ARObject ar = new ARObject();           //See class for info
            RoboticPoint arm = new RoboticPoint();  
            ControlBoard cb = new ControlBoard();

            //Change all settings related to StereoKit here!
            SK.Initialize(new SKSettings { appName = "Project" });

            //Control Board information, define default positions and orientations
            cb.cBPosition = new Vec3(0.5f, 0, -0.5f);                       //Default control board position, with x being left and right, y being up and down, and z being front and back, -z=front. 
            cb.cBOrientation = Quat.LookDir(0, 0, 1);                       //Make control board look at point 0,0,1 by default. 
            cb.controlBoard = new Pose(cb.cBPosition, cb.cBOrientation);    //Mash both position and rotation into one variable. 

            //Error Board, displays if cb.error is true, displays cb.ex, which is the exception. 
            cb.errorPosition = new Vec3(0, 0, -0.35f);
            cb.errorRotation = Quat.LookDir(0, 0, 1);
            cb.errorPose = new Pose(cb.errorPosition, cb.errorRotation);

            //Keep track of what buttons are clicked when. 
            cb.initClicked = false;
            cb.error = false;

            //AR kinda
            ar.videoCapture = new VideoCapture(0); //Create a camera stream, this is where errors are likely to occur. A camera can only provide a stream to 1 app at a time. If you have multiple cameras, you can change which one gets used by incrementing 0. 
            ar.pos = new Vec3(-0.5f, 0.5f, -0.4f);  
            ar.rot = Quat.LookDir(0, 0, 1);
            ar.pose = new Pose(ar.pos, ar.rot);
            ar.size = new Vec2(0.75f, 0.75f);

            //Robotics and the Arm Control Window (The one with 12 big red buttons)
            arm.conState = false;   //Keeps track of the connection status, is set to true if the computer can write a character down a line. 

            arm.bP = Model.FromFile("C:\\Users\\radia\\source\\repos\\EclypsesPresentation\\EclypsesPresentation\\Assets\\PressedButton.glb");      //YOU MUST CHANGE THIS TO YOUR DIRECTORY!
            arm.bU = Model.FromFile("C:\\Users\\radia\\source\\repos\\EclypsesPresentation\\EclypsesPresentation\\Assets\\UnpressedButton.glb");    //YOU MUST CHANGE THIS TO YOUR DIRECTORY!

            arm.txtVec = new Vec3(0, 0.001f, -0.075f);              //Display button text. 
            arm.bRot = Quat.LookDir(0, 1, 0);                       //Button rotation.
            arm.bVec = new Vec3();                                  //Button Position
            arm.scale = 0.0125f;                                    //By default, the buttons are 4 by 4 meters, so scale down.
            arm.txt = Matrix.TRS(arm.txtVec, arm.bRot, -1);         //Text matrix for button text, TRS is transform, rotation, scale. Position = above and in front of button, rotation is same as button, scale is inverse. 
            //IMPORTANT NOTE: STEREOKIT ADDS OBJECTS IN A HIERARCHY, AND EACH OBJECT'S POSITION IS RELATIVE TO THE OBJECT'S POSITION ABOVE IT IN THE HIERARCHY.
            arm.winPose = new Pose(0, 0.25f, -0.5f, Quat.Identity); //Position of button backboard.
            arm.size = new Vec2(1.125f, 0.5f);                      //Size of button backboard.

            arm.bPose = new Pose();                                 //Button position + rotation.

            ////MAIN LOOP, EACH FRAME IN VR IS ONE ITERATION OF THIS LOOP////
            SK.Run(() =>
            {
                //StereoKit territory!
                //Control Board
                UI.WindowBegin("Control Board", ref cb.controlBoard);                       //Each window must have a beginning and an end. 
                UI.Text("Grab to move!");                                                   //Adds text to this window.
                if ((UI.Button("Initialize Arduino Connection") && cb.initClicked == false && arm.conState == false))//Adds a button that returns true on the first frame it is pressed. 
                {
                    cb.initClicked = true;
                }
                if (cb.initClicked)                             //This allows me to keep a new display open when the initialize connection button it clicked. 
                { 
                    getComPorts(arm);
                    
                    foreach (string port in arm.ports)          //Display each port as a button. 
                    {
                        if (UI.Button(port))                    //If the user clicks a button
                        {
                            try                                 //Attempt to connect to that port
                            {
                                arm.selectedPort = port;
                                initializeConnection(arm);
                            }
                            catch (Exception ex)                //If not, throw an error and allow the user to click the button again. 
                            { 
                                cb.error = true;
                                cb.ex = ex;
                            }
                            cb.initClicked = false;
                        }
                    }
                }

                if (UI.Button("Cancel All"))                    //Cancels the inialitzation by closing the list of ports. 
                {
                    cb.initClicked = false;
                }
                if (UI.Button("Decouple Arduino Connection") && arm.conState == true)   //Removes the connection state
                {
                    try                            //Errors commonly occur here, so a try and catch statement ensures that the program doesnt crash if a disconnection is attempted when there is no connection. 
                    {
                        arm.conState = false;
                        decoupleConnection(arm);
                    }
                    catch (Exception ex)
                    {
                        cb.error = true;
                        cb.ex = ex;
                    }
                }
                UI.WindowEnd();     //Every WindowBegin must have a WindowEnd

                //Error Section, if cb.error is true, then display the error in cb.ex
                if (cb.error == true)
                {
                    UI.WindowBegin("An Error Has Occurred!", ref cb.errorPose);
                    UI.Text(cb.ex.ToString() + "\nDecouple the connection and retry.");
                    if (UI.Button("Press to close"))
                    {
                        cb.error = false;
                    }
                    UI.WindowEnd();
                }

                //This section actually creates the button board and allows for the controlling of the robotic arm. I will explain how one button works, as all the buttons are nearly identical. 
                if (arm.conState)
                {
                    //Controlling Arm Section   
                    UI.WindowBegin("Robotic Arm Control Board", ref arm.winPose, arm.size, UIWin.Normal, UIMove.None);  //We define this window not to be movable by the user.

                    //Base Plate
                    arm.bVec = new Vec3(-0.375f, -0.375f, 0.01f);   //First define the position where the button will appear. This is relative to the window backboard, due to how StereoKit works. Everything is HIERARCHY.
                    arm.bPose = new Pose(arm.bVec, arm.bRot);       //Each button shares the same rotation, which was defined in the setup stage. 
                    if (UI.HandleBegin("Base Left", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None)) //Each button is a handle, which returns true every frame it is being GRABBED not pushed. They are scaled down, and have a position
                    {                                                                                              //defined by arm.bPose. We will be drawing the handles ourselves with our button models, and we do not want the user to move the buttons around. 
                        arm.bP.Draw(Matrix.S(arm.scale));                                                          //If the user grabs the button, then display the pressed button model scaled down.
                        basePlate("left", arm);                                                                    //Also call a function to move the baseplate left.
                    }                               
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));                                                          //If the user does not grab a button, then display the unpressed button model scaled down.
                    Text.Add("Base L", arm.txt);                                                                   //Regardless if the user is grabbing the button or not, display some labels above the button.  
                    UI.HandleEnd();                                                                                //Every instance called must also end. 

                    arm.bVec = new Vec3(-0.375f, -0.125f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Base Right", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        basePlate("right", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("Base R", arm.txt);
                    UI.HandleEnd();


                    //Motor 1
                    arm.bVec = new Vec3(-0.25f, -0.375f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Motor 1 Up", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        motor1("up", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("M1 Up", arm.txt);
                    UI.HandleEnd();

                    arm.bVec = new Vec3(-0.25f, -0.125f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Motor 1 Down", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        motor1("down", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("M1 Down", arm.txt);
                    UI.HandleEnd();


                    //Motor 2
                    arm.bVec = new Vec3(-0.125f, -0.375f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Motor 2 Up", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        motor2("up", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("M2 Up", arm.txt);
                    UI.HandleEnd();

                    arm.bVec = new Vec3(-0.125f, -0.125f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Motor 2 Down", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        motor2("down", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("M2 Down", arm.txt);
                    UI.HandleEnd();


                    //Motor 3
                    arm.bVec = new Vec3(0, -0.375f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Motor 3 Up", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        motor3("up", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("M3 Up", arm.txt);
                    UI.HandleEnd();

                    arm.bVec = new Vec3(0, -0.125f, 0.01f);
                    arm.bPose = new Pose(0, -0.125f, 0.01f, arm.bRot);
                    if (UI.HandleBegin("Motor 3 Down", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        motor3("down", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("M3 Down", arm.txt);
                    UI.HandleEnd();


                    //Claw
                    arm.bVec = new Vec3(0.125f, -0.375f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Claw Open", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        claw("open", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("Claw O", arm.txt);
                    UI.HandleEnd();

                    arm.bVec = new Vec3(0.125f, -0.125f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Claw Close", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        claw("close", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("Claw C", arm.txt);
                    UI.HandleEnd();

                    //Light
                    arm.bVec = new Vec3(0.25f, -0.375f, 0.01f);
                    arm.bPose = new Pose(arm.bVec, arm.bRot);
                    if (UI.HandleBegin("Light On", ref arm.bPose, arm.bP.Bounds * arm.scale, false, UIMove.None))
                    {
                        arm.bP.Draw(Matrix.S(arm.scale));
                        light("on", arm);
                    }
                    else
                        arm.bU.Draw(Matrix.S(arm.scale));
                    Text.Add("L On", arm.txt);
                    UI.HandleEnd();

                    UI.WindowEnd();
                }

                //EmGu territory!
                //ARish, this is where we grab the video feed from the camera, convert it to the proper format, apply it to a texture, and display it as a sprite. Credit to https://github.com/mbucchia/OpenXR-Window-Manager/blob/main/Window-Manager/Program.cs#L205
                //for the process and to koujaku (Nick K) on Discord for the help!
                //Get information about the frame.
                Image<Rgb, byte> image = ar.videoCapture.QueryFrame().ToImage<Rgb, byte>(); //By default, EmGu grabs the camera information as an BGR format, which looks weird, convert it back!
                var bitmap = image.ToBitmap<Rgb, byte>();                                   //Convert the rgb image to a bitmap.

                if (ar.tex == null || ar.tex.Width != bitmap.Width || ar.tex.Height != bitmap.Height)
                {
                    ar.tex = Tex.GenColor(StereoKit.Color.BlackTransparent, bitmap.Width, bitmap.Height);   
                    ar.sprite = Sprite.FromTex(ar.tex);                                                      
                }

                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb); //This is where my understanding ends. 
                ar.tex.SetColors(bitmap.Width, bitmap.Height, bitmapData.Scan0);    //Setting colors applies that bitmap data to a texture, which is also applied to a sprite.
                bitmap.UnlockBits(bitmapData);              //Dont know      

                //Display the frame:
                UI.WindowBegin("Display", ref ar.pose, ar.size);
                UI.Image(ar.sprite, ar.size);
                UI.WindowEnd();

                //Background
                Renderer.SkyTex = Tex.FromCubemapEquirectangular("C:\\Users\\radia\\source\\repos\\EclypsesPresentation\\EclypsesPresentation\\Assets\\lilienstein_4k.hdr", out SphericalHarmonics lighting);   //YOU MUST CHANGE THIS DIRECTORY!
                Renderer.SkyLight = lighting;
            });

        }

        //The next three functions detail the process of connecting to the arduino over the wire. The arduino is first plugged into the computer running this code (the host).
        //Then, you get the available com ports with getComPorts, which lists connnections. In my case, the arduino was always on COM3 for some reason. If you are not sure, the arduino IDE will tell you, lookup a basic tutorial on how
        //to connect an arduino to the IDE. After the com port is known, you can initialize the connection with initializeConnection, where it sets up the connection through a process I don't fully understand. 
        //When you are done, or you want to connect to something else, then you can decouple the connection, which just closes the port. 
        static void getComPorts(RoboticPoint r)
        {
            r.ports = SerialPort.GetPortNames();
        }
        static void initializeConnection(RoboticPoint r)
        {
            r.port = new SerialPort(r.selectedPort, 9600, Parity.None, 8, StopBits.One);    //This selects the port, sets up basic data transfer parameters, and puts that information to a SerialPort variable.
            r.port.Open();                                                                  //This opens the port. 
            r.port.Write("y");                                                              //Now we can write what we want!
            r.conState = true;
        }

        static void decoupleConnection(RoboticPoint r)
        { 
            r.port.Write("z");                                                              //Send a signal to the arduino to stop all port operations. In reality, I can remove this. 
            r.port.Close();
            r.conState = false;
        }

        //Arm Command Functions, each function controls a different motor, with parameters defining the direction.
        //In my case, each motor was a DC motor, meaning that they can only move in two directions based on the sign of the voltage passed to them (ie - = left, + = right).
        //Each motor had two relays, one for + and one for -, and each relay was controlled by a different digital output pin on the arduino. 
        //r.port.Write sends a character over the wire to the arduino, where it is received and switch() case: to the appropriate output. 
        static void basePlate(string direction, RoboticPoint r)
        {
            if (direction == "left")
                r.port.Write("a");
            else
                r.port.Write("b");
        }

        static void motor1(string direction, RoboticPoint r)
        {
            if (direction == "up")
                r.port.Write("c");
            else
                r.port.Write("d");

        }
        static void motor2(string direction, RoboticPoint r)
        {
            if (direction == "up")
                r.port.Write("e");
            else
                r.port.Write("f");

        }

        static void motor3(string direction, RoboticPoint r)
        {
            if (direction == "up")
                r.port.Write("g");
            else
                r.port.Write("h");

        }

        static void claw(string direction, RoboticPoint r)
        {
            if (direction == "open")
                r.port.Write("i");
            else
                r.port.Write("j");

        }

        static void light(string status, RoboticPoint r)
        {
            if (status == "on")
                r.port.Write("k");
            else
                r.port.Write("l");
        }
    }
}
