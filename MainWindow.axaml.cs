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
        IGpioPin GPIO_ENCODER_A;
        IGpioPin GPIO_ENCODER_B;
        IGpioPin GPIO_ENCODER_BUTTON;
        IGpioPin GPIO_DOORBELL_BUTTON;

        const int STEPS_PER_ROTATION = 200;

        Button btnCounterClockwise;
        Button btnClockwise;
        Button btnRotaryClick;
        Button btnExit;

        TextBlock MenuPrevious;
        TextBlock MenuCurrent;
        TextBlock MenuNext;
        TextBlock CurrentTime;

        VideoView VLC;

        int remainingDisplayTimer = 0;
        int countA = 0;
        int countB = 0;
        int countC = 0;

        string lastTurn = "NULL";

        LibVLC libvlc;

        Media media;

        int videoByteCount = 0;
        int videoEventCount = 0;
        bool processingInput = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Attach XML controls to local variables
            btnCounterClockwise = this.Find<Button>("btnCounterClockwise");
            btnClockwise = this.Find<Button>("btnClockwise");
            btnRotaryClick = this.Find<Button>("btnRotaryClick");
            btnExit = this.Find<Button>("btnExit");
            MenuPrevious = this.Find<TextBlock>("MenuPrevious");
            MenuCurrent = this.Find<TextBlock>("MenuCurrent");
            MenuNext = this.Find<TextBlock>("MenuNext");
            CurrentTime = this.Find<TextBlock>("TextCurrentTime");
            VLC = this.Find<VideoView>("VLC_View");

            // Add event handlers to XML controls
            btnCounterClockwise.Click += btnCounterClockwise_OnClick;
            btnClockwise.Click += btnClockwise_OnClick;
            btnRotaryClick.Click += btnRotaryClick_OnClick;
            btnExit.Click += btnExit_OnClick;


            // Only execute the following setup if the program is actually running on the Pi
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

            }

        }

        private void InitializeStream()
        {
            libvlc = new LibVLC();
            //media = new Media(libvlc, new Uri(@"172.16.69.40/hls/index.m3u8"));
            string[] args = { };
           //media = new Media(libvlc, new Uri(@"rtsp://127.0.0.1:8080/"));
            media = new Media(libvlc, new Uri(@"http://127.0.0.1:8080/"));

            //media.AddOption(":network-caching=600"); http - reconnect
            //media.AddOption(":demux=h264");
            //media = new Media(libvlc, new Uri(@"http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
            //using var media = new Media(libvlc, new Uri(""));

            //using var mediaplayer = new MediaPlayer(media);

            //mediaplayer.Play();
            VLC.MediaPlayer = new MediaPlayer(libvlc);
            VLC.MediaPlayer.Mute = true;
            //VLC.MediaPlayer.Play(media);
            VLC.MediaPlayer.Play(media);
            /*var videoSettings = new CameraVideoSettings()
            {
                CaptureTimeoutMilliseconds = 0,
                CaptureDisplayPreview = false,
                ImageFlipVertically = true,
                CaptureExposure = CameraExposureMode.Auto,
                CaptureWidth = 400,
                CaptureHeight = 400
            };
            Pi.Camera.OpenVideoStream(videoSettings, onDataCallback: (data) => { videoByteCount += data.Length; },
            onExitCallback: null);*/
            //media = new Media(libvlc, );
            //VLC.MediaPlayer.Play();

        }
        private void DisposeStream()
        {
            VLC.MediaPlayer.Dispose();
            libvlc.Dispose();
            media.Dispose();
            Pi.Camera.CloseVideoStream();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #region Methods




        #endregion


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
                        RotaryUp();
                    }
                    else
                    {
                        while (GPIO_ENCODER_B.Read() == false || GPIO_ENCODER_A.Read() == false)
                        {
                            Pi.Timing.SleepMicroseconds(20);
                        }
                        RotaryDown();
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
                    while (GPIO_DOORBELL_BUTTON.Read() == false)
                    {
                        Pi.Timing.SleepMicroseconds(20);
                    }
                }
                Pi.Timing.SleepMicroseconds(50);
            }
        }


        private void UpdateUI()
        {
            int itterationCount = 0;

            while (true)
            {
                // Execute update on Avalonia's UI thread
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(Update, Avalonia.Threading.DispatcherPriority.Render);
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
                CurrentTime.Text = (System.DateTime.Now.ToString("t"));
                MenuCurrent.Text = countA.ToString();
                MenuNext.Text = countB.ToString();
                MenuPrevious.Text = countC.ToString();
                //Console.WriteLine("Count: "+videoByteCount.ToString());
            }

        }

        #endregion

        #region Event Handlers
        private void btnCounterClockwise_OnClick(object sender, RoutedEventArgs e)
        {
            //Pi.Threading.StartThread(GPIO_Initializer);
            //Pi.Threading.StartThread(TurnDisplayOn);
            //MenuNext.Text = GPIO_STEPPER_EN.Value.ToString();
            var GPIO_PWM = (GpioPin)Pi.Gpio[12];
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
            GPIO_PWM.PinMode = GpioPinDriveMode.PwmOutput;
            GPIO_PWM.PwmMode = PwmMode.Balanced;

        }

        private void btnClockwise_OnClick(object sender, RoutedEventArgs e)
        {
            MenuPrevious.Text = remainingDisplayTimer.ToString();

        }

        private void btnRotaryClick_OnClick(object sender, RoutedEventArgs e)
        {
            //MenuNext.Text = lastTurn;
            //UpdateUI();
            MoveStepper(360);
        }
        private void btnExit_OnClick(object sender, RoutedEventArgs e)
        {
            DisposeStream();
            Pi.Timing.SleepMilliseconds(1000);
            Close();
        }

        private void RotaryClick()
        {
            //IO_DisposeInterrupts();
            Pi.Timing.SleepMicroseconds(100);
                //IO_RegisterInterrupts();
            Pi.Threading.StartThread(TurnDisplayOn);
                countC++;
                int num = 360;
                MoveStepper(num);
                processingInput = false;
            
            


        }

        private void RotaryUp()
        {
            if (!processingInput)
            {
                processingInput = true;
                //GPIO_DisposeInterrupts();
            Pi.Timing.SleepMicroseconds(100);
            //GPIO_RegisterInterrupts();
            countA++;
            TurnDisplayOn();
                processingInput = false;
            }
        }

        private void RotaryDown()
        {
                if (!processingInput)
                {
                    processingInput = true;
                    //GPIO_DisposeInterrupts();
            Pi.Timing.SleepMicroseconds(100);
            //GPIO_RegisterInterrupts();
            countB++;
            TurnDisplayOn();
                processingInput = false;
            }
        }



        #endregion

        #region General_Functions
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
                remainingDisplayTimer = int.Parse(AFSD.Properties.Resources.maxDisplayTimer);
            }
            else
            {

                Pi.Threading.StartThread(CountDown);
                void CountDown()
                {
                    remainingDisplayTimer = int.Parse(AFSD.Properties.Resources.maxDisplayTimer);
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
        #endregion
    }
}
