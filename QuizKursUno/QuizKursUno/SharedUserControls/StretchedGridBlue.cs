using Microsoft.UI;
using static pkar.DotNetExtensions;

namespace QuizKursUno;



public partial class StretchedGridBlue : StretchedGrid
{

    // '<Grid HorizontalAlignment = "Stretch" Margin="0,5,0,0" BorderThickness="2" BorderBrush="Blue" >

    protected override void OnApplyTemplate()
    {

        HorizontalAlignment = HorizontalAlignment.Center;

        BorderThickness = new Thickness(2);
        BorderBrush = new SolidColorBrush(Colors.Blue);

        base.OnApplyTemplate();
    }

    public StretchedGridBlue() : base()
    {
        BorderThickness = new Thickness(2);
        BorderBrush = new SolidColorBrush(Colors.Blue);
    }

}
