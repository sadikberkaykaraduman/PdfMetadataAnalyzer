using UserControl = System.Windows.Controls.UserControl;
using System.Windows;

namespace PdfMetadataAnalyzer.Views;

public partial class RawFieldRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RawFieldRow));
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(RawFieldRow));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public RawFieldRow() => InitializeComponent();
}