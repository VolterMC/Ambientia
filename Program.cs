//                                 LEDSTRIP2           LEDSTRIP3
//                            ══════════════════> ══════════════════>    
//                            2 2 2 2 2 2 2 2 2 2 3 3 3 3 3 3 3 3 3 3    
//                          ^ 1┌───────────────────────────────────┐4 ║ L
//                        L ║ 1│                                   │4 ║ E
//                        E ║ 1│                                   │4 ║ D
//                        D ║ 1│                                   │4 ║ S
//                        S ║ 1│              27" LCD              │4 ║ T
//                        T ║ 1│            (rear view)            │4 ║ R
//                        R ║ 1│                                   │4 ║ I
//                        I ║ 1│                                   │4 ║ P
//                        P ║ 1│                                   │4 ║ 4
//                        1 ║ 1└───────────────────────────────────┘4 V  
//══════════════════════════╝ 6 6 6 6 6 6 6 6 6 6 5 5 5 5 5 5 5 5 5 5    
//  from Lighting Node PRO    ═══════════════════ <══════════════════    
//  or Commander PRO               LEDSTRIP6           LEDSTRIP5

using RGB.NET.Core;
using RGB.NET.Devices.Corsair;
using RGB.NET.Groups;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace Ambientia
{
    class Program
    {
        // Replace 2560x1440 with your screen resolution, 
        // but don't touch the 20x12 one
        static Bitmap BigScreen = new Bitmap(2560, 1440);
        static Bitmap SmallScreen = new Bitmap(20, 12);

        // Can't touch this
        static RGBSurface Surface;
        static List<Led> Leds;
        static bool RunUpdateThread;
        static Stopwatch PotentialFpsMeter;
        static Stopwatch ActualFpsMeter;

        // You might want to lower this to hard limit the FPS, but increasing it won't give you anything
        static int FPS_LIMIT = 30;

        static void Main(string[] args)
        {
            Surface = RGBSurface.Instance;
            // If you happen to have some LED Strips in your system, you don't want to be a part of this project, you should set some device filters here
            Surface.LoadDevices(CorsairDeviceProvider.Instance, RGBDeviceType.LedStripe); 
            Surface.AlignDevices();

            foreach (IRGBDevice device in Surface.GetDevices<IRGBDevice>())
            {
                Console.WriteLine("Found " + device.DeviceInfo.DeviceName);
            }
            ILedGroup stripGroup = new ListLedGroup(Surface.Leds);
            Leds = (List<Led>)stripGroup.GetLeds();

            Thread UpdateThread = new Thread(UpdateLeds);
            RunUpdateThread = true;
            PotentialFpsMeter = new Stopwatch();
            ActualFpsMeter = new Stopwatch();
            UpdateThread.Start();
            Console.WriteLine("Running Ambientia. Press any key or close this window to exit.\n\n");
            Console.ReadKey();
            RunUpdateThread = false;
        }

        static void SetLedColors(List<Led> Leds)
        {
            System.Drawing.Color LedColor;

            using (Graphics g = Graphics.FromImage(BigScreen))
            {
                // Take screenshot
                g.CopyFromScreen(0, 0, 0, 0, BigScreen.Size);
            }

            using (Graphics g = Graphics.FromImage(SmallScreen))
            {
                // Squeeze the screenshot into a 20x12 bitmap
                g.DrawImage(BigScreen, 0, 0, 20, 12);
            }

            for (int i = 0; i < Leds.Count; ++i)
            {
                if (i < 10) // LED Strip 1 (vertical left going from bottom)
                {
                    int j = i;
                    LedColor = SmallScreen.GetPixel(19, 10 - j);
                }
                else if (i < 30) // LED Strips 2 & 3 (horizontal top going from left)
                {
                    int j = i - 10;
                    LedColor = SmallScreen.GetPixel(19 - j, 0);
                }
                else if (i < 40) // LED Strip 4 (vertical right going from top)
                {
                    int j = i - 30;
                    LedColor = SmallScreen.GetPixel(0, j + 1);
                }
                else // LED Strips 5 & 6 (horizontal bottom going from right)
                {
                    int j = i - 40;
                    LedColor = SmallScreen.GetPixel(j, 11);
                }

                Leds[i].Color = new RGB.NET.Core.Color(LedColor.R, LedColor.G, LedColor.B);
            }     
        }

        static void UpdateLeds()
        {
            int cnt = 0;
            while (RunUpdateThread == true)
            {
                ActualFpsMeter.Restart();
                PotentialFpsMeter.Restart();
                SetLedColors(Leds);
                Surface.Update();
                PotentialFpsMeter.Stop();
                if (1000 / FPS_LIMIT - PotentialFpsMeter.ElapsedMilliseconds > 0)
                {
                    Thread.Sleep(1000 / FPS_LIMIT - (int)PotentialFpsMeter.ElapsedMilliseconds);
                }
                ActualFpsMeter.Stop();
                
                if (++cnt == 10)
                {
                    cnt = 0;
                    double ActualFps = 1000.0 / ActualFpsMeter.ElapsedMilliseconds;
                    double PotentialFps = 1000.0 / PotentialFpsMeter.ElapsedMilliseconds;
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine("FPS limit: " + FPS_LIMIT + ", Actual FPS: " + ActualFps.ToString("n2") + ", Potential FPS: " + PotentialFps.ToString("n2"));
                }
            }
        }
    }
}