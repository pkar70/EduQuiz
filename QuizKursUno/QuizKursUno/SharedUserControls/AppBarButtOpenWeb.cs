
using pkar.UI.Extensions;

namespace QuizKursUno;
public partial class AppBarButtOpenWeb : AppBarButton
{
    public Uri Uri { get; set; }

    AppBarButtOpenWeb() : base()
    {
        Icon = new SymbolIcon(Symbol.Globe);
        Label = "Open Web";
        this.Uri = new Uri("http://");  // że niby nullable, do ustawienia w ctor, i w ogóle
        this.Click += PojdzDoWWW;
    }

    private void PojdzDoWWW(Object sender, RoutedEventArgs e)
    {
        this.Uri.OpenBrowser();
    }
}
