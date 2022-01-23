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

Public NotInheritable Class Quiz
    Inherits Page

    Dim mQuiz As JedenQuiz = Nothing
    Dim miCurrQuestion As Integer = -1  ' init potrzebny
    Dim miMaxQuestion As Integer = 0
    Dim mEduQuizDoc As HtmlAgilityPack.HtmlDocument = Nothing
    Dim mRandom As New System.Random

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
            App.gQuizy.SaveAsync()  ' to sie moze dziac w tle
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

        Await GoNextQuestion()
    End Sub

    Private Sub uiGoNext_Click(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()
        GoNextQuestion()
    End Sub

    Private Async Function ReadQuizAsync() As Task
        DumpCurrMethod()

        Dim oRootFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Dim oFold As Windows.Storage.StorageFolder = Await oRootFold.GetFolderAsync(mQuiz.sFolder)
        If Not Await oFold.FileExistsAsync("quizkurs.htm") Then
            Await DialogBoxAsync("ERROR: brak podstawowego pliku!")
            Return
        End If

        Dim oFile As Windows.Storage.StorageFile = Await oFold.GetFileAsync("quizkurs.htm")

        mEduQuizDoc = New HtmlAgilityPack.HtmlDocument()
        mEduQuizDoc.Load(Await oFile.OpenStreamForReadAsync)

        ' policzmy teraz ile jest itemów
        miMaxQuestion = PoliczQuestions()
        If miMaxQuestion < 0 Then
            Await DialogBoxAsync("ERROR: brak pytan?")
            mEduQuizDoc = Nothing
            Return
        End If

        uiProgCnt.Maximum = miMaxQuestion

    End Function

    Private Function PoliczQuestions()
        DumpCurrMethod()

        Dim oBody As HtmlAgilityPack.HtmlNode = mEduQuizDoc.DocumentNode.SelectNodes("//body").ElementAt(0)
        Dim iCount As Integer = oBody.SelectNodes("hr").Count - 1
        If iCount < 1 Then Return -1

        DumpMessage("Doliczylem sie pytan: " & iCount)
        Return iCount
    End Function

    Private Function CreateHtmlHead()

        Dim oHeads As HtmlAgilityPack.HtmlNodeCollection = mEduQuizDoc.DocumentNode.SelectNodes("//head")
        If oHeads Is Nothing Then Return ""

        Return oHeads.ElementAt(0).OuterHtml

    End Function

    Private Function CreateHtmlBody(iQuestion As Integer)
        Dim sHtmlBody As String = ""

        Dim oBody As HtmlAgilityPack.HtmlNode = mEduQuizDoc.DocumentNode.SelectNodes("//body").ElementAt(0)
        Dim oNode As HtmlAgilityPack.HtmlNode = oBody.FirstChild
        Dim iQuestionLoop As Integer = iQuestion


        While oNode IsNot Nothing
            Dim oNode1 As HtmlAgilityPack.HtmlNode = oNode
            oNode = oNode.NextSibling

            If oNode1.Name <> "hr" Then Continue While

            ' jest hr, to teraz - czy to już?
            iQuestionLoop -= 1
            If iQuestionLoop > 0 Then Continue While

            While oNode IsNot Nothing
                If oNode.Name = "hr" Then
                    oNode = Nothing
                    Exit While
                End If

                If oNode.Name = "ul" Then
                    If oNode.InnerHtml.Contains("[QUIZ]") Then
                        ' obsługa PYTAŃ
                        uiQuestionRow.Height = New GridLength(1, GridUnitType.Star) ' połowa ekranu na pytania

                        Dim oAnswerList As New ObservableCollection(Of JednoPytanie)

                        ' *TODO* dodaj pytania
                        Dim oLiList As HtmlAgilityPack.HtmlNodeCollection = oNode.SelectNodes("li")
                        For Each oLiItem As HtmlAgilityPack.HtmlNode In oLiList
                            If oLiItem.InnerHtml.Contains("[QUIZ]") Then Continue For

                            Dim oNew As New JednoPytanie
                            oNew.sTekst = oLiItem.InnerHtml.Replace("[TRUE]", "")
                            If oLiItem.InnerHtml.Contains("[TRUE]") Then oNew.bTrue = True

                            oAnswerList.Add(oNew)
                        Next

                        uiListItems.ItemsSource = oAnswerList

                        Continue While  ' nie dodajemy zawartości tego UL do tekstu głównego
                    End If
                End If

                sHtmlBody += oNode.OuterHtml

                oNode = oNode.NextSibling
            End While

        End While

        Return "<body>" & sHtmlBody & "</body>"
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

    Private Async Function CheckAnswersy() As Task(Of Boolean)
        If uiListItems.ItemsSource Is Nothing Then Return True

        ' sprawdzenie odpowiedzi
        Dim bGood As Boolean = True
        For Each oAnswer As JednoPytanie In uiListItems.ItemsSource
            If oAnswer.bChecked <> oAnswer.bTrue Then bGood = False
        Next

        ' i reakcja na to
        If bGood Then
            Await DialogBoxWithTimeoutAsync("Dobrze!", 1000)
            Return True ' mozna przejść dalej
        Else
            Await DialogBoxWithTimeoutAsync("Niestety, to nie tak...", 1000)

            If mQuiz.bErrIgnore Then Return True

            For Each oAnswer As JednoPytanie In uiListItems.ItemsSource
                oAnswer.bChecked = False
            Next
            Return False
        End If


    End Function

    Private Async Function GoNextQuestion() As Task
        DumpCurrMethod()

        If mEduQuizDoc Is Nothing Then Await ReadQuizAsync()
        If mEduQuizDoc Is Nothing Then Return   ' wczytanie nieudane

        If Not Await CheckAnswersy() Then Return ' coś z odpowiedziami było nie tak, pozostań
        ' *TODO* do sprawdzenia czy zniknęły zaznaczone odpowiedzi

        ' skasowanie listy odpowiedzi
        uiQuestionRow.Height = New GridLength(5)
        uiListItems.ItemsSource = Nothing

        If mQuiz.bRandom Then
            miCurrQuestion = mRandom.Next(1, miMaxQuestion)
            DumpMessage("Losuje pytanie: " & miCurrQuestion)
        Else
            miCurrQuestion += 1
            DumpMessage("Kolejne pytanie: " & miCurrQuestion)
        End If

        If Not mQuiz.bRandom Then
            uiProgCnt.Visibility = Visibility.Visible
            uiProgCnt.Value = miCurrQuestion
        End If

        uiWebView.NavigateToString("<html>" & CreateHtmlHead() & CreateHtmlBody(miCurrQuestion) & "</html>")

    End Function

    Private Sub wbViewer_NavigationStarting(sender As WebView, args As WebViewNavigationStartingEventArgs)
        If args.Uri Is Nothing Then Return

        args.Cancel = True

#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        Windows.System.Launcher.LaunchUriAsync(args.Uri)
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed

    End Sub
End Class
