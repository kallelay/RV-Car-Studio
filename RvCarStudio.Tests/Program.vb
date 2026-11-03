'Runs against a COPY of a Re-Volt folder. Saving tests write into it.
'
'   RvCarStudio.Tests core <copy of Re-Volt folder>
'   RvCarStudio.Tests gui  <copy of Re-Volt folder> <folder for screenshots> [car,car]
'
'   RvCarStudio.Tests real <Re-Volt folder>     writes nothing: loads, checks and "saves" in memory
'   RvCarStudio.Tests config <Re-Volt folder>   first start wizard. WRITES %APPDATA%\RVCS\SETTINGS
Module Program
    Public Fails As Integer = 0
    Public Passes As Integer = 0

    Public Sub Log(ByVal s As String)
        Console.WriteLine(s)
    End Sub
    Public Sub Check(ByVal ok As Boolean, ByVal what As String)
        If ok Then
            Passes += 1
        Else
            Fails += 1
            Log("FAIL " & what)
        End If
    End Sub

    <STAThread()>
    Function Main(ByVal args() As String) As Integer
        If args.Length < 2 Then
            Log("usage: core <rvpath> | gui <rvpath> <shots>")
            Return 2
        End If
        CarStudio.Global_Variables.UseInvariantCulture()
        CarStudio.Global_Variables.RVPATH = args(1)
        Select Case args(0)
            Case "core"
                CoreTests.Run()
            Case "real"
                CoreTests.ReadOnlyPass()
            Case "config"
                GuiTests.RunConfig(args(1))
            Case "gui"
                GuiTests.Run(args(2), If(args.Length > 3, args(3), ""))
        End Select
        Log("passed " & Passes & ", failed " & Fails)
        Environment.Exit(If(Fails = 0, 0, 1))
        Return 0
    End Function
End Module
