Imports CarStudio
Imports System.Diagnostics

'Drives the real program inside this process: real forms, real GL, screenshots to look at
Module GuiTests
    ReadOnly Property F() As CarStudio.My.MyProject.MyForms
        Get
            Return CarStudio.My.MyProject.Forms
        End Get
    End Property
    Dim Shots As String
    Dim Only As String
    Dim Started As Boolean = False

    Sub Run(ByVal shotDir As String, ByVal filter As String)
        Shots = shotDir
        Only = filter
        IO.Directory.CreateDirectory(Shots)

        AddHandler Application.ThreadException, Sub(s, e) Check(False, "UI exception: " & e.Exception.ToString())
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)
        Application.EnableVisualStyles()

        F.SplashScreen.RvPathOverride = RVPATH

        Dim t As New Timer With {.Interval = 200}
        AddHandler t.Tick, Sub(s, e)
                               If Started OrElse Not Initializd Then Exit Sub
                               Started = True
                               t.Stop()
                               Try
                                   Scenario()
                               Catch ex As Exception
                                   Check(False, "scenario: " & ex.ToString())
                               End Try
                               Log("passed " & Passes & ", failed " & Fails)
                               Environment.Exit(If(Fails = 0, 0, 1))
                           End Sub
        t.Start()
        Application.Run(F.SplashScreen)
    End Sub

    'The first start: no settings yet, the wizard asks for the folder
    Sub RunConfig(ByVal rv As String)
        AddHandler Application.ThreadException, Sub(s, e) Check(False, "UI exception: " & e.Exception.ToString())
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)
        Application.EnableVisualStyles()
        Dim st = 0
        Dim t As New Timer With {.Interval = 800}
        AddHandler t.Tick, Sub(s, e)
                               If Initializd Then
                                   t.Stop()
                                   Check(RVPATH = rv, "folder from the wizard: " & RVPATH)
                                   Check(Sett_get("dir", "") = rv, "folder in the settings")
                                   Check(IO.File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\RVCS\SETTINGS").Contains("dir: " & rv), "settings file")
                                   Check(F.CarBrowser.carList.Count > 10, "cars listed")
                                   Log("passed " & Passes & ", failed " & Fails)
                                   Environment.Exit(If(Fails = 0, 0, 1))
                               End If
                               If Not F.Config.Visible Then Exit Sub
                               Select Case st
                                   Case 0 : F.Config.Button1.PerformClick()
                                   Case 1
                                       F.Config.TextBox7.Text = rv
                                       Shot(F.Config, "config_folder")
                                       F.Config.Button1.PerformClick()
                                   Case Else : F.Config.Button1.PerformClick()
                               End Select
                               st += 1
                           End Sub
        t.Start()
        Shots = IO.Path.GetTempPath()
        RVPATH = ""
        F.SplashScreen.ForceConfig = True
        Application.Run(F.SplashScreen)
    End Sub

    Sub Pump(ByVal ms As Integer)
        Dim sw = Stopwatch.StartNew()
        Do While sw.ElapsedMilliseconds < ms
            Application.DoEvents()
            Threading.Thread.Sleep(10)
        Loop
    End Sub

    'From the form itself, never from the screen: other windows must not end up in a picture.
    'The 3D view stays empty here, see ShotGL for it
    Sub Shot(ByVal form As Form, ByVal name As String)
        Pump(150)
        Using b As New Bitmap(form.Width, form.Height)
            form.DrawToBitmap(b, New Rectangle(Point.Empty, form.Size))
            b.Save(Shots & "\" & name & ".png", Imaging.ImageFormat.Png)
        End Using
    End Sub

    Sub SelectCar(ByVal folder As String)
        For Each item In F.CarBrowser.carList
            If F.CarBrowser.CarFolderOf(item).Equals(folder, StringComparison.OrdinalIgnoreCase) Then
                F.CarBrowser.ListBox2.SelectedItem = item
                Exit Sub
            End If
        Next
        Check(False, "car not in list: " & folder)
    End Sub

    Sub Scenario()
        Dim expected = IO.Directory.GetDirectories(RVPATH & "\cars").Count(Function(d) IO.File.Exists(d & "\parameters.txt"))
        Check(F.CarBrowser.carList.Count = expected, "all cars listed: " & F.CarBrowser.carList.Count & " of " & expected)
        Log("GL: " & GL_EXT.getRenderer() & " / " & GL_EXT.getVer())

        PRM.Theta = 18 : PRM.Phi = 215
        F.CarBrowser.Auto_Rotate_car.Checked = False

        'search
        F.CarBrowser.Search.Text = "rc b"
        Pump(100)
        Check(F.CarBrowser.ListBox2.Items.Count >= 1 AndAlso F.CarBrowser.ListBox2.SelectedItem.ToString().Contains("RC Bandit"), "search finds RC Bandit")
        F.CarBrowser.Search.Text = "#stock"
        Pump(100)
        Dim stockCount = F.CarBrowser.ListBox2.Items.Count
        F.CarBrowser.Search.Text = "#custom"
        Pump(100)
        Check(stockCount + F.CarBrowser.ListBox2.Items.Count = F.CarBrowser.carList.Count, "stock + custom = all: " & stockCount & " + " & F.CarBrowser.ListBox2.Items.Count)
        F.CarBrowser.Search.Text = "Search"
        Pump(100)
        Check(F.CarBrowser.ListBox2.Items.Count = F.CarBrowser.carList.Count, "search cleared, no doubles")

        'every car in the browser
        For Each folder In F.CarBrowser.carList.Select(Function(i) F.CarBrowser.CarFolderOf(i)).ToList()
            If Only <> "" AndAlso Not Only.Split(","c).Contains(folder) Then Continue For
            SelectCar(folder)
            Pump(250)
            Check(cars.Count = 1 AndAlso cars(0).DirName.Equals(folder, StringComparison.OrdinalIgnoreCase), "loaded " & folder)
            Log(folder & ": body " & F.CarBrowser.Label4.Text & " wheels " & F.CarBrowser.Label5.Text.Replace(vbNewLine, " ") & " total " & F.CarBrowser.PolyCount & " models " & Models.Count & " by '" & F.CarBrowser.Label22.Text & "'")
            ShotGL(F.CarBrowser.GlControl1, "gl_" & folder)
        Next
        Shot(F.CarBrowser, "browser")

        Editor(If(Only <> "", Only.Split(","c)(0), "adeon"))
    End Sub

    'Just the 3D view from two sides, read from OpenGL
    Sub ShotGL(ByVal gl As OpenTK.GLControl, ByVal name As String)
        Dim w = gl.ClientSize.Width, h = gl.ClientSize.Height
        Using sheet As New Bitmap(w * 2, h, Imaging.PixelFormat.Format24bppRgb)
            Using g = Graphics.FromImage(sheet)
                Dim i = 0
                For Each angle In New Single() {215, 40}
                    PRM.Phi = angle
                    BeginFrame(gl)
                    DrawFloor(FloorHeight(cars(0)))
                    DrawModels(Models)
                    Using one As New Bitmap(w, h, Imaging.PixelFormat.Format32bppRgb)
                        Dim data = one.LockBits(New Rectangle(0, 0, w, h), Imaging.ImageLockMode.WriteOnly, Imaging.PixelFormat.Format32bppRgb)
                        OpenTK.Graphics.OpenGL.GL.ReadPixels(0, 0, w, h, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, data.Scan0)
                        one.UnlockBits(data)
                        'GL rows start at the bottom
                        one.RotateFlip(RotateFlipType.RotateNoneFlipY)
                        g.DrawImage(one, i * w, 0)
                    End Using
                    gl.SwapBuffers()
                    i += 1
                Next
            End Using
            sheet.Save(Shots & "\" & name & ".png", Imaging.ImageFormat.Png)
        End Using
    End Sub

    Sub Editor(ByVal folder As String)
        SelectCar(folder)
        Pump(200)
        F.CarBrowser.Pick_Click(Nothing, Nothing)
        Pump(500)
        Check(F.CarEditor.Visible, "editor open")
        ShotGL(F.CarEditor.GlControl1, "gl_editor")

        PRM.Phi = 215
        Dim emptyView = Nothing

        For i = 0 To F.CarEditor.ComboBox1.Items.Count - 1
            F.CarEditor.ComboBox1.SelectedIndex = i
            Pump(500)
            Shot(F.CarEditor, "editor_" & i.ToString("00") & "_" & F.CarEditor.ComboBox1.Items(i).ToString().Replace(".", ""))
        Next
        F.CarEditor.ComboBox1.SelectedIndex = 0
        Pump(200)

        Dim path = cars(0).Path & "\parameters.txt"
        Dim original = IO.File.ReadAllBytes(path)

        'A value through the real page, then the real save button
        F.CarEditor.ComboBox1.SelectedIndex = 2 'handling
        Pump(300)
        F.Handling.NumericUpDown2.Value = 77.5D
        Pump(100)
        Check(cars(0).Theory.RealInfos.TopSpeed = 77.5, "handling page writes the car")
        Check(cars(0).Sing.Modified, "car is modified")
        F.CarEditor.SaveCar()
        Dim saved = IO.File.ReadAllText(path, Singletons.FileEncoding)
        Check(New Singletons(saved, True).getSingleton("").Num("TopSpeed") = 77.5, "saved in the file")
        Check(IO.File.ReadAllBytes(path & ".bak").SequenceEqual(original), "backup made")
        Check(Not cars(0).Sing.Modified, "saved car isn't modified")

        'Extra pages: change, the value arrives
        F.CarEditor.ComboBox1.SelectedIndex = 8 'aerial
        Pump(300)
        Dim before = Models.Count
        cars(0).Theory.Aerial.length = 30
        F.CarBrowser.ReLoadOneCar()
        Pump(300)
        Shot(F.CarEditor, "aerial_long")

        'Pro editor
        F.CarEditor.Button1.PerformClick()
        F.Parameters_editor.Activate()
        Pump(1000)
        Shot(F.Parameters_editor, "pro_editor")
        Dim txt = F.Parameters_editor.TextBox1.Text
        Check(txt.Contains("TopSpeed"), "pro editor shows the file")
        F.Parameters_editor.Activate()
        F.Parameters_editor.TextBox1.Text = System.Text.RegularExpressions.Regex.Replace(txt, "(?m)^(TopSpeed\s+)\S+", "${1}88.000000")
        Pump(2500)
        Check(cars(0).Theory.RealInfos.TopSpeed = 88, "pro editor text reaches the car: " & cars(0).Theory.RealInfos.TopSpeed)
        F.Parameters_editor.Hide()
        Pump(200)
        Log("models after pro edit: " & Models.Count)

        'Check my car
        Check_My_Car.CheckMyCar()
        Pump(300)
        Shot(F.TextViewer, "check_report")
        F.TextViewer.Hide()

        'shade changes colours, leaving forgets it
        F.CarEditor.ComboBox1.SelectedIndex = 13
        Pump(300)
        Dim body = cars(0).models.BODY
        Dim c0 = body.MyModel.polyl(0).c(0).ReturnLong()
        F.shade.NumericUpDown4.Value = 0.3D
        Pump(300)
        Shot(F.CarEditor, "shade_dark")
        F.CarEditor.ComboBox1.SelectedIndex = 0
        Pump(200)
        Check(cars(0).models.BODY.MyModel.polyl(0).c(0).ReturnLong() = c0, "shade undone when not saved")

        'texture slider, then save
        F.CarEditor.ComboBox1.SelectedIndex = 1
        Pump(400)
        F.Texture_.TrackBar1.Value = 60
        Pump(2000)
        Shot(F.CarEditor, "texture_bright")
        F.Texture_.TrackBar1.Value = 0
        Pump(1000)

        ClickEverything("strict")

        'back to the list, and into another car
        F.CarEditor.Timer2.Stop()
        cars(0).Sing.Rebase(cars(0).Theory)
        F.CarEditor.BackToBrowser()
        Pump(400)
        Check(F.CarBrowser.Visible, "back in the browser")
        SelectCar("rc")
        Pump(400)
        Shot(F.CarBrowser, "browser_back")
        F.CarBrowser.Pick_Click(Nothing, Nothing)
        Pump(500)
        Shot(F.CarEditor, "editor_second_car")
        Check(F.CarEditor.Label4.Text <> "", "second car in the editor")

        IO.File.WriteAllBytes(cars(0).Path & "\..\" & folder & "\parameters.txt", original)
        If IO.File.Exists(path & ".bak") Then IO.File.Delete(path & ".bak")
    End Sub

    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function GetClassName(ByVal hWnd As IntPtr, ByVal name As Text.StringBuilder, ByVal size As Integer) As Integer
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function EnumThreadWindows(ByVal thread As Integer, ByVal cb As EnumProc, ByVal param As IntPtr) As Boolean
    End Function
    <Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Function GetCurrentThreadId() As Integer
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function PostMessage(ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal w As IntPtr, ByVal l As IntPtr) As Boolean
    End Function
    Private Delegate Function EnumProc(ByVal hWnd As IntPtr, ByVal param As IntPtr) As Boolean

    'Closes whatever dialog a button opened
    Private Sub DismissDialogs(ByVal sender As Object, ByVal e As EventArgs)
        EnumThreadWindows(GetCurrentThreadId(), Function(h, p)
                                                    Dim sb As New Text.StringBuilder(64)
                                                    GetClassName(h, sb, 64)
                                                    If sb.ToString() = "#32770" Then PostMessage(h, &H10, IntPtr.Zero, IntPtr.Zero)
                                                    Return True
                                                End Function, IntPtr.Zero)
        For Each f2 As Form In Application.OpenForms.Cast(Of Form)().ToList()
            If f2.Modal Then f2.Close()
        Next
    End Sub

    Private Sub AllButtons(ByVal c As Control, ByVal list As List(Of Button))
        For Each child As Control In c.Controls
            If TypeOf child Is Button Then list.Add(DirectCast(child, Button))
            AllButtons(child, list)
        Next
    End Sub

    'Every button of every page, nothing may crash
    Sub ClickEverything(ByVal tag As String)
        Dim onClick = GetType(Control).GetMethod("OnClick", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
        Dim guard As New Timer With {.Interval = 300}
        AddHandler guard.Tick, AddressOf DismissDialogs
        guard.Start()

        Dim total = 0
        For mode = 0 To F.CarEditor.ComboBox1.Items.Count - 1
            F.CarEditor.ComboBox1.SelectedIndex = mode
            Pump(200)
            Dim list As New List(Of Button)
            AllButtons(F.CarEditor.Panel3, list)
            For Each b In list
                If b.Name = "Button4" AndAlso mode = 0 Then Continue For 'opens an Explorer window
                If b.Text = "other car" Then Continue For
                total += 1
                Try
                    onClick.Invoke(b, New Object() {EventArgs.Empty})
                    Pump(60)
                Catch ex As Exception
                    Dim inner = If(ex.InnerException, ex)
                    Check(False, tag & " mode " & mode & " " & F.CarEditor.ComboBox1.Items(mode).ToString() & " button " & b.Name & " '" & b.Text & "': " & inner.GetType().Name & " " & inner.Message & " @ " & inner.StackTrace.Split(vbLf)(0).Trim())
                End Try
                'things buttons leave open
                For Each f2 As Form In Application.OpenForms.Cast(Of Form)().ToList()
                    If f2 IsNot F.CarEditor AndAlso f2 IsNot F.SplashScreen AndAlso f2 IsNot F.CarBrowser AndAlso f2.Visible Then f2.Hide()
                Next
                PickingPRM = False
                PickingBMP = False
            Next
        Next
        guard.Stop()
        Log(tag & ": clicked " & total & " buttons")
        F.CarEditor.ComboBox1.SelectedIndex = 0
        Pump(200)
    End Sub
End Module
