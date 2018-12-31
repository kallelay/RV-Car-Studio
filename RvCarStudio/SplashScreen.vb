Public NotInheritable Class SplashScreen
    Public INCR_VER As Boolean = True
    Public Initializing = False

    Public ProgressBar As List(Of String)
    'TODO: This form can easily be set as the splash screen for the application by going to the "Application" tab
    '  of the Project Designer ("Properties" under the "Project" menu).

    Dim Tic As Double

    Public ForceConfig = False      'force load 'config'
    Public SkipCarChoice = False    'Skip Car browser
    Public ForceLoadCar = False     'Load car on start
    Public CarToL$ = ""             ' Car to load on start 
    Private Sub SplashScreen_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        ' On Build
        'HACK:  Inc?
        INCR_VER = True  ' True

        If INCR_VER And My.Computer.Name = "KALLEL-PC" And InStr(Application.ExecutablePath, "Debug", CompareMethod.Text) > 0 Then
            Console_.W("Computer KALLEL-PC, Mode Debug")
            Console_.W("Starting building...")
            OnBuild.Go()

        End If




        'Tick count?
        Tic = Environment.TickCount




        'Version
        Me.Vers.Text = "version " & System.String.Format("{0}.{1}", My.Application.Info.Version.Major, My.Application.Info.Version.Minor) ', BuildNumber, Revision)

        'Copyright info
        info.Text = "Now Loading ... " & My.Application.Info.Title & vbNewLine & vbNewLine & _
                    "Version: " & My.Application.Info.Version.ToString & vbNewLine & vbNewLine & _
                    "Description: " & My.Application.Info.Description & vbNewLine & vbNewLine & _
                    "Memory: " & My.Application.Info.WorkingSet / (2 ^ 20) & "MB" & vbNewLine & vbNewLine & _
                    "Copyright: " & My.Application.Info.Copyright

        Dim wait_forcarname = 0
        For Each x In My.Application.CommandLineArgs
            Select Case LCase(x)
                Case "-config"
                    ForceConfig = True
                Case "-skipcarchoice"
                    SkipCarChoice = True
                Case "-build"
                    MakeBuild()
                Case "-readme"
                    GenerateReadMe()
                Case Else

                    'previous  -load waiting for the car name to finish!
                    If wait_forcarname Then
                        If InStr(x, ")") = 0 Then
                            CarToL &= " " & x
                        Else
                            wait_forcarname = False
                            CarToL &= " " & Split(x, ")")(0)
                            ForceLoadCar = True
                        End If
                    End If

                    '-load triggered
                    If InStr(x, "-load") > 0 Then
                        If InStr(x, ")") = 0 Then
                            If InStr(x, "(") > 0 Then CarToL = Split(x, "(")(1) : wait_forcarname = 1 : Continue For
                        Else
                            CarToL = Split(Split(x, "(")(1), ")")(0)
                            ForceLoadCar = True
                        End If
                    End If
            End Select
        Next


    End Sub

    Private Sub MainLayoutPanel_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs)

    End Sub
    Const Pgs = 8 : Dim Ci%
    Sub Progress(ByVal Name$)
        Console_.W(Name)
        Application.DoEvents()
        loading.Text = Name
        Ci += 1
        Label2.Width = Ci * Label3.Width / Pgs
        '  Label2.Width = Label6.Width
    End Sub
    Private Sub SplashScreen_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        'Initializing
        If Initializing Then Exit Sub
        Initializing = True

        Application.DoEvents()
        Progress("Loading Settings...")

        If ForceConfig Then GoTo Config

        If InitSettings() = False Then _
            GoTo Config _
            Else _
            GoTo Loadsettings


        '//kay: not needed
        'If GetSetting("RVCS", "settings", "lang", "") <> "" And IO.File.Exists(GetSetting("RVCS", "settings", "lang", "")) = True Then GoTo Loadsettings

Config:
        '''' CONFIGURATION
        'Find all languages 
        CarBrowser.Hide()
        LanguageFile.FindAllLanguages()


        '  Me.Hide()
        Config.ShowDialog()

Loadsettings:

        Progress("Loading Settings...")
        're-Volt path
        RVPATH = Sett_get("dir", "")
        'RVPATH = GetSetting("Car Load", "settings", "dir", "")

        Progress("Loading language")


        'Langauges
        If Languages.Count = 0 Then AddLanguage(Sett_get("lang", "")) Else Languages(0) = Languages(Config.ComboBox1.SelectedIndex)
        Languages(0).LoadAll()
        Tip.LoadTips(Languages(0))

        If SkipCarChoice Then GoTo skipcarchoice
choosecar:
        CarBrowser.Hide()
        Progress("Loading cars...")
        CarBrowser.LoadAllCarsIntoList()




        'Checking available AA modes?
        Progress("Inspecting Available GL Modes...")
        Dim gfModes As OpenTK.Graphics.GraphicsMode
        If Sett_get("Graphic's Colors", 0) = 0 Then


        End If





        Progress("Initializing OpenGL")
        CarBrowser.Opacity = 0.1
        CarBrowser.Show()

        initGL(CarBrowser.GlControl1)
        AllInit()

        'Checking?
        Console_.W("GL vers: " & GL_EXT.getVer)
        Console_.W("Graphic Supplier: " & GL_EXT.getVendor)
        Console_.W("Graphic Renderer: " & GL_EXT.getRenderer)
        If InStr(DirectCast(GL_EXT.getVendor(), String), "intel", CompareMethod.Text) > 0 Then
            If Sett_get("nagged_about_intel", False) = False Then
                YESNO.Fill("You have got a cursed Graphic card", "And it's called Intel... which for some reason hates me, so if ""render"" fails please report it immediately")
                YESNO.ShowDialog()
                Sett_set("nagged_about_intel", True)
            End If
        End If


        'For Each sample In New Int32() {8, 4, 2, 0}
        'Try
        Dim sample = 1
        gfModes = New OpenTK.Graphics.GraphicsMode(32, 8, 0, sample, 0, 2)
        SAMPLE_ACCEPTED = sample
        Console_.W("Anti-Alias samples :" & sample)
        '    Exit For
        '   Catch ex As Exception

        '   End Try
        ' Next



        '
        Application.DoEvents()
        Progress("starting openGL")
        CarBrowser.Show()
        CarBrowser.Timer2.Start()



        Dim Tim As New Timer
        Tim.Interval = 3000
        AddHandler Tim.Tick, AddressOf CarBrowser.Label3.Hide
        Tim.Start()


        If Not SkipCarChoice And ForceLoadCar Then
            CarBrowser.Search.Text = CarToL
            '  CarBrowser.ListBox2.SelectedIndex = 0
        End If
        CarBrowser.Opacity = 1

        Initializd = True

        CarBrowser.Label3.Text = "Initializing was done in " & Environment.TickCount - Tic & " ms"
        Me.Hide()
        Exit Sub
skipcarchoice:
        CarBrowser.Hide()
        CarEditor.Show()

        initGL(CarEditor.GlControl1)

        AllInit()
        CarBrowser.LoadAllCarsIntoList()
        CarBrowser.Search.Text = CarToL
        CarBrowser.ListBox2.SelectedIndex = 0
        CarBrowser.LoadOneCar()

        CarBrowser.Pick_Click(sender, e)
        CarEditor.Timer2.Start()




        ' CarBrowser.carlist.FormattingEnabled = False

        ' CarBrowser.CreateParams.ClassStyle += &H20000



    End Sub

    Private Sub Copyright_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles info.Click

    End Sub
End Class
