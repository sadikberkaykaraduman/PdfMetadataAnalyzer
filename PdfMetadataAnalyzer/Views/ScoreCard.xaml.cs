using UserControl = System.Windows.Controls.UserControl;
using System.Windows;
using System.Windows.Controls;

namespace PdfMetadataAnalyzer.Views;

public partial class ScoreCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ScoreCard));
    public static readonly DependencyProperty ScoreProperty =
        DependencyProperty.Register(nameof(Score), typeof(double), typeof(ScoreCard));
    public static readonly DependencyProperty TimeMsProperty =
        DependencyProperty.Register(nameof(TimeMs), typeof(long), typeof(ScoreCard));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public double Score { get => (double)GetValue(ScoreProperty); set => SetValue(ScoreProperty, value); }
    public long TimeMs { get => (long)GetValue(TimeMsProperty); set => SetValue(TimeMsProperty, value); }

    public ScoreCard() => InitializeComponent();
}