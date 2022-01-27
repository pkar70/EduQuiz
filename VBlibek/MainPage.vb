
Public NotInheritable Class MainPage

    Public Shared sLastError As String = ""

    Public Shared Async Function NormalizeUrlAsync(sUri As String) As Task(Of Uri)

#If DEBUG Then
        ' tylko wersja debug moze miec zwykły link
        If sUri.StartsWith("http") Then Return New Uri(sUri)
#End If

        If sUri.Contains("/") Or sUri.Contains(":") Then
            sLastError = "ERROR bad quiz ID"
            Return Nothing
        End If

        ' jeśli wywołanie per protocol, zamieniamy na swoje
        If sUri.StartsWith("quizkurs") Then sUri = sUri.Replace("quizkurs://", "")

        sUri = "https://tinyurl.com/" & sUri

        Dim oWebClntHand As Net.Http.HttpClientHandler = New System.Net.Http.HttpClientHandler
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
                sLastError = "ERROR bad quiz ID (net)"
                Return Nothing
            End If

            Dim oRetUri As Uri = New Uri(oHttpResp.Headers.Location.ToString)
            oHttpResp.Dispose()

            Return oRetUri

        End Using

    End Function


    Public Shared Async Function DownloadQuizFileAsync(oUri As Uri, sUserAgent As String, sRootFolder As String, sFilename As String) As Task(Of Boolean)

        Dim oHttp As New System.Net.Http.HttpClient()
        oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(sUserAgent)
        Dim oHttpResp As Net.Http.HttpResponseMessage = Await oHttp.GetAsync(oUri)
        If oHttpResp.StatusCode > 290 Then
            sLastError = "ERROR: HTTP status " & oHttpResp.StatusCode.ToString & " while downloading data"
            Return False
        End If

        'Dim oFile As Windows.Storage.StorageFile = Nothing
        Using oStream As IO.Stream = Await oHttpResp.Content.ReadAsStreamAsync
            ' oFile = Await oFold.CreateFileAsync(sFilename)
            Using oStreamWrite As IO.Stream = System.IO.File.OpenWrite(System.IO.Path.Combine(sRootFolder, sFilename))
                Await oStream.CopyToAsync(oStreamWrite)
                Await oStreamWrite.FlushAsync()
            End Using
        End Using

        Return True

    End Function

    Public Shared Function UnpackQuizFile(sRootFolder As String, sFilename As String) As JedenQuiz

        Dim sSourceFileName As String = System.IO.Path.Combine(sRootFolder, sFilename)

        ' przygotuj nazwę katalogu (jak nazwa pliku, ale ucięte na "__" - jakby były identyfikatory dla kogo dany plik jest i potem kasowany)
        Dim sDirName As String = sFilename
        Dim iInd As Integer = sDirName.IndexOf("__", iInd)
        If iInd > 1 Then sDirName = sDirName.Substring(0, iInd)
        sDirName = sDirName.Replace(".zip", "")

        Dim sDestDir As String = System.IO.Path.Combine(sRootFolder, sDirName)

        ' ucinamy 

        Dim sError As String = ""
        Try
            System.IO.Compression.ZipFile.ExtractToDirectory(sSourceFileName, sDestDir)
        Catch ex As Exception
            sLastError = "ERROR extracting quiz: " & ex.Message
            Return Nothing
        End Try

        Dim sInfoFilename As String = System.IO.Path.Combine(sRootFolder, sDirName, QuizContent.MAIN_INFO_FILE)

        Dim oNewQuiz As JedenQuiz = VBlibek.ListaQuiz.ReadQuizInfo(sInfoFilename, sDirName)
        If oNewQuiz Is Nothing Then
            sLastError = "ERROR: missing files in archive"
            Return Nothing
        End If

        Return oNewQuiz

    End Function

End Class
