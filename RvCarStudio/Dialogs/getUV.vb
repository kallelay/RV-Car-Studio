Imports System.Math
Imports OpenTK

Public Class UVviewer

    Public Const oo = Single.PositiveInfinity
    Dim Angle = PI / 4
    'Easter egg
    Dim Sprites As New List(Of Point)
    Dim iSprites As New List(Of Point)
    Sub InitSprites()

        Randomize()
        For i = 0 To 100
            Dim randY = Rnd() * 200 - 200
            Dim randX = Rnd() * Me.Width
            Sprites.Add(New Point(randX, randY))
            iSprites.Add(New Point(randX, randY))
        Next
    End Sub
    Dim t = 0
    Sub UpdateSprites()
        t += 1
        For i = 0 To Sprites.Count - 1
            If Sprites(i).Y = Me.Height - 40 Then
                Sprites(i) = New Point(Sprites(i).X, Sprites(i).Y + 1)
                Sprites.Add(New Point(Sprites(i).X, 0))
            ElseIf Sprites(i).Y > Me.Height - 40 Then
            Else

                Sprites(i) = New Point(Sprites(i).X + 2 * Cos(t), +Sprites(i).Y + 1)
            End If


        Next
    End Sub
    Sub DrawSprites(ByVal e As System.Drawing.Graphics)

        Me.CreateGraphics.Clear(Color.AliceBlue)
        For i = 0 To Sprites.Count - 1
            e.DrawRectangle(Pens.Black, New Rectangle(Sprites(i), New Size(2, 2)))
        Next
        e.Dispose()
    End Sub





    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
      

        DoWrite("Searching Colors")
        Panel2.BackColor = Color.FromArgb(Sett_get("line.R", 0), Sett_get("line.G", 0), Sett_get("line.B", 0))
        LINECOLOR = Panel2.BackColor
        Panel3.BackColor = Color.FromArgb(Sett_get("back.R", 255), Sett_get("back.G", 255), Sett_get("back.B", 255))
        Panel1.BackColor = Panel3.BackColor



        DoWrite("       version:" & "1.0")


        TextBox2.Text = cars(Active_Car).models.BODY.Directory & "\" & cars(Active_Car).models.BODY.FileName



        SetStyle(ControlStyles.UserPaint, True)
        SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        SetStyle(ControlStyles.DoubleBuffer, True)


        Button1_Click(Nothing, Nothing)

        save_file.InitialDirectory = cars(Active_Car).Path

        'Bonus
        '   FullLoaded = True

    End Sub
    Dim fullLoaded = False
    Private Sub TextBox2_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox2.TextChanged
        If IO.File.Exists(Replace(TextBox2.Text, "%rvdir%", rvpath, , , CompareMethod.Text)) Then

            If LCase(Strings.Right(TextBox2.Text, 3)) = "prm" Or LCase(Strings.Right(TextBox2.Text, 2)) = ".m" Then
                TextBox2.BackColor = Color.Lime
                'SaveSetting("KDL", "UV", "latest", TextBox2.Text)
                Sett_set("UV", TextBox2.Text)
            Else
                TextBox2.BackColor = Color.LightPink

            End If

        Else
            TextBox2.BackColor = Color.LightPink
        End If
    End Sub


    Dim UVform As Form


    Private Sub TextBox3_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox3.TextChanged

    End Sub
    Dim UV As New List(Of Vector2)
    Dim RenderUV As New List(Of Point)
    Dim V As PRM
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        TextBox3.Visible = True



        UV.Clear()
        RenderUV.Clear()

        DoWrite("Loading prm")

        V = cars(Active_Car).models.BODY.Clone


        DoWrite("Extracting UV")
        For i = 0 To V.MyModel.polynum - 1
            RenderUV.Add(New Point(V.MyModel.polyl(i).u0 * 256, V.MyModel.polyl(i).v0 * 256))
            RenderUV.Add(New Point(V.MyModel.polyl(i).u1 * 256, V.MyModel.polyl(i).v1 * 256))
            RenderUV.Add(New Point(V.MyModel.polyl(i).u1 * 256, V.MyModel.polyl(i).v1 * 256))
            RenderUV.Add(New Point(V.MyModel.polyl(i).u2 * 256, V.MyModel.polyl(i).v2 * 256))
            RenderUV.Add(New Point(V.MyModel.polyl(i).u2 * 256, V.MyModel.polyl(i).v2 * 256))


            '   UV.Add(New Vector2D(V.MyModel.polyl(i).u0, V.MyModel.polyl(i).v0))
            '  UV.Add(New Vector2D(V.MyModel.polyl(i).u1, V.MyModel.polyl(i).v1))
            ' UV.Add(New Vector2D(V.MyModel.polyl(i).u2, V.MyModel.polyl(i).v2))

            If V.MyModel.polyl(i).type And 1 Then
                '   UV.Add(New Vector2D(V.MyModel.polyl(i).u3, V.MyModel.polyl(i).v3))
                RenderUV.Add(New Point(V.MyModel.polyl(i).u3 * 256, V.MyModel.polyl(i).v3 * 256))
                RenderUV.Add(New Point(V.MyModel.polyl(i).u3 * 256, V.MyModel.polyl(i).v3 * 256))

            End If
            RenderUV.Add(New Point(V.MyModel.polyl(i).u0 * 256, V.MyModel.polyl(i).v0 * 256))

            '    UV.Add(New Vector2D(V.MyModel.polyl(i).u0, V.MyModel.polyl(i).v0))
            '    If Not V.MyModel.polyl(i).type And 1 Then
            '   UV.Add(New Vector2D(-50, -50)) 'the limiter

            '    End If
        Next

        DoWrite("Saving UV coordinates")

        DoWrite("Rendering UV coordinates")

        DoWrite("   Translating to GDI+")
        ' TranslateToGDIp()
        TextBox3.Visible = False
        DoWrite("   Rendering...")
        Draw()




    End Sub


    Sub TranslateToGDIp()
        Dim i As Int16 = 1
        Do Until i >= UV.Count
            If UV(i).X + UV(i).Y <> -100 And UV(i - 1).X + UV(i - 1).Y <> -100 Then
                '  RenderUV.Add(New Point(Int(UV(i - 1).x * 256), Int(UV(i - 1).y * 256)))
                RenderUV.Add(New Point(Int(UV(i).X * 256), Int(UV(i).Y * 256)))
                i += 1
            Else
                i += 3

            End If


        Loop
    End Sub
    Dim LINECOLOR As Color
    Dim BCKCOLOR As Color
    Sub Draw()
        Dim g = Panel1.CreateGraphics
        For i = 0 To RenderUV.Count - 2 Step 2
            g.DrawLine(New Pen(LINECOLOR), RenderUV(i), RenderUV(i + 1))
        Next
        g.Dispose()
    End Sub

    Sub Draw(ByVal g As System.Drawing.Graphics)
        'Dim g = Panel1.CreateGraphics
        For i = 0 To RenderUV.Count - 2 Step 2
            g.DrawLine(New Pen(LINECOLOR), RenderUV(i), RenderUV(i + 1))
        Next
        g.Dispose()
    End Sub
    Private Sub Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint
        If RenderUV.Count = 0 Then Exit Sub
        Draw(e.Graphics)

    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        clr.Color = Panel2.BackColor
        If clr.ShowDialog = Windows.Forms.DialogResult.OK Then
            Panel2.BackColor = clr.Color
            Sett_set("line.R", clr.Color.R)
            Sett_set("line.G", clr.Color.G)
            Sett_set("line.B", clr.Color.B)

            LINECOLOR = Panel2.BackColor


        End If
        Button1_Click(sender, e)
    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        clr.Color = Panel3.BackColor
        If clr.ShowDialog = Windows.Forms.DialogResult.OK Then
            Panel3.BackColor = clr.Color
            Sett_set("back.R", clr.Color.R)
            Sett_set("back.G", clr.Color.G)
            Sett_set("back.B", clr.Color.B)
            Panel1.BackColor = Panel3.BackColor

        End If
        Button1_Click(sender, e)
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If TextBox1.BackColor <> Color.Lime Then
            Console.Beep()
            Exit Sub
        End If

        If CheckBox1.Checked = False Then Exit Sub
        Try
            Panel1.BackgroundImage = Image.FromFile(Replace(TextBox1.Text, "%rvdir%", rvpath, , , CompareMethod.Text))
        Catch ex As Exception

        End Try


    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        If BMPPICK.ShowDialog = Windows.Forms.DialogResult.OK Then
            TextBox1.Text = BMPPICK.FileName
            Button3_Click(sender, e)
            CheckBox1.Checked = True
        End If
        Button1_Click(sender, e)
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged
        If IO.File.Exists(Replace(TextBox1.Text, "%rvdir%", rvpath, , , CompareMethod.Text)) Then
            TextBox1.BackColor = Color.Lime
        Else
            TextBox1.BackColor = Color.LightPink
        End If
    End Sub

    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked = False Then
            Panel1.BackgroundImage = Nothing
        End If
        Button3_Click(sender, e)

    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        TextBox1.Text = rvpath & "\" & cars(Active_Car).Theory.MainInfos.Tpage
        If IO.File.Exists(TextBox1.Text) = False Then
            TextBox1.Text = IO.Directory.GetFiles(Replace(Replace(TextBox2.Text, Split(TextBox2.Text, "\")(UBound(Split(TextBox2.Text, "\"))), "", , , CompareMethod.Text), "%rvdir%", rvpath), "*.bmp")(0)
        End If
        Button3_Click(sender, e)
        Button1_Click(sender, e)
    End Sub
    Dim jRenderUV As New List(Of Point)
    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click

        If save_file.ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub


        Dim bmp = New Bitmap(256, 256)
        Dim X = System.Drawing.Graphics.FromImage(bmp)

        Save_Bitmap(X, 256)

        X.Dispose()

        bmp.Save(save_file.FileName)


    End Sub
    Sub Save_Bitmap(ByVal X As System.Drawing.Graphics, ByVal SizeA As Integer)

        jRenderUV = New List(Of Point)

        For i = 0 To RenderUV.Count - 1
            jRenderUV.Add(New Point(SizeA / 256 * RenderUV(i).X, SizeA / 256 * RenderUV(i).Y))
        Next



        If Panel1.BackColor <> Color.White Then X.FillRectangle(New SolidBrush(Panel1.BackColor), New Rectangle(0, 0, SizeA, SizeA))

        If CheckBox1.Checked And TextBox1.BackColor = Color.Lime Then
            X.DrawImage(Panel1.BackgroundImage, 0, 0, SizeA, SizeA)
        End If

        For i = 0 To RenderUV.Count - 2 Step 2
            X.DrawLine(New Pen(LINECOLOR), jRenderUV(i), jRenderUV(i + 1))
        Next
    End Sub

    Private Sub Button9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button9.Click
        If save_file.ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub


        Dim bmp = New Bitmap(512, 512)
        Dim X = System.Drawing.Graphics.FromImage(bmp)

        Save_Bitmap(X, 512)

        X.Dispose()

        bmp.Save(save_file.FileName)

    End Sub

    Private Sub Button10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button10.Click
        If save_file.ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub


        Dim bmp = New Bitmap(1024, 1024)
        Dim X = System.Drawing.Graphics.FromImage(bmp)

        Save_Bitmap(X, 1024)

        X.Dispose()

        bmp.Save(save_file.FileName)

    End Sub

    Private Sub Button11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button11.Click
        If save_file.ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub


        Dim bmp = New Bitmap(2048, 2048)
        Dim X = System.Drawing.Graphics.FromImage(bmp)

        Save_Bitmap(X, 2048)

        X.Dispose()

        bmp.Save(save_file.FileName)

    End Sub

    Private Sub Button12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button12.Click
        If save_file.ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub


        Dim bmp = New Bitmap(4096, 4096)
        Dim X = System.Drawing.Graphics.FromImage(bmp)

        Save_Bitmap(X, 4096)

        X.Dispose()

        bmp.Save(save_file.FileName)

    End Sub

    Private Sub Button13_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button13.Click
        If save_file.ShowDialog <> Windows.Forms.DialogResult.OK Then Exit Sub


        Dim bmp = New Bitmap(8192, 8192)
        Dim X = System.Drawing.Graphics.FromImage(bmp)

        Save_Bitmap(X, 8192)

        X.Dispose()

        bmp.Save(save_file.FileName)

    End Sub



    Sub DoWrite(ByVal Str As String)
        TextBox3.AppendText(">> " & Str & vbNewLine)
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        If Not fullLoaded Then Exit Sub
        Me.Refresh()
        UpdateSprites()

        DrawSprites(Me.CreateGraphics())
    End Sub

    Private Sub Form1_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        CarEditor.Timer2.Stop()
        CarEditor.pause.Show()
        If Not fullLoaded Then Exit Sub
        '   InitSprites()

    End Sub

    Private Sub Timer2_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer2.Tick
        fullLoaded = True
        InitSprites()
        Timer2.Stop()
        Timer1.Start()
    End Sub

    Private Sub Button15_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Button1_Click(sender, e)
    End Sub

    Private Sub Button14_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub
End Class
