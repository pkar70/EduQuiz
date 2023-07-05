
' dla Komiksiarzyk2 oraz dla Aśki (papugi)
' moze byc protocol QuizKurs://link


' 2) <ul>[SINGLE] daje radiobuttony (radio , check, tylko jeden z nich Collapsed a drugi pokazywany)

' gdy istnieje plik topics.txt to wtedy mozna wyszukiwac w nim, a to jest przenoszone na <h2> w pliku HTML
' ( find "<h2>" quizkurs.htm > topics.txt )


Imports vb14 = VBlib.pkarlibmodule14


Public NotInheritable Class MainPage
    Inherits Page

    Private miBadCnt As Integer = 0     ' blokada abuse na tinyurl

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        Me.ShowAppVers()
        Me.ProgRingInit(True, False)

        If Not vb14.GetSettingsBool("defaultImported") Then
            Await ImportDefaultKursy()
            vb14.SetSettingsBool("defaultImported", True)
        End If


        Await PokazListeQuizow()
    End Sub

    Private Async Function ImportDefaultKursy() As Task

        Dim oFile As Windows.Storage.StorageFile
        For iLoop = 1 To 9 ' max 9 kursów (żeby jedna cyfra)
            Dim sUri = "ms-appx://defaulty/default" & iLoop.ToString
            Try
                oFile = Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(New Uri(sUri))
                If oFile Is Nothing Then Exit For  ' nie ma, to następnych też nie będzie
            Catch ex As Exception
                ' zapewne brak pliku
                Exit For
            End Try

            Dim oFold As Windows.Storage.StorageFolder = App.GetQuizyRootFolder
            Await oFile.CopyAsync(oFold)

            Dim oNew As VBlib.JedenQuiz = Await UnpackQuizFile("default" & iLoop)
            If oNew Is Nothing Then Return

            App.gQuizy.Add(oNew)
            App.gQuizy.Save()

        Next

    End Function

    Private Async Sub uiDownload_Click(sender As Object, e As RoutedEventArgs)

        Dim sLink As String = Await vb14.DialogBoxInputDirectAsync("Podaj ID quizu:")
        If sLink = "" Then Return

        Dim oUri As Uri = Await NormalizeUrl(sLink)
        If oUri Is Nothing Then Return

        Dim sDirName As String = Await DownloadQuizFile(oUri)
        If sDirName = "" Then Return

        Dim oNew As VBlib.JedenQuiz = Await UnpackQuizFile(sDirName)
        If oNew Is Nothing Then Return

        App.gQuizy.Add(oNew)
        App.gQuizy.Save()
        Await PokazListeQuizow()

    End Sub

    'Private Sub uiSetup_Click(sender As Object, e As RoutedEventArgs)
    '    Me.Frame.Navigate(GetType(Setup))
    'End Sub


    Private Sub uiGoQuiz_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        If oFE Is Nothing Then Return
        Dim oItem As VBlib.JedenQuiz = TryCast(oFE.DataContext, VBlib.JedenQuiz)
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
        Dim oQuiz As VBlib.JedenQuiz = TryCast(oFE.DataContext, VBlib.JedenQuiz)

        If Not Await vb14.DialogBoxYNAsync("Czy na pewno chcesz usunąć Quiz " & oQuiz.sName & "?") Then Return

        Dim oRootFold As Windows.Storage.StorageFolder = App.GetQuizyRootFolder
        Try
            System.IO.Directory.Delete(System.IO.Path.Combine(oRootFold.Path, oQuiz.sFolder), True)
            'Dim oFold As Windows.Storage.StorageFolder = Await oRootFold.GetFolderAsync(oQuiz.sFolder)
            'Await oFold.DeleteAsync
        Catch ex As Exception
            ' bo może to być przecież "(deleted)" z listy, prawda?
        End Try
        ' *TODO* ale zostaje jeszcze ZIP file, który właściwie też powinien zostać usuniety
        App.gQuizy.Delete(oQuiz)     ' z zapisem zmienionej wersji listy

        uiListItems.ItemsSource = Nothing
        uiListItems.ItemsSource = App.gQuizy.GetList

    End Sub

    Private Async Function PokazListeQuizow() As Task

        ' pokaż listę, może jakoś wielkość (w MB) danego quizu?

        ProgRingShow(True)
        App.gQuizy.Load()
        Dim iCount As Integer = App.gQuizy.CheckExistence
        If iCount > 0 Then
            Await vb14.DialogBoxAsync("Zniknięto " & iCount & " quizów!")
        End If
        Dim iCount1 As Integer = App.gQuizy.CheckOrfants
        If iCount1 > 0 Then
            Await vb14.DialogBoxAsync("Znaleziono " & iCount1 & " quizów!")
        End If

        If iCount + iCount1 > 0 Then App.gQuizy.Save()

        ProgRingShow(False)

        uiListItems.ItemsSource = Nothing
        uiListItems.ItemsSource = App.gQuizy.GetList

    End Function

    Private Async Function NormalizeUrl(sUri As String) As Task(Of Uri)
        ' czysty .Net 

        If miBadCnt > 3 Then Return Nothing

        Dim oUri As Uri = Await VBlib.MainPage.NormalizeUrlAsync(sUri)
        If oUri Is Nothing Then
            miBadCnt += 1
            If miBadCnt > 3 Then uiDownload.IsEnabled = False
            Await vb14.DialogBoxAsync(VBlib.MainPage.sLastError)
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
            If Not Await vb14.DialogBoxYNAsync("Taki plik już istnieje, overwrite?") Then Return ""
            System.IO.File.Delete(System.IO.Path.Combine(oFold.Path, sFilename))
        End If

        ProgRingShow(True)

        Dim sUserAgent As String = "QuizKurs " & GetAppVers()
        If Not Await VBlib.MainPage.DownloadQuizFileAsync(oUri, sUserAgent, oFold.Path, sFilename) Then
            Await vb14.DialogBoxAsync(VBlib.MainPage.sLastError)
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

    Private Async Function UnpackQuizFile(sFilename As String) As Task(Of VBlib.JedenQuiz)

        ' ewentualnie szyfrowanie pliku ZIP?

        Me.ProgRingShow(True)

        Dim oNewQuiz As VBlib.JedenQuiz = VBlib.MainPage.UnpackQuizFile(App.GetQuizyRootFolder.Path, sFilename)

        Me.ProgRingShow(False)

        If oNewQuiz Is Nothing Then
            Await vb14.DialogBoxAsync(VBlib.MainPage.sLastError)
            Return Nothing
        End If

        Return oNewQuiz

    End Function



End Class
