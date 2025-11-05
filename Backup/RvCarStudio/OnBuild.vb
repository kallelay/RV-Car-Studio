Module OnBuild

    'This is OnBuild() copy from main folder
    Sub Go()


        '
        My.Computer.FileSystem.CopyDirectory("..\..\data\", "data\", True)

        GoTo skipme
        For i = 0 To IO.Directory.GetFiles("..\..data\", "*.*", IO.SearchOption.AllDirectories).Count - 1

            IO.File.Copy(IO.Directory.GetFiles("..\..\data\", "*.*", IO.SearchOption.AllDirectories)(i), _
                                  "data\" & Split(IO.Directory.GetFiles("..\..\data\", "*.*", IO.SearchOption.AllDirectories)(i), "data")(1), True)



        Next
skipme:


        'Build++ (auto incriment)
        Dim gv$ = IO.File.ReadAllText("..\..\Global_Variables.vb")
        Dim h$ = Split(gv, "  Public Const Revision =")(0)
        Dim v$ = Split(Split(gv, "  Public Const Revision =")(1), Chr(34))(1)
        Dim f$ = Mid(gv, InStr(gv, "Public Const Type"))
        IO.File.WriteAllText("..\..\Global_Variables.vb", h & "  Public Const Revision =" & Chr(34) & Int(v) + 1 & Chr(34) & vbNewLine & f)


    End Sub
End Module
