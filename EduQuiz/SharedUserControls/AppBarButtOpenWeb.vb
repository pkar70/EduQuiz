
Imports pkar.UI.Extensions


#If Not NETFX_CORE And Not PK_WPF Then
' assume WinUI3
Imports Microsoft.UI.Xaml
#End If


Public Class AppBarButtOpenWeb
#If PK_WPF Then
    Inherits AppBarButton
#Else
    Inherits Controls.AppBarButton
#End If

    Public Property Uri As Uri

    Public Sub New()
        MyBase.New

#If PK_WPF Then
        Icon = "Controls.Symbol.Globe"
#Else
        Icon = New Controls.SymbolIcon(Controls.Symbol.Globe)
#End If
        Label = "Open Web"

        AddHandler Me.Click, AddressOf PojdzDoWWW

    End Sub

    Private Sub PojdzDoWWW(sender As Object, e As RoutedEventArgs)
        Uri.OpenBrowser
    End Sub
End Class
