using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using TextReverser.FileProcessor;

namespace TextReverser.ViewModel
{
    public partial class ReverserDataViewModel: BaseDataViewModel
    {

        public ReverserDataViewModel()
        {
            Title = "TextReverser";
        }

        [RelayCommand]
        async Task SelectInputFile()
        {
            try
            {
                var file = await FilePicker.PickAsync(
                new PickOptions()
                {
                    FileTypes = new FilePickerFileType(
                            new Dictionary<DevicePlatform, IEnumerable<string>>()
                            {
                                { DevicePlatform.UWP, new[] { ".txt", ".tab", ".doc", ".docx" } }
                            }
                        )
                }
            );
                if (file != null)
                {
                    ReverserData.InputFile = file.FullPath;
                    InputFileNameText = $"Selected Input File: {file.FileName}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get input file path: {ex.Message}");
                await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
        }

        [RelayCommand]
        async Task SelectInputDirectory()
        {
            try
            {
                CancellationToken cancellationToken = new CancellationToken();
                string currentDirectory = Path.GetDirectoryName(Environment.CurrentDirectory);
                
                var folder = await FolderPicker.PickAsync(currentDirectory, cancellationToken);

                if (folder != null)
                {
                    ReverserData.InputDirectory = folder.Folder.Path;
                    InputDirectoryNameText = $"Selected Input Folder: {folder.Folder.Name}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get input folder path: {ex.Message}");
                await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
        }

        [RelayCommand]
        async Task SelectOutputFile()
        {
            try
            {
                var file = await FilePicker.PickAsync(
                    new PickOptions()
                    {
                        FileTypes = new FilePickerFileType(
                            new Dictionary<DevicePlatform, IEnumerable<string>>()
                            {
                                { DevicePlatform.UWP, new[] { ".txt", ".tab", ".doc", ".docx" } }
                            }
                        )
                    }
                );

                if (file != null)
                {
                    ReverserData.OutputFile = file.FullPath;
                    OutputFileNameText = $"Selected Output File: {file.FileName}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get output file path: {ex.Message}");
                await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
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
                    Shell.Current.DisplayAlert("Alert", errorMessage, "OK");
                    return;
                }

                if(ReverserData.OutputFile == "" || ReverserData.OutputFile == null)
                {
                    string inputFileName = Path.GetFileName(ReverserData.InputFile);
                    ReverserData.OutputFile = $"{Path.GetDirectoryName(ReverserData.InputFile)}/I{ReverserData.ReverseType[0]}_{inputFileName}.{ReverserData.ExtensionType}";
                }
                // Start a new thread or use a Task to call the ProcessFile method
                FileProcessorWorker.ProcessFile(ReverserData);
                Shell.Current.DisplayAlert("Reversed","", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get continue bad parameters: {ex.Message}");
                Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
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
                    Shell.Current.DisplayAlert("Alert", errorMessage, "OK");
                    return;
                }

                if (ReverserData.OutputFile == "" || ReverserData.OutputFile == null)
                {
                    string inputFileName = Path.GetFileName(ReverserData.InputFile);
                    ReverserData.OutputFile = $"{Path.GetDirectoryName(ReverserData.InputFile)}/I{ReverserData.ReverseType[0]}_{inputFileName}.{ReverserData.ExtensionType}";
                }
                // Start a new thread or use a Task to call the ProcessFile method
                FileProcessorWorker.ProcessDirectory(ReverserData, updateProgress);
                
                if (Progress >= 1)
                {
                    Shell.Current.DisplayAlert("Reversed", "", "OK");
                    Progress = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get continue bad parameters: {ex.Message}");
                Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
        }
    }
}
