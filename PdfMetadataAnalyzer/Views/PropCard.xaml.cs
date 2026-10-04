using UserControl = System.Windows.Controls.UserControl;
using System.Windows;

namespace PdfMetadataAnalyzer.Views;

public partial class PropCard : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(PropCard));
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(PropCard));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public PropCard() => InitializeComponent();
}