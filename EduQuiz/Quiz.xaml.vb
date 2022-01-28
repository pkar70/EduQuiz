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

Imports VBlibek.pkarlibmodule

Public NotInheritable Class Quiz
    Inherits Page

    Dim mQuiz As VBlibek.JedenQuiz = Nothing
    Dim miCurrQuestion As Integer = -1  ' init potrzebny
    Dim mRandom As New System.Random
    Dim msAnswerLog As String = ""
    Dim moTimer As New DispatcherTimer

    Private mQuizContent As VBlibek.QuizContent

    Private Shared moMediaPlayer As New Windows.Media.Playback.MediaPlayer

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        DumpCurrMethod()
        Dim sParam As String = e.Parameter.ToString
        mQuiz = App.gQuizy.GetItem(sParam)
    End Sub

    Private Async Function CheckCzyMoznaUruchomic() As Task(Of Boolean)
        DumpCurrMethod()

        ' jesli mamy licznik uruchomień
        If mQuiz.iRuns < Integer.MaxValue Then
            mQuiz.iRuns -= 1
            App.gQuizy.Save()
            If mQuiz.iRuns < 0 Then
                Await DialogBoxAsync("Sorry, za dużo uruchomień")
                Return False
            End If
        End If

        Dim sCurrDate As String = Date.Now.ToString("yyyyMMdd")
        If sCurrDate < mQuiz.sMinDate Then
            DumpMessage("Curr date " & sCurrDate & " < minDate " & mQuiz.sMinDate)
            Await DialogBoxAsync("Sorry, ale jeszcze za wcześnie - poczekaj parę dni")
            Return False
        End If

        If sCurrDate > mQuiz.sMaxDate Then
            DumpMessage("Curr date " & sCurrDate & " > maxDate " & mQuiz.sMaxDate)
            Await DialogBoxAsync("Sorry, ale już jest za późno - szkolenie wygasło")
            Return False
        End If

        Return True

    End Function

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()
        'ProgRingInit(False, True)

        If mQuiz Is Nothing Then Return
        uiTitle.Text = mQuiz.sName
        If Not Await CheckCzyMoznaUruchomic() Then Return

        mQuizContent = New VBlibek.QuizContent(mQuiz, App.GetQuizyRootFolder.Path)
        Await ReadQuizAsync()

        AddHandler moTimer.Tick, AddressOf Timer_Tick

        TryStartBackMusic()

        Await GoNextQuestion()
    End Sub
    Private Async Sub TryStartBackMusic()

        Dim sMusicFilePathname As String = IO.Path.Combine(App.GetQuizyRootFolder.Path, mQuiz.sFolder, "background.mp3")
        If Not IO.File.Exists(sMusicFilePathname) Then Return

        Dim oFile As Windows.Storage.StorageFile = Await Windows.Storage.StorageFile.GetFileFromPathAsync(sMusicFilePathname)
        Dim oMediaSrc = Windows.Media.Core.MediaSource.CreateFromStorageFile(oFile)

        moMediaPlayer.Source = oMediaSrc
        moMediaPlayer.IsLoopingEnabled = True
        moMediaPlayer.Play()

    End Sub

    Private Sub uiGoNext_Click(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()
        GoNextQuestion()
    End Sub

    Private Async Function ReadQuizAsync() As Task
        DumpCurrMethod()

        Dim iMaxQuestion As Integer = mQuizContent.ReadQuiz()
        If iMaxQuestion < 1 Then
            Await DialogBoxAsync(mQuizContent.sLastError)
            Return
        End If

        uiProgCnt.Maximum = iMaxQuestion

    End Function


    Private Async Function DialogBoxWithTimeoutAsync(sMsg As String, iMsTimeout As Integer) As Task
        Dim oMsg As Windows.UI.Popups.MessageDialog = New Windows.UI.Popups.MessageDialog(sMsg)
        Dim oWait = oMsg.ShowAsync
        Await Task.Delay(iMsTimeout)
        Try
            oWait.Cancel()
        Catch ex As Exception
            ' jakby user wcześniej klikął
        End Try
    End Function

    Private Async Function CheckAnswersy(iCurrQuestion As Integer) As Task(Of Boolean)
        If uiListItems.ItemsSource Is Nothing Then Return True

        ' zapisanie odpowiedzi
        msAnswerLog = msAnswerLog & "Quesion: " & iCurrQuestion & vbTab & "answers: "
        For Each oAnswer As VBlibek.JednoPytanie In uiListItems.ItemsSource
            If oAnswer.bChecked Then
                msAnswerLog &= "T"
            Else
                msAnswerLog &= "F"
            End If
        Next
        msAnswerLog &= vbCrLf

        ' sprawdzenie odpowiedzi
        Dim bGood As Boolean = True
        For Each oAnswer As VBlibek.JednoPytanie In uiListItems.ItemsSource
            If oAnswer.bChecked <> oAnswer.bTrue Then bGood = False
        Next

        ' i reakcja na to
        If bGood Then
            Await DialogBoxWithTimeoutAsync("Dobrze!", 1000)
            Return True ' mozna przejść dalej
        Else
            Await DialogBoxWithTimeoutAsync("Niestety, to nie tak...", 1000)

            If mQuiz.bErrIgnore Then Return True

            For Each oAnswer As VBlibek.JednoPytanie In uiListItems.ItemsSource
                oAnswer.bChecked = False
            Next
            Return False
        End If


    End Function

    Private Async Function GoNextQuestion() As Task
        DumpCurrMethod()

        moTimer.Stop()

        If Not mQuizContent.IsLoaded Then mQuizContent.ReadQuiz()
        If Not mQuizContent.IsLoaded Then Return   ' wczytanie nieudane

        If Not Await CheckAnswersy(miCurrQuestion) Then Return ' coś z odpowiedziami było nie tak, pozostań
        ' *TODO* do sprawdzenia czy zniknęły zaznaczone odpowiedzi

        ' skasowanie listy odpowiedzi
        uiQuestionRow.Height = New GridLength(5)
        uiListItems.ItemsSource = Nothing

        If mQuiz.bRandom Then
            miCurrQuestion = mRandom.Next(1, mQuizContent.GetMaxQuestion)
            DumpMessage("Losuje pytanie: " & miCurrQuestion)
        Else

            If miCurrQuestion >= mQuizContent.GetMaxQuestion Then
                Await DialogBoxWithTimeoutAsync("KONIEC :)", 5000)

                If mQuiz.sEmail <> "" Or GetSettingsBool("allowEmail") Then
                    If Await DialogBoxYNAsync("Czy chcesz wysłać rezultat?") Then

                        Dim oMsg As Email.EmailMessage = New Windows.ApplicationModel.Email.EmailMessage()
                        oMsg.Subject = "Rezultat testu/quizu " & mQuiz.sName
                        Dim sTxt As String = "Załączam rezultat dzisiejszego testu" & vbCrLf & vbCrLf &
                                "Data: " & Date.Now & vbCrLf & vbCrLf & msAnswerLog

                        oMsg.Body = sTxt
                        If mQuiz.sEmail <> "" Then oMsg.To.Add(New Email.EmailRecipient(mQuiz.sEmail))

                        Await Email.EmailManager.ShowComposeNewEmailAsync(oMsg)
                    End If

                    Return
                End If
            End If

            miCurrQuestion += 1
            DumpMessage("Kolejne pytanie: " & miCurrQuestion)

        End If

        miCurrQuestion = Math.Max(1, miCurrQuestion)    ' pytania są od 1, nie od zera

        If Not mQuiz.bRandom Then
            uiProgCnt.Visibility = Visibility.Visible
            uiProgCnt.Value = miCurrQuestion
        End If

        Dim sBody As String = mQuizContent.CreateHtmlBody(miCurrQuestion)
        If sBody = "" Then
            ' nie ma, czyli pytanie za daleko?
            uiWebView.NavigateToString("")
        Else
            If mQuizContent.moAnswerList IsNot Nothing Then uiListItems.ItemsSource = mQuizContent.moAnswerList
            sBody = "<body>" & Await mQuizContent.InsertImages(sBody) & "</body>"
            uiWebView.NavigateToString("<html>" & mQuizContent.CreateHtmlHead() & sBody & "</html>")
        End If

        moTimer.Interval = TimeSpan.FromSeconds(mQuiz.iSeconds)
        moTimer.Start()

    End Function

    Private Sub wbViewer_NavigationStarting(sender As WebView, args As WebViewNavigationStartingEventArgs)
        DumpCurrMethod()

        If args.Uri Is Nothing Then Return

        args.Cancel = True

#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        Windows.System.Launcher.LaunchUriAsync(args.Uri)
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed

    End Sub
    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()
        RemoveHandler moTimer.Tick, AddressOf Timer_Tick
        moTimer.Stop()
        moMediaPlayer.Pause()
    End Sub

    Private Sub Timer_Tick(sender As Object, e As Object)
        DumpCurrMethod()
        uiGoNext_Click(Nothing, Nothing)
    End Sub
End Class
