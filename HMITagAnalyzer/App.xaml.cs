using System.Threading.Tasks;
using System.Windows;

namespace HMITagAnalyzer
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }
        
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // Log the exception

            // Show the exception to the user
            MessageBox.Show($"An error occurred in a background task:\n{e.Exception.Message}", "Task Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Prevent the application from crashing
            e.SetObserved();
        }
    }
}