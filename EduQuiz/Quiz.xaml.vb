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
    Dim miMaxQuestion As Integer = 0    ' wyliczane przy wczytywaniu
    Dim mEduQuizDoc As HtmlAgilityPack.HtmlDocument = Nothing
    Dim mRandom As New System.Random
    Dim msAnswerLog As String = ""
    Dim moTimer As New DispatcherTimer

    Private Const MAIN_HTML_FILE As String = "quizkurs.htm"
    Private Const MAIN_INFO_FILE As String = "quizkurs.txt"

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        DumpCurrMethod()
        Dim sParam As String = e.Parameter.ToString
        mQuiz = App.gQuizy.GetItem(sParam)
    End Sub

    Private Async Function GetQuizFolder(bMessage As Boolean) As Task(Of Windows.Storage.StorageFolder)
        Dim oRootFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        If oRootFold Is Nothing Then
            If bMessage Then Await DialogBoxAsync("FATAL impossible: no LocalFolder")
            Return Nothing
        End If

        Dim oFold As Windows.Storage.StorageFolder = Await oRootFold.GetFolderAsync(mQuiz.sFolder)
        If oFold Is Nothing Then
            If bMessage Then Await DialogBoxAsync("FATAL impossible: no quiz folder")
            Return Nothing
        End If

        Return oFold
    End Function


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

        AddHandler moTimer.Tick, AddressOf Timer_Tick

        Await GoNextQuestion()
    End Sub

    Private Sub uiGoNext_Click(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()
        GoNextQuestion()
    End Sub

    Private Async Function ReadQuizAsync() As Task
        DumpCurrMethod()

        Dim oFold As Windows.Storage.StorageFolder = Await GetQuizFolder(True)

        If Not Await oFold.FileExistsAsync(MAIN_HTML_FILE) Then
            Await DialogBoxAsync("ERROR: brak podstawowego pliku!")
            Return
        End If

        Dim oFile As Windows.Storage.StorageFile = Await oFold.GetFileAsync(MAIN_HTML_FILE)

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

                        ' dodaj pytania
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

        Return sHtmlBody

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
        For Each oAnswer As JednoPytanie In uiListItems.ItemsSource
            If oAnswer.bChecked Then
                msAnswerLog &= "T"
            Else
                msAnswerLog &= "F"
            End If
        Next
        msAnswerLog &= vbCrLf

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

        moTimer.Stop()

        If mEduQuizDoc Is Nothing Then Await ReadQuizAsync()
        If mEduQuizDoc Is Nothing Then Return   ' wczytanie nieudane

        If Not Await CheckAnswersy(miCurrQuestion) Then Return ' coś z odpowiedziami było nie tak, pozostań
        ' *TODO* do sprawdzenia czy zniknęły zaznaczone odpowiedzi

        ' skasowanie listy odpowiedzi
        uiQuestionRow.Height = New GridLength(5)
        uiListItems.ItemsSource = Nothing

        If mQuiz.bRandom Then
            miCurrQuestion = mRandom.Next(1, miMaxQuestion)
            DumpMessage("Losuje pytanie: " & miCurrQuestion)
        Else

            If miCurrQuestion >= miMaxQuestion Then
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

        If Not mQuiz.bRandom Then
            uiProgCnt.Visibility = Visibility.Visible
            uiProgCnt.Value = miCurrQuestion
        End If

        Dim sBody As String = CreateHtmlBody(miCurrQuestion)
        If sBody = "" Then
            ' nie ma, czyli pytanie za daleko?
            uiWebView.NavigateToString("")
        Else
            sBody = "<body>" & sBody & "</body>"
            Await WczytajDoWebView(CreateHtmlHead(), sBody)
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
    Private Async Function WczytajDoWebView(sHead As String, sBody As String) As Task
        DumpCurrMethod()

        ' jesli nie ma src innego niz http, to wysyla string
        ' else, zapisuje na dysk jako currpage.htm i to wrzuca do webView
        ' albo jako src=data:
        'Dim oQuestion As New HtmlAgilityPack.HtmlDocument()
        'oQuestion.Load(sBody)

        ' zamiana każdego src='plik' do src='File2Base64(plik)'
        Dim iInd As Integer = sBody.IndexOf("src=""")
        While iInd > 0
            Dim iLen As Integer = Math.Min(250, sBody.Length - iInd - 5)
            Dim sFileName As String = sBody.Substring(iInd + 5, iLen)

            ' link bezwzgledny, wiec go pomijamy
            If sFileName.StartsWith("http") Then
                iInd = sBody.IndexOf("src=\", iInd + 1)
                Continue While
            End If

            Dim iInd1 As Integer = sFileName.IndexOf("""")
            ' jeśli nie natrafiliśmy na koniec nazwy...
            If iInd1 < 1 Then
                iInd = sBody.IndexOf("src=\", iInd + 1)
                Continue While
            End If

            sFileName = sFileName.Substring(0, iInd1)
            Dim sBase64 As String = Await File2Base64(sFileName)

            sBody = sBody.Substring(0, iInd + 5) & sBase64 & sBody.Substring(iInd + 5 + iInd1)

            iInd = sBody.IndexOf("src=\", iInd + sBase64.Length)    ' pomijam całe wstawione - wszak moglby sie tam pojawic przypadkowo string...
        End While

        uiWebView.NavigateToString("<html>" & sHead & sBody & "</html>")
    End Function



    'Private Async Function ToBase64(image As Byte(), height As UInt16, width As UInt16) As Task(Of String)
    ' https://stackoverflow.com/questions/38831434/uwp-app-show-image-from-the-local-folder-in-the-webview
    '    Dim encoded = New Windows.Storage.Streams.InMemoryRandomAccessStream()
    '    Dim encoder = Await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, encoded)
    '    encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, height, width, 96, 96, image)
    '    Await encoder.FlushAsync()
    '    encoded.Seek(0)

    '    Dim bytes = New Byte(encoded.Size)
    '    Await encoded.AsStream().ReadAsync(bytes, 0, encoded.Size)
    '    Return Convert.ToBase64String(bytes)
    'End Function


    'Private Async Function ToBase64(bitmap As WriteableBitmap) As Task(Of String)
    '    Dim bytes = bitmap.PixelBuffer.ToArray()
    '    Return Await ToBase64(bytes, bitmap.PixelWidth, bitmap.PixelHeight)
    'End Function

    'Private Async Function GetFileAsync(sFilename As String) As Task(Of Windows.Storage.StorageFile)
    '    Dim oFold As Windows.Storage.StorageFolder = Await GetQuizFolder(True)
    '    Dim myImage As Windows.Storage.StorageFile = Await oFold.GetFileAsync("myImage.jpg")
    '    Return myImage
    'End Function

    'Private Async Function File2Base64(sFilename As String) As Task(Of String)
    '    Dim myImage As Windows.Storage.StorageFile = Await GetFileAsync(sFilename)

    '    Dim properties As Windows.Storage.FileProperties.ImageProperties = Await myImage.Properties.GetImagePropertiesAsync()
    '    Dim bmp As WriteableBitmap = New WriteableBitmap(properties.Width, properties.Height)
    '    bmp.SetSource(Await myImage.OpenReadAsync())
    '    Dim dataStr As String = Await ToBase64(bmp)
    '    Dim fileType As String = myImage.FileType.Substring(1)
    '    Dim str As String = "<img src=""data:Image/" & myImage.FileType & ";base64," & dataStr & """>"
    '    Return str
    '    ' myWebView.NavigateToString(str)
    'End Function

    Private Async Function File2Base64(sFilename As String) As Task(Of String)
        DumpCurrMethod()

        ' *TODO* obsługa podkatalogów - na razie wtedy ignoruje przerabianie pliku
        If sFilename.Contains("/") Or sFilename.Contains("\") Then Return sFilename

        Dim oFold As Windows.Storage.StorageFolder = Await GetQuizFolder(False)
        If oFold Is Nothing Then Return sFilename

        Dim oFile As Windows.Storage.StorageFile = Await oFold.GetFileAsync(sFilename) ' na razie tylko w głównym katalogu, bez podkatalogów
        Using memStream As New MemoryStream
            Using fileStr As Stream = Await oFile.OpenStreamForReadAsync()
                Await fileStr.CopyToAsync(memStream)
                Dim bytes As Byte() = memStream.ToArray()
                Dim dataStr As String = Convert.ToBase64String(bytes)
                Dim sRet As String = "data:Image/" & oFile.FileType.Substring(1) & ";base64," & dataStr
                Return sRet
            End Using
        End Using

    End Function

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()
        RemoveHandler moTimer.Tick, AddressOf Timer_Tick
        moTimer.Stop()
    End Sub

    Private Sub Timer_Tick(sender As Object, e As Object)
        DumpCurrMethod()
        uiGoNext_Click(Nothing, Nothing)
    End Sub
End Class
