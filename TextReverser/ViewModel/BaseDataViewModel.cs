﻿using CommunityToolkit.Mvvm.ComponentModel;
using FileProcessor.Model;

namespace TextReverser.ViewModel
{
    public partial class BaseDataViewModel : ObservableObject
    {
        public BaseDataViewModel()
        {
            reverserData = new ReverseData();
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
        TimeSpan timeLeft;


        public bool IsCheckBoxVisible => ReverserData?.ReverseType == "Word";
    }
}
