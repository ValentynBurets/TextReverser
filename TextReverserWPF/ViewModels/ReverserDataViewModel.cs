﻿using CommunityToolkit.Mvvm.Input;
using FileProcessor;
using FileProcessor.Model;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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

        [RelayCommand]
        void SelectInputFile()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = textfileFilter;
         
                if (openFileDialog.ShowDialog() != null)
                {
                    ReverserData.InputFile = openFileDialog.FileName;
                    InputFileNameText = Path.GetFileName(openFileDialog.FileName);
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
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                
                dialog.IsFolderPicker = true;
                
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    ReverserData.InputDirectory = dialog.FileName;
                    InputDirectoryNameText = Path.GetFileName(dialog.FileName);
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
                    OutputFileNameText = Path.GetFileName(openFileDialog.FileName);
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
                if (string.IsNullOrEmpty(ReverserData.ReverseType) || string.IsNullOrEmpty(ReverserData.InputFile) || ReverseTypeSelected == "")
                {
                    string errorMessage = "Missing information! Please provide all required fields.";
                    MessageBox.Show(errorMessage, "Alert", MessageBoxButton.OK, MessageBoxImage.Warning); 
                    return;
                }

                ReverserData.ReverseType = ReverseTypeSelected;
                ReverserData.ReverseType = ReverserData.ReverseType.ToLower();
                var fileInfo = new FileInfo(ReverserData.InputFile);
                ReverserData.ExtensionType = fileInfo.Extension;
                ReverserData.AdditionalSigns = AdditionalSigns;

                if (ReverserData.OutputFile == "" || ReverserData.OutputFile == null)
                {
                    string inputFileName = Path.GetFileNameWithoutExtension(ReverserData.InputFile);
                    ReverserData.OutputFile = Path.Combine(Path.GetDirectoryName(ReverserData.InputFile), $"I{ReverserData.ReverseType[0]}_{inputFileName}{ReverserData.ExtensionType}");

                    int index = 1;
                    var tempFolderPath = new StringBuilder(ReverserData.OutputFile);

                    while (File.Exists(tempFolderPath.ToString()))
                    {
                        ++index;
                        tempFolderPath.Clear();
                        tempFolderPath.Append($"{Path.GetDirectoryName(ReverserData.InputFile)}/I{ReverserData.ReverseType[0]}_{inputFileName}_{index}{ReverserData.ExtensionType}");
                    }
                    if(index >= 2)
                    {
                        ReverserData.OutputFile = $"{Path.GetDirectoryName(ReverserData.InputFile)}/I{ReverserData.ReverseType[0]}_{inputFileName}_{index}{ReverserData.ExtensionType}";
                    }
                }

                long totalSizeLeft = fileInfo.Length;

                Action<TimeSpan> updateTimeLeftLable = (TimeSpan timeLeft) =>
                {
                    TimeLeft = "Залишилося часу"
                                + (timeLeft.Days != 0 ? $" : {timeLeft.Days} днів" : "")
                                + (timeLeft.Minutes != 0 ? $" : {timeLeft.Minutes} хвилин" : "")
                                + (timeLeft.Seconds != 0 ? $" : {timeLeft.Seconds} секунд" : "0 секунд");

                };

                Action<TimeSpan> updateTimeSpent = (TimeSpan timeSpent) =>
                {
                    TimeSpent = "Витрачено часу"
                                + (timeSpent.Days != 0 ? $" : {timeSpent.Days} днів" : "")
                                + (timeSpent.Minutes != 0 ? $" : {timeSpent.Minutes} хвилин" : "")
                                + (timeSpent.Seconds != 0 ? $" : {timeSpent.Seconds} секунд" : "0 секунд");

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

                long fileSize = fileInfo.Length;
                int bufferSize = 8192;
                int maxThreads = Environment.ProcessorCount; // Number of threads to use for parallel reading

                int threadCount = Math.Min(maxThreads, (int)Math.Ceiling((double)fileSize / bufferSize));

                double progressStep = 1.0 / threadCount;

                var updateProgress = () => Progress += progressStep;

                UiEnabled = false;

                Stopwatch stopWatchProces = Stopwatch.StartNew();

                // Start a new thread or use a Task to call the ProcessFile method
                await Task.Run(() => { FileProcessorWorker.ProcessFile(ReverserData, updateFileSizeLeft, updateProgress, stopWatchProces, updateTimeSpent); });
                
                ReverserData.OutputFile = "";
                stopWatchProces.Stop();
                MessageBox.Show("Документ інвертовано", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                UiEnabled = true;
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
                string[] fileNames = Directory.GetFiles(ReverserData.InputDirectory);
                double progressStep = 1.0 / fileNames.Length;

                var updateProgress = () => Progress += progressStep;

                Action<TimeSpan> updateTimeLeftLable = (TimeSpan timeLeft) =>
                {
                    TimeLeft = "Залишилося часу"
                                + (timeLeft.Days != 0 ? $" : {timeLeft.Days} днів" : "")
                                + (timeLeft.Minutes != 0 ? $" : {timeLeft.Minutes} хвилин" : "")
                                + (timeLeft.Seconds != 0 ? $" : {timeLeft.Seconds} секунд" : "0 секунд");

                };

                Action<TimeSpan> updateTimeSpent = (TimeSpan timeSpent) =>
                {
                    TimeSpent = "Витрачено часу"
                                + (timeSpent.Days != 0 ? $" : {timeSpent.Days} днів" : "")
                                + (timeSpent.Minutes != 0 ? $" : {timeSpent.Minutes} хвилин" : "")
                                + (timeSpent.Seconds != 0 ? $" : {timeSpent.Seconds} секунд" : "0 секунд");

                };

                if (string.IsNullOrEmpty(ReverserData.ReverseType) || string.IsNullOrEmpty(ReverserData.InputDirectory))
                {
                    string errorMessage = "Missing information! Please provide all required fields.";
                    MessageBox.Show(errorMessage, "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ReverserData.ReverseType = ReverseTypeSelected;
                UiEnabled = false;
                ReverserData.ReverseType = ReverserData.ReverseType.ToLower();
                ReverserData.AdditionalSigns = AdditionalSigns;

                if (ReverserData.OutputFile == "" || ReverserData.OutputFile == null)
                {
                    string inputDirectory = Path.GetFileName(ReverserData.InputDirectory);
                    ReverserData.OutputFile = $"{Path.GetDirectoryName(ReverserData.InputFile)}/I{ReverserData.ReverseType[0]}_{inputDirectory}{ReverserData.ExtensionType}";
                }
                Stopwatch stopWatchProces = Stopwatch.StartNew();
                // Start a new thread or use a Task to call the ProcessFile method
                await FileProcessorWorker.ProcessDirectory(ReverserData, updateProgress, updateTimeLeftLable, stopWatchProces, updateTimeSpent);
                
                if (Progress >= 0.95)
                {
                    MessageBox.Show("Папку інвертовано", "Іноформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    Progress = 0;
                    UiEnabled = true;
                }

                stopWatchProces.Stop();
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
