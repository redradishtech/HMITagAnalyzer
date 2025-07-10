using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using log4net;
using log4net.Config;
using log4net.Layout;
using Microsoft.Win32;
using SEL.API.Controls;

namespace HMITagAnalyzer
{
    using TagName = String;
    using DiagramName = String;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int _invalidTagCount;
        private readonly SortedSet<TagName> _invalidTags = [];
        private Dictionary<TagName, Dictionary<DynamicControl, List<DiagramName>>>? _reusedTagLocations;
        private readonly SortedSet<TagName> _tagUsages = [];
        private int _usedTagCount;
        private string _windowTitle = "SEL HMI Diagram Tag Analyzer";

        private readonly ILog _logger;

        public MainWindow()
        {
            InitializeComponent();
            ConfigureLog4Net();
            DataContext = this; // Set the DataContext for binding. Without this, changes to e.g. InvalidTagCount don't propagate to the UI

            // Link the custom appender to the ActivityPane
            TextBlockLogAppender.LogAction = message =>
            {
                ActivityMessage += $"{message}";
                ActivityPane.ScrollToEnd();
            };

            _logger = LogManager.GetLogger(typeof(MainWindow));
            _logger.Info("Application started.");
        }

        public int InvalidTagCount
        {
            get => _invalidTagCount;
            set
            {
                // update the UI when the value changes
                if (_invalidTagCount != value)
                {
                    _invalidTagCount = value;
                    OnPropertyChanged(nameof(InvalidTagCount));
                }
            }
        }

        public int UsedTagCount
        {
            get => _usedTagCount;
            set
            {
                // update the UI when the value changes
                if (_usedTagCount != value)
                {
                    _usedTagCount = value;
                    OnPropertyChanged(nameof(UsedTagCount));
                }
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string ActivityMessage
        {
            get => ActivityPane.Text;
            set => ActivityPane.Text = value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Create an OpenFileDialog
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select a File",
                Filter = "HMI Project|*.hprb",
                DefaultExt = ".hprb",
                // InitialDirectory = ProjectPathUtils.GetLatestProjectDirectory(),
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() != true) return;
            // Show the dialog and check if the user selected a file
            {
                var button = sender as Button;
                try
                {
                    button.IsEnabled = false;

                    var projectPaths = openFileDialog.FileNames;

                    foreach (var projectPath in projectPaths)
                    {
                        // Read the file contents
                        _logger.Info($"Processing {projectPath}...");
                        WindowTitle = $"HMI Diagram Tag Analyzer - {projectPath}";

                        var projInfo = new HMIProjectInfo(projectPath, _logger);
                        _tagUsages.UnionWith(
                            projInfo.TagUsages.Keys.Select(t => t.StartsWith("Tags.") ? t.Substring(5) : t));
                        _invalidTags.UnionWith(projInfo.InvalidTags);
                        _reusedTagLocations = projInfo.ReusedTagLocations();

                    }

                    UpdateInvalidTagsTextBox();
                    UpdateUsedTagsTextBox();
                    UpdateReusedTagsTextBox();
                    _logger.Info("Finished processing");
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }
        
        private void UpdateReusedTagsTextBox()
        {
            if (_reusedTagLocations != null && _reusedTagLocations.Any())
            {
                var dupTagsOut = new StringWriter();

                foreach (var tagName in _reusedTagLocations.Keys)
                {
                    var tagUsage = _reusedTagLocations[tagName];
                    dupTagsOut.WriteLine(tagName);
                    foreach (var control in tagUsage.Keys)
                    {
                        var diagrams = tagUsage[control];
                        foreach (var diagramName in diagrams)
                            dupTagsOut.WriteLine($"\t\t{diagramName} -> {control.Name}");
                    }

                    dupTagsOut.ToString(); // 
                }

                ReusedTagsTextBox.Text += dupTagsOut.ToString();
            }
        }

        private void UpdateUsedTagsTextBox()
        {
            if (_tagUsages.Any()) UsedTagsTextBox.Text += string.Join(Environment.NewLine, _tagUsages);
            UsedTagCount = _tagUsages.Count;
        }

        private void UpdateInvalidTagsTextBox()
        {
            if (_invalidTags.Any())
            {
                var tagsOut = new StringWriter();
                _logger.Info($"Found {_invalidTags.Count()} invalid tags:");
                foreach (var invalidTag in _invalidTags) tagsOut.WriteLine(invalidTag);

                InvalidTagsTextBox.Text += tagsOut.ToString();
                InvalidTagCount = _invalidTags.Count;
            }
        }

        private void ConfigureLog4Net()
        {
            var layout = new PatternLayout
            {
                ConversionPattern = "%date %-5level - %message%newline"
            };
            layout.ActivateOptions();

            var appender = new TextBlockLogAppender
            {
                Layout = layout
            };
            appender.ActivateOptions();

            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(repository, appender);
        }
    }
}