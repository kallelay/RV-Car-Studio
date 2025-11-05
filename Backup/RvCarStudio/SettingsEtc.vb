Module SettingsEtc
    'Settings file

    Dim Set_Path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\RVCS\"
    Dim Set_Loc$ = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\RVCS\SETTINGS"
    Private Sett$

    'Our settings
    Public D As New Dictionary(Of String, String)
    Function CheckSettings() As Boolean
        Return IO.File.Exists(Set_Loc)
    End Function
    Function InitSettings()
        'Directory doesn't exist? make
        If IO.Directory.Exists(Set_Path) = False Then
            MkDir(Set_Path)
        End If

        'Settings aren't fine?
        If CheckSettings() = False Then
            Return False
        End If

        'get all lines
        Dim lines() As String = IO.File.ReadAllLines(Set_Loc)
        For i = 0 To lines.Count - 1
            If D.ContainsKey(Split(lines(i), ":")(0)) = False Then _
                D.Add(Split(lines(i), ":")(0), Mid(lines(i), InStr(lines(i), ":") + 2)) Else _
                D(Split(lines(i), ":")(0)) = Mid(lines(i), InStr(lines(i), ":") + 2)

        Next
        Return True
    End Function
    Function Sett_get(ByVal key$, ByVal IfFail As Object) As Object
        'not found
        If Not D.ContainsKey(key) Then Return IfFail

        Return D(key)

    End Function

    Sub Sett_set(ByVal key$, ByVal Value As Object)
        'not found
        If D.ContainsKey(key) Then
            D(key) = Value
        Else
            D.Add(key, Value)
        End If

        Save()


    End Sub
    Dim INPUT As IO.FileStream
    Function Binarize(ByVal str$) As Byte()
        Dim B(Len(str)) As Byte
        For i = 0 To B.Count - 2
            B(i) = Asc(str(i))
        Next
        Return B
    End Function
    Sub Save()
        INPUT = IO.File.OpenWrite(Set_Loc)
        Dim Pos As ULong = 0, l As Long = 0

        For i = 0 To D.Count - 1
            l = Len(D.Keys(i)) + Len(D(D.Keys(i))) + 3
            INPUT.Write(Binarize(D.Keys(i) & ": " & D(D.Keys(i)) & vbNewLine), Pos, l)
            l += Pos
        Next
        INPUT.Flush()
        INPUT.Close()

    End Sub
End Module
