using UserControl = System.Windows.Controls.UserControl;
using System.Windows;
using PdfMetadataAnalyzer.Models;

namespace PdfMetadataAnalyzer.Views;

public partial class LibraryCard : UserControl
{
    public static readonly DependencyProperty RawProperty =
        DependencyProperty.Register(nameof(Raw), typeof(RawResult), typeof(LibraryCard));

    public RawResult Raw { get => (RawResult)GetValue(RawProperty); set => SetValue(RawProperty, value); }

    public LibraryCard() => InitializeComponent();
}