Imports OpenTK.Graphics.OpenGL
Imports OpenTK.Graphics

Public Class _main

    Dim LoadingMain As Boolean = False
    Public Sub DoLoad()



        If Not Initializd Then Exit Sub
        TextBox1.Text = cars(Active_Car).Theory.MainInfos.Name
        LoadingMain = True
        TextBox2.Text = cars(Active_Car).DirName
        lastfine = TextBox2.Text
        CheckBox1.Checked = cars(Active_Car).Theory.RealInfos.ClothFX
        CheckBox5.Checked = cars(Active_Car).Theory.RealInfos.Flying
        CheckBox6.Checked = cars(Active_Car).Theory.RealInfos.Flippable
        LoadingMain = False
        Panel10.BackColor = Color.FromArgb(cars(Active_Car).Theory.MainInfos.EnvRGB.R * 255, cars(Active_Car).Theory.MainInfos.EnvRGB.G * 255, cars(Active_Car).Theory.MainInfos.EnvRGB.B * 255)

        Label1.Text = "(" & Panel10.BackColor.R & ", " & Panel10.BackColor.G & ", " & Panel10.BackColor.B & ")"
    End Sub

    Private Sub TextBox1_MouseHover(ByVal sender As Object, ByVal e As System.EventArgs) Handles TextBox1.MouseHover
        RequestTip("name")
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged
        If cars.Count = 0 Then Exit Sub
        cars(Active_Car).Theory.MainInfos.Name = TextBox1.Text

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        YESNO.Fill("Warning", "If you reload the car, the changes will be discarded, continue?")
        If YESNO.ShowDialog = Windows.Forms.DialogResult.No Then Exit Sub
        CarBrowser.ReLoadOneCar(False, True)
        CarEditor.ComboBox1_SelectedIndexChanged(sender, e)

    End Sub

    Private Sub Panel9_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel9.Paint

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Dim file = Car.GamePath(cars(Active_Car).Theory.MainInfos.Tpage)
        If file Is Nothing Then Beep() : Exit Sub
        Dim X As New Form
        X.BackgroundImage = Image.FromStream(New IO.MemoryStream(IO.File.ReadAllBytes(file)))
        X.ClientSize = X.BackgroundImage.Size
        X.StartPosition = FormStartPosition.CenterScreen
        X.BackgroundImageLayout = ImageLayout.Stretch
        X.Text = "Preview Bitmap"
        X.TopMost = True

        X.Show()

    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        If cars(Active_Car).Path = "" Then Exit Sub
        Shell("explorer.exe " & Chr(34) & Replace(cars(Active_Car).Path, "\\", "\") & Chr(34), AppWinStyle.NormalFocus)
    End Sub

    Private Sub Button12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button12.Click
        FORCE_DO_NOT_RENDER = True
        CarEditor.Timer2.Stop()


        Me.ColorDialog1.AllowFullOpen = True
        Me.ColorDialog1.FullOpen = True
        Me.ColorDialog1.Color = Panel10.BackColor
        If Me.ColorDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            Panel10.BackColor = Me.ColorDialog1.Color
            cars(Active_Car).Theory.MainInfos.EnvRGB = New Color4(Me.ColorDialog1.Color.R / 255.0F, Me.ColorDialog1.Color.G / 255.0F, Me.ColorDialog1.Color.B / 255.0F, 1.0F)
            Label1.Text = "(" & Panel10.BackColor.R & ", " & Panel10.BackColor.G & ", " & Panel10.BackColor.B & ")"
        End If
        FORCE_DO_NOT_RENDER = False
        CarEditor.Timer2.Start()

    End Sub

    Private Sub _main_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Dim m As New mCheckbox
        Me.Controls.Add(m)
        'A small joke
        If Int(Rnd() * 5) = 1 Then
            CheckBox6.Text = "Flip!!!"
            Dim a As New Timers.Timer()
            a.Interval = 20000
            AddHandler a.Elapsed, AddressOf EndJoke
            a.Start()


        End If

    End Sub

    'End joke
    Sub EndJoke()
        CheckBox6.Text = "Flippable"
    End Sub
    Private Sub CheckBox3_CheckedChanged() Handles CheckBox3.CheckedChanged
        LIGHTS_ON = CheckBox3.Checked
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        CarBrowser.ReLoadOneCar(Not CheckBox4.Checked, True)
    End Sub

    Private Sub CheckBox2_CheckedChanged() Handles CheckBox2.CheckedChanged
        Timer1.Enabled = CheckBox2.Checked
    End Sub

    Private Sub GroupBox2_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GroupBox2.Enter

    End Sub

    Private Sub GroupBox2_MouseHover(ByVal sender As Object, ByVal e As System.EventArgs) Handles GroupBox2.MouseMove
        RequestTip("envcolor")
    End Sub

    Private Sub Checkbox_CheckedChanged() Handles CheckBox1.CheckedChanged, CheckBox5.CheckedChanged, CheckBox6.CheckedChanged
        If Not Initializd Or LoadingMain Or cars.Count = 0 Then Exit Sub
        cars(Active_Car).Theory.RealInfos.ClothFX = CheckBox1.Checked
        cars(Active_Car).Theory.RealInfos.Flying = CheckBox5.Checked
        cars(Active_Car).Theory.RealInfos.Flippable = CheckBox6.Checked
    End Sub
    Private Sub CheckBox5_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles CheckBox5.MouseMove
        RequestTip("flying")
    End Sub
    Private Sub CheckBox6_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles CheckBox6.MouseMove
        RequestTip("flippable")
    End Sub
    Private Sub CheckBox1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles CheckBox1.MouseMove
        RequestTip("clothfx")
    End Sub

    Private Sub GroupBox2_MouseHover(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles GroupBox2.MouseMove

    End Sub

    'Run the game
    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Dim exe = ""
        For Each candidate In New String() {"rvgl.exe", "revolt.exe"}
            If IO.File.Exists(RVPATH & "\" & candidate) Then exe = RVPATH & "\" & candidate : Exit For
        Next
        If exe = "" Then
            MsgBox("No rvgl.exe or revolt.exe in " & RVPATH, MsgBoxStyle.Exclamation, APPNAME)
            Exit Sub
        End If
        CarEditor.SaveCar()
        Dim info As New ProcessStartInfo(exe, "-window -nointro -sload" & If(MCheckbox1.Checked, " -dev", ""))
        info.WorkingDirectory = RVPATH
        Process.Start(info)
    End Sub

    '------------------------ Folder of the car ----------------------------'
    Dim lastfine$ = ""

    Public Shared Function ValidFolderName(ByVal name As String) As Boolean
        If name.Trim() = "" Then Return False
        Return name.IndexOfAny(IO.Path.GetInvalidFileNameChars()) < 0 AndAlso name.IndexOf(";"c) < 0
    End Function

    'Only colours here: the folder is renamed when you leave the box
    Private Sub TextBox2_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox2.TextChanged
        If LoadingMain Then Exit Sub
        Dim same = TextBox2.Text.Equals(lastfine, StringComparison.OrdinalIgnoreCase)
        Dim fine = same OrElse (ValidFolderName(TextBox2.Text) AndAlso Not IO.Directory.Exists(RVPATH & "\cars\" & TextBox2.Text))
        Label5.ForeColor = If(fine, Color.Gray, Color.DarkRed)
        TextBox2.ForeColor = If(fine, Color.White, Color.Red)
    End Sub

    Private Sub TextBox2_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles TextBox2.KeyDown
        If e.KeyCode = Keys.Enter Then RenameFolder()
    End Sub

    Private Sub TextBox2_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles TextBox2.LostFocus
        RenameFolder()
    End Sub

    Sub RenameFolder()
        If LoadingMain Or cars.Count = 0 Then Exit Sub
        If lastfine = "" Or TextBox2.Text = lastfine Then Exit Sub
        Try
            If Not ValidFolderName(TextBox2.Text) OrElse IO.Directory.Exists(RVPATH & "\cars\" & TextBox2.Text) Then Throw New IO.IOException("bad or used name")
            RenameCarFolder(cars(Active_Car), TextBox2.Text)
            CarBrowser.LoadAllCarsIntoList()
            lastfine = TextBox2.Text
        Catch ex As Exception
            Console_.W("Error renaming " & cars(Active_Car).DirName & ": " & ex.Message)
            MsgBox("Can't rename the folder: " & ex.Message, MsgBoxStyle.Exclamation, APPNAME)
            LoadingMain = True
            TextBox2.Text = lastfine
            LoadingMain = False
        End Try
        Label5.ForeColor = Color.Gray
        TextBox2.ForeColor = Color.White
    End Sub

    ''' <summary>Rename the folder of a car and the paths in its parameters</summary>
    Public Shared Sub RenameCarFolder(ByVal c As Car, ByVal newName As String)
        Dim oldPath = c.Path
        Dim newPath = RVPATH & "\cars\" & newName
        If c.Sing.Modified Then c.Sing.SaveToFile()
        IO.Directory.Move(oldPath, newPath)
        RepointCar(c, newName)
        c.Path = newPath
        c.Sing.SaveToFile()
    End Sub

    ''' <summary>Paths in the parameters: from the old folder of the car to the new one</summary>
    Public Shared Sub RepointCar(ByVal c As Car, ByVal newName As String)
        Dim oldPrefix = "cars\" & c.DirName & "\"
        Dim fix = Function(p As String) If(p IsNot Nothing AndAlso p.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase), "cars\" & newName & "\" & p.Substring(oldPrefix.Length), p)
        With c.Theory.MainInfos
            For i = 0 To .Model.Length - 1
                .Model(i) = fix(.Model(i))
            Next
            .Tpage = fix(.Tpage)
            .CollFile = fix(.CollFile)
            .TCarBox = fix(.TCarBox)
            .TShadow = fix(.TShadow)
            .SfxEngine = fix(.SfxEngine)
            .SfxHonk = fix(.SfxHonk)
            .SfxServo = fix(.SfxServo)
        End With
        c.DirName = newName
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Check_My_Car.CheckMyCar()
    End Sub

    Private Sub CheckBox5_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub MCheckbox1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub CheckBox2_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox2.Load

    End Sub

   
    Private Sub Checkbox_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox6.CheckedChanged, CheckBox5.CheckedChanged, CheckBox1.CheckedChanged

    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub

    Private Sub Panel10_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel10.Paint

    End Sub
End Class