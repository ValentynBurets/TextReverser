using CommunityToolkit.Mvvm.ComponentModel;
using FileProcessor.Model;

namespace TextReverserWPF.ViewModel
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

        //[ObservableProperty]
        //double progress;

        [ObservableProperty]
        bool uiEnabled = true;


        private double progress;
        public double Progress
        {
            get { return progress; }
            set
            {
                if (progress != value)
                {
                    progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        public bool IsCheckBoxVisible => ReverserData?.ReverseType == "Word";
    }
}
