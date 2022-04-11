using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;
using Plugin.SimpleAudioPlayer;
using System.Reflection;
using System.IO;
using Xamarin.Essentials;

namespace Cycling_Workout_Trainer
{
    public partial class MainPage : ContentPage
    {
        static readonly string TimerFormat = "mm:ss";
        static readonly TimeSpan Second = new TimeSpan(0, 0, 1);
        static readonly string metronome1Path = "Resources.metronome1.mp3";
        static readonly string metronome2Path = "Resources.metronome2.mp3";

        StackLayout layout = new StackLayout();
        Label labelTotalProgress = new Label() { HorizontalTextAlignment = TextAlignment.Center, FontSize = 32 };
        Label labelCurrentRPM = new Label() { HorizontalTextAlignment = TextAlignment.Center, FontSize = 40, Padding = new Thickness(0, 30, 0, 5) };
        Label labelCurrentTimer = new Label() { HorizontalTextAlignment = TextAlignment.Center, FontSize = 60 };
        Label labelCurrentDescription = new Label() { HorizontalTextAlignment = TextAlignment.Center, FontSize = 28 };
        Label labelNextData = new Label() { HorizontalTextAlignment = TextAlignment.End, FontSize = 24, Padding = new Thickness(0, 25, 20, 0) };
        Label labelNextDescription = new Label() { HorizontalTextAlignment = TextAlignment.End, FontSize = 24, Padding = new Thickness(0, 0, 20, 20) };
        Button buttonNextStep = new Button() { Text = "Next Step" };
        Picker trainingPicker = new Picker() { Title = "Trainings", VerticalOptions = LayoutOptions.CenterAndExpand };
        Label labelStrobe = new Label() { Text = "Strobe", HorizontalTextAlignment = TextAlignment.Start, FontSize = 22 };
        Label labelMetronome = new Label() { Text = "Metronome", HorizontalTextAlignment = TextAlignment.Start, FontSize = 22 };
        CheckBox checkBoxStrobe = new CheckBox();
        CheckBox checkBoxMetronome = new CheckBox();

        bool strobeState = false;
        bool metronomeState = false;
        ISimpleAudioPlayer metronome1 = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
        ISimpleAudioPlayer metronome2 = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();

        List<Training> trainingList = new List<Training>();
        Training currentTraining = null;
        TimeSpan stepStopwatch = TimeSpan.Zero;
        TimeSpan trainingStopwatch = TimeSpan.Zero;
        Timer rpmTimer = new Timer();

        public MainPage()
        {
            //UIApplication.SharedApplication.IdleTimerDisabled = true;
            DeviceDisplay.KeepScreenOn = true;

            InitializeComponent();
            BindingContext = this;

#if DEBUG
            /*
            labelTotalProgress.Text = "Total: 42:00  -  Now: 62%";
            labelCurrentRPM.Text = "85 RPM";
            labelCurrentTimer.Text = "02:16";
            labelCurrentDescription.Text = "Интенсивно (3-3)";
            labelNextData.Text = "Next: 03:20, 60 RPM";
            labelNextDescription.Text = "Режим труда";
            
            rpmTimer.Interval = 60 * 1000 / 80 * 2; // 60 seconds / 2 * RPM
            */

            trainingList.Add(Training.Debug);
#endif
            trainingList.Add(Training.Default);
            trainingList.Add(Training.DefaultSmall);

            foreach (var training in trainingList)
                trainingPicker.Items.Add(training.Name);


            rpmTimer.Elapsed += RpmTimer_Elapsed;
            buttonNextStep.Clicked += ButtonNextStep_Clicked;
            trainingPicker.SelectedIndexChanged += TrainingPicker_SelectedIndexChanged;

            Grid grid = new Grid
            {
                VerticalOptions = LayoutOptions.CenterAndExpand,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                }
            };

            grid.Children.Add(checkBoxStrobe, 0, 0);
            grid.Children.Add(labelStrobe, 1, 0);
            grid.Children.Add(checkBoxMetronome, 2, 0);
            grid.Children.Add(labelMetronome, 3, 0);

            layout.Padding = new Thickness(10, 10, 10, 5);
            layout.Children.Add(trainingPicker);
            layout.Children.Add(labelTotalProgress);
            layout.Children.Add(labelCurrentRPM);
            layout.Children.Add(labelCurrentTimer);
            layout.Children.Add(labelCurrentDescription);
            layout.Children.Add(labelNextData);
            layout.Children.Add(labelNextDescription);
            layout.Children.Add(buttonNextStep);
            layout.Children.Add(grid);
            
            Content = layout;



            var assembly = IntrospectionExtensions.GetTypeInfo(typeof(MainPage)).Assembly;
            string resource1Path = nameof(Cycling_Workout_Trainer) + "." + metronome1Path;
            string resource2Path = nameof(Cycling_Workout_Trainer) + "." + metronome2Path;
            //foreach (var res in assembly.GetManifestResourceNames())
            //    System.Diagnostics.Debug.WriteLine("Found resource: " + res);

            Stream stream1 = assembly.GetManifestResourceStream(resource1Path);
            Stream stream2 = assembly.GetManifestResourceStream(resource2Path);
            metronome1.Load(stream1);
            metronome2.Load(stream2);
        }

        private void StartTraining(string name)
        {
            currentTraining = trainingList.FirstOrDefault(t => t.Name == name);
            rpmTimer.Stop();

            stepStopwatch = TimeSpan.Zero;
            trainingStopwatch = TimeSpan.Zero;

            currentTraining.NextStepEvent += CurrentTraining_NextStepEvent;
            currentTraining.TickEvent += CurrentTraining_TickEvent;
            TrainingStep firstStep = currentTraining.Start();
        }

        private void UpdateNewStepData(TrainingStep step, TrainingStep nextStep)
        {
            rpmTimer.Interval = 60 * 1000 / (2 * step.RPM);
            rpmTimer.Stop();
            rpmTimer.Start();

            stepStopwatch = TimeSpan.Zero + new TimeSpan(0, 0, step.Seconds);

            labelCurrentRPM.Text = string.Format("{0} RPM", step.RPM);
            //labelCurrentTimer.Text = string.Format("{0}", SecondsToTimerText(step.Seconds));
            labelCurrentDescription.Text = step.Description;

            if (nextStep != null)
            {
                labelNextData.Text = string.Format("Next: {0} - {1} RPM", SecondsToTimerText(nextStep.Seconds), nextStep.RPM);
                labelNextDescription.Text = nextStep.Description;
            }
            else
            {
                labelNextData.Text = "Finish!";
                labelNextDescription.Text = "";
            }
        }

        private void UpdateProgress(int remainingStepSeconds)
        {
            labelCurrentTimer.Text = string.Format("{0}", SecondsToTimerText(Convert.ToInt32(stepStopwatch.TotalSeconds)));
            labelTotalProgress.Text = string.Format(@"{0}\{1} - Step {2}\{3}",
                SecondsToTimerText(Convert.ToInt32(trainingStopwatch.TotalSeconds)), SecondsToTimerText(currentTraining.TotalTrainingTime), currentTraining.CurrentStep, currentTraining.TotalSteps);

            trainingStopwatch += Second;
            stepStopwatch -= Second;
        }

        private string SecondsToTimerText(int seconds)
        {
            return new DateTime().AddSeconds(seconds).ToString(TimerFormat);
        }

        private void CurrentTraining_NextStepEvent(object sender, NextStepEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UpdateNewStepData(e.NewStep, e.NextStep);
            });
        }

        private void CurrentTraining_TickEvent(object sender, TickEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UpdateProgress(e.RemainingStepSeconds);
            });
        }

        private void RpmTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (checkBoxStrobe.IsChecked)
                {
                    BackgroundColor = strobeState ? Color.Pink : Color.LightBlue;
                    layout.BackgroundColor = strobeState ? Color.Pink : Color.LightBlue;

                    strobeState = !strobeState;
                }

                if (checkBoxMetronome.IsChecked)
                {
                    if (metronomeState)
                        metronome1.Play();
                    else
                        metronome2.Play();

                    metronomeState = !metronomeState;
                }
            });
        }

        private void TrainingPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentTraining != null)
                currentTraining.Stop();
            StartTraining((sender as Picker).SelectedItem.ToString());
        }

        private void ButtonNextStep_Clicked(object sender, EventArgs e)
        {
            TrainingStep step = currentTraining.NextStep();
            if (step != null)
            {
                currentTraining.SyncTimer();
                trainingStopwatch = new TimeSpan(0, 0, currentTraining.GetCurrentProgress());
            }
        }
    }
}
