using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LibVLCSharp.Shared;
using LibVLCSharp.Avalonia;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.WiringPi;
using System;
using Unosquare.RaspberryIO.Camera;
using System.Diagnostics;
using CliWrap;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Avalonia.Media;
using System.Drawing;
using System.Drawing.Imaging;
using Brushes = Avalonia.Media.Brushes;

namespace AFSD
{
    public partial class MainWindow : Window
    {
        // GPIO Definitions
        IGpioPin GPIO_DISPLAYPOWER;
        IGpioPin GPIO_BUZZER;
        IGpioPin GPIO_STEPPER_DIR;
        IGpioPin GPIO_STEPPER_STEP;
        IGpioPin GPIO_STEPPER_EN;
        IGpioPin GPIO_STEPPER_MS1;
        IGpioPin GPIO_STEPPER_MS2;
        IGpioPin GPIO_LOCKED_ENDSTOP;
        IGpioPin GPIO_UNLOCKED_ENDSTOP;
        IGpioPin GPIO_ENCODER_A;
        IGpioPin GPIO_ENCODER_B;
        IGpioPin GPIO_ENCODER_BUTTON;
        IGpioPin GPIO_DOORBELL_BUTTON;

        // Settings object
        ApplicationSettings settings = new ApplicationSettings();

        // Twilio Interop object
        TwilioInterop twilio;

        const int STEPS_PER_ROTATION = 200;


        Button btnCounterClockwise;
        Button btnClockwise;
        Button btnRotaryClick;
        Button btnExit;

        TextBlock CurrentTime;
        TextBlock MenuOptionTop;
        TextBlock MenuOptionMiddle;
        TextBlock MenuOptionBottom;
        TextBlock DebugMenuGPIO;
        TextBlock DebugMenuSettings;
        TextBlock DebugTop;
        TextBlock DebugMiddle;
        TextBlock DebugBottom;
        TextBlock DebugBig;

        Grid DebugMenu;

        Panel Panel_VideoFrame;

        VideoView VLC;

        int remainingDisplayTimer = 0;
        const int MenuMin = 0;
        int MenuMax = 5;
        int menuCurrentIndex = 0;
        int countA = 0;
        int countB = 0;
        int countC = 0;

        List<Action> MenuMethods = new List<Action>();
        List<string> MenuTexts = new List<string>();

        LibVLC libvlc;

        Media media;

        int videoByteCount = 0;
        int videoEventCount = 0;
        bool processingInput = false;

        bool showingDebug = false;
        

        public MainWindow()
        {
            InitializeComponent();

            void InitializeComponent()
            {
                AvaloniaXamlLoader.Load(this);
            }

            // Attach XML controls to local variables
            btnCounterClockwise = this.Find<Button>("btnCounterClockwise");
            btnClockwise = this.Find<Button>("btnClockwise");
            btnRotaryClick = this.Find<Button>("btnRotaryClick");
            btnExit = this.Find<Button>("btnExit");
            CurrentTime = this.Find<TextBlock>("TextCurrentTime");
            VLC = this.Find<VideoView>("VLC_View");


            MenuOptionTop = this.Find<TextBlock>("MenuOptionTop");
            MenuOptionMiddle = this.Find<TextBlock>("MenuOptionMiddle");
            MenuOptionBottom = this.Find<TextBlock>("MenuOptionBottom");

            /*DebugTop = this.Find<TextBlock>("DebugTop");
            DebugMiddle = this.Find<TextBlock>("DebugMiddle");
            DebugBottom = this.Find<TextBlock>("DebugBottom");
            DebugBig = this.Find<TextBlock>("DebugBig");*/
            DebugMenuSettings = this.Find<TextBlock>("DebugSettings");
            DebugMenuGPIO = this.Find<TextBlock>("DebugGPIO");

            DebugMenu = this.Find<Grid>("DebugMenu");

            Panel_VideoFrame = this.Find<Panel>("Panel_VideoFrame");

            // Add event handlers to XML controls [local functions to generalized ones]
            btnCounterClockwise.Click += btnCounterClockwise_OnClick;
            void btnCounterClockwise_OnClick(object sender, RoutedEventArgs e) => RotaryDown();

            btnClockwise.Click += btnClockwise_OnClick;
            void btnClockwise_OnClick(object sender, RoutedEventArgs e) => RotaryUp();

            btnRotaryClick.Click += btnRotaryClick_OnClick;
            void btnRotaryClick_OnClick(object sender, RoutedEventArgs e) => RotaryClick();

            btnExit.Click += btnExit_OnClick;
            void btnExit_OnClick(object sender, RoutedEventArgs e) => CloseProgram();



            // All hardware executions are locked behind this to allow the design preview to work in dev
            if (!Design.IsDesignMode)
            {
                // Initialize VLC and Raspberry Pi wiring
                Pi.Init<BootstrapWiringPi>();
                Core.Initialize();
                Pi.Timing.SleepMilliseconds(500);

                // Setup and initialize GPIO pins
                GPIO_Initializer();

                // Start thread for updating UI
                Pi.Threading.StartThread(UpdateUI);

                InitializeStream();

                settings.LoadSettingsFile();
            }

            twilio = new TwilioInterop(settings.twilioSID, settings.twilioTOKEN, settings.twilioNUM, settings.twilioUSER, settings.twilioPASS);

            // Setup the menu structures
            //MenuMethods.Add(() => Doorbell_Pressed());
            //MenuTexts.Add("Doorbell Noise");
            //MenuMethods.Add(() => ShowDebugMenu());
            //MenuTexts.Add("Show Debug Menu");
            MenuMethods.Add(() => Menu_Lock_Door());
            MenuTexts.Add("Lock Door");
            MenuMethods.Add(() => Menu_Unlock_Door());
            MenuTexts.Add("Unlock Door");
            //MenuMethods.Add(() => TakePicture());
            //MenuTexts.Add("TakePicture");
            //MenuMethods.Add(() => twilio.TestMessage("+12609204093"));
            //MenuTexts.Add("Send Message");
            MenuMethods.Add(() => Reboot_System());
            MenuTexts.Add("Restart System");
            MenuMax = MenuTexts.Count - 1;

        }

        #region Threads
        // Continuously scans the input GPIO pins for level changes
        private void ScanInputs()
        {
            while (true)
            {
                // Check encoder state
                if (GPIO_ENCODER_A.Read() == false)
                {
                    if (GPIO_ENCODER_B.Read() == false)
                    {
                        // Wait until the 'pulse' stops
                        while (GPIO_ENCODER_B.Read() == false || GPIO_ENCODER_A.Read() == false)
                        {
                            Pi.Timing.SleepMicroseconds(20);
                        }
                        RotaryDown();
                    }
                    else
                    {
                        while (GPIO_ENCODER_B.Read() == false || GPIO_ENCODER_A.Read() == false)
                        {
                            Pi.Timing.SleepMicroseconds(20);
                        }
                        RotaryUp();
                    }
                    Pi.Timing.SleepMicroseconds(250);
                }

                // Check encoder button
                if (GPIO_ENCODER_BUTTON.Read() == false)
                {
                    while (GPIO_ENCODER_BUTTON.Read() == false)
                    {
                        Pi.Timing.SleepMicroseconds(20);
                    }
                    Pi.Timing.SleepMilliseconds(5);
                    RotaryClick();
                }

                // Check doorbell button
                if (GPIO_DOORBELL_BUTTON.Read() == false)
                {
                    int count = 0;
                    while (GPIO_DOORBELL_BUTTON.Read() == false)
                    {
                        count++;
                        Pi.Timing.SleepMicroseconds(20);
                    }
                    if (count >= 20000)
                    {
                        Menu_Lock_Door();
                    }
                    else if (200 < count && count < 20000)
                    {
                        Doorbell_Pressed();
                    }
                }
                Pi.Timing.SleepMicroseconds(10);
            }
        }


        private void UpdateUI()
        {
            int itterationCount = 0;
            int maxSettingTitleLength = 14;


            while (true)
            {
                // Execute update on Avalonia's UI thread
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(Update, Avalonia.Threading.DispatcherPriority.Render);
                itterationCount++;
                if (itterationCount > 100)
                {
                    itterationCount = 0;
                }
                Pi.Timing.SleepMilliseconds(50);
            }
            void Update()
            {
                if (itterationCount > 500 && remainingDisplayTimer == 0)
                {
                    GPIO_DISPLAYPOWER.Write(true);
                    Pi.Timing.SleepMicroseconds(50);
                    GPIO_DISPLAYPOWER.Write(false);
                    //DisposeStream();
                    Pi.Timing.SleepMilliseconds(1500);
                    //InitializeStream();
                    itterationCount = 0;
                }
                //itterationCount++;
                UpdateMenus();
                CurrentTime.Text = (System.DateTime.Now.ToString("t"));
                //DebugTop.Text = GPIO_UNLOCKED_ENDSTOP.Read().ToString();
                //DebugMiddle.Text = GPIO_DOORBELL_BUTTON.Read().ToString();
                //DebugBottom.Text = GPIO_LOCKED_ENDSTOP.Read().ToString();
                //DebugBig.Text = settings.DisplayOffTimer.ToString() + " " + settings.ftpHost + " " + settings.ftpPort + " " + settings.ftpUsername + " " + settings.ftpPassword + " ";
                if (itterationCount == 100)
                {
                    DebugMenuSettings.Text = "Setting                       Value\n";
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.DisplayOffTimer), settings.DisplayOffTimer.ToString());
                    DebugMenuSettings.Text += "\n";
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.ftpHost), settings.ftpHost);
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.ftpPath), settings.ftpPath);
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.ftpPort), settings.ftpPort);
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.ftpUsername), settings.ftpUsername);
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.ftpPassword), settings.ftpPassword);
                    DebugMenuSettings.Text += "\n";
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.twilioSID), settings.twilioSID);
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.twilioTOKEN), settings.twilioTOKEN);
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.twilioUSER), settings.twilioUSER);
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.twilioPASS), settings.twilioPASS);
                    DebugMenuSettings.Text += PrintSetting(nameof(settings.twilioNUM), settings.twilioNUM);


                    string PrintSetting(string name, string value)
                    {
                        string builder;
                        if (name.Length > maxSettingTitleLength)
                        {
                            builder = name.Substring(0, maxSettingTitleLength-2) + "..";
                        } 
                        else
                        {
                            builder = name;
                            for (int i = 0; i < maxSettingTitleLength - name.Length; i++)
                            {
                                builder += " ";
                            }
                        }
                        builder += value;
                        builder += "\n";
                        return builder;
                    }

                }



            }

        }

        #endregion

        #region Event Handlers
        private void Doorbell_Pressed()
        {
            BuzzerSound(3);
            Pi.Threading.StartThread(TakePicture);
        }

        private void RotaryClick()
        {
            MenuMethods[menuCurrentIndex]();
        }

        private void RotaryUp()
        {
           // if (!processingInput)
            //{
                //processingInput = true;
                //GPIO_DisposeInterrupts();
           // Pi.Timing.SleepMicroseconds(100);
            //GPIO_RegisterInterrupts();
            //countA++;

            if (menuCurrentIndex < MenuMax)
            {
                menuCurrentIndex++;
            }

            TurnDisplayOn();
                //processingInput = false;
            //}
            //Pi.Threading.StartThread(GPIO_Initializer);
            //Pi.Threading.StartThread(TurnDisplayOn);
            //MenuNext.Text = GPIO_STEPPER_EN.Value.ToString();
            //var GPIO_PWM = (GpioPin)Pi.Gpio[12];
            //GPIO_PWM.PinMode = GpioPinDriveMode.PwmOutput;
            /*GPIO_PWM.SoftToneFrequency = 0;
            Pi.Timing.SleepMilliseconds(500);
            GPIO_PWM.SoftToneFrequency = 500;
            Pi.Timing.SleepMilliseconds(500);
            GPIO_PWM.SoftToneFrequency = 1000;
            Pi.Timing.SleepMilliseconds(500);
            GPIO_PWM.SoftToneFrequency = 1500;
            Pi.Timing.SleepMilliseconds(500);
            GPIO_PWM.SoftToneFrequency = 2000;
            Pi.Timing.SleepMilliseconds(500);
            GPIO_PWM.SoftToneFrequency = 2500;
            Pi.Timing.SleepMilliseconds(500);
            GPIO_PWM.SoftToneFrequency = 3000;
            */
            //GPIO_PWM.PinMode = GpioPinDriveMode.PwmOutput;
            //GPIO_PWM.PwmMode = PwmMode.Balanced;
        }

        private void RotaryDown()
        {
            //if (!processingInput)
            //{
            //processingInput = true;
            //GPIO_DisposeInterrupts();
            //Pi.Timing.SleepMicroseconds(100);
            //GPIO_RegisterInterrupts();
            //countB++;
            if (menuCurrentIndex > MenuMin)
            {
                menuCurrentIndex--;
            }
            TurnDisplayOn();
            // processingInput = false;
            //}
            //DisposeStream();
        }

        #endregion

        #region General_Functions
        private void UpdateMenus()
        {
            // Draw new menus            
            if (menuCurrentIndex == 0)
            {
                MenuOptionTop.Text = MenuTexts[0];
                MenuOptionMiddle.Text = MenuTexts[1];
                MenuOptionBottom.Text = MenuTexts[2];

                MenuOptionTop.Background = Brushes.DarkGoldenrod;
                MenuOptionMiddle.Background = Brushes.BlanchedAlmond;
                MenuOptionBottom.Background = Brushes.BlanchedAlmond;


            }
            else if (menuCurrentIndex == MenuMax)
            {
                MenuOptionTop.Text = MenuTexts[MenuMax-2];
                MenuOptionMiddle.Text = MenuTexts[MenuMax -1];
                MenuOptionBottom.Text = MenuTexts[MenuMax];



                MenuOptionTop.Background = Brushes.BlanchedAlmond;
                MenuOptionMiddle.Background = Brushes.BlanchedAlmond;
                MenuOptionBottom.Background = Brushes.DarkGoldenrod;
            }
            else
            {
                MenuOptionTop.Text = MenuTexts[menuCurrentIndex-1];
                MenuOptionMiddle.Text = MenuTexts[menuCurrentIndex];
                MenuOptionBottom.Text = MenuTexts[menuCurrentIndex+1];


                MenuOptionTop.Background = Brushes.BlanchedAlmond;
                MenuOptionMiddle.Background = Brushes.DarkGoldenrod;
                MenuOptionBottom.Background = Brushes.BlanchedAlmond;
            }



        }
        private void GPIO_Initializer()
        {

            //////////
            //INPUTS//
            //////////

            // Rotary Encoder Input A
            GPIO_ENCODER_A = Pi.Gpio[17];
            GPIO_ENCODER_A.PinMode = GpioPinDriveMode.Input;
            GPIO_ENCODER_A.InputPullMode = GpioPinResistorPullMode.PullUp;

            // Rotary Encoder Input B
            GPIO_ENCODER_B = Pi.Gpio[27];
            GPIO_ENCODER_B.PinMode = GpioPinDriveMode.Input;
            GPIO_ENCODER_B.InputPullMode = GpioPinResistorPullMode.PullUp;

            // Rotary Encoder Button Click
            GPIO_ENCODER_BUTTON = Pi.Gpio[22];
            GPIO_ENCODER_BUTTON.PinMode = GpioPinDriveMode.Input;
            GPIO_ENCODER_BUTTON.InputPullMode = GpioPinResistorPullMode.PullUp;

            // Doorbell Button
            GPIO_DOORBELL_BUTTON = Pi.Gpio[6];
            GPIO_DOORBELL_BUTTON.PinMode = GpioPinDriveMode.Input;
            GPIO_DOORBELL_BUTTON.InputPullMode = GpioPinResistorPullMode.PullUp;

            // Door lock Unlocked Magnetic Switch
            GPIO_UNLOCKED_ENDSTOP = Pi.Gpio[2];
            GPIO_UNLOCKED_ENDSTOP.PinMode = GpioPinDriveMode.Input;
            GPIO_UNLOCKED_ENDSTOP.InputPullMode = GpioPinResistorPullMode.PullUp;
            
            // Door lock Unlocked Magnetic Switch
            GPIO_LOCKED_ENDSTOP = Pi.Gpio[3];
            GPIO_LOCKED_ENDSTOP.PinMode = GpioPinDriveMode.Input;
            GPIO_LOCKED_ENDSTOP.InputPullMode = GpioPinResistorPullMode.PullUp;
            
            //Console.WriteLine(Pi.Gpio);
            //GPIO_RegisterInterrupts();
            Pi.Threading.StartThread(ScanInputs);

            ///////////
            //OUTPUTS//
            ///////////

            // Controls the Power to LCD Display
            GPIO_DISPLAYPOWER = Pi.Gpio[26];
            GPIO_DISPLAYPOWER.PinMode = GpioPinDriveMode.Output;
            TurnDisplayOn();

            // Controls the State of the Buzzer
            GPIO_BUZZER = Pi.Gpio[12];
            GPIO_BUZZER.PinMode = GpioPinDriveMode.Output;

            // Controls the Direction Pin of the Stepper Motor
            GPIO_STEPPER_DIR = Pi.Gpio[16];
            GPIO_STEPPER_DIR.PinMode = GpioPinDriveMode.Output;
            GPIO_STEPPER_DIR.Write(true);

            // Controls the Step Pin of the Stepper Motor
            GPIO_STEPPER_STEP = Pi.Gpio[20];
            GPIO_STEPPER_STEP.PinMode = GpioPinDriveMode.Output;
            GPIO_STEPPER_STEP.Write(true);

            // Controls the Enable Pin of the Stepper Motor
            GPIO_STEPPER_EN = Pi.Gpio[5];
            GPIO_STEPPER_EN.PinMode = GpioPinDriveMode.Output;
            GPIO_STEPPER_EN.Write(true);

            // Controls the MS1 Pin of the Stepper Motor
            GPIO_STEPPER_MS1 = Pi.Gpio[23];
            GPIO_STEPPER_MS1.PinMode = GpioPinDriveMode.Output;
            GPIO_STEPPER_MS1.Write(true);

            // Controls the MS2 Pin of the Stepper Motor
            GPIO_STEPPER_MS2 = Pi.Gpio[24];
            GPIO_STEPPER_MS2.PinMode = GpioPinDriveMode.Output;
            GPIO_STEPPER_MS2.Write(true);

        }
        // Sound buzzer using selector for type of sounds [0-Error, 1-SoftAlert, 2-ConfirmationDing, 3-Doorbell]
        private void BuzzerSound(int soundSelect)
        {
            if (soundSelect == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    MakeSound(800, 300);
                }
            }
            else if (soundSelect == 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    MakeSound(300, 500);
                }
            }
            else if (soundSelect == 2)
            {
                MakeSound(500, 500);
            } 
            else if (soundSelect == 3)
            {
                MakeSound(200, 500);
                MakeSound(800, 300);
                MakeSound(800, 300);
                MakeSound(800, 300);
            }
            void MakeSound(uint timeOn, uint timeOff)
            {
                GPIO_BUZZER.Write(true);
                Pi.Timing.SleepMilliseconds(timeOn);
                GPIO_BUZZER.Write(false);
                Pi.Timing.SleepMilliseconds(timeOff);
            }
        }
        private void MoveStepper(int degrees)
        {



            double steps = (Math.Abs(double.Parse(degrees.ToString())) / 360.0) * STEPS_PER_ROTATION * 16;

            if (degrees > 0)
            {
                GPIO_STEPPER_EN.Write(false);
                GPIO_STEPPER_DIR.Write(false);
            }
            else if (degrees < 0)
            {
                GPIO_STEPPER_EN.Write(false);
                GPIO_STEPPER_DIR.Write(true);
            }
            Pi.Timing.SleepMicroseconds(100);
            for (int i = 0; i < steps; i++)
            {
                GPIO_STEPPER_STEP.Write(true);
                Pi.Timing.SleepMicroseconds(60);
                GPIO_STEPPER_STEP.Write(false);
                Pi.Timing.SleepMicroseconds(60);
            }

            GPIO_STEPPER_EN.Write(true);
        }

        private void TurnDisplayOn()
        {


            if (remainingDisplayTimer > 0)
            {
                remainingDisplayTimer = settings.DisplayOffTimer;
            }
            else
            {

                Pi.Threading.StartThread(CountDown);
                void CountDown()
                {
                    remainingDisplayTimer = settings.DisplayOffTimer;
                    // turn display on
                    GPIO_DISPLAYPOWER.Write(true);
                    // count down to 0
                    while (remainingDisplayTimer > 0)
                    {
                        remainingDisplayTimer--;
                        Pi.Timing.SleepMilliseconds(1000);
                    }
                    // turn display off
                    GPIO_DISPLAYPOWER.Write(false);
                }

            }
        }

        private void InitializeStream()
        {
            // Start the stream
// ExecuteBash("sudo service AFSD_Stream start");

            // Wait a few seconds for the service to start before loading it into the view
            Pi.Timing.SleepMilliseconds(15000);

            libvlc = new LibVLC();
            //media = new Media(libvlc, new Uri(@"rtsp://127.0.0.1:8080/"));
            //media = new Media(libvlc, new Uri(@"rtsp://127.0.0.1:8554/mystream"));
            media = new Media(libvlc, new Uri(@"tcp://127.0.0.1:8554"));
            media.AddOption(":demux=h264,clock-jitter=0,network-caching=3000 --file-caching --network-caching");
            //media = new Media(libvlc, new Uri(@"rtmp://127.0.0.1:/live/stream"));
            VLC.MediaPlayer = new MediaPlayer(libvlc);
            VLC.MediaPlayer.Mute = true;
            VLC.MediaPlayer.Play(media);
        }

        private void DisposeStream()
        {
            VLC.MediaPlayer.Stop();
            VLC.MediaPlayer = null;
            //ExecuteBash("sudo service AFSD_Stream stop");

            //libvlc.Dispose();
            //media.Dispose();
            //Pi.Camera.CloseVideoStream();
        }

        private void TakePicture()
        {
            // var pictureBytes = Pi.Camera.CaptureImageJpeg(640, 480);
            //var targetPath = "/home/pi/picture.jpg";
            //if (File.Exists(targetPath))
            //File.Delete(targetPath);

            //File.WriteAllBytes(targetPath, pictureBytes);
            //Console.WriteLine($"Took picture -- Byte count: {pictureBytes.Length}");
            //ExecuteBash("libcamera-still -o /home/pi/capture.jpg");
            string captureFolder = AppDomain.CurrentDomain.BaseDirectory + "captures";
            if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory))
            {
                Directory.CreateDirectory(captureFolder);
            }
            string shortFilename = DateTimeOffset.Now.ToString("s") + ".jpg";
            string screenshotFilename = captureFolder + "/" + shortFilename;

            var captureBmp = new Bitmap(320, 320, PixelFormat.Format32bppArgb);
            using (var captureGraphic = Graphics.FromImage(captureBmp))
            {
                captureGraphic.CopyFromScreen(0, 0, 0, 0, captureBmp.Size);
                captureBmp.Save(screenshotFilename, ImageFormat.Jpeg);
            }


            string ftpUpload = "curl --user ";
            ftpUpload += settings.ftpUsername + ":" + settings.ftpPassword + " ";
            ftpUpload += "--upload-file " + screenshotFilename + " ";
            ftpUpload += "ftp://" + settings.ftpHost + settings.ftpPath;
            Console.WriteLine(ftpUpload);
            ExecuteBash(ftpUpload);

            twilio.MessageWithPicture("+12609204093",settings.ftpPathHTTP+shortFilename);


        }

        private void ExecuteBash(string CMD)
        {
            string targetPath = "/home/pi/AFSD/LiveCMD.sh";
            if (File.Exists(targetPath))
                File.Delete(targetPath);

            //File.CreateText(targetPath);

            try
            {
                using (StreamWriter writer = File.CreateText(targetPath))
                {
                    writer.WriteLine("#!/bin/bash");
                    //writer.WriteLine(CMD);
                    writer.WriteLine("sed -i \"/^.*init_script.*$/d\" ~/.bashrc");
                    writer.WriteLine("rm $0");
                    writer.WriteLine(CMD);
                }
            }
            catch (Exception exp)
            {
                Console.Write(exp.Message);
            }
            //File.AppendText("#!bin/bash");
            //File.AppendText(CMD);
            //File.AppendText("sed - i \"/^.*init_script.*$/d\" ~/.bashrc");
            //File.AppendText("rm $0");

            //File.
        }

        private void CloseProgram()
        {
            DisposeStream();
            settings.SaveSettingsFile();
            Pi.Timing.SleepMilliseconds(200);
            Close();
        }
        #endregion

        #region Menu_Functions
        private void Menu_Debug()
        {
            Console.WriteLine(menuCurrentIndex);
        }
        private void Menu_Lock_Door()
        {

            int steps = 16;
            GPIO_STEPPER_EN.Write(false);
            GPIO_STEPPER_DIR.Write(true);
            while (GPIO_LOCKED_ENDSTOP.Read())
            {
                Pi.Timing.SleepMicroseconds(100);
                for (int i = 0; i < steps; i++)
                {
                    GPIO_STEPPER_STEP.Write(true);
                    Pi.Timing.SleepMicroseconds(60);
                    GPIO_STEPPER_STEP.Write(false);
                    Pi.Timing.SleepMicroseconds(60);
                }
            }
            GPIO_STEPPER_EN.Write(true);

            Console.WriteLine(GPIO_LOCKED_ENDSTOP.Read());
        }
        private void Menu_Unlock_Door()
        {
            int steps = 16;
            GPIO_STEPPER_EN.Write(false);
            GPIO_STEPPER_DIR.Write(false);
            while (GPIO_UNLOCKED_ENDSTOP.Read())
            {
                Pi.Timing.SleepMicroseconds(100);
                for (int i = 0; i < steps; i++)
                {
                    GPIO_STEPPER_STEP.Write(true);
                    Pi.Timing.SleepMicroseconds(60);
                    GPIO_STEPPER_STEP.Write(false);
                    Pi.Timing.SleepMicroseconds(60);
                }
            }
            GPIO_STEPPER_EN.Write(true);

        }
        private void ShowDebugMenu()
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(ToggleView, Avalonia.Threading.DispatcherPriority.MaxValue);

            void ToggleView(){
                if (!DebugMenu.IsVisible)
                {
                    Panel_VideoFrame.IsVisible = false;
                    DebugMenu.IsVisible = true;
                }
                else
                {
                    Panel_VideoFrame.IsVisible = true;
                    DebugMenu.IsVisible = false;
                }
            }   

        }
        private void Reboot_System()
        {
            ExecuteBash("sudo reboot");
        }
        #endregion
    }
}
