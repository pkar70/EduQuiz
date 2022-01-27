'Public Class JedenQuiz
'    Public Property sName As String
'    Public Property sFolder As String
'    Public Property sDesc As String
'    Public Property iRuns As Integer = Integer.MaxValue
'    Public Property sMaxDate As String = "99999999"
'    Public Property sMinDate As String = "19999999"
'    Public Property bRandom As Boolean = False
'    Public Property iSeconds As Integer = Integer.MaxValue
'    Public Property bErrIgnore As Boolean = False
'    Public Property sEmail As String = ""
'End Class

'Public Class ListaQuiz
'    Private mItems As List(Of JedenQuiz)

'    Private Const sFileName As String = "quizy.json"

'    Public Async Function LoadAsync(Optional bForce As Boolean = False) As Task(Of Boolean)
'        If IsLoaded() AndAlso Not bForce Then Return True

'        Dim sTxt As String = Await App.GetQuizyRootFolder.ReadAllTextFromFileAsync(sFileName)
'        If sTxt Is Nothing OrElse sTxt.Length < 5 Then
'            mItems = New List(Of JedenQuiz)
'            Return False
'        End If

'        mItems = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(List(Of JedenQuiz)))

'        Return True

'    End Function

'    Public Async Function SaveAsync(Optional bForce As Boolean = False) As Task(Of Boolean)
'        If mItems.Count < 1 Then Return False

'        Dim oFold As Windows.Storage.StorageFolder = App.GetQuizyRootFolder
'        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(mItems, Newtonsoft.Json.Formatting.Indented)

'        Await oFold.WriteAllTextToFileAsync(sFileName, sTxt, Windows.Storage.CreationCollisionOption.ReplaceExisting)

'        'bModified = False

'        Return True

'    End Function

'    Public Function Add(oNew As JedenQuiz) As Boolean
'        If oNew Is Nothing Then Return False

'        If mItems Is Nothing Then
'            mItems = New List(Of JedenQuiz)
'        End If

'        For Each oItem As JedenQuiz In mItems
'            If oItem.sFolder = oNew.sFolder Then Return False ' nie umiem updatować (na razie)
'        Next

'        ' bModified = True

'        mItems.Add(oNew)

'        Return True
'    End Function

'    Public Function IsLoaded() As Boolean
'        If mItems Is Nothing Then Return False
'        Return True
'    End Function

'    Public Function GetList() As List(Of JedenQuiz)
'        Return mItems
'    End Function

'    Public Function Count() As Integer
'        If mItems Is Nothing Then Return -1
'        Return mItems.Count
'    End Function

'    Public Async Function CheckExistence() As Task(Of Integer)

'        Dim iCount As Integer = 0

'        Dim oRootFold As Windows.Storage.StorageFolder = App.GetQuizyRootFolder
'        For Each oQuiz As JedenQuiz In mItems
'            Try
'                Dim oFold As Windows.Storage.StorageFolder = Await oRootFold.GetFolderAsync(oQuiz.sFolder)
'            Catch ex As Exception
'                iCount += 1
'                oQuiz.sDesc = "(removed)"
'            End Try
'        Next

'        Return iCount
'    End Function

'    Public Async Function CheckOrfants() As Task(Of Integer)

'        Dim iCount As Integer = 0

'        Dim oFold As Windows.Storage.StorageFolder = App.GetQuizyRootFolder

'        For Each oFolder As Windows.Storage.StorageFolder In Await oFold.GetFoldersAsync
'            Dim bNew As Boolean = True
'            For Each oQuiz As JedenQuiz In mItems
'                If oQuiz.sFolder = oFolder.Name Then
'                    bNew = False
'                    Exit For
'                End If
'            Next


'            If bNew Then

'                ' Dim oNew As New JedenQuiz
'                Dim sInfoFilename As String = System.IO.Path.Combine(oFolder.Path, Quiz.MAIN_INFO_FILE)
'                Dim oNew As JedenQuiz = ReadQuizInfo(sInfoFilename, oFolder.Name)

'                If oNew IsNot Nothing Then
'                    iCount += 1
'                    'oNew.sFolder = oFolder.Name
'                    'oNew.sName = oFolder.Name
'                    oNew.sDesc = "(orfant)"
'                    mItems.Add(oNew)
'                End If
'            End If

'        Next

'        Return iCount
'    End Function

'    Public Sub Delete(oItem As JedenQuiz)
'        mItems.Remove(oItem)
'        SaveAsync()
'    End Sub

'    Public Function GetItem(sName As String) As JedenQuiz
'        For Each oItem In mItems
'            If oItem.sName = sName Then Return oItem
'        Next
'        Return Nothing
'    End Function

'    Public Function ReadQuizInfo(sInfoFilename As String, sDirName As String) As JedenQuiz
'        ' czysty .Net 

'        If Not System.IO.File.Exists(sInfoFilename) Then Return Nothing

'        'Dim aLines As String() = sFileContent.Split(vbCrLf)
'        Dim aLines As String() = System.IO.File.ReadAllLines(sInfoFilename)

'        Dim oNew As New JedenQuiz
'        oNew.sFolder = sDirName
'        oNew.sName = sDirName   ' potem będzie zamienione

'        For Each sLine As String In aLines
'            Dim sTmp As String = sLine.Trim
'            Dim aFields As String() = sTmp.Split(vbTab)
'            If sTmp.StartsWith("Name") Then oNew.sName = aFields(1)
'            If sTmp.StartsWith("Desc") Then oNew.sDesc = aFields(1)
'            If sTmp.StartsWith("Till") Then oNew.sMaxDate = aFields(1)
'            If sTmp.StartsWith("From") Then oNew.sMinDate = aFields(1)
'            If sTmp.StartsWith("Runs") Then Integer.TryParse(aFields(1), oNew.iRuns)
'            If sTmp.StartsWith("Secs") Then Integer.TryParse(aFields(1), oNew.iSeconds)
'            If sTmp.StartsWith("Random") Then oNew.bRandom = True
'            If sTmp.StartsWith("ErrIgnore") Then oNew.bErrIgnore = True
'            If sTmp.StartsWith("Mail") Then oNew.sEmail = aFields(1)
'        Next

'        Return oNew

'    End Function


'End Class

'Public Class JednoPytanie
'    Public Property sTekst As String
'    Public Property bTrue As Boolean = False
'    Public Property bChecked As Boolean = False
'End Class