Imports OpenTK.Graphics.OpenGL
Imports OpenTK.Graphics

Public Class _main

    Public Sub DoLoad()



        If Not Initializd Then Exit Sub
        TextBox1.Text = cars(Active_Car).Theory.MainInfos.Name
        TextBox2.Text = cars(Active_Car).DirName
        CheckBox1.Checked = cars(Active_Car).Theory.RealInfos.ClothFX
        CheckBox2.Checked = cars(Active_Car).Theory.RealInfos.Flying
        CheckBox3.Checked = cars(Active_Car).Theory.RealInfos.Flippable
        Panel10.BackColor = Color.FromArgb(cars(Active_Car).Theory.MainInfos.EnvRGB.R * 255, cars(Active_Car).Theory.MainInfos.EnvRGB.G * 255, cars(Active_Car).Theory.MainInfos.EnvRGB.B * 255)

        Label1.Text = "(" & Panel10.BackColor.R & ", " & Panel10.BackColor.G & ", " & Panel10.BackColor.B & ")"
    End Sub

    Private Sub TextBox1_MouseHover(ByVal sender As Object, ByVal e As System.EventArgs) Handles TextBox1.MouseHover
        RequestTip("name")
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged
        cars(Active_Car).Theory.MainInfos.Name = TextBox1.Text

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        YESNO.Fill("Warning", "If you reload the car, the changes will be discarded, continue?")
        If YESNO.ShowDialog = Windows.Forms.DialogResult.No Then Exit Sub
        CarBrowser.ReLoadOneCar(, True)

    End Sub

    Private Sub Panel9_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel9.Paint

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If cars(Active_Car).Theory.MainInfos.Tpage = "" Then Beep() : Exit Sub
        Dim X As New Form
        X.Size = New Size(256, 256)
        X.StartPosition = FormStartPosition.CenterScreen

        X.BackgroundImage = Image.FromFile(RVPATH & "\" & Replace(Replace(Replace(cars(Active_Car).Theory.MainInfos.Tpage, "'", ""), ",", "."), Chr(9), ""))
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
        If CheckBox3.Checked Then
            GL.Enable(EnableCap.Lighting)
            GL.Enable(EnableCap.Light1)
            GL.Enable(EnableCap.Light2)
        Else
            GL.Disable(EnableCap.Lighting)
            GL.Disable(EnableCap.Light1)
            GL.Enable(EnableCap.Light2)
        End If


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
        If Not Initializd Then Exit Sub
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

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Dim p = CurDir()
        ChDir(RVPATH)
        If MCheckbox1.Checked Then

            Shell("revolt -window -sli -nointro -sload -useallcpu -dev", AppWinStyle.NormalFocus)
        Else

            Shell("revolt -window -sli -nointro -sload -useallcpu ", AppWinStyle.NormalFocus)
        End If
        ChDir(p)



    End Sub
    Dim lastfine$ = ""
    Dim CycledOnce = False

    Private Sub TextBox2_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles TextBox2.LostFocus
        TextBox2.Text = lastfine
        Label5.ForeColor = Color.Gray
        TextBox2.ForeColor = Color.White

    End Sub
    Private Sub TextBox2_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox2.TextChanged
        Label5.ForeColor = Color.DarkRed
        TextBox2.ForeColor = Color.Red

        If Not CycledOnce Then
            Label5.ForeColor = Color.Gray
            TextBox2.ForeColor = Color.White
            CycledOnce = True

        End If

        If TextBox2.Text = "" Then Exit Sub
        If cars(Active_Car).DirName = TextBox2.Text Then Exit Sub
        If InStr(TextBox2.Text, "*") + InStr(TextBox2.Text, "/") + InStr(TextBox2.Text, "\") + InStr(TextBox2.Text, ":") + _
         InStr(TextBox2.Text, "?") + InStr(TextBox2.Text, """") + InStr(TextBox2.Text, "<") + InStr(TextBox2.Text, ">") + _
          InStr(TextBox2.Text, "|") + InStr(TextBox2.Text, ";") > 0 Then Exit Sub

        Label5.ForeColor = Color.Gray
        TextBox2.ForeColor = Color.Black



        If IO.Directory.Exists(RVPATH & "\cars\" & TextBox2.Text) Then
            Label5.ForeColor = Color.DarkRed
            TextBox2.ForeColor = Color.Red
            Exit Sub
        Else
            Label5.ForeColor = Color.Gray
            TextBox2.ForeColor = Color.Black
        End If

        Try
            My.Computer.FileSystem.RenameDirectory( _
            RVPATH & "\cars\" & cars(Active_Car).DirName, _
           TextBox2.Text)


            For i = 0 To 19

                cars(Active_Car).Theory.MainInfos.Model(i) = Replace(cars(Active_Car).Theory.MainInfos.Model(i), _
                                                                     "cars\" & cars(Active_Car).DirName, "cars\" & TextBox2.Text, , , CompareMethod.Text)

            Next
            cars(Active_Car).DirName = TextBox2.Text
            cars(Active_Car).Path = RVPATH & "\cars\" & TextBox2.Text

            cars(Active_Car).Sing.SaveToFile()

            lastfine = TextBox2.Text
        Catch ex As Exception
            Console_.W("Error renaming! " & cars(Active_Car).OriginalTheory.MainInfos.Name)
            Label5.ForeColor = Color.DarkRed
            TextBox2.ForeColor = Color.Red
        End Try
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