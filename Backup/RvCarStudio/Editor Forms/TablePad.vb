Imports System.Math
Imports System.Drawing
Imports OpenTK
Imports OpenTK.Graphics.OpenGL


Public Class Tablepad
    Public Enum CurEdMod
        none
        CoM
        Weapon
        Body
        wheels
        spring0
        spring1
        spring2
        spring3
        pin0
        pin1
        pin2
        pin3
        axle0
        axle1
        axle2
        axle3
        spinner_offset
        spinner_axis
        aerial_offset
        aerial_direction

    End Enum
    Public Current_Editing_Mode As CurEdMod = CurEdMod.none

    Private Sub Button11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button11.Click
        CarEditor.Panel3.Enabled = True
        Me.Hide()

    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click

    End Sub
    Dim mpos As Point
    Dim PosXY As Point
    Dim PosZ As Point
    Private Sub Label1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Label1.MouseMove
        If e.Button = Windows.Forms.MouseButtons.Left Then
            Me.Location += MousePosition - mpos
            mpos = MousePosition
        Else
            mpos = MousePosition
        End If
    End Sub

    Private Sub Panel1_MouseLeave(ByVal sender As Object, ByVal e As System.EventArgs) Handles Panel1.MouseLeave
        Panel1.BackColor = Drawing.Color.AliceBlue
    End Sub
    Dim incX = -0.1
    Dim incY = 0.1

    Dim incZ = -0.1

    Dim invert = False

    Private Sub Panel1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Panel1.MouseMove
        If Panel1.BackColor <> Drawing.Color.LavenderBlush Then
            Panel1.BackColor = Drawing.Color.LavenderBlush
        End If




        If e.Button = Windows.Forms.MouseButtons.Left Then
            Dim delta As PointF
            delta = New PointF((MousePosition.X - PosXY.X) * incX, (MousePosition.Y - PosXY.Y) * incY)

            Select Case Current_Editing_Mode
                Case CurEdMod.Body
                    _Body.NumericUpDown1.Value += delta.X
                    _Body.NumericUpDown3.Value -= delta.Y
                Case CurEdMod.CoM
                    _Body.NumericUpDown6.Value += delta.X
                    _Body.NumericUpDown4.Value += delta.Y
                Case CurEdMod.Weapon
                    _Body.NumericUpDown9.Value += delta.X
                    _Body.NumericUpDown7.Value -= delta.Y
                Case CurEdMod.wheels
                    Wheels.NumericUpDown3.Value += delta.X
                    Wheels.NumericUpDown1.Value -= delta.Y

            End Select







            PosXY = MousePosition


            'Y offset
        ElseIf e.Button = Windows.Forms.MouseButtons.Middle Then





        Else


            Select Case getPositionBlock(GlobalPosition, New Vector3)
                Case 3
                    invert = False
                    incX = -0.1
                    incY = 0.1

                Case 1
                    invert = False
                    incX = 0.1
                    incY = -0.1
                Case 0
                    invert = True

                    incX = 0.1
                    incY = 0.1
                Case 2
                    incX = -0.1
                    incY = -0.1
                    invert = True

            End Select

            If CheckBox1.Checked Then
                incX *= 0.05
                incY *= 0.05
                incZ = 0.05 * -0.1
            Else
                incZ = -0.1
            End If

            PosZ = MousePosition
            PosXY = MousePosition
        End If



    End Sub

    Private Sub Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint

    End Sub

    Private Sub Panel2_MouseLeave(ByVal sender As Object, ByVal e As System.EventArgs) Handles Panel2.MouseLeave
        Panel2.BackColor = Drawing.Color.AliceBlue
    End Sub

    Private Sub Panel2_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Panel2.MouseMove
        If Panel2.BackColor <> Drawing.Color.LavenderBlush Then
            Panel2.BackColor = Drawing.Color.LavenderBlush
        End If




        If e.Button = Windows.Forms.MouseButtons.Left Then

            Dim DeltaZ! = (MousePosition - PosZ).Y

            Select Case Current_Editing_Mode
                Case CurEdMod.Body
                    _Body.NumericUpDown2.Value += DeltaZ
                Case CurEdMod.CoM
                    _Body.NumericUpDown5.Value += DeltaZ
                Case CurEdMod.wheels
                    Wheels.NumericUpDown2.Value += DeltaZ
            End Select

            PosZ = MousePosition

        Else


            PosZ = MousePosition
        End If




    End Sub

    Private Sub Panel2_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel2.Paint

    End Sub
    Sub Hover(ByVal cmd As Button)
        Do Until cmd.Top <= 324
            Application.DoEvents()
            cmd.Top -= 2
            Threading.Thread.Sleep(10)
        Loop
    End Sub
    Sub HoverPanel(ByVal cmd As Button)
        Do Until cmd.Left <= 469
            Application.DoEvents()
            cmd.Left -= 2
            Threading.Thread.Sleep(10)
        Loop
    End Sub
    Sub unhover(ByVal cmd As Button)
        Do Until cmd.Top > 328
            Application.DoEvents()
            cmd.Top += 2
        Loop
    End Sub

    Sub unhoverPanel(ByVal cmd As Button)
        Do Until cmd.Left >= 479
            Application.DoEvents()
            cmd.Left += 2
        Loop
    End Sub

    Private Sub Tablepad_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Private Sub Label3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label3.Click

    End Sub


    Private Sub Button1_MouseMove(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.MouseMove
        Hover(Button1)
    End Sub
    Private Sub Button1_MouseOut(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.MouseLeave
        unhover(Button1)
    End Sub
    Private Sub button2_MouseMove(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.MouseMove
        Hover(Button2)
    End Sub
    Private Sub button2_MouseOut(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.MouseLeave
        unhover(Button2)
    End Sub
    Private Sub button3_MouseMove(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.MouseMove
        Hover(Button3)
    End Sub
    Private Sub button3_MouseOut(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.MouseLeave
        unhover(Button3)
    End Sub
    Private Sub button4_MouseMove(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.MouseMove
        Hover(Button4)
    End Sub
    Private Sub button4_MouseOut(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.MouseLeave
        unhover(Button4)
    End Sub
    Private Sub button5_MouseMove(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.MouseMove
        Hover(Button5)
    End Sub
    Private Sub button5_MouseOut(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.MouseLeave
        unhover(Button5)
    End Sub
    Private Sub button6_MouseMove(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.MouseMove
        Hover(Button6)
    End Sub
    Private Sub button6_MouseOut(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.MouseLeave
        unhover(Button6)
    End Sub

    Dim IncVec As New Vector3
    Dim nPos As New Vector3

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        PRM.Phi = Math.PI / 2
        'TranslateCamera(GlobalPosition, New Vector3(0, 0, -15))
        'TranslateCamera(GlobalPosition, New Vector3(0, 15, 0))
    End Sub



    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        '  TranslateCamera(GlobalPosition, New Vector3(0, -15, 0))
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click


        PRM.Phi = 90





    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        PRM.Phi = -90
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        PRM.Phi = 0
    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click

        PRM.Phi = 180
    End Sub





    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox1.CheckedChanged

    End Sub
End Class