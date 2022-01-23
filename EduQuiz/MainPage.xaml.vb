
' dla Komiksiarzyk2 oraz dla Aśki (papugi)

'plik jako html, albo jako zip, z hasłem?, tak zeby mogly byc w srodku takze obrazki?
'rozpakowywanie do LocalTemp albo tp.

' moze byc protocol EduQuiz://link

Public NotInheritable Class MainPage
    Inherits Page

    Private miBadCnt As Integer = 0     ' blokada abuse na tinyurl

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.ShowAppVers()
        ProgRingInit(True, False)

        Await PokazListeQuizow()
    End Sub

    Private Async Sub uiDownload_Click(sender As Object, e As RoutedEventArgs)

        Dim sLink As String = Await DialogBoxInputDirectAsync("Podaj ID quizu:")
        If sLink = "" Then Return

        Dim oUri As Uri = Await NormalizeUrl(sLink)
        If oUri Is Nothing Then Return

        Dim sDirName As String = Await DownloadQuizFile(oUri)
        If sDirName = "" Then Return

        Dim oNew As JedenQuiz = Await UnpackQuizFile(sDirName)
        If oNew Is Nothing Then Return

        App.gQuizy.Add(oNew)
        Await App.gQuizy.SaveAsync
        Await PokazListeQuizow()

    End Sub

    Private Sub uiSetup_Click(sender As Object, e As RoutedEventArgs)
        ' *TODO* w tym chyba czas pomiędzy kolejnymi punktami (przy auto-zmianie)
        Me.Frame.Navigate(GetType(Setup))
    End Sub


    Private Sub uiGoQuiz_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        If oFE Is Nothing Then Return
        Dim oItem As JedenQuiz = TryCast(oFE.DataContext, JedenQuiz)
        If oItem Is Nothing Then Return
        Dim sQuizName As String = oItem.sName

        Me.Frame.Navigate(GetType(Quiz), sQuizName)
    End Sub

    Private Sub uiStartQuiz_Click(sender As Object, e As RoutedEventArgs)
        uiGoQuiz_Tapped(sender, Nothing)
    End Sub

    Private Async Sub uiDelQuiz_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        If oFE Is Nothing Then Return
        Dim oQuiz As JedenQuiz = TryCast(oFE.DataContext, JedenQuiz)

        If Not Await DialogBoxYNAsync("Czy na pewno chcesz usunąć Quiz " & oQuiz.sName & "?") Then Return

        Dim oRootFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Dim oFold As Windows.Storage.StorageFolder = Await oRootFold.GetFolderAsync(oQuiz.sFolder)
        Await oFold.DeleteAsync
        App.gQuizy.Delete(oQuiz)     ' z zapisem zmienionej wersji listy

    End Sub

    Private Async Function PokazListeQuizow() As Task
        ' pokaż listę, może jakoś wielkość (w MB) danego quizu?

        ProgRingShow(True)
        Await App.gQuizy.LoadAsync
        Dim iCount As Integer = Await App.gQuizy.CheckExistence
        If iCount > 0 Then
            Await DialogBoxAsync("Zniknięto " & iCount & " quizów!")
        End If
        Dim iCount1 As Integer = Await App.gQuizy.CheckOrfants
        If iCount1 > 0 Then
            Await DialogBoxAsync("Znaleziono " & iCount1 & " quizów!")
        End If

        If iCount + iCount1 > 0 Then Await App.gQuizy.SaveAsync

        ProgRingShow(False)

        uiListItems.ItemsSource = Nothing
        uiListItems.ItemsSource = App.gQuizy.GetList

    End Function

    Private Async Function NormalizeUrl(sUri As String) As Task(Of Uri)

        If miBadCnt > 3 Then Return Nothing

#If DEBUG Then
        ' tylko wersja debug moze miec zwykły link
        If sUri.StartsWith("http") Then Return New Uri(sUri)
#End If

        If sUri.Contains("/") Or sUri.Contains(":") Then
            Await DialogBoxAsync("ERROR bad quiz ID")
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
                miBadCnt += 1
                If miBadCnt > 3 Then uiDownload.IsEnabled = False
                Await DialogBoxAsync("ERROR bad quiz ID (net)")
                oHttpResp.Dispose()
                Return Nothing
            End If

            Dim oRetUri As Uri = New Uri(oHttpResp.Headers.Location.ToString)
            oHttpResp.Dispose()

            Return oRetUri

        End Using

    End Function


    Private Async Function DownloadQuizFile(oUri As Uri) As Task(Of String)
        ' zwraca "" gdy nie ma poprawnego pliku
        ' albo Filename (bez path) pliku .zip

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder

        Dim sFilename As String = oUri.AbsoluteUri
        Dim iInd As Integer = sFilename.LastIndexOf("/")
        sFilename = sFilename.Substring(iInd + 1)
        If Await oFold.FileExistsAsync(sFilename) Then
            If Not Await DialogBoxYNAsync("Taki plik już istnieje, overwrite?") Then Return ""
            Dim oFileDel As Windows.Storage.StorageFile = Await oFold.GetFileAsync(sFilename)
            Await oFileDel.DeleteAsync
        End If

        ProgRingShow(True)

        ' *TODO* ściągnięcie i weryfikacja czy to może być (pełny link skąd jest, itp.)

        Dim oHttp As New System.Net.Http.HttpClient()
        oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd("QuizKurs " & GetAppVers())
        Dim oHttpResp As Net.Http.HttpResponseMessage = Await oHttp.GetAsync(oUri)
        If oHttpResp.StatusCode > 290 Then
            Await DialogBoxAsync("HTTP status " & oHttpResp.StatusCode.ToString & " while downloading data")
            Return ""
        End If

        Dim oFile As Windows.Storage.StorageFile = Nothing
        Using oStream As Stream = Await oHttpResp.Content.ReadAsStreamAsync
            oFile = Await oFold.CreateFileAsync(sFilename)
            Using oStreamWrite As Stream = Await oFile.OpenStreamForWriteAsync
                Await oStream.CopyToAsync(oStreamWrite)
                Await oStreamWrite.FlushAsync()
            End Using
        End Using

        ProgRingShow(False)

        'If Not VerifyCorrectZip(oFile.Path) Then
        '    ' *TODO* weryfikacja: czy jest w srodku plik eduquiz.txt z parametrami
        '    Await oFile.DeleteAsync
        '    Await DialogBoxAsync("ERROR: błędna struktura ściągniętego pliku")
        '    Return ""
        'End If
        ' https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchive?f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.IO.Compression.ZipArchive);k(TargetFrameworkMoniker-.NETCore,Version%253Dv5.0);k(DevLang-VB)%26rd%3Dtrue&view=net-6.0
        ' ale to ze Stream się bierze, więc już za dużo tego

        Return oFile.Name

    End Function

    Private Async Function UnpackQuizFile(sFilename As String) As Task(Of JedenQuiz)
        ' zwraca NULL gdy jest coś nie tak
        ' albo gotowy oNew do wstawienia do listy quizów

        ' ewentualnie szyfrowanie pliku ZIP?

        ProgRingShow(True)

        Dim sSourceFileName As String = Windows.Storage.ApplicationData.Current.LocalFolder.Path & "\" & sFilename

        ' przygotuj nazwę katalogu (jak nazwa pliku, ale ucięte na "__" - jakby były identyfikatory dla kogo dany plik jest i potem kasowany)
        Dim sDirName As String = sFilename
        Dim iInd As Integer = sDirName.IndexOf("__", iInd)
        If iInd > 1 Then sDirName = sDirName.Substring(0, iInd)
        sDirName = sDirName.Replace(".zip", "")

        Dim sDestDir As String = Windows.Storage.ApplicationData.Current.LocalFolder.Path & "\" & sDirName

        ' ucinamy 

        Dim sError As String = ""
        Try
            Compression.ZipFile.ExtractToDirectory(sSourceFileName, sDestDir)
        Catch ex As Exception
            sError = ex.Message
        End Try

        ProgRingShow(False)

        If sError <> "" Then
            Await DialogBoxAsync("ERROR extracting quiz: " & sError)
            Return Nothing
        End If

        Dim oZipFold As Windows.Storage.StorageFolder = Await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(sDirName)
        If Not Await oZipFold.FileExistsAsync("eduquiz.txt") Then
            Await DialogBoxAsync("ERROR: missing files in archive")
            Return Nothing
        End If

        Dim oInfoFile As Windows.Storage.StorageFile = Await oZipFold.GetFileAsync("eduquiz.txt")
        Dim sTxt As String = Await oInfoFile.ReadAllTextAsync
        Dim aLines As String() = sTxt.Split(vbCrLf)

        Dim oNew As New JedenQuiz
        oNew.sFolder = sDirName
        oNew.sName = sDirName   ' potem będzie zamienione

        ' *TODO* tu można dodać różne pola, np. autozmianę, blokady różne itp.
        For Each sLine As String In aLines
            Dim sTmp As String = sLine.Trim
            Dim aFields As String() = sTmp.Split(vbTab)
            If sTmp.StartsWith("Name") Then oNew.sName = aFields(1)
            If sTmp.StartsWith("Desc") Then oNew.sDesc = aFields(1)
            If sTmp.StartsWith("Till") Then oNew.sMaxDate = aFields(1)
            If sTmp.StartsWith("From") Then oNew.sMinDate = aFields(1)
            If sTmp.StartsWith("Runs") Then Integer.TryParse(aFields(1), oNew.iRuns)
            If sTmp.StartsWith("Secs") Then Integer.TryParse(aFields(1), oNew.iSeconds)
            If sTmp.StartsWith("Random") Then oNew.bRandom = True
            If sTmp.StartsWith("ErrIgnore") Then oNew.bErrIgnore = True
        Next

        Return oNew

    End Function


End Class
