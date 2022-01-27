
' dla Komiksiarzyk2 oraz dla Aśki (papugi)
' moze byc protocol QuizKurs://link
' *TODO* losowa kolejność odpowiedzi

' niektore funkcje przerobiłem z uzywania Windows.* na System.*, co daje uniezależnienie od UWP i możliwość późniejszego wykorzystania jako VB-LIB w MAUI
' oznaczyłem je ' .Net

Imports VBlibek

Public NotInheritable Class MainPage
    Inherits Page

    Private miBadCnt As Integer = 0     ' blokada abuse na tinyurl

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        VBlibek.pkarlibmodule.InitDump(GetSettingsInt("debugLogLevel"), Windows.Storage.ApplicationData.Current.TemporaryFolder.Path)

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
        App.gQuizy.Save()
        Await PokazListeQuizow()

    End Sub

    Private Sub uiSetup_Click(sender As Object, e As RoutedEventArgs)
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

        Dim oRootFold As Windows.Storage.StorageFolder = App.GetQuizyRootFolder
        Try
            System.IO.Directory.Delete(System.IO.Path.Combine(oRootFold.Path, oQuiz.sFolder), True)
            'Dim oFold As Windows.Storage.StorageFolder = Await oRootFold.GetFolderAsync(oQuiz.sFolder)
            'Await oFold.DeleteAsync
        Catch ex As Exception
            ' bo może to być przecież "(deleted)" z listy, prawda?
        End Try
        ' *TODO* ale zostaje jeszcze ZIP file, który właściwie też powinien zostać usunieyu
        App.gQuizy.Delete(oQuiz)     ' z zapisem zmienionej wersji listy

    End Sub

    Private Async Function PokazListeQuizow() As Task

        ' pokaż listę, może jakoś wielkość (w MB) danego quizu?

        ProgRingShow(True)
        App.gQuizy.Load()
        Dim iCount As Integer = App.gQuizy.CheckExistence
        If iCount > 0 Then
            Await DialogBoxAsync("Zniknięto " & iCount & " quizów!")
        End If
        Dim iCount1 As Integer = App.gQuizy.CheckOrfants
        If iCount1 > 0 Then
            Await DialogBoxAsync("Znaleziono " & iCount1 & " quizów!")
        End If

        If iCount + iCount1 > 0 Then App.gQuizy.Save()

        ProgRingShow(False)

        uiListItems.ItemsSource = Nothing
        uiListItems.ItemsSource = App.gQuizy.GetList

    End Function

    Private Async Function NormalizeUrl(sUri As String) As Task(Of Uri)
        ' czysty .Net 

        If miBadCnt > 3 Then Return Nothing

        Dim oUri As Uri = Await VBlibek.MainPage.NormalizeUrlAsync(sUri)
        If oUri Is Nothing Then
            miBadCnt += 1
            If miBadCnt > 3 Then uiDownload.IsEnabled = False
            Await DialogBoxAsync(VBlibek.MainPage.sLastError)
            Return Nothing
        End If

        Return oUri
    End Function


    Private Async Function DownloadQuizFile(oUri As Uri) As Task(Of String)
        ' czysty .Net 

        ' zwraca "" gdy nie ma poprawnego pliku
        ' albo Filename (bez path) pliku .zip

        Dim oFold As Windows.Storage.StorageFolder = App.GetQuizyRootFolder

        Dim sFilename As String = oUri.AbsoluteUri
        Dim iInd As Integer = sFilename.LastIndexOf("/")
        sFilename = sFilename.Substring(iInd + 1)
        If System.IO.File.Exists(System.IO.Path.Combine(oFold.Path, sFilename)) Then
            If Not Await DialogBoxYNAsync("Taki plik już istnieje, overwrite?") Then Return ""
            System.IO.File.Delete(System.IO.Path.Combine(oFold.Path, sFilename))
        End If

        ProgRingShow(True)

        Dim sUserAgent As String = "QuizKurs " & GetAppVers()
        If Not Await VBlibek.MainPage.DownloadQuizFileAsync(oUri, sUserAgent, oFold.Path, sFilename) Then
            Await DialogBoxAsync(VBlibek.MainPage.sLastError)
        End If

        ProgRingShow(False)

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

    Private Async Function UnpackQuizFile(sFilename As String) As Task(Of JedenQuiz)

        ' ewentualnie szyfrowanie pliku ZIP?

        ProgRingShow(True)

        Dim oNewQuiz As JedenQuiz = VBlibek.MainPage.UnpackQuizFile(App.GetQuizyRootFolder.Path, sFilename)

        ProgRingShow(False)

        If oNewQuiz Is Nothing Then
            Await DialogBoxAsync(VBlibek.MainPage.sLastError)
            Return Nothing
        End If

        Return oNewQuiz

    End Function



End Class
