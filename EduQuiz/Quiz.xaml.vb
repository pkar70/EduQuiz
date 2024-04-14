


'plik wejsciowy
'<html> <body> <h1> tytuł pliku</h1>
'<hr>
'<cokolwiek>
'<ul>
'<li corr = True > answer
'(albo definicjami, albo <b> w <li>, albo [TRUE] na początku, pierwsze LI z [quiz] włącza quizowość, czy tp. - tak, żeby tworzenie pliku było proste.
'<hr>
'...

'quizPage:
'<row> <TextBlock text = h1 >
'<row><webview=between(hr)
'<row><listview><textblocks text=li>

' może być usuwanie odpowiedzi, gdy ich nie ma (row=0)

Imports vb14 = VBlib.pkarlibmodule14
Imports pkar.UI.Extensions
Imports Windows.System
Imports VBlib



#If PK_WPF Then
Imports System.Windows.Threading
Imports Windows.ApplicationModel
#Else
imports Windows.Storage ' bo WPF ma swoje
#End If

Public NotInheritable Class Quiz
    Inherits Page

    Public mQuiz As VBlib.JedenQuiz = Nothing
    Private miCurrQuestion As Integer = -1  ' init potrzebny
    Private ReadOnly mRandom As New System.Random
    Private ReadOnly moTimer As New DispatcherTimer
    Private mQuizContent As VBlib.QuizContent

#If WINDOWS8_0_OR_GREATER Or NETFX_CORE Then
    Private Shared ReadOnly moMediaPlayer As New Windows.Media.Playback.MediaPlayer
#End If

#If PK_WPF Then
    Public Sub SetQuiz(quizName As String)
        VBlib.DumpCurrMethod("name: " & quizName)
        mQuiz = VBlib.MainPage._Quizy.GetItem(quizName)
    End Sub
#Else

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        VBlib.DumpCurrMethod()
        Dim sParam As String = e.Parameter.ToString
        mQuiz = VBlib.MainPage._Quizy.GetItem(sParam)
    End Sub
#End If

    Private Function CheckCzyMoznaUruchomic() As Boolean
        VBlib.DumpCurrMethod()

        ' jesli mamy licznik uruchomień
        If mQuiz.iRuns < Integer.MaxValue Then
            mQuiz.iRuns -= 1
            VBlib.MainPage._Quizy.Save()
            If mQuiz.iRuns < 0 Then
                Me.MsgBox("Sorry, za dużo uruchomień")
                Return False
            End If
        End If

        Dim sCurrDate As String = Date.Now.ToString("yyyyMMdd")
        If sCurrDate < mQuiz.sMinDate Then
            vb14.DumpMessage("Curr date " & sCurrDate & " < minDate " & mQuiz.sMinDate)
            Me.MsgBox("Sorry, ale jeszcze za wcześnie - poczekaj parę dni")
            Return False
        End If

        If sCurrDate > mQuiz.sMaxDate Then
            vb14.DumpMessage("Curr date " & sCurrDate & " > maxDate " & mQuiz.sMaxDate)
            Me.MsgBox("Sorry, ale już jest za późno - szkolenie wygasło")
            Return False
        End If

        Return True

    End Function

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        Me.InitDialogs

        'ProgRingInit(False, True)

        If mQuiz Is Nothing Then Return
        uiTitle.Text = mQuiz.sName
        If Not CheckCzyMoznaUruchomic() Then Return

        mQuizContent = New VBlib.QuizContent(mQuiz, VBlib.MainPage._QuizyRootFolder)
        Dim iMaxQuestion As Integer = mQuizContent.ReadQuiz()
        uiProgCnt.Maximum = iMaxQuestion

        If mQuiz.sSearchHdr = "" Then
            uiSearchGrid.Visibility = Visibility.Collapsed
        Else
            uiSearchGrid.Visibility = Visibility.Visible
        End If

#If PK_WPF Then
        Await uiWebView.EnsureCoreWebView2Async
#End If

        AddHandler moTimer.Tick, AddressOf Timer_Tick

        TryStartBackMusic()

        Await GoNextQuestion()
    End Sub

    ' to MA await, ale dla UWP
#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Private Async Sub TryStartBackMusic()
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

        Dim sMusicFilePathname As String = IO.Path.Combine(VBlib.MainPage._QuizyRootFolder, mQuiz.sFolder, "background.mp3")
        If Not IO.File.Exists(sMusicFilePathname) Then Return

#If WINDOWS8_0_OR_GREATER Or NETFX_CORE Then
        Dim oFile As Windows.Storage.StorageFile = Await Windows.Storage.StorageFile.GetFileFromPathAsync(sMusicFilePathname)
        Dim oMediaSrc = Windows.Media.Core.MediaSource.CreateFromStorageFile(oFile)

        moMediaPlayer.Source = oMediaSrc
        moMediaPlayer.IsLoopingEnabled = True
        moMediaPlayer.Play()
#End If
    End Sub

    Private Sub uiGoNext_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        GoNextQuestion()
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    End Sub

    Private Async Function DialogBoxWithTimeoutAsync(sMsg As String, iMsTimeout As Integer) As Task
#If PK_WPF Then
        Await Me.MsgBoxAsync(sMsg)
#Else
        Dim oMsg As New Windows.UI.Popups.MessageDialog(sMsg)
        Dim oWait = oMsg.ShowAsync

        Await Task.Delay(iMsTimeout)
        Try
            oWait.Cancel()
        Catch ex As Exception
            ' jakby user wcześniej klikął
        End Try
#End If

    End Function

    Private Async Function CheckAnswersy(iCurrQuestion As Integer) As Task(Of Boolean)
        If uiListItems.ItemsSource Is Nothing Then Return True

        Dim bGood As Boolean = mQuizContent.CheckAnswersy()

        If mQuizContent.currPage.bDisableConfirm Then Return True

        ' i reakcja na to
        If bGood Then
            Await DialogBoxWithTimeoutAsync("Dobrze!", 1000)
            Return True ' mozna przejść dalej
        Else
            Await DialogBoxWithTimeoutAsync("Niestety, to nie tak...", 1000)
            If mQuiz.bErrIgnore And Not mQuizContent.currPage.bErrStop Then Return True

            For Each oAnswer As VBlib.JednoPytanie In mQuizContent.currPage.moAnswerList
                oAnswer.bChecked = False
            Next
            Return False
        End If

    End Function

    Private Async Function GoNextQuestion() As Task
        vb14.DumpCurrMethod()

        moTimer.Stop()

        If Not mQuizContent.IsLoaded Then mQuizContent.ReadQuiz()
        If Not mQuizContent.IsLoaded Then Return   ' wczytanie nieudane

        If Not Await CheckAnswersy(miCurrQuestion) Then Return ' coś z odpowiedziami było nie tak, pozostań

        ' skasowanie listy odpowiedzi
        uiQuestionRow.Height = New GridLength(5)
        uiListItems.ItemsSource = Nothing

        If mQuiz.bRandom Then
            miCurrQuestion = mRandom.Next(1, mQuizContent.GetMaxQuestion)
            vb14.DumpMessage("Losuje pytanie: " & miCurrQuestion)
        Else

            If miCurrQuestion >= mQuizContent.GetMaxQuestion Then
                Await DialogBoxWithTimeoutAsync("KONIEC :)", 5000)

                If mQuiz.sEmail <> "" Or vb14.GetSettingsBool("allowEmail") Then
                    If Await Me.DialogBoxYNAsync("Czy chcesz wysłać rezultat?") Then

#If Not PK_WPF Then
                        Dim oMsg As New Windows.ApplicationModel.Email.EmailMessage()
                        oMsg.Subject = "Rezultat testu/quizu " & mQuiz.sName
                        Dim sTxt As String = "Załączam rezultat dzisiejszego testu" & vbCrLf & vbCrLf &
                                "Data: " & Date.Now & vbCrLf & vbCrLf & mQuizContent.msAnswerLog

                        oMsg.Body = sTxt
                        If mQuiz.sEmail <> "" Then oMsg.To.Add(New Email.EmailRecipient(mQuiz.sEmail))

                        Await Email.EmailManager.ShowComposeNewEmailAsync(oMsg)
#Else
                        Me.MsgBox("Wysyłanie email niestety nie działa w WPF")
#End If
                    End If

                    Return
                End If
            End If

            miCurrQuestion += 1
            vb14.DumpMessage("Kolejne pytanie: " & miCurrQuestion)

        End If

        miCurrQuestion = Math.Max(1, miCurrQuestion)    ' pytania są od 1, nie od zera

        IdzDoPytania(miCurrQuestion)

        If mQuiz.iSeconds < Int16.MaxValue Then
            ' WPF ma tutaj problem z dużymi liczbami
            ' TimeSpan period must be less than or equal to Int32.MaxValue
            moTimer.Interval = TimeSpan.FromSeconds(mQuiz.iSeconds)
            moTimer.Start()
        Else
            moTimer.Stop()
        End If
    End Function

    Private Sub IdzDoPytania(iNumer As Integer)
        ' progressbar ma sens tylko przy sekwencyjnym (nie przy losowym)
        If Not mQuiz.bRandom Then
            uiProgCnt.Visibility = Visibility.Visible
            uiProgCnt.Value = iNumer
        End If

        Dim oCurrPage As VBlib.QuizPage = mQuizContent.IdzDoPytania(iNumer)

        If oCurrPage Is Nothing Then
            ' nie ma, czyli pytanie za daleko?
            uiWebView.NavigateToString("")
        Else
            If oCurrPage.moAnswerList IsNot Nothing Then
                uiQuestionRow.Height = New GridLength(1, GridUnitType.Star)
                uiListItems.ItemsSource = Nothing
                uiListItems.ItemsSource = mQuizContent.currPage.moAnswerList
            End If

            Try
                uiWebView.NavigateToString(oCurrPage.htmlPage)
                Return
            Catch ex As Exception
            End Try

            Try
                uiWebView.NavigateToString(oCurrPage.htmlPageFallback)
                Return
            Catch ex As Exception
            End Try

            Dim sHtml As String = "<html><body>Nieudane pokazanie treści z wstawionymi binariami oraz nawet czystego HTML</body></html>"
            Try
                uiWebView.NavigateToString(sHtml)
            Catch ex As Exception
            End Try

        End If
    End Sub


#If PK_WPF Then
    Private Sub wbViewer_NavigationStarting(sender As Microsoft.Web.WebView2.Wpf.WebView2, args As Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs)

#Else
    Private Sub wbViewer_NavigationStarting(sender As WebView, args As WebViewNavigationStartingEventArgs)
#End If
        vb14.DumpCurrMethod()

        If args.Uri Is Nothing Then Return
#If PK_WPF Then
        ' a w WPF do lokalnych też jest wywoływany!
        If args.Uri.StartsWith("data") Then Return
#End If
        args.Cancel = True

#If PK_WPF Then
        Dim addr As New Uri(args.Uri)
#Else
        Dim addr as Uri = args.Uri
#End If
        addr.OpenBrowser

    End Sub
    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()
        RemoveHandler moTimer.Tick, AddressOf Timer_Tick
        moTimer.Stop()
#If WINDOWS8_0_OR_GREATER Then
        moMediaPlayer.Pause()
#End If
    End Sub

    Private Sub Timer_Tick(sender As Object, e As Object)
        vb14.DumpCurrMethod()
        uiGoNext_Click(Nothing, Nothing)
    End Sub

    Private Sub uiSearchTerm_TextChanged(sender As Object, e As TextChangedEventArgs)
        If uiSearchTerm.Text.Length < 3 Then
            uiSearchList.ItemsSource = Nothing
            Return
        End If

        Dim oLista As New List(Of JedenSearchTerm)
        Dim iNumer As Integer = 1
        For Each sTerm As String In mQuizContent.maSearchTerms
            If sTerm.ToLower.Contains(uiSearchTerm.Text) Then
                Dim oNew As New JedenSearchTerm
                oNew.sTekst = sTerm
                oNew.iNumer = iNumer
                oLista.Add(oNew)
            End If
            iNumer += 1
        Next

        uiSearchList.ItemsSource = oLista

    End Sub

    ' *TODO* tu zmieniam na próbę
    Private Sub uiGoTerm_Tapped(sender As Object, e As Object)
        Dim oFE As FrameworkElement = sender
        Dim oItem As JedenSearchTerm = TryCast(oFE.DataContext, JedenSearchTerm)
        If oItem Is Nothing Then Return

#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        IdzDoPytania(oItem.iNumer)
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    End Sub

    Private Sub Answer_Checked(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oAnswer As JednoPytanie = oFE?.DataContext
        If oAnswer Is Nothing Then Return

        If Not oAnswer.bSingleAnswer Then Return

        For Each oItem As JednoPytanie In mQuizContent.currPage.moAnswerList
            oItem.bChecked = (oItem.sTekst = oAnswer.sTekst)
        Next

        uiListItems.ItemsSource = Nothing
        uiListItems.ItemsSource = mQuizContent.currPage.moAnswerList

    End Sub

End Class

Public Class JedenSearchTerm
    Public Property sTekst As String
    Public Property iNumer As Integer
End Class


