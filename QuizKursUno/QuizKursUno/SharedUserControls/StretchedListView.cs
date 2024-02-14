
//' zamiennik dla:
//'<ListView ...  >
//'<ListView.ItemContainerStyle>
//'<Style TargetType = "ListViewItem" >
//'                    <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
//'                </Style>
//'            </ListView.ItemContainerStyle>


namespace QuizKursUno;


public partial class StretchedListView : ListView
{

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var stylesetter = new Style(typeof(ListViewItem));
        stylesetter.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        this.ItemContainerStyle = stylesetter;
    }

}
