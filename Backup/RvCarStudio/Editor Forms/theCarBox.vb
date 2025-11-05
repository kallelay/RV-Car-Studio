Public Class TheCarBox

    Private Sub Panel3_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel3.Paint

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        'PICK_BMP.VariateTexture = False
        PICK_BMP.Location = New Point(-CarEditor.Panel9.Location.X, CarEditor.Panel9.Location.Y) + CarEditor.Panel3.Location + CarEditor.Location - New Point(CarEditor.Panel3.Width, 0)
        PICK_BMP.Config.Filler = Button1
        PICK_BMP.Show()
        PICK_BMP.DoLoad(False)
    End Sub
    Dim loading As Boolean = False
    Sub DOLOAD()
        Loading = True

        Button1.Text = cars(Active_Car).Theory.MainInfos.TCarBox

        MCheckbox1.Checked = True
        MCheckbox2.Checked = True


        CarEditor.hider1.Show()
        CarEditor.hider0.Show()


        Loading = False

        pv_carbox.Show()

    End Sub
    Sub UnLoad()
        CarEditor.hider0.Hide()
        CarEditor.hider1.Hide()

        MCheckbox1.Checked = False
        MCheckbox2.Checked = False

        pv_carbox.Hide()
    End Sub
    Public tBMP As Integer(,)

    Private Sub Button1_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Button1.TextChanged
        If Not Initializd Then Exit Sub
        If Button1.Text = "pick" Then Exit Sub

  
        cars(Active_Car).Theory.MainInfos.TCarBox = Button1.Text
        Try
            If Button1.Text = "NONE" Then Exit Sub
            If Not IO.File.Exists(RVPATH & "\" & Button1.Text) Then Exit Sub
            Dim x As New IO.FileStream(RVPATH & "\" & Button1.Text, IO.FileMode.Open, IO.FileAccess.Read)
            pv_carbox.PictureBox1.Image = Image.FromStream(x)
            x.Close()
            x = Nothing
        Catch ex As Exception

        End Try



        '   CarBrowser.ReLoadOneCar()


    End Sub


    Private Sub MCheckbox1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub


    Private Sub TheCarBox_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub
    Dim ChSp = 1.1!
    Public R!
    Public G!
    Public B!
    Dim SizeF = 40.0!
    Dim QFconfig As QuickFont.QFontBuilderConfiguration
    Dim def_font As Font = New Font("Impact", SizeF, FontStyle.Regular, GraphicsUnit.Pixel)
    Private Sub MCheckbox1_CheckedChanged() Handles MCheckbox1.CheckedChanged
        If MCheckbox1.Checked Then
            Randomize()
            R = Rnd()
            G = Rnd()
            B = Rnd()

            'QFconfig.ShadowConfig = New QuickFont.QFontShadowConfiguration
            'QFconfig.ShadowConfig.blurPasses = 1
            'QFconfig.ShadowConfig.blurRadius = 0.5
            ' QFconfig.TextGenerationRenderHint = QuickFont.TextGenerationRenderHint.AntiAlias
            QFconfig = New QuickFont.QFontBuilderConfiguration

            def_font = New Font(def_font.Name, HScrollBar2.Value, def_font.Style, GraphicsUnit.Pixel)
            CarEditor.qfont = New QuickFont.QFont(def_font, QFconfig)


            CarEditor.qfont.Options.Colour = Color.FromArgb(255, R * 255, G * 255, B * 255)
            CarEditor.qfont.Options.Monospacing = QuickFont.QFontMonospacing.Yes
            CarEditor.qfont.Options.CharacterSpacing = (HScrollBar1.Value - 500) / 100
            CarEditor.qfont.Options.UseDefaultBlendFunction = True


            CarEditor.RENDER_FONT = True
        Else

            CarEditor.RENDER_FONT = False
        End If
    End Sub


    Private Sub MCheckbox1_Load_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MCheckbox1.Load

    End Sub

    Private Sub MCheckbox2_CheckedChanged() Handles MCheckbox2.CheckedChanged
        If MCheckbox2.Checked Then
            OpenTK.Graphics.OpenGL.GL.ClearColor(255, 255, 255, 255)
        Else
            OpenTK.Graphics.OpenGL.GL.ClearColor(Color.AliceBlue)
        End If
    End Sub

    Private Sub MCheckbox2_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MCheckbox2.Load

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Randomize()
        R = Rnd()
        G = Rnd()
        B = Rnd()

    End Sub

    Private Sub HScrollBar1_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar1.Scroll ', HScrollBar3.Scroll

        If CarEditor.qfont Is Nothing Then Exit Sub
        If CarEditor.qfont.Options Is Nothing Then Exit Sub
        CarEditor.qfont.Options.CharacterSpacing = (HScrollBar1.Value - 500) / 100
    End Sub

    Private Sub HScrollBar2_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar2.Scroll

        SizeF = HScrollBar2.Value

        CarEditor.qfont.Dispose()
        def_font = New Font(def_font.Name, HScrollBar2.Value, def_font.Style, GraphicsUnit.Pixel)
        CarEditor.qfont = New QuickFont.QFont(def_font, QFconfig)


        CarEditor.qfont.Options.Colour = Color.FromArgb(255, R * 255, G * 255, B * 255)
        CarEditor.qfont.Options.Monospacing = QuickFont.QFontMonospacing.Yes
        CarEditor.qfont.Options.CharacterSpacing = (HScrollBar1.Value - 500) / 100
        CarEditor.qfont.Options.UseDefaultBlendFunction = True

    End Sub

    Public CountTogenerate = 5
    Public Transparency_factor = 1.0F

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        CarEditor.pause.Show()
        CarEditor.Timer2.Stop()
        If FontDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            def_font = FontDialog1.Font
        End If

        CarEditor.qfont.Dispose()
        def_font = New Font(def_font.Name, HScrollBar2.Value, def_font.Style, GraphicsUnit.Pixel)
        CarEditor.qfont = New QuickFont.QFont(def_font, QFconfig)


        CarEditor.pause.Hide()
        CarEditor.Timer2.Start()
    End Sub
    Public ScreenShot As Boolean = False
  
    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        RaiseFlag(Screenshot)

    End Sub
    Public VSpacer! = 50
    Private Sub HScrollBar3_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar3.Scroll
        VSpacer = HScrollBar3.Value
    End Sub

    Private Sub HScrollBar4_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar4.Scroll
        Transparency_factor = HScrollBar4.Value / 20
    End Sub

    Private Sub HScrollBar5_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar5.Scroll
        CountTogenerate = HScrollBar5.Value
    End Sub
    Public FirstAlpha = 255, SecondAlpha = 128 ' As Integer
    Private Sub HScrollBar7_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar7.Scroll
        FirstAlpha = HScrollBar7.Value
    End Sub

    Private Sub HScrollBar6_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar6.Scroll
        SecondAlpha = HScrollBar6.Value
    End Sub
    Public LineWrap = True
    Private Sub MCheckbox3_CheckedChanged() Handles MCheckbox3.CheckedChanged
        LineWrap = MCheckbox3.Checked
    End Sub

    Private Sub MCheckbox3_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MCheckbox3.Click

    End Sub

    Private Sub MCheckbox3_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MCheckbox3.Load

    End Sub
End Class