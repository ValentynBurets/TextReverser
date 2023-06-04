using CommunityToolkit.Mvvm.ComponentModel;
using FileProcessor.Model;
using System;
using System.Windows;

namespace TextReverserWPF.ViewModel
{
    public partial class BaseDataViewModel : ObservableObject
    {
        public BaseDataViewModel()
        {
            reverserData = new ReverseData();
            reverserData.ReverseType = "Char";
            reverserData.ArchiveType = "none";
            timeLeft = String.Empty;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCheckBoxVisible))]
        ReverseData reverserData;

        [ObservableProperty]
        string title;

        [ObservableProperty]
        string inputFileNameText;


        [ObservableProperty]
        string outputFileNameText;

        [ObservableProperty]
        string inputDirectoryNameText;

        [ObservableProperty]
        double progress;

        [ObservableProperty]
        bool uiEnabled = true;

        [ObservableProperty]
        string timeLeft;
 
        public Visibility IsCheckBoxVisible => 
            (System.Windows.Visibility.Visible 
                                           );
    }
}
