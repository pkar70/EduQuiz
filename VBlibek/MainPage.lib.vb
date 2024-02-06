
Public NotInheritable Class MainPage

    Public Shared _sLastError As String = ""
    Public Shared _iBadCnt As Integer = 0     ' blokada abuse na tinyurl
    Public Shared _Quizy As ListaQuiz
    Public Shared _QuizyRootFolder As String

    Public Shared Async Function NormalizeUrlAsync(sUri As String) As Task(Of Uri)
        ' czysty .Net 
        If _iBadCnt > 3 Then Return Nothing

        Dim oUri As Uri = Await VBlib.MainPage.NormalizeUrlMainAsync(sUri)
        If oUri Is Nothing Then
            _iBadCnt += 1
            Await DialogBoxAsync(VBlib.MainPage._sLastError)
            Return Nothing
        End If

        Return oUri
    End Function

    Private Shared Async Function NormalizeUrlMainAsync(sUri As String) As Task(Of Uri)

#If DEBUG Then
        ' tylko wersja debug moze miec zwykły link
        If sUri.StartsWith("http") Then Return New Uri(sUri)
#End If

        If sUri.Contains("/") Or sUri.Contains(":") Then
            _sLastError = "ERROR bad quiz ID"
            Return Nothing
        End If

        ' jeśli wywołanie per protocol, zamieniamy na swoje
        If sUri.StartsWith("quizkurs") Then sUri = sUri.Replace("quizkurs://", "")

        sUri = "https://tinyurl.com/" & sUri

        Dim oWebClntHand As New System.Net.Http.HttpClientHandler
        oWebClntHand.AllowAutoRedirect = False

        Using oHttp As New System.Net.Http.HttpClient(oWebClntHand, True)

            Dim oHttpResp As Net.Http.HttpResponseMessage

            Try
                oHttpResp = Await oHttp.GetAsync(New Uri(sUri))
            Catch ex As Exception
                ' jakby było przekierowanie na http, to nie moze zaczynac z https
                ' bo error, hresult=-2147012856, "A redirect request will change a secure to a non-secure connection"
                oHttpResp = Nothing
            End Try
            If oHttpResp Is Nothing Then
                oHttpResp = Await oHttp.GetAsync(New Uri(sUri.Replace("https:", "http:")))
            End If

            If oHttpResp.StatusCode <> 301 Then
                oHttpResp.Dispose()
                _sLastError = "ERROR bad quiz ID (net)"
                Return Nothing
            End If

            Dim oRetUri As New Uri(oHttpResp.Headers.Location.ToString)
            oHttpResp.Dispose()

            Return oRetUri

        End Using

    End Function

    ''' <summary>
    ''' Gdy nie było importu, to zaimportuj; sprawdź czy czegoś nie zniknięto
    ''' </summary>
    ''' <param name="foldFrom">Folder z defaultowymi kursami</param>
    ''' <param name="foldTo">Folder docelowy (app)</param>
    Public Shared Async Function UstalListeQuizow(foldFrom As String, foldTo As String) As Task
        _QuizyRootFolder = foldTo
        _Quizy = New VBlib.ListaQuiz(foldTo)

        If Not GetSettingsBool("defaultImported") Then
            Await ImportDefaultKursyAsync(foldFrom, foldTo)
            SetSettingsBool("defaultImported", True)
        End If

        _Quizy.Load()
        Dim iCount As Integer = _Quizy.CheckExistence
        If iCount > 0 Then
            Await DialogBoxAsync("Zniknięto " & iCount & " quizów!")
        End If
        Dim iCount1 As Integer = _Quizy.CheckOrfants
        If iCount1 > 0 Then
            Await DialogBoxAsync("Znaleziono " & iCount1 & " quizów!")
        End If

        If iCount + iCount1 > 0 Then _Quizy.Save()

    End Function

    Public Shared Async Function ImportDefaultKursyAsync(fromDir As String, toDir As String) As Task

        If String.IsNullOrWhiteSpace(fromDir) Then Return

        For iLoop = 1 To 9 ' max 9 kursów (żeby jedna cyfra)

            Dim filename As String = "default" & iLoop

            Dim fromFile As String = IO.Path.Combine(fromDir, filename)
            If Not IO.File.Exists(fromFile) Then Exit For ' nie ma, to następnych też nie będzie

            Dim toFile As String = IO.Path.Combine(toDir, filename)

            IO.File.Copy(fromFile, toFile)

            Dim oNew As VBlib.JedenQuiz = Await UnpackQuizFileAsync(filename)
            If oNew Is Nothing Then Return

            _Quizy.Add(oNew)
            _Quizy.Save()

        Next

    End Function

    ''' <summary>
    ''' z podanego Uri ściąga plik, zwraca lokalną nazwę (bez path)
    ''' </summary>
    ''' <param name="oUri">skąd ściągnąć</param>
    ''' <param name="sUserAgent">jakiego userAgent użyć</param>
    ''' <returns>filename.ext ściągniętego pliku</returns>
    Public Shared Async Function DownloadQuizFileAsync(oUri As Uri, sUserAgent As String) As Task(Of String)

        Dim sFilename As String = oUri.AbsoluteUri
        Dim iInd As Integer = sFilename.LastIndexOf("/")
        sFilename = sFilename.Substring(iInd + 1)
        If IO.File.Exists(System.IO.Path.Combine(_QuizyRootFolder, sFilename)) Then
            If Not Await DialogBoxYNAsync("Taki plik już istnieje, overwrite?") Then Return ""
            IO.File.Delete(System.IO.Path.Combine(_QuizyRootFolder, sFilename))
        End If

        Dim oHttp As New System.Net.Http.HttpClient()
        oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(sUserAgent)
        Dim oHttpResp As Net.Http.HttpResponseMessage = Await oHttp.GetAsync(oUri)
        If oHttpResp.StatusCode > 290 Then
            _sLastError = "ERROR: HTTP status " & oHttpResp.StatusCode.ToString & " while downloading data"
            Await DialogBoxAsync(_sLastError)
            Return ""
        End If

        'Dim oFile As Windows.Storage.StorageFile = Nothing
        Using oStream As IO.Stream = Await oHttpResp.Content.ReadAsStreamAsync
            ' oFile = Await oFold.CreateFileAsync(sFilename)
            Using oStreamWrite As IO.Stream = IO.File.OpenWrite(IO.Path.Combine(_QuizyRootFolder, sFilename))
                Await oStream.CopyToAsync(oStreamWrite)
                Await oStreamWrite.FlushAsync()
            End Using
        End Using

        'If Not VerifyCorrectZip(oFile.Path) Then
        '    ' *TODO* weryfikacja: czy jest w srodku plik quizkurs.txt z parametrami
        '    Await oFile.DeleteAsync
        '    Await DialogBoxAsync("ERROR: błędna struktura ściągniętego pliku")
        '    Return ""
        'End If
        ' https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchive?f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.IO.Compression.ZipArchive);k(TargetFrameworkMoniker-.NETCore,Version%253Dv5.0);k(DevLang-VB)%26rd%3Dtrue&view=net-6.0
        ' ale to ze Stream się bierze, więc już za dużo tego

        Return sFilename


    End Function

    Public Shared Async Function UnpackQuizFileAsync(sFilename As String) As Task(Of JedenQuiz)
        DumpCurrMethod()

        Dim sSourceFileName As String = System.IO.Path.Combine(_QuizyRootFolder, sFilename)

        ' przygotuj nazwę katalogu (jak nazwa pliku, ale ucięte na "__" - jakby były identyfikatory dla kogo dany plik jest i potem kasowany)
        Dim sDirName As String = sFilename
        Dim iInd As Integer = sDirName.IndexOf("__", iInd)
        If iInd > 1 Then sDirName = sDirName.Substring(0, iInd)
        sDirName = sDirName.Replace(".zip", "")

        Dim sDestDir As String = System.IO.Path.Combine(_QuizyRootFolder, sDirName)

        ' ucinamy 

        _sLastError = ""
        Try
            IO.Compression.ZipFile.ExtractToDirectory(sSourceFileName, sDestDir)
        Catch ex As Exception
            _sLastError = "ERROR extracting quiz: " & ex.Message
        End Try
        If _sLastError <> "" Then
            Await DialogBoxAsync(_sLastError)
            Return Nothing
        End If

        Dim oNewQuiz As JedenQuiz = VBlib.ListaQuiz.TryReadQuizInfo(_QuizyRootFolder, sDirName)
        If oNewQuiz Is Nothing Then
            _sLastError = "ERROR: missing files in archive"
        End If
        If _sLastError <> "" Then
            Await DialogBoxAsync(_sLastError)
            Return Nothing
        End If

        If oNewQuiz.sSearchHdr <> "" Then
            _sLastError = TryCreateSearchIndex(oNewQuiz, _QuizyRootFolder)
            If _sLastError <> "" Then Return Nothing
        End If

        Return oNewQuiz

    End Function

    ''' <summary>
    ''' Stwórzy indeks (do wyszukiwania) - jeśli takiego pliku jeszcze nie ma. Indeksuje HTML tags = sSearchHdr
    ''' </summary>
    ''' <param name="oNewQuiz">dla jakiego Quizu</param>
    ''' <param name="sRootFolder"></param>
    ''' <returns></returns>
    Private Shared Function TryCreateSearchIndex(oNewQuiz As JedenQuiz, sRootFolder As String) As String
        DumpCurrMethod()
        ' jest to szukalne, to sprawdzamy czy trzeba utworzyć plik indeksowy do szukania, czy nie

        Dim sQuizFolder As String = System.IO.Path.Combine(sRootFolder, oNewQuiz.sFolder)
        If Not System.IO.Directory.Exists(sQuizFolder) Then Return "ERROR: no quiz folder?" ' nie powinno się zdarzyć, bo właśnie rozpakowaliśmy tam quiz

        Dim sIndexFile As String = System.IO.Path.Combine(sQuizFolder, QuizContent.MAIN_INFO_INDEX_FILE)
        If System.IO.File.Exists(sIndexFile) Then Return "" ' jest indeks, to go nie robimy

        Dim sHtmlFile As String = System.IO.Path.Combine(sQuizFolder, QuizContent.MAIN_HTML_FILE)
        If Not System.IO.File.Exists(sHtmlFile) Then
            Return "ERROR: brak podstawowego pliku!"
        End If

        Dim oEduQuizDoc As New HtmlAgilityPack.HtmlDocument
        oEduQuizDoc.Load(sHtmlFile)

        Dim oBody As HtmlAgilityPack.HtmlNode = oEduQuizDoc.DocumentNode.SelectNodes("//body").ElementAt(0)
        If oBody Is Nothing Then Return "ERROR: no BODY in main file"
        Dim oSrchHdrs As HtmlAgilityPack.HtmlNodeCollection = oBody.SelectNodes(oNewQuiz.sSearchHdr)
        If oSrchHdrs Is Nothing Then Return "ERROR: no entries for index"
        DumpMessage("Bedzie w indeksie entries: " & oSrchHdrs.Count)
        Dim sIndex As String = ""
        For Each oHeader As HtmlAgilityPack.HtmlNode In oSrchHdrs
            sIndex = sIndex & oHeader.InnerText & vbCrLf
        Next

        System.IO.File.WriteAllText(sIndexFile, sIndex)

        Return ""

    End Function

    Public Shared Async Function DeleteQuiz(oQuiz As VBlib.JedenQuiz) As Task
        If oQuiz Is Nothing Then Return

        If Not Await DialogBoxYNAsync("Czy na pewno chcesz usunąć Quiz " & oQuiz.sName & "?") Then Return

        Try
            IO.Directory.Delete(IO.Path.Combine(_QuizyRootFolder, oQuiz.sFolder), True)
            'Dim oFold As Windows.Storage.StorageFolder = Await oRootFold.GetFolderAsync(oQuiz.sFolder)
            'Await oFold.DeleteAsync
        Catch ex As Exception
            ' bo może to być przecież "(deleted)" z listy, prawda?
        End Try
        ' *TODO* ale zostaje jeszcze ZIP file, który właściwie też powinien zostać usuniety
        _Quizy.Delete(oQuiz)     ' z zapisem zmienionej wersji listy

    End Function

    Public Shared Async Function DownloadNewQuizButton(sUserAgent As String) As Task(Of Boolean)

        Dim sLink As String = Await DialogBoxInputDirectAsync("Podaj ID quizu:")
        If sLink = "" Then Return False

        Dim oUri As Uri = Await NormalizeUrlAsync(sLink)
        If oUri Is Nothing Then Return False

        Dim sDirName As String = Await DownloadQuizFileAsync(oUri, sUserAgent)
        If sDirName = "" Then Return False

        Dim oNew As VBlib.JedenQuiz = Await UnpackQuizFileAsync(sDirName)
        If oNew Is Nothing Then Return False

        _Quizy.Add(oNew)
        _Quizy.Save()

        Return True

    End Function

End Class
