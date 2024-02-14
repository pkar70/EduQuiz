
namespace QuizKursUno;



public partial class TextBlockPageTitle : UserControl
{

    private TextBlock _TBlock;
    public String Text
    {
        get => _TBlock.Text;
        set => _TBlock.Text = value;
    }

    public TextBlockPageTitle()
    {
        _TBlock = new TextBlock
        {
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        this.Content = _TBlock;
    }
}
