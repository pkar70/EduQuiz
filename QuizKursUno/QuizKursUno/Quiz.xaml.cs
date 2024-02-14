using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

using vb14 = VBlib.pkarlibmodule14;
using pkar.UI.Extensions;
using VBlib;
using Windows.Media.Playback;


#nullable disable

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace QuizKursUno;

public sealed partial class Quiz : Page
{
    public Quiz()
    {
        this.InitializeComponent();
    }

    public VBlib.JedenQuiz mQuiz = null;

    private int miCurrQuestion = -1; // ' init potrzebny
    private Random mRandom = new System.Random();
    private DispatcherTimer moTimer = new DispatcherTimer();

    private VBlib.QuizContent mQuizContent;

    // to będzie kompilowane dla Windows
#if !HAS_UNO
    private static readonly Windows.Media.Playback.MediaPlayer moMediaPlayer = new Windows.Media.Playback.MediaPlayer();
#endif

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        //VBlib.DumpCurrMethod();
        String sParam = e.Parameter.ToString();
        mQuiz = VBlib.MainPage._Quizy.GetItem(sParam);
    }

    private async Task<bool> CheckCzyMoznaUruchomic()
    {
        //VBlib.DumpCurrMethod()

        //' jesli mamy licznik uruchomień
        if (mQuiz.iRuns < int.MaxValue)
        {
            mQuiz.iRuns -= 1;
            VBlib.MainPage._Quizy.Save();
            if (mQuiz.iRuns < 0)
            {
                await vb14.DialogBoxAsync("Sorry, za dużo uruchomień");
                return false;
            }
        }

        String sCurrDate = DateTime.Now.ToString("yyyyMMdd");
        if (sCurrDate.CompareTo(mQuiz.sMinDate) < 0)
        {
            vb14.DumpMessage("Curr date " + sCurrDate + " < minDate " + mQuiz.sMinDate);
            await vb14.DialogBoxAsync("Sorry, ale jeszcze za wcześnie - poczekaj parę dni");
            return false;
        }

        if (sCurrDate.CompareTo(mQuiz.sMaxDate) > 0)
        {
            vb14.DumpMessage("Curr date " + sCurrDate + " > maxDate " + mQuiz.sMaxDate);
            await vb14.DialogBoxAsync("Sorry, ale już jest za późno - szkolenie wygasło");
            return false;
        }

        return true;

    }

    private async void Page_Loaded(Object sender, RoutedEventArgs e)
    {
        //vb14.DumpCurrMethod()
        //'ProgRingInit(false, True)

        if (mQuiz is null) return;
        uiTitle.Text = mQuiz.sName;
        if (!await CheckCzyMoznaUruchomic()) return;

        mQuizContent = new VBlib.QuizContent(mQuiz, VBlib.MainPage._QuizyRootFolder);
        int iMaxQuestion = mQuizContent.ReadQuiz();
        uiProgCnt.Maximum = iMaxQuestion;

        if (mQuiz.sSearchHdr == "")
            uiSearchGrid.Visibility = Visibility.Collapsed;
        else
            uiSearchGrid.Visibility = Visibility.Visible;

        moTimer.Tick += Timer_Tick;

        TryStartBackMusic();

        await GoNextQuestion();
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async void TryStartBackMusic()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        String sMusicFilePathname = Path.Combine(VBlib.MainPage._QuizyRootFolder, mQuiz.sFolder, "background.mp3");
        if (!File.Exists(sMusicFilePathname)) return;

#if !HAS_UNO
        Windows.Storage.StorageFile oFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(sMusicFilePathname);
        var oMediaSrc = Windows.Media.Core.MediaSource.CreateFromStorageFile(oFile);

        moMediaPlayer.Source = oMediaSrc;
        moMediaPlayer.IsLoopingEnabled = true;
        moMediaPlayer.Play();
#endif
    }

    private void uiGoNext_Click(Object sender, RoutedEventArgs e)
    {
        //vb14.DumpCurrMethod()
#pragma warning disable CS4014
        GoNextQuestion();
#pragma warning restore CS4014
    }

    private async Task DialogBoxWithTimeoutAsync(String sMsg, int iMsTimeout)
    {
        var oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
        var oWait = oMsg.ShowAsync();

        await System.Threading.Tasks.Task.Delay(iMsTimeout);
        try
        {
            oWait.Cancel();
        }
        catch
        {
            // jakby user wcześniej klikął
        }

    }
    private async Task<bool> CheckAnswersy(int iCurrQuestion)
    {
        if (uiListItems.ItemsSource is null) return true;

        bool bGood = mQuizContent.CheckAnswersy();

        if (mQuizContent.currPage.bDisableConfirm) return true;

        //' i reakcja na to
        if (bGood)
        {
            await DialogBoxWithTimeoutAsync("Dobrze!", 1000);
            return true; // ' mozna przejść dalej
        }
        else
        {
            await DialogBoxWithTimeoutAsync("Niestety, to nie tak...", 1000);
            if (mQuiz.bErrIgnore && !mQuizContent.currPage.bErrStop) return true;

            foreach (VBlib.JednoPytanie oAnswer in mQuizContent.currPage.moAnswerList)
                oAnswer.bChecked = false;

            return false;
        }

    }

    private async Task GoNextQuestion()
    {
        //vb14.DumpCurrMethod()

        moTimer.Stop();

        if (!mQuizContent.IsLoaded()) mQuizContent.ReadQuiz();
        if (!mQuizContent.IsLoaded()) return; //   ' wczytanie nieudane

        if (!await CheckAnswersy(miCurrQuestion)) return; // ' coś z odpowiedziami było nie tak, pozostań

        // ' skasowanie listy odpowiedzi
        uiQuestionRow.Height = new GridLength(5);
        uiListItems.ItemsSource = null;

        if (mQuiz.bRandom)
        {
            miCurrQuestion = mRandom.Next(1, mQuizContent.GetMaxQuestion());
            vb14.DumpMessage("Losuje pytanie: " + miCurrQuestion);
        }
        else
        {
            if (miCurrQuestion >= mQuizContent.GetMaxQuestion())
            {
                await DialogBoxWithTimeoutAsync("KONIEC :)", 5000);

                if (mQuiz.sEmail != "" || vb14.GetSettingsBool("allowEmail"))
                {
                    if (await vb14.DialogBoxYNAsync("Czy chcesz wysłać rezultat?"))
                    {
                        var oMsg = new Windows.ApplicationModel.Email.EmailMessage();
                        oMsg.Subject = "Rezultat testu/quizu " + mQuiz.sName;
                        String sTxt = "Załączam rezultat dzisiejszego testu\n\n" +
                                "Data: " + DateTime.Now + "\n\n" + mQuizContent.msAnswerLog;

                        oMsg.Body = sTxt;
                        if (mQuiz.sEmail != "")
                            oMsg.To.Add(new Windows.ApplicationModel.Email.EmailRecipient(mQuiz.sEmail));

                        await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(oMsg);
                    }

                    return;
                }
            }

            miCurrQuestion += 1;
            vb14.DumpMessage("Kolejne pytanie: " + miCurrQuestion);
        }

        miCurrQuestion = Math.Max(1, miCurrQuestion);    //' pytania są od 1, nie od zera

        IdzDoPytania(miCurrQuestion);

        if (mQuiz.iSeconds < Int16.MaxValue)
        {
            //' WPF ma tutaj problem z dużymi liczbami
            //' TimeSpan period must be less than or equal to Int32.MaxValue
            moTimer.Interval = TimeSpan.FromSeconds(mQuiz.iSeconds);
            moTimer.Start();
        }
        else
            moTimer.Stop();
    }

    private void IdzDoPytania(int iNumer)
    {
        //' progressbar ma sens tylko przy sekwencyjnym (nie przy losowym)
        if (!mQuiz.bRandom)
        {
            uiProgCnt.Visibility = Visibility.Visible;
            uiProgCnt.Value = iNumer;
        }

        VBlib.QuizPage oCurrPage = mQuizContent.IdzDoPytania(iNumer);

        if (oCurrPage is null)
        {
            //' nie ma, czyli pytanie za daleko?
            uiWebView.NavigateToString("");
        }
        else
        {
            if (!(oCurrPage.moAnswerList is null))
            {
                uiQuestionRow.Height = new GridLength(1, GridUnitType.Star);
                uiListItems.ItemsSource = null;
                uiListItems.ItemsSource = mQuizContent.currPage.moAnswerList;
            }

            try
            {
                uiWebView.NavigateToString(oCurrPage.htmlPage);
                return;
            }
            catch { }

            try
            {
                uiWebView.NavigateToString(oCurrPage.htmlPageFallback);
                return;
            }
            catch { }

            String sHtml = "<html><body>Nieudane pokazanie treści z wstawionymi binariami oraz nawet czystego HTML</body></html>";
            try
            {
                uiWebView.NavigateToString(sHtml);
            }
            catch { }

        }
    }


#if PK_WEBVIEW1
    private void wbViewer_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
#else
    private void wbViewer_NavigationStarting(WebView2 sender , Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args )
#endif
    {
        //vb14.DumpCurrMethod()

        if (args.Uri is null) return;
#if !PK_WEBVIEW1
        // ' a w WPF do lokalnych też jest wywoływany!
        if (args.Uri.StartsWith("data")) return;
#endif
        args.Cancel = true;

#if !PK_WEBVIEW1
        Uri addr = new Uri(args.Uri);
#else
        Uri addr = args.Uri;
#endif 
        addr.OpenBrowser();

    }

    private void Page_Unloaded(Object sender, RoutedEventArgs e)
    {
        //vb14.DumpCurrMethod()
        moTimer.Tick -= Timer_Tick;
        moTimer.Stop();
#if !HAS_UNO
        moMediaPlayer.Pause();
#endif
    }

    private void Timer_Tick(Object sender, Object e)
    {
        //vb14.DumpCurrMethod()
        uiGoNext_Click(null, null);
    }

    private void uiSearchTerm_TextChanged(Object sender, TextChangedEventArgs e)
    {
        if (uiSearchTerm.Text.Length < 3)
        {
            uiSearchList.ItemsSource = null;
            return;
        }

        var oLista = new List<JedenSearchTerm>();
        int iNumer = 1;
        foreach (String sTerm in mQuizContent.maSearchTerms)
        {
            if (sTerm.ToLower().Contains(uiSearchTerm.Text))
            {
                var oNew = new JedenSearchTerm();
                oNew.sTekst = sTerm;
                oNew.iNumer = iNumer;
                oLista.Add(oNew);
            }
            iNumer += 1;
        }

        uiSearchList.ItemsSource = oLista;
    }

    private void uiGoTerm_Tapped(Object sender, Object e)
    {
        FrameworkElement oFE = sender as FrameworkElement;
        JedenSearchTerm oItem = oFE?.DataContext as JedenSearchTerm;
        if (oItem is null) return;

#pragma warning disable BC42358
        IdzDoPytania(oItem.iNumer);
#pragma warning restore BC42358
    }

    private void Answer_Checked(Object sender, RoutedEventArgs e)
    {
        FrameworkElement oFE = sender as FrameworkElement;
        JednoPytanie oAnswer = oFE?.DataContext as JednoPytanie;
        if (oAnswer is null) return;

        if (!oAnswer.bSingleAnswer) return;

        foreach (JednoPytanie oItem in mQuizContent.currPage.moAnswerList)
            oItem.bChecked = (oItem.sTekst == oAnswer.sTekst);

        uiListItems.ItemsSource = null;
        uiListItems.ItemsSource = mQuizContent.currPage.moAnswerList;

    }

}


public class JedenSearchTerm
{
    public String sTekst { get; set; }
    public int iNumer { get; set; }
}

