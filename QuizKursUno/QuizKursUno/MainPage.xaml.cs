namespace QuizKursUno;
using pkar.UI.Extensions;

#nullable disable

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }


    private async void Page_Loaded(Object sender, RoutedEventArgs e)
    {

        this.ProgRingInit(true, false);
        String sFoldFrom = "";

        try
        {
            Windows.Storage.StorageFolder oFoldFrom = await Windows.Storage.StorageFolder.GetFolderFromPathAsync("ms-appx://defaulty");
            sFoldFrom = oFoldFrom.Path;
        }
        catch
        {
            sFoldFrom = "";
        }

        String sFoldTo = Windows.Storage.ApplicationData.Current.LocalFolder.Path;

        await VBlib.MainPage.UstalListeQuizow(sFoldFrom, sFoldTo);

        var cos = new StretchedGrid();

        PokazListeQuizow();
    }

    private async void uiDownload_Click(Object sender, RoutedEventArgs e)
    {
        String sUserAgent = "QuizKurs " + AsVbHelpers.GetAppVers();

        String sLink = await this.InputBoxAsync("Podaj ID quizu:");
        if (sLink == "") return;

        if (await VBlib.MainPage.DownloadNewQuizButtonUno(sUserAgent, sLink))
            PokazListeQuizow();
        else
            if (VBlib.MainPage._iBadCnt > 3) uiDownload.IsEnabled = false;
    }

    private void uiGoQuiz_Tapped(Object sender, RoutedEventArgs e)
    {
        FrameworkElement oFE = sender as FrameworkElement;
        if (oFE is null) return;
        VBlib.JedenQuiz oItem = oFE.DataContext as VBlib.JedenQuiz;
        if (oItem is null) return;
        String sQuizName = oItem.sName;

        this.Frame.Navigate(typeof(Quiz), sQuizName);
    }

    private void uiStartQuiz_Click(Object sender, RoutedEventArgs e)
    {
        uiGoQuiz_Tapped(sender, null);
    }

    private async void uiDelQuiz_Click(Object sender, RoutedEventArgs e)
    {
        FrameworkElement oFE = sender as FrameworkElement;
        VBlib.JedenQuiz oQuiz = oFE?.DataContext as VBlib.JedenQuiz;

        await VBlib.MainPage.DeleteQuiz(oQuiz);

        uiListItems.ItemsSource = null;
        uiListItems.ItemsSource = VBlib.MainPage._Quizy;

    }

    private void PokazListeQuizow()
    {
        //' pokaż listę, może jakoś wielkość (w MB) danego quizu?
        uiListItems.ItemsSource = null;
        uiListItems.ItemsSource = VBlib.MainPage._Quizy;
    }

}

//public class KonwersjaVisibility : ValueConverterOneWay
//{

//    public override Object Convert(Object value,
//    Type targetType, Object parameter,
//    String language)
//    {
//        bool bTemp = (bool)value;

//        if (!(parameter is null))
//        {
//            String sParam = parameter as String;
//            if (sParam.ToUpperInvariant() == "NEG") bTemp = !bTemp;
//        }
//        if (bTemp) return Visibility.Visible;
//        return Visibility.Collapsed;
//    }

//}
