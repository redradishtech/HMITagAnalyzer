using System;
using System.Windows;
using log4net.Appender;
using log4net.Core;

namespace HMITagAnalyzer
{
    /**
     * Logging appender that writes to the GUI.
     */
    public class TextBlockLogAppender : AppenderSkeleton
    {
        public static Action<string> LogAction { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            // Format the log message
            var message = RenderLoggingEvent(loggingEvent);

            // Ensure the log action is called on the UI thread
            Application.Current.Dispatcher.Invoke(() => { LogAction?.Invoke(message); });
        }
    }
}