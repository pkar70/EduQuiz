
Imports pkar.DotNetExtensions

Public Class JedenQuiz
    Inherits pkar.BaseStruct

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
    Inherits pkar.BaseList(Of JedenQuiz)

    'Private mItems As List(Of JedenQuiz)

    ''Private Const msFileName As String = "quizy.json"
    Private ReadOnly msRootPath As String = ""

    Public Sub New(sRootPath As String)
        MyBase.New(sRootPath, "quizy.json")
        msRootPath = sRootPath
    End Sub



    ''' <summary>
    ''' własna implementacja, bo sprawdzamy istnienie
    ''' </summary>
    ''' <returns>TRUE gdy dodane, FALSE gdy nie dodane (bo np już jest a nie umiemy update)</returns>
    Public Overloads Function Add(oNew As JedenQuiz) As Boolean
        If oNew Is Nothing Then Return False

        'If mItems Is Nothing Then
        '    mItems = New List(Of JedenQuiz)
        'End If

        For Each oItem As JedenQuiz In Me
            If oItem.sFolder = oNew.sFolder Then Return False ' nie umiem updatować (na razie)
        Next

        ' bModified = True

        MyBase.Add(oNew)

        Return True
    End Function

    ''' <summary>
    ''' Sprawdź istnienie katalogów quizów (każdy quiz ma swój katalog)
    ''' </summary>
    ''' <returns>liczba skasowanych katalogów quizów</returns>
    Public Function CheckExistence() As Integer

        Dim iCount As Integer = 0

        For Each oQuiz As JedenQuiz In Me
            If Not IO.Directory.Exists(IO.Path.Combine(msRootPath, oQuiz.sFolder)) Then
                iCount += 1
                oQuiz.sDesc = "(removed)"
            End If
        Next

        Return iCount
    End Function

    ''' <summary>
    ''' Sprawdź czy może istnieją katalogi o których oficjalnie nic nie wiemy
    ''' </summary>
    ''' <returns>liczba katalogów które są, a nie ma ich w pliku danych</returns>
    Public Function CheckOrfants() As Integer

        Dim iCount As Integer = 0

        If Not IO.Directory.Exists(msRootPath) Then Return 0

        For Each sFolder As String In IO.Directory.EnumerateDirectories(msRootPath)
            Dim bNew As Boolean = True
            For Each oQuiz As JedenQuiz In Me
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
                    Add(oNew)
                End If
            End If

        Next

        Return iCount
    End Function

    ''' <summary>
    ''' Usuń z pliku danych, i ten plik zapisz
    ''' </summary>
    Public Sub Delete(oItem As JedenQuiz)
        'mItems.Remove(oItem)
        MyBase.Remove(oItem)
        Save()
    End Sub

    ''' <summary>
    ''' Znajdź dane dla quizu sName
    ''' </summary>
    Public Function GetItem(sName As String) As JedenQuiz

        ' Return Find(Function(x) x.sName = sName) // ale to zwraca default(t) gdy nie ma, a co to jest default tutaj?

        For Each oItem In Me
            If oItem.sName = sName Then Return oItem
        Next
        Return Nothing
    End Function

    ''' <summary>
    ''' Wczytaj metadane quizu, JSON a jak nie ma to TXT
    ''' </summary>
    ''' <param name="sRootFolder">root katalog dla quizów</param>
    ''' <param name="sDirName">nazwa katalogu z konkretnym quizem</param>
    ''' <returns>metadane quizu</returns>
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
            If sTmp.StartsWithCI("Name") Then oNew.sName = aFields(1)
            If sTmp.StartsWithCI("Desc") Then oNew.sDesc = aFields(1)
            If sTmp.StartsWithCI("Till") Then oNew.sMaxDate = aFields(1)
            If sTmp.StartsWithCI("From") Then oNew.sMinDate = aFields(1)
            If sTmp.StartsWithCI("Runs") Then Integer.TryParse(aFields(1), oNew.iRuns)
            If sTmp.StartsWithCI("Secs") Then Integer.TryParse(aFields(1), oNew.iSeconds)
            If sTmp.StartsWithCI("Random") Then oNew.bRandom = True
            If sTmp.StartsWithCI("ErrIgnore") Then oNew.bErrIgnore = True
            If sTmp.StartsWithCI("Mail") Then oNew.sEmail = aFields(1)
            If sTmp.StartsWithCI("Email") Then oNew.sEmail = aFields(1)
            If sTmp.StartsWithCI("Search") Then oNew.sSearchHdr = aFields(1)
        Next

        Return oNew
    End Function

    Private Shared Function ReadQuizInfoJSON(sInfoFilename As String, sDirName As String) As JedenQuiz
        If Not System.IO.File.Exists(sInfoFilename) Then Return Nothing

        Dim sTxt As String = System.IO.File.ReadAllText(sInfoFilename)
        If sTxt Is Nothing OrElse sTxt.Length < 5 Then Return Nothing

        ' Return LoadItem(sTxt) // tak się nie da, bo mamy tutaj shared

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
    Inherits pkar.BaseStruct

    Public Property sTekst As String
    Public Property bTrue As Boolean = False
    Public Property bChecked As Boolean = False
    Public Property bSingleAnswer As Boolean = False
    Public Property iNum As Integer
End Class