#If Not NETFX_CORE and not PK_WPF Then
' assume WinUI3
Imports Microsoft.UI.Xaml
#End If

Public Class TextBlockPageTitle
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
            .FontSize = 20,
            .HorizontalAlignment = HorizontalAlignment.Center
        }
        Me.Content = _TBlock
    End Sub

End Class
