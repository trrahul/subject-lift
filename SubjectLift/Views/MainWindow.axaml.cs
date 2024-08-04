using SubjectLift.Controls;
using SubjectLift.ViewModels;
using Window = Avalonia.Controls.Window;

namespace SubjectLift.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void MaskedImage_ImageClicked(object sender, ImageClickedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SegmentCommand.Execute((e.ClickPosition, e.PixelPosition));
        }
    }
}