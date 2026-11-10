using System.Windows;
using GratingRuler.ViewModels;
using GratingRuler.Views;

namespace GratingRuler;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var viewModel = new MainViewModel();
        var window = new MainWindow { DataContext = viewModel };
        window.Closed += (_, _) => viewModel.Dispose();
        MainWindow = window;
        window.Show();
    }
}
