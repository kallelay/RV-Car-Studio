Imports OpenTK.Graphics.OpenGL
Imports OpenTK.Graphics
Imports OpenTK
Imports System.Math
Imports CarStudio.CarBrowser


Public Class CarEditor
    Dim ang! = PI / 4
    Dim TICK% = Environment.TickCount
    Dim cnt = 0
    Dim Swatch As New Stopwatch


'
    Public qfont As QuickFont.QFont '(New Font("Impact", 40, FontStyle.Regular, GraphicsUnit.Pixel), New QuickFont.QFontBuilderConfiguration)
    Public RENDER_FONT = False


    Private Sub Timer2_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer2.Tick
        'timer2: the responsible of rendering time

        'if it's not a sperate thread, make sure that threads are separated!
        If Not DO_NOT_THREAD Then If GlControl1.Context.IsCurrent And Not DO_NOT_THREAD Then GlControl1.Context.MakeCurrent(Nothing)

        'if Thread didn't start, start it and start rendering
        'if Thread isn't allowed to be used then render normally!
        If Not TH_STARTED And Not DO_NOT_THREAD Then BackgroundWorker1.RunWorkerAsync() : TH_STARTED = True Else If Not FORCE_DO_NOT_RENDER Then DoRender()

        'get car name + FPS
        If cnt = 0 Then Swatch.Reset() : Swatch.Start() : TICK% = Swatch.ElapsedTicks : Label4.Text = cars(Active_Car).Theory.MainInfos.Name


        'counter
        cnt += 1
        If cnt > 58 Then
            FORCE_DO_NOT_RENDER = True
            Label5.Text = "FPS: " & fps
            Label9.Text = "LAG: " & lag
            Label9.ForeColor = Color.FromArgb(192, Min(Max(lag * 255.0!, 255.0), 0), Max(Min(1 - lag * 1.0F, 0), 1) * 255, 128)
            cnt = 0
            FORCE_DO_NOT_RENDER = False
        End If



        If CarIsLoading = True Then Exit Sub 'if car is loading then exit sub 
        StartedRendering = True
        ' If DO_NOT_THREAD Then
        'DO_NOT_RENDER = False
        '  DoRender()
        '  Label5.Text &= "[T]"

        '  End If



    End Sub
    Sub DoRender()

        'don't render twice & don't render if forced not to render

        If FORCE_DO_NOT_RENDER Then
            DO_NOT_RENDER_ACCEPETED = True
            ' pause.Show()
            Exit Sub

        Else
            DO_NOT_RENDER_ACCEPETED = False
            ' pause.Hide()
        End If


        If DO_NOT_RENDER Then Exit Sub

        If DO_NOT_RENDER Or FORCE_DO_NOT_RENDER Then Exit Sub
        ' If FORCE_DO_NOT_RENDER Then DO_NOT_RENDER_ACCEPETED = True : DoRender()


        'ok, render what again?
        If GlControl1 Is Nothing Then Exit Sub



        'locked rendering, calculating fps,lag
        DO_NOT_RENDER = True
        fps = Int(60 * 1000 / (Swatch.ElapsedMilliseconds))
        lag = Strings.Format(1 - (937.5 / (Swatch.ElapsedMilliseconds)), "0.00")

        If Not DO_NOT_THREAD Then
            If GlControl1.Context.IsCurrent Then GlControl1.Context.MakeCurrent(Nothing)
            GlControl1.MakeCurrent()
        End If
        ' ' Catch ex As Exception
        ' DO_NOT_RENDER = False
        '  End Try

        'clear
        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit)
        '    If (Not FinishedRendering) and sinceLastFrame < 2  Then Exit Sub
        GL.MatrixMode(MatrixMode.Modelview)
        GL.LoadIdentity()


        'if fps > 10 we can stop to do events
        If fps > 10 Then Application.DoEvents()



        'lights (if activated)
        GL.Light(LightName.Light1, LightParameter.Specular, cars(Active_Car).Theory.MainInfos.EnvRGB)
        GL.Light(LightName.Light1, LightParameter.Ambient, Color4.White)
        GL.Light(LightName.Light1, LightParameter.Diffuse, cars(Active_Car).Theory.MainInfos.EnvRGB)
        GL.Light(LightName.Light1, LightParameter.Position, New Vector4(-1, -1, 1, 0))

        GL.Light(LightName.Light2, LightParameter.Specular, cars(Active_Car).Theory.MainInfos.EnvRGB)
        GL.Light(LightName.Light2, LightParameter.Ambient, Color4.White)
        GL.Light(LightName.Light2, LightParameter.Diffuse, cars(Active_Car).Theory.MainInfos.EnvRGB)
        GL.Light(LightName.Light2, LightParameter.Position, New Vector4(1, -1, -1, 0))

        'if problem, then reinit
        If Single.IsNaN(GlobalPosition.X) Then GlobalPosition.X = -0.85
        If Single.IsNaN(GlobalPosition.Y) Then GlobalPosition.Y = 2
        If Single.IsNaN(GlobalPosition.Z) Then GlobalPosition.Z = 0.25


        'camera position
        Dim Eye = New Vector3(GlobalPosition.Length * Cos(PRM.Theta * PI / 180) * Sin(PRM.Phi * PI / 180), _
                              -GlobalPosition.LengthFast * Sin(PRM.Theta * PI / 180) * Sin(PRM.Phi * PI / 180), _
                              GlobalPosition.LengthFast * Cos(PRM.Phi * PI / 180))


        'Build project matrix + load
        GL.LoadMatrix(Matrix4.LookAt(Eye, New Vector3, New Vector3(0, 1, 0)))

        'get current theta 
        GL.Rotate(2 * PRM.Theta, New Vector3(1, 0, 0))

        'qfont.Options.Colour = New OpenTK.Graphics.Color4(Rnd() / 2, Rnd() / 2, Rnd() / 2, 1)






        If Not PickingPRM Then 'if we're not picking a car body
            For Each MODEL In PermaModels.Union(Models) 'for each models & permanent models

                'Extra
                'Fireworks
                If (MODEL.FileName.IndexOf("firework") <> -1) And MODEL.isVisible Then
                    ang += 0.05

                    If ang < PI / 2 Then
                        MODEL.MATRIX = Matrix4.CreateRotationX(ang)
                        MODEL.MATRIX *= Matrix4.CreateTranslation(cars(Active_Car).Theory.RealInfos.WeaponGeneration * Zoom + New Vector3(0, 0, ang * Zoom * 10))
                    Else
                        MODEL.MATRIX *= Matrix4.CreateTranslation(New Vector3(0, 0, ang * Zoom * 15))


                    End If

                    If ang > 3 * PI Then ang = PI / 4 : MODEL.isVisible = False

                End If




                'Render
                MODEL.Render()

            Next

        End If

        If PickingPRM Then
            If PICK_PRM.ThisPRM IsNot Nothing Then PICK_PRM.ThisPRM.Render() 'only render prm being picked
        End If






        '   GL.Rotate(-PRM.Theta, New Vector3(0, 1, 0))
        '  GL.Rotate(-PRM.Phi, New Vector3(1, 0, 0))
        '
        ' GL.Translate(-GlobalPosition)
        '
        ' GL.Rotate(-PRM.Theta, New Vector3(1, 0, 0))
        '   GL.Rotate(-PRM.Phi, New Vector3(0, 1, 0))
        ' GL.Flush()



        'Render font (car box making)
        If RENDER_FONT Then
            GL.LoadIdentity()




            GL.Translate(0, 0, -30)

            QuickFont.QFont.Begin()


            If Not TheCarBox.CheckBox1.Checked Then

                For i = 0 To TheCarBox.CountTogenerate - 1
                    qfont.Options.Colour = Color.FromArgb(Max(0, If(i = 0, TheCarBox.FirstAlpha, TheCarBox.SecondAlpha) - i * TheCarBox.Transparency_factor), TheCarBox.R * 255, TheCarBox.G * 255, TheCarBox.B * 255)
                    qfont.Print(UCase(cars(Active_Car).Theory.MainInfos.Name), If(TheCarBox.LineWrap, 540, 2000), QuickFont.QFontAlignment.Centre, New Vector2(270, TheCarBox.VSpacer / 4 + i * TheCarBox.VSpacer))
                Next


                '  qfont.Options.Colour = Color.FromArgb(128, TheCarBox.R * 255, TheCarBox.G * 255, TheCarBox.B * 255)


                'qfont.Options.Colour = Color.FromArgb(80, TheCarBox.R * 255, TheCarBox.G * 255, TheCarBox.B * 255)

            Else

                ' qfont.Options.Colour = Color.FromArgb(255, TheCarBox.R * 255, TheCarBox.G * 255, TheCarBox.B * 255)
                '  qfont.Print(cars(Active_Car).Theory.MainInfos.Name, 540, QuickFont.QFontAlignment.Centre, New Vector2(270, 400 - TheCarBox.VSpacer))

                For i = 0 To TheCarBox.CountTogenerate - 1
                    qfont.Options.Colour = Color.FromArgb(Max(0, If(i = 0, TheCarBox.FirstAlpha, TheCarBox.SecondAlpha) - i * TheCarBox.Transparency_factor), TheCarBox.R * 255, TheCarBox.G * 255, TheCarBox.B * 255)
                    qfont.Print(UCase(cars(Active_Car).Theory.MainInfos.Name), If(TheCarBox.LineWrap, 540, 2000), QuickFont.QFontAlignment.Centre, New Vector2(270, 360 - TheCarBox.VSpacer / 4 - i * TheCarBox.VSpacer))
                Next


            End If


            QuickFont.QFont.End()
            GL.Translate(0, 0, 30)
        End If

        'Process take screenshot
        If TheCarBox.ScreenShot Then
            CurSS += 1
            If CurSS = 0 Then RENDER_FONT = False Or Not TheCarBox.MCheckbox4.Checked
            If CurSS = 10 Or CurSS = 0 Then
                If IO.File.Exists(cars(Active_Car).Path & "\carbox_RVCS" & CurSS & ".bmp") Then _
                     My.Computer.FileSystem.DeleteFile(cars(Active_Car).Path & "\carbox_RVCS" & CurSS & ".bmp", FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
                GrabScreenshot().Save(cars(Active_Car).Path & "\carbox_RVCS" & CurSS & ".bmp")
                '  Dim b As New IO.FileStream(cars(Active_Car).Path & "\carbox_RVCS" & CurSS & ".bmp", IO.FileMode.Open, IO.FileAccess.Read)
                '  b.Close()
            End If

            If CurSS = 10 Then
                LowerFlag(TheCarBox.ScreenShot) : RENDER_FONT = True : CurSS = -1
                Dim str = IO.File.Open(cars(Active_Car).Path & "\carbox_RVCS" & 0 & ".bmp", IO.FileMode.Open)
                Dim str2 = IO.File.Open(cars(Active_Car).Path & "\carbox_RVCS" & 10 & ".bmp", IO.FileMode.Open)
                Dim BMP As New Bitmap(str)
                Dim BMP2 As New Bitmap(str2)

                Dim g = System.Drawing.Graphics.FromImage(BMP)
                BMP2.MakeTransparent(g.GetNearestColor(Color.White))

                g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBilinear
                g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                g.CompositingMode = Drawing2D.CompositingMode.SourceOver
                g.CompositingQuality = Drawing2D.CompositingQuality.HighQuality
                '   For i = 0 To 255
                '   For j = 0 To 255
                '  Dim pix = BMP2.GetPixel(i, j)
                '     If Math.Sqrt(pix.R ^ 2 + pix.G ^ 2 + pix.B ^ 2) < 433 Then
                '         BMP.SetPixel(i, j, pix)
                '        End If
                '    Next
                ' Next

                g.DrawImage(BMP2, New Point)



                If IO.File.Exists(cars(Active_Car).Path & "\carbox.bmp") Then _
               My.Computer.FileSystem.DeleteFile(cars(Active_Car).Path & "\carbox.bmp", FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)


                BMP.Save(cars(Active_Car).Path & "\carbox.bmp", Imaging.ImageFormat.Bmp)
                BMP.Dispose()
                BMP2.Dispose()

                cars(Active_Car).Theory.MainInfos.TCarBox = "cars\" & cars(Active_Car).DirName & "\carbox.bmp"
                TheCarBox.Button1.Text = cars(Active_Car).Theory.MainInfos.TCarBox
                str.Close()
                str = IO.File.Open(cars(Active_Car).Path & "\carbox.bmp", IO.FileMode.Open, IO.FileAccess.Read)
                pv_carbox.PictureBox1.Image = Image.FromStream(str)
                str2.Close()
                str.Close()



                Try

                    IO.File.Delete(cars(Active_Car).Path & "\carbox_RVCS" & 0 & ".bmp")
                Catch ex As Exception

                End Try
                Try
                    IO.File.Delete(cars(Active_Car).Path & "\carbox_RVCS" & 10 & ".bmp")
                Catch ex As Exception

                End Try

            End If
        End If


        GlControl1.SwapBuffers()
        If Not DO_NOT_THREAD Then GlControl1.Context.MakeCurrent(Nothing)
        DO_NOT_RENDER = False


        FinishedRendering = True
        StartedRendering = False
        '  Loop
    End Sub
    Dim CurSS = -1




    Private Sub CarEditor_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        ComboBox1.SelectedIndex = 0

        'Load pics
        Body_0.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Body.png")
        Wheel_1.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Wheel.png")
        Wheel_2.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Wheel.png")
        Wheel_3.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Wheel.png")
        Wheel_4.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Wheel.png")

        Pin1_0.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Pin1.png")
        Pin1_1.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Pin1.png")
        Pin2_0.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Pin2.png")
        Pin2_1.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\Pin2.png")

        Spring1_0.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\spring1.png")
        Spring1_1.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\spring1.png")
        Spring2_0.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\spring2.png")
        Spring2_1.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\spring2.png")

        Axle1_0.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\axle1.png")
        Axle1_1.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\axle1.png")
        Axle2_0.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\axle2.png")
        Axle2_1.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\axle2.png")

        Aerial_1.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\aerial.png")

        Prv.Location = GlControl1.Location
        Prv.Size = GlControl1.Size
        Prv.Hide()

        hider0.Hide()
        hider1.Hide()

        For Each x As Control In Panel3.Controls
            AddHandler x.GotFocus, AddressOf CarEditor_GotFocus
        Next


    End Sub

    Dim CurPos As Point
    Private Sub Label1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Panel2.MouseMove
        If e.Button = Windows.Forms.MouseButtons.Left Then
            Me.Location += MousePosition - CurPos
            CurPos = MousePosition
            '  If Panel2.BackgroundImage IsNot My.Resources.button3 Then Panel2.BackgroundImage = My.Resources.button3
        Else
            '  If Panel2.BackgroundImage IsNot My.Resources.button2 Then Panel2.BackgroundImage = My.Resources.button2
            CurPos = MousePosition
        End If
    End Sub



    Private Sub GlControl1_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles GlControl1.MouseDoubleClick
        'If paused then resume
        Timer2.Enabled = True
        pause.Hide()
    End Sub

    Private Sub GlControl1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles GlControl1.MouseMove
        'same as carbrowser thingy
        CarBrowser.GlControl1_MouseMove(sender, e)
    End Sub

    Private Sub GlControl1_MouseWheel(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles GlControl1.MouseWheel
        'same as carbrowser thingy
        CarBrowser.GlControl1_MouseWheel(sender, e)
    End Sub

    Private Sub GlControl1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles GlControl1.Resize
        'Viewport setup
        GL.Viewport(0, 0, GlControl1.ClientSize.Width, GlControl1.ClientSize.Height)

    End Sub

    Private Sub Panel7_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel7.Paint

    End Sub
    Public Saved = False
    Private Sub PictureBox1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PictureBox1.Click
        'TODO: saved?
        YESNO.Question.Text = "This will exit " & APPNAME & "." & vbNewLine & " Proceed?"
        If YESNO.ShowDialog = Windows.Forms.DialogResult.Yes Then
            End
        End If
    End Sub



    '----------------------Profile manager: what do you want to edit today--------------------------

    'unload everything
    Public Sub ReturnItToItsDefaultPlace()
        '0
        _main.Controls.Add(_main.Panel9)

        '1
        Texture_.Controls.Add(Texture_.Panel9)
        Texture_.Unload()


        '2
        Handling.Controls.Add(Handling.Panel9)
        Handling.Unload()

        '3 frontend
        Frontend.Controls.Add(Frontend.Panel9)
        Frontend.unload()

        '4
        _Body.Controls.Add(_Body.Panel9)
        _Body.UnLoad()

        '5 
        Wheels.Controls.Add(Wheels.Panel9)
        Wheels.unload()

        '6 
        Suspens_.Controls.Add(Suspens_.suspens)
        Suspens_.unload()

        '7
        Spinner_.Controls.Add(Spinner_.Panel9)
        Spinner_.unload()

        '12
        TheCarBox.Controls.Add(TheCarBox.Panel9)
        TheCarBox.UnLoad()

        '13
        shade.Controls.Add(shade.Panel9)
        shade.Unload()


        '18
        _About.Controls.Add(_About.Panel9)


        '19
        Console_.Controls.Add(Console_.Panel9)
    End Sub

    'if edit mode is set, load it!
    Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged
        ReturnItToItsDefaultPlace()




        Select Case ComboBox1.SelectedIndex
            Case 0 'MAIN
                Panel3.Controls.Add(_main.Panel9)
                _main.DoLoad()

            Case 1 'BMP
                Panel3.Controls.Add(Texture_.Panel9)
                Texture_.DoLoad()

            Case 2 'Handling
                Panel3.Controls.Add(Handling.Panel9)
                Handling.DOLOAD()

            Case 3 'Frontend
                Panel3.Controls.Add(Frontend.Panel9)
                Frontend.DoLoad()

            Case 4 'body
                Panel3.Controls.Add(_Body.Panel9)
                _Body.DOLOAD()

            Case 5 'wheels
                Panel3.Controls.Add(Wheels.Panel9)
                Wheels.DOLOAD()


            Case 6 'susp
                Panel3.Controls.Add(Suspens_.suspens)
                Suspens_.DoLoad()


            Case 7 'spinner
                Panel3.Controls.Add(Spinner_.Panel9)
                Spinner_.DoLoad()

            Case 12 'TCARBOX
                Panel3.Controls.Add(TheCarBox.Panel9)
                TheCarBox.DOLOAD()

            Case 13 'SHADE
                Panel3.Controls.Add(shade.Panel9)
                shade.DoLoad()

            Case 18 ' ABOUT
                Panel3.Controls.Add(_About.Panel9)
                _About.DoLoad()


            Case 19
                Panel3.Controls.Add(Console_.Panel9)
                'Console_.DoLoad()


        End Select
        'wireframes, vertex, solid, half trans etc
        ManageMeAll()

        Panel9.Focus()

    End Sub
    '0Main
    '1Texture
    '2Handling
    '3Frontend Stuffs
    '4Body
    '5heels
    '6Suspensions
    '7Spinner
    '8A'rial
    '9AI
    '10Camera
    '11Collision
    '12Carbox
    '13Shade
    '14Shadow
    '15Misc.
    '16Readme
    '17Packing
    '18About
    '19Console

    'render possiblities
    Enum RenderState
        Solid = 0
        Lines = 1
        Invisible = 2
        Points = 3
        Half_Trans = 4
        X = 5
    End Enum

    Dim WheelFR, WheelFL, WheelBR, WheelBL, _
        Body, AxleFR, AxleFL, AxleBR, AxleBL, _
        SpringFR, SpringFL, SpringBR, SpringBL, _
        PINFR, PINFL, PINBR, PINBL, _
        Spinner, _
        Aerial _
As RenderState


    'toggle between different render mode, again what's with this weird naming? -_-...
    Sub ManageMeAll()
        ManageMe(Body, cars(0).models.BODY, Body_0)
    End Sub
    Sub ManageMe(ByRef State As RenderState, ByRef Model As PRM, ByRef PicBox_Or_Panel As Object)
        'change rendering mode for models
        If Model Is Nothing Then Exit Sub
        Select Case State
            Case RenderState.Solid
                GL.Enable(EnableCap.CullFace)
                PicBox_Or_Panel.BorderStyle = BorderStyle.None
                Model.GlobalAlpha = 1
                Model.isVisible = True
                Model.RenderType = BeginMode.Triangles
                PicBox_Or_Panel.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\" & If(InStr(PicBox_Or_Panel.name, "_") = 0, PicBox_Or_Panel.Name, Split(PicBox_Or_Panel.Name, "_")(0)) & ".png")

            Case RenderState.Lines
                Model.isVisible = True
                Model.RenderType = BeginMode.LineStrip
                PicBox_Or_Panel.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\" & If(InStr(PicBox_Or_Panel.name, "_") = 0, PicBox_Or_Panel.Name, Split(PicBox_Or_Panel.Name, "_")(0)) & "_wire.png")

            Case RenderState.Points
                PicBox_Or_Panel.BorderStyle = BorderStyle.None
                PicBox_Or_Panel.BackgroundImage = Nothing
                Model.isVisible = True
                Model.RenderType = BeginMode.Points
                PicBox_Or_Panel.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\" & If(InStr(PicBox_Or_Panel.name, "_") = 0, PicBox_Or_Panel.Name, Split(PicBox_Or_Panel.Name, "_")(0)) & "_points.png")

            Case RenderState.Invisible
                Model.isVisible = False
                PicBox_Or_Panel.BackgroundImage = Nothing
                PicBox_Or_Panel.BorderStyle = BorderStyle.FixedSingle

            Case RenderState.Half_Trans
                PicBox_Or_Panel.BackgroundImage = Image.FromFile(Application.StartupPath & "\data\gfx\" & If(InStr(PicBox_Or_Panel.name, "_") = 0, PicBox_Or_Panel.Name, Split(PicBox_Or_Panel.Name, "_")(0)) & ".png")

                GL.Disable(EnableCap.CullFace)
                PicBox_Or_Panel.BorderStyle = BorderStyle.FixedSingle
                Model.RenderType = BeginMode.Triangles
                Model.GlobalAlpha = 0.5
                Model.isVisible = True
        End Select


    End Sub


    Sub ManageMePlusOne(ByRef State As RenderState, ByRef Model As PRM, ByRef PicBox_Or_Panel As Object)
        State += 1
        If State = 5 Then State = RenderState.Solid

        ManageMe(State, Model, PicBox_Or_Panel)
    End Sub
    Private Sub Panel14_Click(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Body_0.MouseClick
        ManageMePlusOne(Body, cars(Active_Car).models.BODY, sender)
    End Sub

    Private Sub Wheel_1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Wheel_1.Click
        ManageMePlusOne(WheelFL, cars(Active_Car).models.Wheel(1), Wheel_1)
        cars(Active_Car).models.Wheel(1).isVisible = cars(Active_Car).models.Wheel(1).isVisible And cars(Active_Car).Theory.wheel(1).IsPresent
    End Sub
    Private Sub Wheel_2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Wheel_2.Click
        ManageMePlusOne(WheelFR, cars(Active_Car).models.Wheel(0), Wheel_2)
        cars(Active_Car).models.Wheel(0).isVisible = cars(Active_Car).models.Wheel(0).isVisible And cars(Active_Car).Theory.wheel(0).IsPresent
    End Sub

    Private Sub Wheel_3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Wheel_3.Click
        ManageMePlusOne(WheelBL, cars(Active_Car).models.Wheel(3), Wheel_3)
        cars(Active_Car).models.Wheel(3).isVisible = cars(Active_Car).models.Wheel(3).isVisible And cars(Active_Car).Theory.wheel(3).IsPresent
    End Sub
    Private Sub Wheel_4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Wheel_4.Click
        ManageMePlusOne(WheelBR, cars(Active_Car).models.Wheel(2), Wheel_4)
        cars(Active_Car).models.Wheel(2).isVisible = cars(Active_Car).models.Wheel(2).isVisible And cars(Active_Car).Theory.wheel(2).IsPresent
    End Sub


    Dim lastused$

    '--------------------------------Tips & advices engine----------------------------------------
    Public Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick

        Dim mo = getRegion(ActiveTip)
        If mo Is Nothing Then
            Console_.W("Warning: (Tip) Region " & ActiveTip & " is not declared")
            Exit Sub
        End If
        If mo.Stats.Count = 0 Then
            Console_.W("Warning: (Tip) Region " & ActiveTip & " is empty")
            Exit Sub
        End If
        Dim m = mo.getRandomStat()
        Label7.ForeColor = Color.White
        Label8.Text = ""
        Label7.Text = m(1)
        For i = 0 To 10
            Label7.ForeColor = Color.FromArgb(255 - i * 25, 255 - i * 25, 255 - i * 25)
            'Threading.Thread.Sleep(40)
            'Application.DoEvents()
        Next
        'TypeEffect(m(0))
        Label8.Text = (m(0))
        lastused = ActiveTip


    End Sub
    'simulate type effect
    Sub TypeEffect(ByVal str$)

        For i = 0 To Len(str) - 1
            Label8.Text &= str(i)
            'Threading.Thread.Sleep(80)
        Next
    End Sub

    Private Sub CarEditor_MouseHover(ByVal sender As Object, ByVal e As System.EventArgs) Handles Panel1.MouseHover
        RequestTip("")
    End Sub




    Private Sub CarEditor_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.GotFocus
        Timer2.Start()
    End Sub



    Private Sub RenderSet_MouseHover(ByVal sender As Object, ByVal e As System.EventArgs) Handles RenderSet.MouseMove
        RequestTip("rendersett")
    End Sub



    Private Sub Autosave_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Autosave.Tick
        cars(Active_Car).Sing.SaveToFile(cars(Active_Car).Path & "\parameters.txt")
    End Sub



    'shaded?
    Private Sub MCheckbox2_3() Handles MCheckbox2.CheckedChanged
        If Not Initializd Then Exit Sub
        RENDER_SHADE = MCheckbox2.Checked


        '    GL.Enable(EnableCap.LineSmooth)
        '    GL.Enable(EnableCap.PolygonSmooth)
     
    End Sub


    'poly smooth? 'TODO: Not wronking
    Private Sub MCheckbox4_CheckedChanged() Handles MCheckbox4.CheckedChanged
        If Not Initializd Then Exit Sub
        If MCheckbox4.Checked Then GL.Enable(EnableCap.PolygonSmooth) Else GL.Disable(EnableCap.PolygonSmooth)

    End Sub

    'Pro editor
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Parameters_editor.Show()

    End Sub


    'auto save?
    Private Sub CheckBox2_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox2.CheckedChanged
        Autosave.Enabled = CheckBox2.Checked
    End Sub

    'save
    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        cars(Active_Car).Sing.SaveToFile(cars(Active_Car).Path & "\parameters.txt")
    End Sub


    'multithreading
    Private Sub BackgroundWorker1_DoWork_1(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork



        If CarIsLoading = True Then Exit Sub
        StartedRendering = True


        DoRender()

        FinishedRendering = True
        StartedRendering = False


    End Sub

    'restart thread
    Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        '   If DO_NOT_THREAD Then Exit Sub
        BackgroundWorker1.RunWorkerAsync()
    End Sub

    'toggle edit mode
    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        If ComboBox1.SelectedIndex < ComboBox1.Items.Count - 1 Then ComboBox1.SelectedIndex += 1 Else ComboBox1.SelectedIndex = 0
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        If ComboBox1.SelectedIndex > 0 Then ComboBox1.SelectedIndex -= 1 Else ComboBox1.SelectedIndex = ComboBox1.Items.Count - 1
    End Sub

    'textured?

    Private Sub MCheckbox1_CheckedChanged1() Handles MCheckbox1.CheckedChanged
        PRM.TEXTURED = MCheckbox1.Checked
    End Sub


    'oldjunk code
    Private Sub refresh_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs)
        DO_NOT_RENDER = True
        Application.DoEvents()
        Timer2.Start()
        DO_NOT_RENDER = False
    End Sub

End Class