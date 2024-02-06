Imports pkar.DotNetExtensions


#If Not NETFX_CORE And Not PK_WPF Then
' assume WinUI3
Imports Microsoft.UI.Xaml
#End If

Public Class StretchedGrid
    Inherits Controls.Grid

    Public Property Cols As String
        Get
            Dim colwym As String = ""
            For Each col In ColumnDefinitions
                If colwym <> "" Then colwym &= ","
                colwym &= col.Width.ToString
            Next
            Return colwym
        End Get
        Set(value As String)
            Dim aArr As String() = value.Split(",")
            ColumnDefinitions.Clear()
            For Each col In aArr
                ColumnDefinitions.Add(New Controls.ColumnDefinition() With {.Width = Text2GridLen(col)})
            Next
        End Set
    End Property

    Public Property Rows As String
        Get
            Dim colwym As String = ""
            For Each col In RowDefinitions
                If colwym <> "" Then colwym &= ","
                colwym &= col.Height.ToString
            Next
            Return colwym
        End Get
        Set(value As String)
            Dim aArr As String() = value.Split(",")
            RowDefinitions.Clear()
            For Each col In aArr
                RowDefinitions.Add(New Controls.RowDefinition() With {.Height = Text2GridLen(col)})
            Next
        End Set
    End Property


    Private Function Text2GridLen(text As String) As GridLength
        If text.EqualsCI("Auto") Then Return New GridLength(0, GridUnitType.Auto)

        Dim typek As GridUnitType = GridUnitType.Pixel
        If text.Contains("*") Then
            typek = GridUnitType.Star
            text = text.Replace("*", "")
        End If

        Dim dbl As Double
        If Not Double.TryParse(text, dbl) Then Return New GridLength(1, GridUnitType.Star)

        Return New GridLength(dbl, typek)
    End Function


    '<Grid HorizontalAlignment="Stretch"  >

#If PK_WPF Then
    Public Overrides Sub OnApplyTemplate()
#Else
    Protected Overrides Sub OnApplyTemplate()
#End If
        HorizontalAlignment = HorizontalAlignment.Center

        MyBase.OnApplyTemplate()
    End Sub


End Class
