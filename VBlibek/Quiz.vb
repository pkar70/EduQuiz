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

Public Class QuizContent
    Private mQuiz As JedenQuiz = Nothing

    Private miCurrQuestion As Integer = -1  ' init potrzebny
    Private miMaxQuestion As Integer = 0    ' wyliczane przy wczytywaniu
    Private mEduQuizDoc As HtmlAgilityPack.HtmlDocument = Nothing
    Private mRandom As New System.Random
    Private mQuizRootFolder As String = ""

    Private Const MAIN_HTML_FILE As String = "quizkurs.htm"
    Public Const MAIN_INFO_FILE As String = "quizkurs.txt"

    Public sLastError As String = ""

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
            sLastError = "No Quiz folder"
            Return 0
        End If

        Dim sHtmlFile As String = System.IO.Path.Combine(sFold, MAIN_HTML_FILE)
        If Not System.IO.File.Exists(sHtmlFile) Then
            sLastError = "ERROR: brak podstawowego pliku!"
            Return 0
        End If

        mEduQuizDoc = New HtmlAgilityPack.HtmlDocument()
        mEduQuizDoc.Load(sHtmlFile)

        ' policzmy teraz ile jest itemów
        miMaxQuestion = PoliczQuestions()
        If miMaxQuestion < 0 Then
            sLastError = "ERROR: brak pytan?"
            mEduQuizDoc = Nothing
            Return 0
        End If

        Return miMaxQuestion

    End Function

    Private Function PoliczQuestions()
        DumpCurrMethod()

        Dim oBody As HtmlAgilityPack.HtmlNode = mEduQuizDoc.DocumentNode.SelectNodes("//body").ElementAt(0)
        Dim iCount As Integer = oBody.SelectNodes("hr").Count - 1
        If iCount < 1 Then Return -1

        DumpMessage("Doliczylem sie pytan: " & iCount)
        Return iCount
    End Function

    Public Function CreateHtmlHead()

        Dim oHeads As HtmlAgilityPack.HtmlNodeCollection = mEduQuizDoc.DocumentNode.SelectNodes("//head")
        If oHeads Is Nothing Then Return ""

        Return oHeads.ElementAt(0).OuterHtml

    End Function

    Public moAnswerList As ObjectModel.ObservableCollection(Of JednoPytanie) = Nothing

    Public Function CreateHtmlBody(iQuestion As Integer) As String
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
                        moAnswerList = New ObjectModel.ObservableCollection(Of JednoPytanie)

                        ' dodaj pytania
                        Dim oLiList As HtmlAgilityPack.HtmlNodeCollection = oNode.SelectNodes("li")
                        For Each oLiItem As HtmlAgilityPack.HtmlNode In oLiList
                            If oLiItem.InnerHtml.Contains("[QUIZ]") Then Continue For

                            Dim oNew As New JednoPytanie
                            oNew.sTekst = oLiItem.InnerHtml.Replace("[TRUE]", "")
                            If oLiItem.InnerHtml.Contains("[TRUE]") Then oNew.bTrue = True

                            moAnswerList.Add(oNew)
                        Next

                        Continue While  ' nie dodajemy zawartości tego UL do tekstu głównego
                    End If
                End If

                sHtmlBody += oNode.OuterHtml

                oNode = oNode.NextSibling
            End While

        End While

        Return sHtmlBody

    End Function

    Public Async Function InsertImages(sBody As String) As Task(Of String)
        DumpCurrMethod()

        ' jesli nie ma src innego niz http, to wysyla string
        ' else, zapisuje na dysk jako currpage.htm i to wrzuca do webView
        ' albo jako src=data:

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

        Return sBody
    End Function

    Private Async Function File2Base64(sFilename As String) As Task(Of String)
        DumpCurrMethod()

        Dim sFold As String = GetQuizFolder()
        If sFold = "" Then Return sFilename

        Dim sFilePathname As String = System.IO.Path.Combine(sFold, sFilename)

        Using memStream As New IO.MemoryStream
            Using fileStr As IO.Stream = IO.File.OpenRead(sFilePathname)
                Await fileStr.CopyToAsync(memStream)
                Dim bytes As Byte() = memStream.ToArray()
                Dim dataStr As String = Convert.ToBase64String(bytes)
                Dim sRet As String = "data:Image/" & System.IO.Path.GetExtension(sFilename).Substring(1) & ";base64," & dataStr
                Return sRet
            End Using
        End Using

    End Function
End Class

Public Module Quiz

End Module
