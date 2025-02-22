using System;
using System.Diagnostics;
using ShellProgressBar;

namespace Ovation.FasterQC.Net.Utils
{
    public class TimedSequenceProgressBar : ProgressBar
    {
        private readonly Stopwatch elapsedTime = new();

        private readonly ISequenceReader sequenceReader;

        private static readonly ProgressBarOptions progressBarOptions = new()
        {
            ProgressCharacter = '=',
            DisplayTimeInRealTime = false,
            ShowEstimatedDuration = true
        };

        public TimedSequenceProgressBar(ISequenceReader sequenceReader) : base(100, "Processing...", progressBarOptions)
        {
            this.sequenceReader = sequenceReader;
            elapsedTime.Start();
        }

        public void Update(bool force = false)
        {
            var read = sequenceReader.SequencesRead;
            var percent = sequenceReader.ApproximateCompletion;

            if (force || read % CliOptions.UpdatePeriod == 0)
            {
                if (percent > 0)
                {
                    var remainingTime = elapsedTime.ElapsedMilliseconds / percent * 100.0;
                    EstimatedDuration = TimeSpan.FromMilliseconds(remainingTime);
                }

                AsProgress<double>().Report(percent / 100.0);
                Message = $"{read.WithSsiUnits()} sequences completed";
            }
        }
    }
}
