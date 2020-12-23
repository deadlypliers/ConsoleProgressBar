using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace ConsoleProgressBar
{
    /// <summary>
    /// An ASCII progress bar
    /// </summary>
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private const int BlockCount = 20;
        private const string Animation = @"|/-\";

        private readonly Timer timer;
        private readonly TimeSpan animationInterval;

        private double currentProgress;
        private string currentText;
        private string verboseProgressText;
        private bool disposed;
        private int animationIndex;

        private readonly int total;

        public ProgressBar()
        {
            timer = new Timer(TimerEventHandler);
            animationInterval = TimeSpan.FromSeconds(1.0 / 8);
            currentText = string.Empty;

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
                ResetTimer();
        }

        public ProgressBar(int total)
            : this()
        {
            this.total = total;
        }

        /// <summary>
        /// Report progress as a percentage
        /// </summary>
        /// <param name="progress">Decimal value in [0..1] range</param>
        public void Report(double progress)
        {
            if (progress > 1)
                throw new ArgumentOutOfRangeException(nameof(progress), progress, "Progress must be a decimal between [0..1]");

            Interlocked.Exchange(ref currentProgress, progress);
        }

        /// <summary>
        /// Report progress via index and total (Useful for iterating over collections)
        /// </summary>
        /// <param name="progress">Decimal value in [0..1] range</param>
        /// <param name="verboseProgress">A string representing a verbose progress message</param>
        public void Report(double progress, string verboseProgress)
        {
            if (progress > 1)
                throw new ArgumentOutOfRangeException(nameof(progress), progress, "Progress must be a decimal between [0..1]");

            Interlocked.Exchange(ref verboseProgressText, verboseProgress);
            Interlocked.Exchange(ref currentProgress, progress);
        }

        public void Report(int index)
        {
            if (total == 0)
                throw new ArgumentOutOfRangeException(nameof(total), total, "Total must be defined in the constructor to call this method.");

            if (index > total)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be less than or equal to total");

            var progress = (double) index / total;

            Interlocked.Exchange(ref currentProgress, progress);
        }

        public void Report(int index, string verboseProgress)
        {
            if (total == 0)
                throw new ArgumentOutOfRangeException(nameof(total), total, "Total must be defined in the constructor to call this method.");

            if (index > total)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be less than or equal to total");

            var progress = (double)index / total;

            Interlocked.Exchange(ref verboseProgressText, verboseProgress);
            Interlocked.Exchange(ref currentProgress, progress);
        }

        private void TimerEventHandler(object state)
        {
            lock (timer)
            {
                if (disposed)
                    return;

                var progressBlockCount = (int) (currentProgress * BlockCount);
                var text = $"[{new string('#', progressBlockCount)}{new string('.', BlockCount - progressBlockCount)}]" +
                    $" {currentProgress.ToString("P", new CultureInfo("en-US"))}";


                if (!string.IsNullOrWhiteSpace(verboseProgressText))
                    text += $" {verboseProgressText}";

                text += $" {Animation[animationIndex++]}";

                if (animationIndex > 3)
                    animationIndex = 0;

                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            // Get length of common portion
            var commonPrefixLength = 0;
            var commonLength = Math.Min(currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            var outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            var overlapCount = currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            currentText = text;
        }

        private void ResetTimer()
        {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (timer)
            {
                disposed = true;
                UpdateText(string.Empty);
            }
        }
    }
}