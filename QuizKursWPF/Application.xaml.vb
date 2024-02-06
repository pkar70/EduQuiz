
Imports pkar.UI.Extensions

Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.
#Region "Back button"
    Private Sub OnNavigatedAddBackButton(sender As Object, e As NavigationEventArgs)
        Try
            Dim oFrame As Frame = TryCast(sender, Frame)
            If oFrame Is Nothing Then Exit Sub
            If Not oFrame.CanGoBack Then Return

            Dim oPage As Page = TryCast(oFrame.Content, Page)
            If oPage Is Nothing Then Return

            Dim oGrid As Grid = TryCast(oPage.Content, Grid)
            If oGrid Is Nothing Then Return

            ' New SymbolIcon(Symbol.Back),
            Dim oButton As New Button With {
            .Content = " ← ",
            .FontSize = 16,
            .Name = "uiPkAutoBackButton",
                    .VerticalAlignment = VerticalAlignment.Top,
                    .HorizontalAlignment = HorizontalAlignment.Left}
            AddHandler oButton.Click, AddressOf OnBackButtonPressed

            Dim iCols As Integer = 0
            If oGrid.ColumnDefinitions IsNot Nothing Then iCols = oGrid.ColumnDefinitions.Count ' może być 0
            Dim iRows As Integer = 0
            If oGrid.RowDefinitions IsNot Nothing Then iRows = oGrid.RowDefinitions.Count ' może być 0
            If iRows > 1 Then
                Grid.SetRow(oButton, 0)
                Grid.SetRowSpan(oButton, iRows)
            End If
            If iCols > 1 Then
                Grid.SetColumn(oButton, 0)
                Grid.SetColumnSpan(oButton, iCols)
            End If
            oGrid.Children.Add(oButton)


        Catch ex As Exception
            pkar.CrashMessageExit("@OnNavigatedAddBackButton", ex.Message)
        End Try

    End Sub

    Private Sub OnBackButtonPressed(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oPage As Page = Nothing

        While True
            oPage = TryCast(oFE, Page)
            If oPage IsNot Nothing Then Exit While
            oFE = oFE.Parent
            If oFE Is Nothing Then Return
        End While

        oPage.GoBack

    End Sub

    Private Sub Application_LoadCompleted(sender As Object, e As NavigationEventArgs)
        AddHandler Me.Navigated, AddressOf OnNavigatedAddBackButton
        InitLib(Nothing)
    End Sub
#End Region

End Class
