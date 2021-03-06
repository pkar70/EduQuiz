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
    Public Property sEmail As String = ""
    Public Property sSearchHdr As String = ""
End Class

Public Class ListaQuiz
    Private mItems As List(Of JedenQuiz)

    Private Const msFileName As String = "quizy.json"
    Private ReadOnly msRootPath As String = ""

    Public Sub New(sRootPath As String)
        msRootPath = sRootPath
    End Sub

    Public Function Load(Optional bForce As Boolean = False) As Boolean
        If IsLoaded() AndAlso Not bForce Then Return True

        Dim sFilename As String = System.IO.Path.Combine(msRootPath, msFileName)
        Dim sTxt As String = System.IO.File.ReadAllText(sFilename)
        If sTxt Is Nothing OrElse sTxt.Length < 5 Then
            mItems = New List(Of JedenQuiz)
            Return False
        End If

        mItems = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(List(Of JedenQuiz)))

        Return True

    End Function

    Public Function Save(Optional bForce As Boolean = False) As Boolean
        If mItems.Count < 1 Then Return False

        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(mItems, Newtonsoft.Json.Formatting.Indented)
        Dim sFilename As String = System.IO.Path.Combine(msRootPath, msFileName)
        System.IO.File.WriteAllText(sFilename, sTxt)

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

    Public Function CheckExistence() As Integer

        Dim iCount As Integer = 0

        For Each oQuiz As JedenQuiz In mItems
            If Not IO.Directory.Exists(IO.Path.Combine(msRootPath, oQuiz.sFolder)) Then
                iCount += 1
                oQuiz.sDesc = "(removed)"
            End If
        Next

        Return iCount
    End Function

    Public Function CheckOrfants() As Integer

        Dim iCount As Integer = 0

        For Each sFolder As String In IO.Directory.EnumerateDirectories(msRootPath)
            Dim bNew As Boolean = True
            For Each oQuiz As JedenQuiz In mItems
                If oQuiz.sFolder = IO.Path.GetFileName(sFolder) Then
                    bNew = False
                    Exit For
                End If
            Next


            If bNew Then

                ' Dim oNew As New JedenQuiz
                Dim oNew As JedenQuiz = TryReadQuizInfo(sFolder, System.IO.Path.GetFileName(sFolder))

                If oNew IsNot Nothing Then
                    iCount += 1
                    'oNew.sFolder = oFolder.Name
                    'oNew.sName = oFolder.Name
                    oNew.sDesc = "(orfant)"
                    mItems.Add(oNew)
                End If
            End If

        Next

        Return iCount
    End Function

    Public Sub Delete(oItem As JedenQuiz)
        mItems.Remove(oItem)
        Save()
    End Sub

    Public Function GetItem(sName As String) As JedenQuiz
        For Each oItem In mItems
            If oItem.sName = sName Then Return oItem
        Next
        Return Nothing
    End Function

    Public Shared Function TryReadQuizInfo(sRootFolder As String, sDirName As String) As JedenQuiz
        ' czysty .Net 

        Dim sInfoFilename As String = System.IO.Path.Combine(sRootFolder, sDirName, QuizContent.MAIN_INFO_JSON_FILE)
        If IO.File.Exists(sInfoFilename) Then Return ReadQuizInfoJSON(sInfoFilename, sDirName)

        sInfoFilename = System.IO.Path.Combine(sRootFolder, sDirName, QuizContent.MAIN_INFO_TXT_FILE)
        If IO.File.Exists(sInfoFilename) Then Return ReadQuizInfoTXT(sInfoFilename, sDirName)

        Return Nothing

    End Function

    Private Shared Function ReadQuizInfoTXT(sInfoFilename As String, sDirName As String) As JedenQuiz
        If Not System.IO.File.Exists(sInfoFilename) Then Return Nothing

        'Dim aLines As String() = sFileContent.Split(vbCrLf)
        Dim aLines As String() = System.IO.File.ReadAllLines(sInfoFilename)

        Dim oNew As New JedenQuiz
        oNew.sFolder = sDirName
        oNew.sName = sDirName   ' potem będzie zamienione

        For Each sLine As String In aLines
            Dim sTmp As String = sLine.Trim
            Dim aFields As String() = sTmp.Split(vbTab)
            If sTmp.StartsWith("Name") Then oNew.sName = aFields(1)
            If sTmp.StartsWith("Desc") Then oNew.sDesc = aFields(1)
            If sTmp.StartsWith("Till") Then oNew.sMaxDate = aFields(1)
            If sTmp.StartsWith("From") Then oNew.sMinDate = aFields(1)
            If sTmp.StartsWith("Runs") Then Integer.TryParse(aFields(1), oNew.iRuns)
            If sTmp.StartsWith("Secs") Then Integer.TryParse(aFields(1), oNew.iSeconds)
            If sTmp.StartsWith("Random") Then oNew.bRandom = True
            If sTmp.StartsWith("ErrIgnore") Then oNew.bErrIgnore = True
            If sTmp.StartsWith("Mail") Then oNew.sEmail = aFields(1)
            If sTmp.StartsWith("Search") Then oNew.sSearchHdr = aFields(1)
        Next

        Return oNew
    End Function

    Private Shared Function ReadQuizInfoJSON(sInfoFilename As String, sDirName As String) As JedenQuiz
        If Not System.IO.File.Exists(sInfoFilename) Then Return Nothing

        Dim sTxt As String = System.IO.File.ReadAllText(sInfoFilename)
        If sTxt Is Nothing OrElse sTxt.Length < 5 Then Return Nothing

        Try
            Dim oNew As JedenQuiz = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(JedenQuiz))
            oNew.sFolder = sDirName
            If String.IsNullOrEmpty(oNew.sName) Then oNew.sName = sDirName
            Return oNew
        Catch ex As Exception
            Return Nothing
        End Try

    End Function
End Class

Public Class JednoPytanie
    Public Property sTekst As String
    Public Property bTrue As Boolean = False
    Public Property bChecked As Boolean = False
    Public Property bSingleAnswer As Boolean = False
End Class