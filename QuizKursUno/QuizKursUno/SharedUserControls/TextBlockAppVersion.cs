using pkar.UI.Extensions;

namespace QuizKursUno;


public partial class TextBlockAppVersion : UserControl
{
    private TextBlock _TBlock;
    public String Text
    {
        get => _TBlock.Text;
        set => _TBlock.Text = value;
    }

    public TextBlockAppVersion()
    {
        _TBlock = new TextBlock
        {
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 10)
        };

#if DEBUG
        _TBlock.ShowAppVers(true);
#else
    _TBlock.ShowAppVers(false);
#endif

        this.Content = _TBlock;
    }

}
