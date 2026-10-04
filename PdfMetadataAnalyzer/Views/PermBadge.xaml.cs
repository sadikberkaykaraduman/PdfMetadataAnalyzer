using UserControl = System.Windows.Controls.UserControl;
using System.Windows;

namespace PdfMetadataAnalyzer.Views;

public partial class PermBadge : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(PermBadge));
    public static readonly DependencyProperty AllowedProperty =
        DependencyProperty.Register(nameof(Allowed), typeof(bool), typeof(PermBadge));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public bool Allowed { get => (bool)GetValue(AllowedProperty); set => SetValue(AllowedProperty, value); }

    public PermBadge() => InitializeComponent();
}