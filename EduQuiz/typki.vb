Public Class JedenQuiz
    Public Property sName As String
    Public Property sFolder As String
    Public Property sDesc As String
    Public Property iRuns As Integer = Integer.MaxValue
    Public Property sMaxDate As String = "99999999"
    Public Property sMinDate As String = "19999999"
    Public Property bRandom As Boolean = False
    Public Property iSeconds As Integer = Integer.MaxValue

    Public Property bErrIgnore As Boolean = False
End Class

Public Class ListaQuiz
    Private mItems As List(Of JedenQuiz)

    Private Const sFileName As String = "quizy.json"

    Public Async Function LoadAsync(Optional bForce As Boolean = False) As Task(Of Boolean)
        If IsLoaded() AndAlso Not bForce Then Return True

        Dim sTxt As String = Await Windows.Storage.ApplicationData.Current.LocalFolder.ReadAllTextFromFileAsync(sFileName)
        If sTxt Is Nothing OrElse sTxt.Length < 5 Then
            mItems = New List(Of JedenQuiz)
            Return False
        End If

        mItems = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(List(Of JedenQuiz)))

        Return True

    End Function

    Public Async Function SaveAsync(Optional bForce As Boolean = False) As Task(Of Boolean)
        If mItems.Count < 1 Then Return False

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(mItems, Newtonsoft.Json.Formatting.Indented)

        Await oFold.WriteAllTextToFileAsync(sFileName, sTxt, Windows.Storage.CreationCollisionOption.ReplaceExisting)

        'bModified = False

        Return True

    End Function

    Public Function Add(oNew As JedenQuiz) As Boolean
        If oNew Is Nothing Then Return False

        If mItems Is Nothing Then
            mItems = New List(Of JedenQuiz)
        End If

        For Each oItem As JedenQuiz In mItems
            If oItem.sFolder = oNew.sFolder Then Return False ' nie umiem updatować (na razie)
        Next

        ' bModified = True

        mItems.Add(oNew)

        Return True
    End Function

    Public Function IsLoaded() As Boolean
        If mItems Is Nothing Then Return False
        Return True
    End Function

    Public Function GetList() As List(Of JedenQuiz)
        Return mItems
    End Function

    Public Function Count() As Integer
        If mItems Is Nothing Then Return -1
        Return mItems.Count
    End Function

    Public Async Function CheckExistence() As Task(Of Integer)

        Dim iCount As Integer = 0

        Dim oRootFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        For Each oQuiz As JedenQuiz In mItems
            Try
                Dim oFold As Windows.Storage.StorageFolder = Await oRootFold.GetFolderAsync(oQuiz.sFolder)
            Catch ex As Exception
                iCount += 1
                oQuiz.sDesc = "(removed)"
            End Try
        Next

        Return iCount
    End Function

    Public Async Function CheckOrfants() As Task(Of Integer)

        Dim iCount As Integer = 0

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder

        For Each oFolder As Windows.Storage.StorageFolder In Await oFold.GetFoldersAsync
            Dim bNew As Boolean = True
            For Each oQuiz As JedenQuiz In mItems
                If oQuiz.sFolder = oFolder.Name Then
                    bNew = False
                    Exit For
                End If
            Next


            If bNew Then
                iCount += 1
                Dim oNew As New JedenQuiz
                oNew.sFolder = oFolder.Name
                oNew.sName = oFolder.Name
                oNew.sDesc = "(orfant)"
                mItems.Add(oNew)
            End If

        Next

        Return iCount
    End Function

    Public Sub Delete(oItem As JedenQuiz)
        mItems.Remove(oItem)
        SaveAsync()
    End Sub

    Public Function GetItem(sName As String) As JedenQuiz
        For Each oItem In mItems
            If oItem.sName = sName Then Return oItem
        Next
        Return Nothing
    End Function

End Class

Public Class JednoPytanie
    Public Property sTekst As String
    Public Property bTrue As Boolean = False
    Public Property bChecked As Boolean = False
End Class