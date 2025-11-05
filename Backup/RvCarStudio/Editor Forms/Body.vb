Imports OpenTK
Imports System.Math
Public Class _Body

  
    Public Loading = False
    Sub DOLOAD()

        Loading = True

        'Body's model
        '  If cars(Active_Car).Theory.Body.modelNumber <> -1 Then _
        'Button1.Text = cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Body.modelNumber)


        'Body's offset 
        PrintVecToObjectsV(cars(Active_Car).Theory.Body.Offset, NumericUpDown1, NumericUpDown2, NumericUpDown3)


        'COM
        PrintVecToObjectsV(cars(Active_Car).Theory.RealInfos.COM, NumericUpDown6, NumericUpDown5, NumericUpDown4)

        'Weapon
        PrintVecToObjectsV(cars(Active_Car).Theory.RealInfos.WeaponGeneration, NumericUpDown9, NumericUpDown8, NumericUpDown7)



        'mass 
        NumericUpDown13.Value = cars(Active_Car).Theory.Body.Mass


        'inertia
        PrintVecToObjectsT(cars(Active_Car).Theory.Body.Inertia(0), TextBox1, TextBox2, TextBox3)
        PrintVecToObjectsT(cars(Active_Car).Theory.Body.Inertia(1), TextBox4, TextBox5, TextBox6)
        PrintVecToObjectsT(cars(Active_Car).Theory.Body.Inertia(2), TextBox7, TextBox8, TextBox9)


        'Dynamics...
        NumericUpDown11.Value = cars(Active_Car).Theory.Body.Hardness
        NumericUpDown12.Value = cars(Active_Car).Theory.Body.Resistance
        NumericUpDown14.Value = cars(Active_Car).Theory.Body.AngleRes
        NumericUpDown10.Value = cars(Active_Car).Theory.Body.ResMode
        NumericUpDown15.Value = cars(Active_Car).Theory.Body.Grip
        NumericUpDown16.Value = cars(Active_Car).Theory.Body.StaticFriction
        NumericUpDown17.Value = cars(Active_Car).Theory.Body.KineticFriction





        ShowCOM()
        ShowAxis()
        ShowWG()

        UpdateAxis(cars(Active_Car).models.BODY)
        UpdateCOM(cars(Active_Car).Theory.RealInfos.COM)
        UpdateWG(cars(Active_Car).Theory.RealInfos.WeaponGeneration)

        body_aider.Show()

        Loading = False
    End Sub
 
    Sub UnLoad()
        HideCOM()
        HideAxis()
        HideWG()
        body_aider.Hide()


    End Sub
    Sub CarMoved(ByVal sender As Object, ByVal e As EventArgs) Handles NumericUpDown1.ValueChanged, NumericUpDown2.ValueChanged, NumericUpDown3.ValueChanged
        Patch(sender)
        If Loading Then Exit Sub
        If Not Initializd Then Exit Sub

        cars(Active_Car).models.BODY.Position = New Vector3(NumericUpDown1.Value, NumericUpDown2.Value, NumericUpDown3.Value) * Zoom

        cars(Active_Car).Theory.Body.Offset = New Vector3(NumericUpDown1.Value, NumericUpDown2.Value, NumericUpDown3.Value)

        UpdateAxis(cars(Active_Car).models.BODY)


    End Sub
    Sub ComMoved(ByVal sender As Object, ByVal e As EventArgs) Handles NumericUpDown6.ValueChanged, NumericUpDown5.ValueChanged, NumericUpDown4.ValueChanged
        Patch(sender)
        If Loading Then Exit Sub
        If Not Initializd Then Exit Sub
        cars(Active_Car).Theory.RealInfos.COM = New Vector3(NumericUpDown6.Value, NumericUpDown5.Value, NumericUpDown4.Value)
        UpdateCOM(cars(Active_Car).Theory.RealInfos.COM)
    End Sub
    Sub WGMoved(ByVal sender As Object, ByVal e As EventArgs) Handles NumericUpDown9.ValueChanged, NumericUpDown8.ValueChanged, NumericUpDown4.ValueChanged
        Patch(sender)
        If Loading Then Exit Sub
        If Not Initializd Then Exit Sub
        cars(Active_Car).Theory.RealInfos.WeaponGeneration = New Vector3(NumericUpDown9.Value, NumericUpDown8.Value, NumericUpDown7.Value)
        UpdateWG(cars(Active_Car).Theory.RealInfos.WeaponGeneration)
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button45.Click
        'PICK_PRM.PreviewMode = True
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Body
        PICK_PRM.Show()
    End Sub



    'to be moved?
    Public Function getFreeSlot() As Integer
        For i = 0 To 18
            If cars(Active_Car).Theory.MainInfos.Model(i) = "NONE" Then Return i
        Next

    End Function


    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Tablepad.Label1.Text = "Table Pad (Body Offset)"
        Tablepad.Current_Editing_Mode = Tablepad.CurEdMod.Body
        Panel3.Enabled = False
        Tablepad.Show()


    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        NumericUpDown1.Value = cars(Active_Car).OriginalTheory.Body.Offset.X
        NumericUpDown2.Value = cars(Active_Car).OriginalTheory.Body.Offset.Y
        NumericUpDown3.Value = cars(Active_Car).OriginalTheory.Body.Offset.Z

    End Sub

    Sub KeyPresses(ByVal sender As Object, ByVal e As Windows.Forms.KeyPressEventArgs) Handles TextBox1.KeyPress, TextBox2.KeyPress, TextBox3.KeyPress, _
    TextBox4.KeyPress, TextBox5.KeyPress, TextBox6.KeyPress, _
    TextBox7.KeyPress, TextBox8.KeyPress, TextBox9.KeyPress

        If (e.KeyChar < "0" Or e.KeyChar > "9") Then _
        If e.KeyChar <> Chr(8) And e.KeyChar <> Chr(46) Then e.KeyChar = "" '8=Backspace, 46=delete


    End Sub


    Private Sub GroupBox1_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GroupBox1.Enter

    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        NumericUpDown6.Value = cars(Active_Car).OriginalTheory.RealInfos.COM.X
        NumericUpDown5.Value = cars(Active_Car).OriginalTheory.RealInfos.COM.Y
        NumericUpDown4.Value = cars(Active_Car).OriginalTheory.RealInfos.COM.Z

    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Tablepad.Label1.Text = "Table Pad (Center of Mass)"
        Tablepad.Current_Editing_Mode = Tablepad.CurEdMod.CoM
        Panel3.Enabled = False
        Tablepad.Show()

    End Sub


    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        NumericUpDown9.Value = cars(Active_Car).OriginalTheory.RealInfos.WeaponGeneration.X
        NumericUpDown8.Value = cars(Active_Car).OriginalTheory.RealInfos.WeaponGeneration.Y
        NumericUpDown7.Value = cars(Active_Car).OriginalTheory.RealInfos.WeaponGeneration.Z

    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        Tablepad.Label1.Text = "Table Pad (Weapon Generation)"
        Tablepad.Current_Editing_Mode = Tablepad.CurEdMod.Weapon
        Panel3.Enabled = False
        Tablepad.Show()
    End Sub
    Dim FW As PRM
    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click
        If FW Is Nothing Then FW = New PRM("data\models\firework.m")
        FW.MATRIX = Matrix4.CreateRotationX(MathHelper.PiOver4)
        FW.TextureI = 2



        FW.Position = cars(Active_Car).Theory.RealInfos.WeaponGeneration * Zoom
        FW.isVisible = True

    End Sub




    Private Sub Button12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button12.Click
        NumericUpDown9.Value = 0
        NumericUpDown8.Value = -cars(Active_Car).models.BODY.BoundingBox.MinY
        NumericUpDown7.Value = cars(Active_Car).models.BODY.BoundingBox.maxZ
    End Sub

    Private Sub NumericUpDown13_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown13.ValueChanged
        If Not Initializd Then Exit Sub
        cars(Active_Car).Theory.Body.Mass = NumericUpDown13.Value
    End Sub

    Private Sub Inertia0_Changed(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged, TextBox2.TextChanged, TextBox3.TextChanged
        If TextBox3.Text = "" Then Exit Sub
        cars(Active_Car).Theory.Body.Inertia(0) = New Vector3(TextBox1.Text, TextBox2.Text, TextBox3.Text)
    End Sub
    Private Sub Inertia1_Changed(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox4.TextChanged, TextBox5.TextChanged, TextBox6.TextChanged
        If TextBox6.Text = "" Then Exit Sub
        cars(Active_Car).Theory.Body.Inertia(1) = New Vector3(TextBox4.Text, TextBox5.Text, TextBox6.Text)
    End Sub
    Private Sub Inertia2_Changed(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox7.TextChanged, TextBox8.TextChanged, TextBox9.TextChanged
        If TextBox9.Text = "" Then Exit Sub
        cars(Active_Car).Theory.Body.Inertia(2) = New Vector3(TextBox7.Text, TextBox8.Text, TextBox9.Text)
    End Sub

    Private Sub Button9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button9.Click
        PICK_MATRIX.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_MATRIX.Show()

    End Sub

    Private Sub Button10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button10.Click ', Button40.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY) / 2
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ) / 2

        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) / 5
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) / 5
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) / 5


    End Sub

    Private Sub Button11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button11.Click
        PrintVecToObjectsT(cars(Active_Car).OriginalTheory.Body.Inertia(0), TextBox1, TextBox2, TextBox3)
        PrintVecToObjectsT(cars(Active_Car).OriginalTheory.Body.Inertia(1), TextBox4, TextBox5, TextBox6)
        PrintVecToObjectsT(cars(Active_Car).OriginalTheory.Body.Inertia(2), TextBox7, TextBox8, TextBox9)
    End Sub



    Private Sub Button13_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button13.Click
        NumericUpDown11.Value = cars(Active_Car).OriginalTheory.Body.Hardness
    End Sub

    Private Sub Button14_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button14.Click
        NumericUpDown12.Value = cars(Active_Car).OriginalTheory.Body.Resistance
    End Sub

    Private Sub Button15_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button15.Click
        NumericUpDown14.Value = cars(Active_Car).OriginalTheory.Body.AngleRes
    End Sub

    Private Sub Button16_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button16.Click
        NumericUpDown10.Value = cars(Active_Car).OriginalTheory.Body.ResMode
    End Sub

    Private Sub Button17_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button17.Click
        NumericUpDown15.Value = cars(Active_Car).OriginalTheory.Body.Grip
    End Sub

    Private Sub Button18_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button18.Click
        NumericUpDown16.Value = cars(Active_Car).OriginalTheory.Body.StaticFriction
    End Sub

    Private Sub Button19_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button19.Click
        NumericUpDown17.Value = cars(Active_Car).OriginalTheory.Body.KineticFriction
    End Sub

    Private Sub Dynamics_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown11.ValueChanged, NumericUpDown12.ValueChanged, NumericUpDown14.ValueChanged, NumericUpDown10.ValueChanged, NumericUpDown15.ValueChanged, NumericUpDown16.ValueChanged, NumericUpDown17.ValueChanged
        Patch(sender)
        If Loading Then Exit Sub


        cars(Active_Car).Theory.Body.Hardness = NumericUpDown11.Value
        cars(Active_Car).Theory.Body.Resistance = NumericUpDown12.Value

        cars(Active_Car).Theory.Body.AngleRes = NumericUpDown14.Value
        cars(Active_Car).Theory.Body.ResMode = NumericUpDown10.Value

        cars(Active_Car).Theory.Body.Grip = NumericUpDown15.Value

        cars(Active_Car).Theory.Body.StaticFriction = NumericUpDown16.Value

        cars(Active_Car).Theory.Body.KineticFriction = NumericUpDown17.Value
    End Sub
    Dim LastPOS As Point
    '   Private Sub Panel9_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Panel9.MouseMove
    '       If e.Button = Windows.Forms.MouseButtons.Middle Then
    '         Panel9.ScrollControlIntoView(Geometrics)
    '
    '
    ''    End If
    '    LastPOS = Cursor.Position
    '   End Sub
    '
    Private Sub Panel9_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel9.Paint

    End Sub
  
    Private Sub Button21_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button21.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY) / 4
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ) / 2

        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) * 3 / 10
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) * 3 / 10
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) * 3 / 10

    End Sub

    Private Sub Button22_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button22.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY) / 4
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ) / 2

        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) '* 3 / 10
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) * 3 / 2 ' * 3 / 2 '* 3 / 10
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) '* 3 / 10
    End Sub

    Private Sub Panel5_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs)

    End Sub

    Private Sub Button23_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button23.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX)
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY)
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ)



        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) * 3 / 2 / PI
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) * 3 / 2 / PI ' * 3 / 2 '* 3 / 10
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) * 3 / 2 / PI ' 
    End Sub

    Private Sub Button24_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button24.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY) / 2
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ) / 2

        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) ^ 5 / 1000 ^ 5
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) ^ 5 / 1000 ^ 5 ' * 3 / 2 '* 3 / 10
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) ^ 5 / 1000 ^ 5 ' 
    End Sub

    Private Sub Button25_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button25.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY) / 2
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ) / 2

        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) ^ 5 / 1000 ^ 5 * 10 ^ 2
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) ^ 5 / 1000 ^ 5 * 10 ^ 2 ' * 3 / 2 '* 3 / 10
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) ^ 5 / 1000 ^ 5 * 10 ^ 4 ' 
    End Sub
   

    Private Sub Button29_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        UVviewer.Show()

    End Sub


    Private Sub _Body_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Private Sub Button30_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button30.Click
        Dim p As New Vector3
        For i = 0 To cars(Active_Car).models.BODY.MyModel.vertnum - 1
            p += New Vector3(cars(Active_Car).models.BODY.MyModel.vexl(i).Position.X, _
            -cars(Active_Car).models.BODY.MyModel.vexl(i).Position.Y, _
            cars(Active_Car).models.BODY.MyModel.vexl(i).Position.Z)

        Next
        p /= cars(Active_Car).models.BODY.MyModel.polynum
        p += New Vector3(cars(Active_Car).Theory.Body.Offset.X, _
        cars(Active_Car).Theory.Body.Offset.Y, _
        cars(Active_Car).Theory.Body.Offset.Z) * cars(Active_Car).Theory.Body.Mass * 2 / 3

        p /= 3
        'p/= cars(Active_Car).Theory.
        NumericUpDown6.Value = p.X
        NumericUpDown5.Value = p.Y
        NumericUpDown4.Value = p.Z

    End Sub

    Private Sub Button31_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button31.Click
        Dim p As New Vector3d
        p += cars(Active_Car).Theory.wheel(0).Offset(1)
        p += cars(Active_Car).Theory.wheel(1).Offset(1)
        p += cars(Active_Car).Theory.wheel(2).Offset(1)
        p += cars(Active_Car).Theory.wheel(3).Offset(1)
        'p.Y *= -1

        p /= 4
        ' p += New Vector3d(0, 0, -8)


        'p/= cars(Active_Car).Theory.
        NumericUpDown6.Value = p.X
        NumericUpDown5.Value = p.Y
        NumericUpDown4.Value = p.Z

    End Sub

    Private Sub UVW_MouseMov(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)

    End Sub

    Private Sub Ed3d_MouseMov(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)

    End Sub

    Private Sub Dyn_MouseMov(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)

    End Sub

    Private Sub Geo_MouseMov(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)

    End Sub

    Private Sub Pos_MouseMov(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)

    End Sub

    Private Sub Button32_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button32.Click

        Dim p As New Vector3
        '  p += New Vector3(0, cars(Active_Car).models.BODY.BoundingBox.maxY, 5) + cars(Active_Car).Theory.Body.Offset

        p = (1 / cars(Active_Car).Theory.Weight) * ( _
            cars(Active_Car).models.BODY.BoundingBox.getCenter * cars(Active_Car).Theory.Body.Mass + _
          cars(Active_Car).models.Wheel(0).BoundingBox.getCenter * cars(Active_Car).Theory.wheel(0).Mass + _
           cars(Active_Car).models.Wheel(1).BoundingBox.getCenter * cars(Active_Car).Theory.wheel(1).Mass + _
            cars(Active_Car).models.Wheel(2).BoundingBox.getCenter * cars(Active_Car).Theory.wheel(2).Mass + _
             cars(Active_Car).models.Wheel(3).BoundingBox.getCenter * cars(Active_Car).Theory.wheel(3).Mass)


        NumericUpDown6.Value = p.X
        NumericUpDown5.Value = -p.Y
        NumericUpDown4.Value = p.Z

    End Sub



    Private Sub MCheckbox1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button33_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button33.Click

        Dim p As New Vector3
        '  p += New Vector3(0, cars(Active_Car).models.BODY.BoundingBox.maxY, 5) + cars(Active_Car).Theory.Body.Offset

        p = (1 / cars(Active_Car).Theory.Weight) * ( _
            cars(Active_Car).models.BODY.BoundingBox.getCenter * cars(Active_Car).Theory.Body.Mass + _
          cars(Active_Car).models.Wheel(0).BoundingBox.getCenter * cars(Active_Car).Theory.wheel(0).Mass + _
           cars(Active_Car).models.Wheel(1).BoundingBox.getCenter * cars(Active_Car).Theory.wheel(1).Mass + _
            cars(Active_Car).models.Wheel(2).BoundingBox.getCenter * cars(Active_Car).Theory.wheel(2).Mass + _
             cars(Active_Car).models.Wheel(3).BoundingBox.getCenter * cars(Active_Car).Theory.wheel(3).Mass)


        NumericUpDown6.Value = p.X
        NumericUpDown5.Value = -p.Y / 2
        NumericUpDown4.Value = p.Z - 5

    End Sub

    Private Sub Button34_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button34.Click

        Dim p As New Vector3
        '  p += New Vector3(0, cars(Active_Car).models.BODY.BoundingBox.maxY, 5) + cars(Active_Car).Theory.Body.Offset

        p = (1 / cars(Active_Car).Theory.Weight) * ( _
            cars(Active_Car).models.BODY.getCom * cars(Active_Car).Theory.Body.Mass + _
          cars(Active_Car).models.Wheel(0).getcom * cars(Active_Car).Theory.wheel(0).Mass + _
           cars(Active_Car).models.Wheel(1).getcom * cars(Active_Car).Theory.wheel(1).Mass + _
            cars(Active_Car).models.Wheel(2).getcom * cars(Active_Car).Theory.wheel(2).Mass + _
             cars(Active_Car).models.Wheel(3).getcom * cars(Active_Car).Theory.wheel(3).Mass)


        NumericUpDown6.Value = p.X
        NumericUpDown5.Value = -p.Y
        NumericUpDown4.Value = p.Z
    End Sub

    Private Sub Button35_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button35.Click
        Dim p As New Vector3
        '  p += New Vector3(0, cars(Active_Car).models.BODY.BoundingBox.maxY, 5) + cars(Active_Car).Theory.Body.Offset

        p = (1 / cars(Active_Car).Theory.Weight) * ( _
            cars(Active_Car).models.BODY.getQuadCoM * cars(Active_Car).Theory.Body.Mass + _
          cars(Active_Car).models.Wheel(0).getQuadCoM * cars(Active_Car).Theory.wheel(0).Mass + _
           cars(Active_Car).models.Wheel(1).getQuadCoM * cars(Active_Car).Theory.wheel(1).Mass + _
            cars(Active_Car).models.Wheel(2).getQuadCoM * cars(Active_Car).Theory.wheel(2).Mass + _
             cars(Active_Car).models.Wheel(3).getQuadCoM * cars(Active_Car).Theory.wheel(3).Mass)


        NumericUpDown6.Value = p.X
        NumericUpDown5.Value = -p.Y
        NumericUpDown4.Value = p.Z
    End Sub

    Private Sub Button36_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button36.Click
        NumericUpDown4.Value = (cars(Active_Car).Theory.wheel(0).Offset(1).Z + cars(Active_Car).Theory.wheel(3).Offset(1).Z) / 2
    End Sub

    Private Sub Button37_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button37.Click

     

        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.MinX - cars(Active_Car).models.BODY.BoundingBox.maxX)
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.MinY - cars(Active_Car).models.BODY.BoundingBox.maxY)
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.MinZ - cars(Active_Car).models.BODY.BoundingBox.maxZ)


        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) / 24
        TextBox2.Text = 0
        TextBox3.Text = 0

        TextBox4.Text = 0
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) / 18
        TextBox6.Text = 0

        TextBox7.Text = 0
        TextBox8.Text = 0
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) / 12

    End Sub

    Private Sub Button38_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button38.Click


        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.MinX - cars(Active_Car).models.BODY.BoundingBox.maxX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.MinY - cars(Active_Car).models.BODY.BoundingBox.maxY) / 2
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.MinZ - cars(Active_Car).models.BODY.BoundingBox.maxZ) / 2

        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) * 2 / 3
        TextBox2.Text = 0
        TextBox3.Text = 0

        TextBox4.Text = 0
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) * 2 / 3
        TextBox6.Text = 0

        TextBox7.Text = 0
        TextBox8.Text = 0
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) * 2 / 3

    End Sub

    Private Sub Button39_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button39.Click

        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.MinX - cars(Active_Car).models.BODY.BoundingBox.maxX)
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.MinY - cars(Active_Car).models.BODY.BoundingBox.maxY)
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.MinZ - cars(Active_Car).models.BODY.BoundingBox.maxZ)


        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) * 4 / (15 * 12)
        TextBox2.Text = 0
        TextBox3.Text = 0

        TextBox4.Text = 0
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) * 4 / (15 * 12)
        TextBox6.Text = 0

        TextBox7.Text = 0
        TextBox8.Text = 0
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) * 4 / (15 * 12)

    End Sub

    Private Sub Button40_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button40.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY) / 2
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ) / 2

        Dim H1 = Abs(cars(Active_Car).models.Wheel(0).BoundingBox.maxX - cars(Active_Car).models.Wheel(0).BoundingBox.MinX) ' / 2
        Dim H2 = Abs(cars(Active_Car).models.Wheel(1).BoundingBox.maxX - cars(Active_Car).models.Wheel(1).BoundingBox.MinX) ' / 2
        Dim H3 = Abs(cars(Active_Car).models.Wheel(2).BoundingBox.maxX - cars(Active_Car).models.Wheel(2).BoundingBox.MinX) ' / 2
        Dim H4 = Abs(cars(Active_Car).models.Wheel(3).BoundingBox.maxX - cars(Active_Car).models.Wheel(3).BoundingBox.MinX) ' / 2
        Dim H = (H1 + H2 + H3 + H4) / 4

        Dim R1 = Abs(cars(Active_Car).models.Wheel(0).BoundingBox.maxZ - cars(Active_Car).models.Wheel(0).BoundingBox.MinZ) / 2
        Dim R2 = Abs(cars(Active_Car).models.Wheel(1).BoundingBox.maxZ - cars(Active_Car).models.Wheel(1).BoundingBox.MinZ) / 2
        Dim R3 = Abs(cars(Active_Car).models.Wheel(2).BoundingBox.maxZ - cars(Active_Car).models.Wheel(2).BoundingBox.MinZ) / 2
        Dim R4 = Abs(cars(Active_Car).models.Wheel(3).BoundingBox.maxZ - cars(Active_Car).models.Wheel(3).BoundingBox.MinZ) / 2

        Dim R = (R1 + R2 + R3 + R4) / 4

        Dim M2 = 0.25 * (cars(Active_Car).Theory.wheel(0).Mass + cars(Active_Car).Theory.wheel(1).Mass + _
                         cars(Active_Car).Theory.wheel(2).Mass + cars(Active_Car).Theory.wheel(3).Mass)






        TextBox1.Text = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) / 5 + M2 * R ^ 2 / 2 + M2 * Sqrt(H) ^ 3 / 12
        TextBox2.Text = 0
        TextBox3.Text = 0

        TextBox4.Text = 0
        TextBox5.Text = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) / 5 + M2 * R ^ 2 / 2 + M2 * Sqrt(H) ^ 3 / 12
        TextBox6.Text = 0

        TextBox7.Text = 0
        TextBox8.Text = 0
        TextBox9.Text = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) / 5 + M2 * R ^ 2
    End Sub

    Private Sub Button41_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button41.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY) / 2
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ) / 2

        Dim H1 = Abs(cars(Active_Car).models.Wheel(0).BoundingBox.maxX - cars(Active_Car).models.Wheel(0).BoundingBox.MinX) ' / 2
        Dim H2 = Abs(cars(Active_Car).models.Wheel(1).BoundingBox.maxX - cars(Active_Car).models.Wheel(1).BoundingBox.MinX) ' / 2
        Dim H3 = Abs(cars(Active_Car).models.Wheel(2).BoundingBox.maxX - cars(Active_Car).models.Wheel(2).BoundingBox.MinX) ' / 2
        Dim H4 = Abs(cars(Active_Car).models.Wheel(3).BoundingBox.maxX - cars(Active_Car).models.Wheel(3).BoundingBox.MinX) ' / 2
        '   Dim H = (H1 + H2 + H3 + H4) / 4

        Dim R1 = Abs(cars(Active_Car).models.Wheel(0).BoundingBox.maxZ - cars(Active_Car).models.Wheel(0).BoundingBox.MinZ) / 2
        Dim R2 = Abs(cars(Active_Car).models.Wheel(1).BoundingBox.maxZ - cars(Active_Car).models.Wheel(1).BoundingBox.MinZ) / 2
        Dim R3 = Abs(cars(Active_Car).models.Wheel(2).BoundingBox.maxZ - cars(Active_Car).models.Wheel(2).BoundingBox.MinZ) / 2
        Dim R4 = Abs(cars(Active_Car).models.Wheel(3).BoundingBox.maxZ - cars(Active_Car).models.Wheel(3).BoundingBox.MinZ) / 2

        '  Dim R = (R1 + R2 + R3 + R4) / 4


        Dim I(4, 3) As Single

        'Ixx Iyy Izz
        '            W1
        '            w2
        '            w3
        '            w4

        'For Each Wheel
        I(1, 1) = cars(Active_Car).Theory.wheel(0).Mass * R1 ^ 2 / 2 + cars(Active_Car).Theory.wheel(0).Mass * Sqrt(H1) ^ 3 / 12
        I(2, 1) = cars(Active_Car).Theory.wheel(1).Mass * R2 ^ 2 / 2 + cars(Active_Car).Theory.wheel(1).Mass * Sqrt(H2) ^ 3 / 12
        I(3, 1) = cars(Active_Car).Theory.wheel(2).Mass * R3 ^ 2 / 2 + cars(Active_Car).Theory.wheel(2).Mass * Sqrt(H3) ^ 3 / 12
        I(4, 1) = cars(Active_Car).Theory.wheel(3).Mass * R4 ^ 2 / 2 + cars(Active_Car).Theory.wheel(3).Mass * Sqrt(H4) ^ 3 / 12

        I(1, 2) = I(1, 1)
        I(2, 2) = I(2, 1)
        I(3, 2) = I(3, 1)
        I(4, 2) = I(4, 1)


        I(1, 3) = cars(Active_Car).Theory.wheel(0).Mass * R1 ^ 2
        I(2, 3) = cars(Active_Car).Theory.wheel(1).Mass * R2 ^ 2
        I(3, 3) = cars(Active_Car).Theory.wheel(2).Mass * R3 ^ 2
        I(4, 3) = cars(Active_Car).Theory.wheel(3).Mass * R4 ^ 2


        'Huygens
        I(1, 1) += cars(Active_Car).Theory.wheel(0).Mass * (cars(Active_Car).Theory.wheel(0).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).Y) + _
                                                            cars(Active_Car).Theory.wheel(0).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).Z))
        I(2, 1) += cars(Active_Car).Theory.wheel(1).Mass * (cars(Active_Car).Theory.wheel(1).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).Y) + _
                                                          cars(Active_Car).Theory.wheel(1).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).Z))
        I(3, 1) += cars(Active_Car).Theory.wheel(2).Mass * (cars(Active_Car).Theory.wheel(2).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).Y) + _
                                                          cars(Active_Car).Theory.wheel(2).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).Z))
        I(4, 1) += cars(Active_Car).Theory.wheel(3).Mass * (cars(Active_Car).Theory.wheel(3).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).Y) + _
                                                          cars(Active_Car).Theory.wheel(3).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).Z))

        I(1, 2) += cars(Active_Car).Theory.wheel(0).Mass * (cars(Active_Car).Theory.wheel(0).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(0).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).Z))
        I(2, 2) += cars(Active_Car).Theory.wheel(1).Mass * (cars(Active_Car).Theory.wheel(1).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(1).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).Z))
        I(3, 2) += cars(Active_Car).Theory.wheel(2).Mass * (cars(Active_Car).Theory.wheel(2).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(2).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).Z))
        I(4, 2) += cars(Active_Car).Theory.wheel(3).Mass * (cars(Active_Car).Theory.wheel(3).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(3).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).Z))


        I(1, 3) += cars(Active_Car).Theory.wheel(0).Mass * (cars(Active_Car).Theory.wheel(0).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).X) + _
                                                  cars(Active_Car).Theory.wheel(0).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).Y))
        I(2, 3) += cars(Active_Car).Theory.wheel(1).Mass * (cars(Active_Car).Theory.wheel(1).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(1).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).Y))
        I(3, 3) += cars(Active_Car).Theory.wheel(2).Mass * (cars(Active_Car).Theory.wheel(2).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(2).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).Y))
        I(4, 3) += cars(Active_Car).Theory.wheel(3).Mass * (cars(Active_Car).Theory.wheel(3).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(3).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).Y))

        'Body's Inertia....
        Dim B(3) As Single
        B(1) = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) / 5
        B(2) = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) / 5
        B(3) = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) / 5

        'Huygens/body
        B(1) += cars(Active_Car).Theory.Body.Mass * (cars(Active_Car).Theory.Body.Offset.Y ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.Y ^ 2) + _
        cars(Active_Car).Theory.Body.Offset.Z ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.Z ^ 2))

        B(2) += cars(Active_Car).Theory.Body.Mass * (cars(Active_Car).Theory.Body.Offset.X ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.X ^ 2) + _
      cars(Active_Car).Theory.Body.Offset.Z ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.Z ^ 2))

        B(3) += cars(Active_Car).Theory.Body.Mass * (cars(Active_Car).Theory.Body.Offset.Y ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.Y ^ 2) + _
      cars(Active_Car).Theory.Body.Offset.X ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.X ^ 2))

        '
        TextBox1.Text = B(1) + I(1, 1) + I(2, 1) + I(3, 1) + I(4, 1)
        TextBox2.Text = 0
        TextBox3.Text = 0

        TextBox4.Text = 0
        TextBox5.Text = B(2) + I(1, 2) + I(2, 2) + I(3, 2) + I(4, 2)
        TextBox6.Text = 0

        TextBox7.Text = 0
        TextBox8.Text = 0
        TextBox9.Text = B(3) + I(1, 3) + I(2, 3) + I(3, 3) + I(4, 3)
    End Sub

    Private Sub Button42_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button42.Click
        Dim X = Abs(cars(Active_Car).models.BODY.BoundingBox.maxX - cars(Active_Car).models.BODY.BoundingBox.MinX) / 2
        Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY - cars(Active_Car).models.BODY.BoundingBox.MinY) / 4
        Dim Z = Abs(cars(Active_Car).models.BODY.BoundingBox.maxZ - cars(Active_Car).models.BODY.BoundingBox.MinZ) / 2

        Dim H1 = Abs(cars(Active_Car).models.Wheel(0).BoundingBox.maxX - cars(Active_Car).models.Wheel(0).BoundingBox.MinX) ' / 2
        Dim H2 = Abs(cars(Active_Car).models.Wheel(1).BoundingBox.maxX - cars(Active_Car).models.Wheel(1).BoundingBox.MinX) ' / 2
        Dim H3 = Abs(cars(Active_Car).models.Wheel(2).BoundingBox.maxX - cars(Active_Car).models.Wheel(2).BoundingBox.MinX) ' / 2
        Dim H4 = Abs(cars(Active_Car).models.Wheel(3).BoundingBox.maxX - cars(Active_Car).models.Wheel(3).BoundingBox.MinX) ' / 2
        '   Dim H = (H1 + H2 + H3 + H4) / 4

        Dim R1 = Abs(cars(Active_Car).models.Wheel(0).BoundingBox.maxZ - cars(Active_Car).models.Wheel(0).BoundingBox.MinZ) / 2
        Dim R2 = Abs(cars(Active_Car).models.Wheel(1).BoundingBox.maxZ - cars(Active_Car).models.Wheel(1).BoundingBox.MinZ) / 2
        Dim R3 = Abs(cars(Active_Car).models.Wheel(2).BoundingBox.maxZ - cars(Active_Car).models.Wheel(2).BoundingBox.MinZ) / 2
        Dim R4 = Abs(cars(Active_Car).models.Wheel(3).BoundingBox.maxZ - cars(Active_Car).models.Wheel(3).BoundingBox.MinZ) / 2

        '  Dim R = (R1 + R2 + R3 + R4) / 4


        Dim I(4, 3) As Single

        'Ixx Iyy Izz
        '            W1
        '            w2
        '            w3
        '            w4

        'For Each Wheel
        I(1, 1) = cars(Active_Car).Theory.wheel(0).Mass * R1 ^ 2 / 2 + cars(Active_Car).Theory.wheel(0).Mass * Sqrt(H1) ^ 3 / 12
        I(2, 1) = cars(Active_Car).Theory.wheel(1).Mass * R2 ^ 2 / 2 + cars(Active_Car).Theory.wheel(1).Mass * Sqrt(H2) ^ 3 / 12
        I(3, 1) = cars(Active_Car).Theory.wheel(2).Mass * R3 ^ 2 / 2 + cars(Active_Car).Theory.wheel(2).Mass * Sqrt(H3) ^ 3 / 12
        I(4, 1) = cars(Active_Car).Theory.wheel(3).Mass * R4 ^ 2 / 2 + cars(Active_Car).Theory.wheel(3).Mass * Sqrt(H4) ^ 3 / 12

        I(1, 2) = I(1, 1)
        I(2, 2) = I(2, 1)
        I(3, 2) = I(3, 1)
        I(4, 2) = I(4, 1)


        I(1, 3) = cars(Active_Car).Theory.wheel(0).Mass * R1 ^ 2
        I(2, 3) = cars(Active_Car).Theory.wheel(1).Mass * R2 ^ 2
        I(3, 3) = cars(Active_Car).Theory.wheel(2).Mass * R3 ^ 2
        I(4, 3) = cars(Active_Car).Theory.wheel(3).Mass * R4 ^ 2


        'Huygens
        I(1, 1) += cars(Active_Car).Theory.wheel(0).Mass * (cars(Active_Car).Theory.wheel(0).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).Y) + _
                                                            cars(Active_Car).Theory.wheel(0).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).Z))
        I(2, 1) += cars(Active_Car).Theory.wheel(1).Mass * (cars(Active_Car).Theory.wheel(1).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).Y) + _
                                                          cars(Active_Car).Theory.wheel(1).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).Z))
        I(3, 1) += cars(Active_Car).Theory.wheel(2).Mass * (cars(Active_Car).Theory.wheel(2).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).Y) + _
                                                          cars(Active_Car).Theory.wheel(2).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).Z))
        I(4, 1) += cars(Active_Car).Theory.wheel(3).Mass * (cars(Active_Car).Theory.wheel(3).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).Y) + _
                                                          cars(Active_Car).Theory.wheel(3).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).Z))

        I(1, 2) += cars(Active_Car).Theory.wheel(0).Mass * (cars(Active_Car).Theory.wheel(0).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(0).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).Z))
        I(2, 2) += cars(Active_Car).Theory.wheel(1).Mass * (cars(Active_Car).Theory.wheel(1).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(1).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).Z))
        I(3, 2) += cars(Active_Car).Theory.wheel(2).Mass * (cars(Active_Car).Theory.wheel(2).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(2).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).Z))
        I(4, 2) += cars(Active_Car).Theory.wheel(3).Mass * (cars(Active_Car).Theory.wheel(3).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(3).Offset(1).Z ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).Z))


        I(1, 3) += cars(Active_Car).Theory.wheel(0).Mass * (cars(Active_Car).Theory.wheel(0).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).X) + _
                                                  cars(Active_Car).Theory.wheel(0).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(0).Offset(1).Y))
        I(2, 3) += cars(Active_Car).Theory.wheel(1).Mass * (cars(Active_Car).Theory.wheel(1).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(1).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(1).Offset(1).Y))
        I(3, 3) += cars(Active_Car).Theory.wheel(2).Mass * (cars(Active_Car).Theory.wheel(2).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(2).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(2).Offset(1).Y))
        I(4, 3) += cars(Active_Car).Theory.wheel(3).Mass * (cars(Active_Car).Theory.wheel(3).Offset(1).X ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).X) + _
                                                          cars(Active_Car).Theory.wheel(3).Offset(1).Y ^ 2 * Sign(cars(Active_Car).Theory.wheel(3).Offset(1).Y))

        'Body's Inertia....
        Dim B(3) As Single
        B(1) = NumericUpDown13.Value * (Y ^ 2 + Z ^ 2) / 5
        B(2) = NumericUpDown13.Value * (X ^ 2 + Z ^ 2) / 5
        B(3) = NumericUpDown13.Value * (X ^ 2 + Y ^ 2) / 5

        'Huygens/body
        B(1) += cars(Active_Car).Theory.Body.Mass * (cars(Active_Car).Theory.Body.Offset.Y ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.Y ^ 2) + _
        cars(Active_Car).Theory.Body.Offset.Z ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.Z ^ 2))

        B(2) += cars(Active_Car).Theory.Body.Mass * (cars(Active_Car).Theory.Body.Offset.X ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.X ^ 2) + _
      cars(Active_Car).Theory.Body.Offset.Z ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.Z ^ 2))

        B(3) += cars(Active_Car).Theory.Body.Mass * (cars(Active_Car).Theory.Body.Offset.Y ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.Y ^ 2) + _
      cars(Active_Car).Theory.Body.Offset.X ^ 2 * Sign(cars(Active_Car).Theory.Body.Offset.X ^ 2))

        '
        TextBox1.Text = B(1) + I(1, 1) + I(2, 1) + I(3, 1) + I(4, 1)
        TextBox2.Text = 0
        TextBox3.Text = 0

        TextBox4.Text = 0
        TextBox5.Text = B(2) + I(1, 2) + I(2, 2) + I(3, 2) + I(4, 2)
        TextBox6.Text = 0

        TextBox7.Text = 0
        TextBox8.Text = 0
        TextBox9.Text = B(3) + I(1, 3) + I(2, 3) + I(3, 3) + I(4, 3)
    End Sub

    Private Sub Button43_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button43.Click
        TextBox1.Text = CSng(TextBox10.Text) * NumericUpDown13.Value + (NumericUpDown13.Value - 1) * CSng(TextBox11.Text) * NumericUpDown13.Value
        TextBox2.Text = 0
        TextBox3.Text = 0

        TextBox4.Text = 0
        TextBox5.Text = TextBox1.Text * 1.5
        TextBox6.Text = 0

        TextBox7.Text = 0
        TextBox8.Text = 0
        TextBox9.Text = TextBox1.Text * 0.5
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub
End Class