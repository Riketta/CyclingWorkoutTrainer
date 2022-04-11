using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace Cycling_Workout_Trainer
{
    public class TrainingStep
    {
        public int Seconds;
        public int RPM;
        public string Description;
    }

    public class TickEventArgs
    {
        public TickEventArgs(int remainingSeconds) { RemainingStepSeconds = remainingSeconds; }
        public int RemainingStepSeconds { get; }
    }

    public class NextStepEventArgs
    {
        public NextStepEventArgs(TrainingStep step, TrainingStep nextStep) { NewStep = step; NextStep = nextStep; }
        public TrainingStep NewStep { get; }
        public TrainingStep NextStep { get; }
    }

    class Training
    {
        public static Training Default = new Training("Big Training")
        {
            trainingSteps = new TrainingStep[]
            {
                // Разминка
                new TrainingStep() { Seconds = 5 * 60, RPM = 60, Description = "Режим отдыха" },
                new TrainingStep() { Seconds = 2 * 60, RPM = 80, Description = "Круиз (2-1)" },
                new TrainingStep() { Seconds = 3 * 60, RPM = 60, Description = "Режим отдыха" },
                
                // Основная часть
                new TrainingStep() { Seconds = 8 * 60, RPM = 80, Description = "Круиз (2-1)" },
                new TrainingStep() { Seconds = 2 * 60, RPM = 60, Description = "Режим отдыха (вода)" },
                new TrainingStep() { Seconds = 8 * 60, RPM = 80, Description = "Круиз (2-2)" },
                new TrainingStep() { Seconds = 2 * 60, RPM = 60, Description = "Режим отдыха (вода)" },
                new TrainingStep() { Seconds = 5 * 60, RPM = 80, Description = "Интенсивно (2-3)" },
                new TrainingStep() { Seconds = 5 * 60, RPM = 80, Description = "Круиз (2-1)" },

                // Заминка
                new TrainingStep() { Seconds = 5 * 60, RPM = 60, Description = "Режим отдыха" },
            },
        };

        public static Training DefaultSmall = new Training("Small Training")
        {
            trainingSteps = new TrainingStep[]
            {
                // Разминка
                new TrainingStep() { Seconds = 3 * 60, RPM = 60, Description = "Режим отдыха" },
                
                // Основная часть
                new TrainingStep() { Seconds = 6 * 60, RPM = 80, Description = "Круиз (2-1)" },
                new TrainingStep() { Seconds = 2 * 60, RPM = 60, Description = "Режим отдыха (вода)" },
                new TrainingStep() { Seconds = 6 * 60, RPM = 80, Description = "Интенсивно (2-2)" },

                // Заминка
                new TrainingStep() { Seconds = 3 * 60, RPM = 60, Description = "Режим отдыха" },
            },
        };

        public static Training Debug = new Training("Debug Training")
        {
            trainingSteps = new TrainingStep[]
            {
                new TrainingStep() { Seconds = 8, RPM = 60, Description = "Режим отдыха" },
                new TrainingStep() { Seconds = 15, RPM = 80, Description = "Круиз (2-1)" },
                new TrainingStep() { Seconds = 8, RPM = 60, Description = "Режим отдыха (вода)" },
                new TrainingStep() { Seconds = 15, RPM = 80, Description = "Интенсивно (2-2)" },
                new TrainingStep() { Seconds = 8, RPM = 60, Description = "Режим отдыха" },
            },
        };

        public delegate void TickEventHandler(object sender, TickEventArgs e);
        public event TickEventHandler TickEvent;
        public delegate void NextStepEventHandler(object sender, NextStepEventArgs e);
        public event NextStepEventHandler NextStepEvent;

        public readonly string Name;
        public int TotalSteps { get => trainingSteps.Length; }
        public int CurrentStep { get => step + 1; }
        public int TotalTrainingTime
        {
            get
            {
                if (totalTrainingTime == 0)
                    foreach (var step in trainingSteps)
                        totalTrainingTime += step.Seconds;

                return totalTrainingTime;
            }
        }
        int totalTrainingTime = 0;

        TrainingStep[] trainingSteps;
        int step = 0;
        int remainingStepSeconds = 0; // countdown to 0

        Timer timer = new Timer();

        public Training(string name)
        {
            Name = name;
        }

        public TrainingStep Start()
        {
            if (trainingSteps == null || trainingSteps.Length == 0)
                return null;

            step = 0;
            remainingStepSeconds = trainingSteps[0].Seconds;

            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 1000;
            timer.Start();

            NextStepEvent?.Invoke(this, new NextStepEventArgs(trainingSteps[0], PeekNextStep()));
            TickEvent?.Invoke(this, new TickEventArgs(remainingStepSeconds)); // TODO: remove?

            return trainingSteps[0];
        }

        public void Stop()
        {
            timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            remainingStepSeconds--;

            if (remainingStepSeconds <= 0)
            {
                TrainingStep nextStep = NextStep();
                if (nextStep == null)
                    (sender as Timer).Stop();
            }

            TickEvent?.Invoke(this, new TickEventArgs(remainingStepSeconds));
        }

        public int GetCurrentProgress()
        {
            int currentSeconds = 0; // time spent on previous steps

            for (int i = 0; i < step; i++)
                currentSeconds += trainingSteps[i].Seconds;

            return currentSeconds;
        }

        public TrainingStep GetCurrentStep()
        {
            return trainingSteps[step];
        }

        public TrainingStep PeekNextStep()
        {
            if (step + 1 < trainingSteps.Length)
                return trainingSteps[step + 1];

            return null;
        }

        public TrainingStep NextStep()
        {
            TrainingStep nextStep = PeekNextStep();
            if (nextStep != null)
            {
                remainingStepSeconds = nextStep.Seconds;
                step++;
                TrainingStep nextNextStep = PeekNextStep();
                NextStepEvent?.Invoke(this, new NextStepEventArgs(nextStep, nextNextStep)); // TODO: invoke even if null to allow training finnalyzing?
            }

            return nextStep;
        }

        public void SyncTimer()
        {
            timer.Stop();
            TickEvent?.Invoke(this, new TickEventArgs(remainingStepSeconds));
            timer.Start();
        }
    }
}
