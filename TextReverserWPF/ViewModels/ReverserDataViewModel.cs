using CommunityToolkit.Mvvm.Input;
using FileProcessor;
using FileProcessor.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace TextReverserWPF.ViewModel
{
    public partial class ReverserDataViewModel : BaseDataViewModel
    {
        private const string textfileFilter = "Text Files (*.txt, *.tab)|*.txt;*.tab|Office Documents (*.docx, *.doc)|*.docx;*.doc";

        public List<string> ReverseTypes => new() { 
            "Char",
            "Word",
            "Sentence",
        };
        
        public List<string> ArchiveTypes => new() { 
            "zip",
            "7z",
            "none",
        };

        public ReverserDataViewModel()
        {
            Title = "TextReverser";
        }

        [RelayCommand]
        void SelectInputFile()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = textfileFilter;
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
              
                if (openFileDialog.ShowDialog() != null)
                {
                    ReverserData.InputFile = openFileDialog.FileName;
                    InputFileNameText = openFileDialog.FileName.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get input file path: {ex.Message}");
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        void SelectInputDirectory()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    ValidateNames = false,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = ReverserData.InputDirectory
                };
                string folder = null;
                if (dialog.ShowDialog() == true && dialog.FileName != null)
                {
                    folder = System.IO.Path.GetDirectoryName(dialog.FileName);
                    ReverserData.InputDirectory = folder;
                    InputDirectoryNameText = folder.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get input folder path: {ex.Message}");
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        void SelectOutputFile()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = textfileFilter;
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (openFileDialog.ShowDialog() != null)
                {
                    ReverserData.OutputFile = openFileDialog.FileName;
                    OutputFileNameText = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get output file path: {ex.Message}");
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        async Task StartFileProcessing()
        {
            Mutex mutexTimeSync = new Mutex();
            try
            {
                if (string.IsNullOrEmpty(ReverserData.ReverseType) || string.IsNullOrEmpty(ReverserData.InputFile))
                {
                    string errorMessage = "Missing information! Please provide all required fields.";
                    MessageBox.Show(errorMessage, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning); 
                    return;
                }

                ReverserData.ReverseType = ReverserData.ReverseType.ToLower();

                if (ReverserData.OutputFile == "" || ReverserData.OutputFile == null)
                {
                    string inputFileName = Path.GetFileName(ReverserData.InputFile);
                    ReverserData.OutputFile = $"{Path.GetDirectoryName(ReverserData.InputFile)}/I{ReverserData.ReverseType[0]}_{inputFileName}.{ReverserData.ExtensionType}";
                }

                long totalSizeLeft = new FileInfo(ReverserData.InputFile).Length;

                Action<TimeSpan> updateTimeLeftLable = (TimeSpan timeLeft) =>
                {
                    TimeLeft = "Залишилося часу"
                                + (timeLeft.Days != 0 ? $" : {timeLeft.Days} днів" : "")
                                + (timeLeft.Minutes != 0 ? $" : {timeLeft.Minutes} хвилин" : "")
                                + (timeLeft.Seconds != 0 ? $" : {timeLeft.Seconds} секунд" : "0 секунд");

                };

                Action<long> updateFileSizeLeft = (long portionSize) =>
                {
                    totalSizeLeft -= portionSize;

                    mutexTimeSync.WaitOne();

                    if (FileProcessorWorker.reversalTimeOfTenKiloBites.Count > 0)
                    {
                        double averageValue = FileProcessorWorker.reversalTimeOfTenKiloBites.Where(item => !double.IsInfinity(item)).Average();

                        TimeSpan averageTimeSpan = TimeSpan.FromMilliseconds(averageValue * (totalSizeLeft / 1024));

                        updateTimeLeftLable(averageTimeSpan);
                    }
                    mutexTimeSync.ReleaseMutex();
                };

                // Start a new thread or use a Task to call the ProcessFile method
                await Task.Run(() => { FileProcessorWorker.ProcessFile(ReverserData, updateFileSizeLeft); });
                
                Progress = 1;
                ReverserData.OutputFile = "";
                MessageBox.Show("Документ інвертовано", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                Progress = 0;
                TimeLeft = "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get continue bad parameters: {ex.Message}");
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        
        [RelayCommand]
        async Task StartDirectoryProcessing()
        {
            try
            {
                
                Action<double> updateProgress = (double progressStep) => Progress += progressStep;

                Action<TimeSpan> updateTimeLeftLable = (TimeSpan timeLeft) =>
                {
                    TimeLeft = "Залишилося часу"
                                + (timeLeft.Days != 0 ? $" : {timeLeft.Days} днів" : "")
                                + (timeLeft.Minutes != 0 ? $" : {timeLeft.Minutes} хвилин" : "")
                                + (timeLeft.Seconds != 0 ? $" : {timeLeft.Seconds} секунд" : "0 секунд");

                };

                if (string.IsNullOrEmpty(ReverserData.ReverseType) || string.IsNullOrEmpty(ReverserData.InputDirectory))
                {
                    string errorMessage = "Missing information! Please provide all required fields.";
                    MessageBox.Show(errorMessage, "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                UiEnabled = false;
                ReverserData.ReverseType = ReverserData.ReverseType.ToLower();

                if (ReverserData.OutputFile == "" || ReverserData.OutputFile == null)
                {
                    string inputDirectory = Path.GetFileName(ReverserData.InputDirectory);
                    ReverserData.OutputFile = $"{Path.GetDirectoryName(ReverserData.InputFile)}/I{ReverserData.ReverseType[0]}_{inputDirectory}.{ReverserData.ExtensionType}";
                }

                // Start a new thread or use a Task to call the ProcessFile method
                await FileProcessorWorker.ProcessDirectory(ReverserData, updateProgress, updateTimeLeftLable);
  
                if (Progress >= 1)
                {
                    MessageBox.Show("Папку інвертовано", "Іноформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    Progress = 0;
                    UiEnabled = true;
                }
                ReverserData.OutputFile = "";
                TimeLeft = "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get continue bad parameters: {ex.Message}");
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
