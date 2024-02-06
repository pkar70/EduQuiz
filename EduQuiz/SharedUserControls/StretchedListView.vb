
#If Not NETFX_CORE And Not PK_WPF Then
' assume WinUI3
Imports Microsoft.UI.Xaml
#End If

' zamiennik dla:
'<ListView ...  >
'<ListView.ItemContainerStyle>
'<Style TargetType = "ListViewItem" >
'                    <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
'                </Style>
'            </ListView.ItemContainerStyle>


Public Class StretchedListView
    Inherits Controls.ListView

#If PK_WPF Then
    Public Overrides Sub OnApplyTemplate()
#Else
    Protected Overrides Sub OnApplyTemplate()
#End If
        MyBase.OnApplyTemplate()

        Dim stylesetter As New Style(GetType(Controls.ListViewItem))
        stylesetter.Setters.Add(New Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch))
        Me.ItemContainerStyle = stylesetter

#If PK_WPF Then
        Me.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled)
#End If
    End Sub

End Class
