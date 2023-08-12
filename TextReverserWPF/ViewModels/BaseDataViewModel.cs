using CommunityToolkit.Mvvm.ComponentModel;
using FileProcessor.Model;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Serialization;

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
            additionalSigns = String.Empty;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCheckBoxVisible))]
        string reverseTypeSelected = "Char";

        [ObservableProperty]
        ReverseData reverserData;

        [ObservableProperty]
        string inputFileNameText;

        [ObservableProperty]
        string outputFileNameText;

        [ObservableProperty]
        string inputDirectoryNameText;

        [ObservableProperty]
        string additionalSigns;

        [ObservableProperty]
        double progress;

        [ObservableProperty]
        bool uiEnabled = true;

        [ObservableProperty]
        string timeLeft;

        [ObservableProperty]
        string timeSpent;

        public Visibility IsCheckBoxVisible => (
            (reverseTypeSelected == "Word" || reverseTypeSelected == "Char") 
                                                    ? Visibility.Visible 
                                                    : Visibility.Hidden);
    }
}
