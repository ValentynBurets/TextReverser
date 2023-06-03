using CommunityToolkit.Mvvm.Input;
using FileProcessor;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                    InputFileNameText = $"Selected Input File: {openFileDialog.FileName}";
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
                CancellationToken cancellationToken = new CancellationToken();
                string currentDirectory = Path.GetDirectoryName(Environment.CurrentDirectory);
                var dialog = new OpenFileDialog
                {
                    ValidateNames = false,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "Select a folder"
                };
                string folder = null;
                if (dialog.ShowDialog() == true && dialog.FileName != null)
                {
                    folder = System.IO.Path.GetDirectoryName(dialog.FileName);
                    // Do something with the selected directory path
                }

                if (folder != null)
                {
                    ReverserData.InputDirectory = folder;
                    InputDirectoryNameText = $"Selected Input Folder: {folder}";
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
                    OutputFileNameText = $"Selected Output File: {openFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get output file path: {ex.Message}");
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        void StartFileProcessing()
        {
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
                    ReverserData.OutputFile = $"{Path.GetDirectoryName(ReverserData.InputFile)}/i{ReverserData.ReverseType[0]}_{inputFileName}.{ReverserData.ExtensionType}";
                }

                // Start a new thread or use a Task to call the ProcessFile method
                FileProcessorWorker.ProcessFile(ReverserData);
                ReverserData.OutputFile = "";
                MessageBox.Show("", "Reversed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get continue bad parameters: {ex.Message}");
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        
        [RelayCommand]
        void StartDirectoryProcessing()
        {
            try
            {
                Action<double> updateProgress = (double newProgress) => Progress += newProgress;
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
                    ReverserData.OutputFile = $"{Path.GetDirectoryName(ReverserData.InputFile)}/i{ReverserData.ReverseType[0]}_{inputDirectory}.{ReverserData.ExtensionType}";
                }

                // Start a new thread or use a Task to call the ProcessFile method
                FileProcessorWorker.ProcessDirectory(ReverserData, updateProgress);
                ReverserData.OutputFile = "";
                if (Progress >= 1)
                {
                    MessageBox.Show("", "Reversed", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    Progress = 0;
                    UiEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get continue bad parameters: {ex.Message}");
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
