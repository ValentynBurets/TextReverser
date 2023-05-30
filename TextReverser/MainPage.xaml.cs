using TextReverser.ViewModel;

namespace TextReverser;

public partial class MainPage : ContentPage
{
    public MainPage(ReverserDataViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

