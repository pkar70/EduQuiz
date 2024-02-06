Imports pkar.UI.Extensions

#If Not NETFX_CORE And Not PK_WPF Then
' assume WinUI3
Imports Microsoft.UI.Xaml
#End If

Public Class TextBlockAppVersion
    Inherits Controls.UserControl

    Private _TBlock As Controls.TextBlock

    Public Property Text As String
        Get
            Return _TBlock.Text
        End Get
        Set(value As String)
            _TBlock.Text = value
        End Set
    End Property

    Public Sub New()
        _TBlock = New Controls.TextBlock With
            {
            .FontSize = 10,
            .HorizontalAlignment = HorizontalAlignment.Center,
            .Margin = New Thickness(0, 5, 0, 10)
        }
#If DEBUG Then
        _TBlock.ShowAppVers(True)
#Else
        _TBlock.ShowAppVers(false)
#End If

        Me.Content = _TBlock
    End Sub

End Class
