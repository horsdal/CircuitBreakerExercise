using System.Windows;

namespace CircuitBreakerExercise
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private BreakingkService service;

    public MainWindow()
    {
      service = new BreakingkService();
      InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
      this.result.Text = this.service.GetMagicValue();
    }
  }
}
