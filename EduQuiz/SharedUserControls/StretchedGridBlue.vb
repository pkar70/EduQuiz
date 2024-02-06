Imports pkar.DotNetExtensions


#If NETFX_CORE Then
Imports Windows.UI
#ElseIf Not PK_WPF Then
' assume WinUI3
Imports Microsoft.UI.Xaml
Imports Microsoft.UI
#End If


Public Class StretchedGridBlue
    Inherits StretchedGrid

    '<Grid HorizontalAlignment = "Stretch" Margin="0,5,0,0" BorderThickness="2" BorderBrush="Blue" >

#If PK_WPF Then
    Public Overrides Sub OnApplyTemplate()
#Else
    Protected Overrides Sub OnApplyTemplate()
#End If

        HorizontalAlignment = HorizontalAlignment.Center

#If Not PK_WPF Then
        BorderThickness = New Thickness(2)
        BorderBrush = New Media.SolidColorBrush(Colors.Blue)
#End If

        MyBase.OnApplyTemplate()
    End Sub

    Public Sub New()
        MyBase.New
#If Not PK_WPF Then
        BorderThickness = New Thickness(2)
        BorderBrush = New Media.SolidColorBrush(Colors.Blue)
#End If
    End Sub

End Class
