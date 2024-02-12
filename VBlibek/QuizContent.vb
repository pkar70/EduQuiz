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

Imports pkar.DotNetExtensions

Public Class QuizContent
    Private ReadOnly mQuiz As JedenQuiz = Nothing

    ' Private miCurrQuestion As Integer = -1  ' init potrzebny
    Private miMaxQuestion As Integer = 0    ' wyliczane przy wczytywaniu
    Private mEduQuizDoc As HtmlAgilityPack.HtmlDocument = Nothing
    ' Private mRandom As New System.Random
    Private ReadOnly mQuizRootFolder As String = ""

    Public maSearchTerms As String()

    Public Const MAIN_HTML_FILE As String = "quizkurs.htm"
    Public Const MAIN_INFO_TXT_FILE As String = "quizkurs.txt"
    Public Const MAIN_INFO_JSON_FILE As String = "quizkurs.json"
    Public Const MAIN_INFO_INDEX_FILE As String = "quizkurs.index.txt"

    Public Property currPage As QuizPage
    Public msAnswerLog As String = ""
    Private Property _htmlHead As String

    Public Property _TestGoodCnt As Integer
    Public Property _TestFailCnt As Integer

    ' Public _sLastError As String = ""

    Public Sub New(oItem As JedenQuiz, sRootFolder As String)
        mQuiz = oItem
        mQuizRootFolder = sRootFolder
    End Sub

    Public Function GetMaxQuestion() As Integer
        Return miMaxQuestion
    End Function

    Private Function GetQuizFolder() As String
        Dim sFolder = System.IO.Path.Combine(mQuizRootFolder, mQuiz.sFolder)
        If Not System.IO.Directory.Exists(sFolder) Then Return ""
        Return sFolder
    End Function

    Public Function IsLoaded() As Boolean
        Return (mEduQuizDoc IsNot Nothing)
    End Function

    Public Function ReadQuiz() As Integer
        DumpCurrMethod()

        Dim sFold As String = GetQuizFolder()
        If sFold = "" Then
            DialogBox("No Quiz folder")
            Return 0
        End If

        Dim sHtmlFile As String = System.IO.Path.Combine(sFold, MAIN_HTML_FILE)
        If Not System.IO.File.Exists(sHtmlFile) Then
            DialogBox("ERROR: brak podstawowego pliku!")
            Return 0
        End If

        mEduQuizDoc = New HtmlAgilityPack.HtmlDocument()
        mEduQuizDoc.Load(sHtmlFile)

        ' policzmy teraz ile jest itemów
        miMaxQuestion = PoliczQuestions()
        If miMaxQuestion < 0 Then
            DialogBox("ERROR: brak pytan?")
            mEduQuizDoc = Nothing
            Return 0
        End If

        Dim sIndexFile As String = System.IO.Path.Combine(sFold, MAIN_INFO_INDEX_FILE)
        If IO.File.Exists(sIndexFile) Then
            maSearchTerms = IO.File.ReadAllLines(sIndexFile)
        End If

        _htmlHead = CreateHtmlHead()

        _TestFailCnt = 0
        _TestGoodCnt = 0

        Return miMaxQuestion

    End Function

    Private Function CreateHtmlHead() As String

        Dim oHeads As HtmlAgilityPack.HtmlNodeCollection = mEduQuizDoc.DocumentNode.SelectNodes("//head")
        If oHeads Is Nothing Then Return ""

        Return oHeads.ElementAt(0).OuterHtml

    End Function


    Private Function PoliczQuestions()
        DumpCurrMethod()

        Dim oBody As HtmlAgilityPack.HtmlNode = mEduQuizDoc.DocumentNode.SelectNodes("//body").ElementAt(0)
        Dim iCount As Integer = oBody.SelectNodes("hr").Count - 1
        If iCount < 1 Then Return -1

        DumpMessage("Doliczylem sie pytan: " & iCount)
        Return iCount
    End Function

    Public Function IdzDoPytania(iNum As Integer) As QuizPage
        currPage = New QuizPage(mEduQuizDoc, iNum, _htmlHead, GetQuizFolder, _TestGoodCnt, _TestFailCnt)
        Return currPage
    End Function

    ''' <summary>
    ''' zapisuje log i sprawdza odpowiedzi, aktualizuje liczniki - FALSE gdy jest jakaś nieprawidłowa, TRUE: wszystko OK lub nie było pytań
    ''' </summary>
    Public Function CheckAnswersy() As Boolean

        If currPage.moAnswerList Is Nothing Then Return True
        If currPage.moAnswerList.Count < 2 Then Return True

        msAnswerLog &= currPage.GetLogOdpowiedzi

        Dim bOk As Boolean = currPage.CheckAnswersy
        If bOk Then
            _TestGoodCnt += 1
        Else
            _TestFailCnt += 1
        End If

        Return bOk
    End Function

End Class


Public Class QuizPage
    Public Property htmlPage As String
    Public Property htmlPageFallback As String
    Public Property moAnswerList As ObjectModel.ObservableCollection(Of JednoPytanie) = Nothing
    Public Property mPageNum As Integer
    Public Property bDisableConfirm As Boolean
    Public Property bErrStop As Boolean
    Public Property bRandom As Boolean

    Public Sub New(htmlDoc As HtmlAgilityPack.HtmlDocument, iNum As Integer, htmlHead As String, QuizRootFolder As String, testGoodCnt As Integer, testFailCnt As Integer)
        mPageNum = iNum

        Dim sBody As String = CreateHtmlBody(htmlDoc, iNum, testGoodCnt, testFailCnt)

        sBody = "<body>" & sBody & "</body>"
        htmlPageFallback = "<html>" & htmlHead & sBody & "</html>"

        sBody = InsertImages(sBody, QuizRootFolder)
        htmlPage = "<html>" & htmlHead & sBody & "</html>"

    End Sub

    Private Function CreateHtmlBody(htmlDoc As HtmlAgilityPack.HtmlDocument, iQuestion As Integer, testGoodCnt As Integer, testFailCnt As Integer) As String
        Dim sHtmlBody As String = ""

        Dim oBody As HtmlAgilityPack.HtmlNode = htmlDoc.DocumentNode.SelectNodes("//body").ElementAt(0)
        Dim oNode As HtmlAgilityPack.HtmlNode = oBody.FirstChild
        Dim iQuestionLoop As Integer = iQuestion

        moAnswerList = Nothing

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

                If oNode.Name = "ul" AndAlso oNode.InnerHtml.ContainsCI("[QUIZ]") Then
                    ' obsługa PYTAŃ
                    moAnswerList = New ObjectModel.ObservableCollection(Of JednoPytanie)
                    Dim bSingleAnswer As Boolean = False
                    If oNode.InnerHtml.ContainsCI("[SINGLE]") Then bSingleAnswer = True
                    If oNode.InnerHtml.ContainsCI("[NOCONF]") Then bDisableConfirm = True
                    If oNode.InnerHtml.ContainsCI("[ERRSTOP") Then bErrStop = True
                    If oNode.InnerHtml.ContainsCI("[RANDOM") Then bRandom = True

                    ' dodaj pytania
                    Dim oLiList As HtmlAgilityPack.HtmlNodeCollection = oNode.SelectNodes("li")
                    Dim iNum As Integer = 0
                    For Each oLiItem As HtmlAgilityPack.HtmlNode In oLiList
                        If oLiItem.InnerHtml.ContainsCI("[QUIZ]") Then Continue For

                        Dim oNew As New JednoPytanie
                        oNew.sTekst = oLiItem.InnerHtml.Replace("[TRUE]", "")
                        If oLiItem.InnerHtml.Contains("[TRUE]") Then oNew.bTrue = True
                        oNew.bSingleAnswer = bSingleAnswer
                        oNew.iNum = iNum
                        iNum += 1
                        moAnswerList.Add(oNew)
                    Next

                    oNode = oNode.NextSibling
                    Continue While  ' nie dodajemy zawartości tego UL do tekstu głównego
                End If

                sHtmlBody += oNode.OuterHtml

                oNode = oNode.NextSibling
            End While

        End While

        If bRandom Then
            ' https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
            Dim rndm As New Random

            ' System.ValueTuple do tego jest potrzebny
            moAnswerList = moAnswerList.
                            Select(Function(x) (x, rndm.Next)).
                            OrderBy(Function(Tuple) Tuple.Item2).
                            Select(Function(Tuple) Tuple.Item1)
        End If

        sHtmlBody = sHtmlBody.Replace("[ANSWERSSUMMARY]", GetSummaryText)

        sHtmlBody = sHtmlBody.Replace("[ANSWERSGOOD]", testGoodCnt)
        sHtmlBody = sHtmlBody.Replace("[ANSWERSFAIL]", testFailCnt)
        sHtmlBody = sHtmlBody.Replace("[ANSWERSTOTAL]", testFailCnt + testGoodCnt)
        sHtmlBody = sHtmlBody.Replace("[ANSWERSPERCENT]", CInt(100 * testGoodCnt / (testFailCnt + testGoodCnt)))

        Return sHtmlBody

    End Function

    Private Function InsertImages(sBody As String, QuizRootFolder As String) As String
        DumpCurrMethod()

        ' jesli nie ma src innego niz http, to wysyla string
        ' else, zapisuje na dysk jako currpage.htm i to wrzuca do webView
        ' albo jako src=data:

        ' zamiana każdego src='plik' do src='File2Base64(plik)'
        Dim iInd As Integer = sBody.IndexOf("src=""")
        While iInd > 0
            'iLen jest ogranicznikiem, chcemy całej nazwy pliku, ale nie całej strony
            Dim iLen As Integer = Math.Min(250, sBody.Length - iInd - 5)
            Dim sFileName As String = sBody.Substring(iInd + 5, iLen)

            ' link bezwzgledny, wiec go pomijamy
            If sFileName.StartsWith("http") Then
                iInd = sBody.IndexOf("src=""", iInd + 1)
                Continue While
            End If

            Dim iInd1 As Integer = sFileName.IndexOf("""")
            ' jeśli nie natrafiliśmy na koniec nazwy...
            If iInd1 < 1 Then
                iInd = sBody.IndexOf("src=""", iInd + 1)
                Continue While
            End If

            sFileName = sFileName.Substring(0, iInd1)
            Dim sBase64 As String = File2Base64(sFileName, QuizRootFolder)

            sBody = sBody.Substring(0, iInd + 5) & sBase64 & sBody.Substring(iInd + 5 + iInd1)

            iInd = sBody.IndexOf("src=""", iInd + sBase64.Length)    ' pomijam całe wstawione - wszak moglby sie tam pojawic przypadkowo string...
        End While

        Return sBody
    End Function

    Private Function File2MimeType(sFilename As String) As String
        Dim sExt As String = System.IO.Path.GetExtension(sFilename).Substring(1)
        If sExt = "mp3" Then Return "audio/mpeg"
        Return "Image/" & sExt
    End Function

    Private Function File2Base64(sFilename As String, QuizRootFolder As String) As String
        DumpCurrMethod()

        If QuizRootFolder = "" Then Return sFilename

        Dim sFilePathname As String = System.IO.Path.Combine(QuizRootFolder, sFilename)

        Using memStream As New IO.MemoryStream
            Using fileStr As IO.Stream = IO.File.OpenRead(sFilePathname)
                ' WISI na Copy!
                'Await fileStr.CopyToAsync(memStream)
                fileStr.CopyTo(memStream)
                Dim bytes As Byte() = memStream.ToArray()
                Dim dataStr As String = Convert.ToBase64String(bytes)
                Dim sRet As String = "data:" & File2MimeType(sFilename) & ";base64," & dataStr
                Return sRet
            End Using
        End Using

    End Function

    Public Function GetLogOdpowiedzi() As String
        If moAnswerList Is Nothing Then Return ""
        If moAnswerList.Count < 2 Then Return ""

        Dim ret As String = "Quesion: " & mPageNum & vbTab & "answers: "
        For Each oAnswer As VBlib.JednoPytanie In From c In moAnswerList Order By c.iNum
            ret &= If(oAnswer.iNum, "T", "F")
        Next

        ret &= vbCrLf

        Return ret
    End Function

    ''' <summary>
    ''' sprawdza odpowiedzi - FALSE gdy jest jakaś nieprawidłowa, TRUE: wszystko OK lub nie było pytań
    ''' </summary>
    Public Function CheckAnswersy() As Boolean
        If moAnswerList Is Nothing Then Return True
        If moAnswerList.Count < 2 Then Return True

        ' sprawdzenie odpowiedzi
        For Each oAnswer As VBlib.JednoPytanie In moAnswerList
            If oAnswer.bChecked <> oAnswer.bTrue Then Return False
        Next

        Return True
    End Function

    Private Function GetSummaryText() As String
        Return "<b>Podsumowanie odpowiedzi:</b><br>" & vbCrLf &
            "Poprawnych odpowiedzi: [ANSWERSPERCENT] %, tj. [ANSWERSGOOD] na [ANSWERSTOTAL]"
    End Function
End Class

