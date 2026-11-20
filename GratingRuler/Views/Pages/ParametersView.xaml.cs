using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using GratingRuler.ViewModels;

namespace GratingRuler.Views.Pages;

public partial class ParametersView : UserControl
{
    public ParametersView() => InitializeComponent();

    private void Add_Click(object sender, RoutedEventArgs e) => OpenEditor(false);
    private void Edit_Click(object sender, RoutedEventArgs e) => OpenEditor(true);
    private void Node_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item) FlowNodes.SelectedItem = item.DataContext;
        OpenEditor(true);
        e.Handled = true;
    }

    private void OpenEditor(bool edit)
    {
        if (DataContext is not ParametersViewModel model || !model.CanEdit || (edit && model.SelectedStep is null)) return;
        new StepConfigurationWindow(model, edit) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}

