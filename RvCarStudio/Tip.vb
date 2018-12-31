Module Tip
    Public ActiveTip$ = ""
    Sub RequestTip(ByVal Type$)
        ActiveTip = Type
        CarEditor.timer1_tick(Nothing, Nothing)

    End Sub
    Dim Listlist As New List(Of String())


    Dim Tipa As IO.TextReader
    ' Dim Tipfw As IO.TextWriter
    '   Dim Tipfr As IO.TextReader
    '  Dim Tipf As IO.Stream
    Public Class Region
        Public Name$
        Public Stats As New List(Of String())
        Public Sub New(ByVal Name_$)
            Name = Name_
            Regions.Add(Me)
        End Sub

        Public Sub AddStat(ByVal line$)
            If InStr(line, ":") = 0 Then Exit Sub

            Dim s(1) As String
            s(0) = (Mid(line, 1, InStr(line, ":") - 1))
            s(1) = (Mid(line, InStr(line, ":") + 1))
            Stats.Add(s)
        End Sub
        Public Function getStat(ByVal n%) As String()
            Return Stats(n)
        End Function
        Public Function getRandomStat() As String()
            If Stats.Count = 0 Then
                Console_.W("Error requesting: an empty " & Me.Name)
                Return Nothing
            End If
            Return Stats(Int(Rnd() * Stats.Count))
        End Function
    End Class
    Public Regions As New List(Of Region)
    Dim lastsIndex = 0 'To boost speed of searching region!

    Function getRegion(ByVal name$) As Region
        If name = "" Then Return If(Regions.Count > 0, Regions(0), Nothing)
        If LCase(name) = LCase(Regions(lastsIndex).Name) Then Return Regions(lastsIndex)
        For i = 0 To Regions.Count - 1
            If LCase(name) = LCase(Regions(i).Name) Then
                lastsIndex = i
                Return Regions(lastsIndex)
            End If
        Next
        Return Nothing
    End Function

    Sub LoadTips(ByVal LANG As LANGUAGE)
        Dim ra As New Region("") 'Init!

        Randomize()

        'obsolte
        'If IO.Directory.Exists(My.Computer.FileSystem.SpecialDirectories.Temp & "\CS") = False Then IO.Directory.CreateDirectory(My.Computer.FileSystem.SpecialDirectories.Temp & "\CS")



        Tipa = If(IO.File.Exists(Application.StartupPath & "\data\tips\" & LANG.Name & ".txt"), _
                  IO.File.OpenText(Application.StartupPath & "\data\tips\" & LANG.Name & ".txt"), _
                    IO.File.OpenText(Application.StartupPath & "\data\tips\English.txt"))



        Dim gen = Int(Rnd() * 2000)
        'Tipf = New IO.FileStream(My.Computer.FileSystem.SpecialDirectories.Temp & "\CS\" & gen, IO.FileMode.OpenOrCreate)

        'Tipfr = New IO.StreamReader(Tipf)
        '  Tipfw = New IO.StreamWriter(Tipf)


        'define constants
        Dim in$ = "0"
        Do Until LCase([in]) = "#exit "
            [in] = Tb(Tipa.ReadLine())
            If [in] Is Nothing Then Continue Do
            If [in](0) = "#" Then
                Select Case LCase(Split([in], " ")(0))
                    Case "#const"
                        Dim k(1) As String
                        k(0) = Split(Split(Replace(Replace([in], "= ", "="), " =", "="), " ")(1), "=")(0) : k(1) = Split(Replace(Replace([in], "= ", "="), " =", "="), "=")(1)

                        Listlist.Add(k)


                    Case "#exit"
                        Tipa.Close()

                        Tipa = If(IO.File.Exists(Application.StartupPath & "\data\tips\" & LANG.Name & ".txt"), _
            IO.File.OpenText(Application.StartupPath & "\data\tips\" & LANG.Name & ".txt"), _
              IO.File.OpenText(Application.StartupPath & "\data\tips\English.txt"))


                        Exit Do
                End Select

            End If

        Loop

        Dim reg$ = ""
        Dim arg$
        [in] = ""

        'no const!
        Do Until LCase([in]) = "#exit "

            'input in , and see
            [in] = Tb(Tipa.ReadLine()) & " "
            If Len([in]) < 3 Then Continue Do
            If [in] Is Nothing Then Continue Do
            If InStr([in], "#") < 5 And InStr([in], "#") <> 0 Then Continue Do

            arg = Split([in], " ")(1)
            'Let's recognize!
            Select Case LCase(Split([in], " ")(0))
                Case "region"
                    reg = Split([in], " ")(1)
                    Dim r As New Region(reg)
                Case "end"
                    'End what?
                    Select Case LCase(Split([in], " ")(1))
                        Case "region"
                            reg = ""

                    End Select


                Case Else

                    For i = 0 To Listlist.Count - 1
                        [in] = Replace([in], Listlist(i)(0), Listlist(i)(1))
                    Next
                    getRegion(reg).AddStat([in])


            End Select



        Loop



    End Sub
    Function Binarize(ByVal str$) As Byte()
        Dim b(Len(str) - 1) As Byte
        '  ReDim Binarize(Len(str) - 1)
        For i = 0 To Len(str) - 1
            b(i) = Asc(str(i))
        Next
        Return b
    End Function
    Function Tb(ByVal str$) As String
        str = Replace(str, "  ", " ")
        str = Replace(str, " ", " ")
        If Len(str) < 2 Then Return str

        Do Until (str(0) <> " " And str(0) <> Chr(9)) Or Len(str) = 1
            str = str.Substring(1)

        Loop


        Return str
    End Function
End Module
