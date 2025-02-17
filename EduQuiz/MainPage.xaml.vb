
' dla Komiksiarzyk2 oraz dla Aśki (papugi)
' moze byc protocol QuizKurs://link


' 2) <ul>[SINGLE] daje radiobuttony (radio , check, tylko jeden z nich Collapsed a drugi pokazywany)

' gdy istnieje plik topics.txt to wtedy mozna wyszukiwac w nim, a to jest przenoszone na <h2> w pliku HTML
' ( find "<h2>" quizkurs.htm > topics.txt )


Imports pkar.UI.Extensions

Public NotInheritable Class MainPage
    Inherits Page

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        Me.InitDialogs

        Me.ProgRingInit(True, False)
        Dim sFoldFrom As String = ""

#If Not PK_WPF Then
        Try
            Dim oFoldFrom As Windows.Storage.StorageFolder = Await Windows.Storage.StorageFolder.GetFolderFromPathAsync("ms-appx://defaulty")
            sFoldFrom = oFoldFrom.Path
        Catch ex As Exception
            sFoldFrom = ""
        End Try

        Dim sFoldTo As String = Windows.Storage.ApplicationData.Current.LocalFolder.Path
#Else
        Dim sFoldTo As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        sFoldTo = IO.Path.Combine(sFoldTo, "QuizKurs")
        IO.Directory.CreateDirectory(sFoldTo)
#End If

        Await VBlib.MainPage.UstalListeQuizow(sFoldFrom, sFoldTo)

        PokazListeQuizow()
    End Sub

    Private Async Sub uiDownload_Click(sender As Object, e As RoutedEventArgs)

        Dim sUserAgent As String = "QuizKurs " & GetAppVers()

        ' bez tego Add robi crash - bo ObservableList z innego thread, więc muszę wygasić listę
        uiListItems.ItemsSource = Nothing

        If Await VBlib.MainPage.DownloadNewQuizButton(sUserAgent) Then
            PokazListeQuizow()
        Else
            If VBlib.MainPage._iBadCnt > 3 Then uiDownload.IsEnabled = False
        End If

    End Sub

    ' *TODO* tu zmieniam na próbę
#If PK_WPF Then
    Private Sub uiGoQuiz_Tapped(sender As Object, e As MouseButtonEventArgs)
#Else
    Private Sub uiGoQuiz_Tapped(sender As Object, e As Object)
#End If

        Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        If oFE Is Nothing Then Return
        Dim oItem As VBlib.JedenQuiz = TryCast(oFE.DataContext, VBlib.JedenQuiz)
        If oItem Is Nothing Then Return
        Dim sQuizName As String = oItem.sName

#If PK_WPF Then
        Dim target As Quiz = New Quiz()
        target.SetQuiz(sQuizName)
        Me.NavigationService.Navigate(target)
        ' inaczej być nie może, bo:
        ' (a) WPF nie ma przekazywania parametru inaczej niż w NEW
        ' (b) ale wtedy nie działa Page_Loaded (nie wchodzi do tego)
#Else
        Me.Frame.Navigate(GetType(Quiz), sQuizName)
#End If
    End Sub

    Private Sub uiStartQuiz_Click(sender As Object, e As RoutedEventArgs)
        uiGoQuiz_Tapped(sender, Nothing)
    End Sub

    Private Async Sub uiDelQuiz_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        Dim oQuiz As VBlib.JedenQuiz = TryCast(oFE?.DataContext, VBlib.JedenQuiz)

        Await VBlib.MainPage.DeleteQuiz(oQuiz)

        'uiListItems.ItemsSource = Nothing
        'uiListItems.ItemsSource = VBlib.MainPage._Quizy

    End Sub

    Private Sub PokazListeQuizow()
        ' pokaż listę, może jakoś wielkość (w MB) danego quizu?
        uiListItems.ItemsSource = Nothing
        uiListItems.ItemsSource = VBlib.MainPage._Quizy
    End Sub

End Class
