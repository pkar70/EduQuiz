

using static pkar.DotNetExtensions;

namespace QuizKursUno;

public partial class StretchedGrid : Grid
{
    public String Cols
    {
        get
        {
            String colwym = "";
            foreach (var col in ColumnDefinitions)
            {
                if (colwym != "") colwym += ",";
                colwym += col.Width.ToString();
            }
            return colwym;
        }
        set
        {
            String[] aArr = value.Split(",");
            ColumnDefinitions.Clear();
            foreach (String col in aArr)
                ColumnDefinitions.Add(new ColumnDefinition() { Width = Text2GridLen(col) });
        }
    }

    public String Rows
    {
        get
        {
            String colwym = "";
            foreach (var col in RowDefinitions)
            {
                if (colwym != "") colwym += ",";
                colwym += col.Height.ToString();
            }
            return colwym;
        }
        set
        {
            String[] aArr = value.Split(",");
            RowDefinitions.Clear();
            foreach (String col in aArr)
                RowDefinitions.Add(new RowDefinition() { Height = Text2GridLen(col) });
        }
    }


    private GridLength Text2GridLen(String text)
    {
        if (text.EqualsCI("Auto")) return new GridLength(0, GridUnitType.Auto);

        GridUnitType typek = GridUnitType.Pixel;
        if (text.Contains("*"))
        {
            typek = GridUnitType.Star;
            text = text.Replace("*", "");
        }

        Double dbl;
        if (!Double.TryParse(text, out dbl)) return new GridLength(1, GridUnitType.Star);

        return new GridLength(dbl, typek);
    }


    // '<Grid HorizontalAlignment="Stretch"  >

    protected override void OnApplyTemplate()
    {
        HorizontalAlignment = HorizontalAlignment.Center;

        base.OnApplyTemplate();
    }
}
