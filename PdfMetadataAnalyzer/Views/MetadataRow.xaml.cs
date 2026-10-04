using UserControl = System.Windows.Controls.UserControl;
using System.Windows;
using System.Windows.Input;
using PdfMetadataAnalyzer.Models;

namespace PdfMetadataAnalyzer.Views;

public partial class MetadataRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(MetadataRow));
    public static readonly DependencyProperty FieldProperty =
        DependencyProperty.Register(nameof(Field), typeof(MetadataField), typeof(MetadataRow));
    public static readonly DependencyProperty CopyCmdProperty =
        DependencyProperty.Register(nameof(CopyCmd), typeof(ICommand), typeof(MetadataRow));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public MetadataField Field { get => (MetadataField)GetValue(FieldProperty); set => SetValue(FieldProperty, value); }
    public ICommand CopyCmd { get => (ICommand)GetValue(CopyCmdProperty); set => SetValue(CopyCmdProperty, value); }

    public MetadataRow() => InitializeComponent();
}