Imports System.Windows.Forms
Imports OpenTK

Public Class Wheels

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.wheel(0).modelNumber)

        '   PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.wheel(0)
        PICK_PRM.Show()


    End Sub
  

    Dim Loading As Boolean = True
    Sub DOLOAD()

        ' Loading = True


        If ModWheel.Count = 0 Then ModWheel.Add(0) 'init?
        'Body's model
        '  If cars(Active_Car).Theory.wheel(0).modelNumber <> -1 Then _
        '  Button1.Text = cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.wheel(0).modelNumber)


        Dim ActiveWheel = cars(Active_Car).Theory.wheel(ModWheel(0))


        ' offset 
        PrintVecToObjectsV(ActiveWheel.Offset(1), NumericUpDown3, NumericUpDown2, NumericUpDown1)
        PrintVecToObjectsV(ActiveWheel.Offset(2), NumericUpDown6, NumericUpDown5, NumericUpDown4)





        'splitters
        For j = 0 To ModWheel.Count - 1

            UpdateSplitter(ModWheel(j), cars(Active_Car).models.Wheel(ModWheel(j)), cars(Active_Car).Theory.wheel(ModWheel(j)).Offset(2) * Zoom)
        Next


        With cars(Active_Car).Theory.wheel(ModWheel(0))
            steerRat.Value = .SteerRatio
            engineRat.Value = .EngineRatio
            Rad.Value = .Radius
            Mass.Value = .Mass
            skidw.Value = .SkidWidth
            toein.Value = .ToeInn
            axlefric.Value = .AxleFriction
            grip.Value = .Grip
            statfric.Value = .StaticFriction
            kinefric.Value = .KineticFriction
        End With

        '   ShowAxis()



        ' UpdateAxis(cars(Active_Car).models.BODY)


        MCheckbox1.Checked = ActiveWheel.IsPresent
        MCheckbox2.Checked = ActiveWheel.IsPowered
        MCheckbox3.Checked = ActiveWheel.IsTurnable

        Loading = False

    End Sub
    Sub unload()

    End Sub

    Private Sub Button45_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button45.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.wheel(1).modelNumber)

        '   PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.wheel(1)
        PICK_PRM.Show()

    End Sub

    Private Sub Button46_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button46.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.wheel(2).modelNumber)
        '  PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.wheel(2)
        PICK_PRM.Show()

    End Sub

    Private Sub Button47_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button47.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.wheel(3).modelNumber)
        ' PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.wheel(3)
        PICK_PRM.Show()

    End Sub

    Private Sub Button48_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button48.Click

        Dim index(3) As Integer  'index of every
        Dim isset(3) As Boolean

        'init 
        For i = 0 To 3
            CheckModelNumberAndFix(cars(Active_Car).Theory.wheel(i).modelNumber)
            index(i) = cars(Active_Car).Theory.wheel(i).modelNumber
        Next

        'one wheel for 'em all!

        If IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*prm").Count = 1 Then
            For i = 0 To 3
                cars(Active_Car).Theory.MainInfos.Model(index(i)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*prm")(0), RVPATH, ""), 2)
                cars(Active_Car).Theory.wheel(i).modelNumber = index(i)
            Next
            GoTo finished
        End If


        'APPROACH BY R,L
        'L
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*l*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(0)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*l*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.wheel(0).modelNumber = index(0)
            cars(Active_Car).Theory.MainInfos.Model(index(2)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*l*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.wheel(2).modelNumber = index(2)
        End If
        'R
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*r*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(1)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*r*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.wheel(1).modelNumber = index(1)
            cars(Active_Car).Theory.MainInfos.Model(index(3)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*r*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.wheel(2).modelNumber = index(3)
        End If





        'APPROACH BY FR,FL,BR,BL
        'FL
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*fl*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(0)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*fl*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.wheel(0).modelNumber = index(0)
        End If


        'FR
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*fr*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(1)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*fr*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.wheel(1).modelNumber = index(1)
        End If



        'bl
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*bl*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(2)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*bl*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.wheel(2).modelNumber = index(2)
        End If


        'br
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*br*").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(3)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "wheel*br*")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.wheel(3).modelNumber = index(3)
        End If




finished:
        CarBrowser.ReLoadOneCar()


    End Sub

    Private Sub Button49_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button49.Click
        For i = 0 To 3
            cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).OriginalTheory.wheel(i).modelNumber) = cars(Active_Car).OriginalTheory.MainInfos.Model(cars(Active_Car).OriginalTheory.wheel(i).modelNumber)

            cars(Active_Car).Theory.wheel(i).modelNumber = cars(Active_Car).OriginalTheory.wheel(i).modelNumber
        Next

        CarBrowser.ReLoadOneCar()
    End Sub
    Dim ModWheel As New List(Of Integer)
    Private Sub RadioButton1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton1.CheckedChanged, RadioButton2.CheckedChanged, _
        RadioButton3.CheckedChanged, RadioButton4.CheckedChanged, RadioButton5.CheckedChanged, RadioButton6.CheckedChanged, RadioButton7.CheckedChanged, _
    RadioButton8.CheckedChanged, RadioButton9.CheckedChanged
        If Loading Then Exit Sub
        ModWheel.Clear()
        If RadioButton1.Checked Then selected.Text = "All" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {0, 1, 2, 3})
        If RadioButton2.Checked Then selected.Text = "Left (Front+Rear)" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {0, 2})
        If RadioButton3.Checked Then selected.Text = "Right (Front+Rear)" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {1, 3})
        If RadioButton4.Checked Then selected.Text = "Forward (Left+Right)" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {0, 1})
        If RadioButton5.Checked Then selected.Text = "Rear (Left+Right)" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {2, 3})
        If RadioButton6.Checked Then selected.Text = "Forward Left" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {0})
        If RadioButton7.Checked Then selected.Text = "Forward Right" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {1})
        If RadioButton8.Checked Then selected.Text = "Rear Left" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {2})
        If RadioButton9.Checked Then selected.Text = "Rear right" : ModWheel.Clear() : ModWheel.AddRange(New Integer() {3})

        'reload
        DOLOAD()

        For i = 0 To 3
            '     Axis_Grid_Etc.SPL(i).isVisible = MCheckbox4.Checked And ModWheel.Contains(i)
        Next



    End Sub

    Private Sub TabPage1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TabPage1.Click

    End Sub

    Private Sub Offset_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown3.ValueChanged, NumericUpDown2.ValueChanged, NumericUpDown1.ValueChanged
        Patch(sender)
        If Loading Or Not Initializd Then Exit Sub
        Dim Fact = New Integer() {1, 1}


        Select Case ModWheel(0)
            Case 0 : Fact = New Integer() {-1, 1}
            Case 1 : Fact = New Integer() {1, 1}
            Case 2 : Fact = New Integer() {-1, -1}
            Case 3 : Fact = New Integer() {1, -1}
        End Select


        'If ModWheel.Count = 4 Then
        For j = 0 To ModWheel.Count - 1
            cars(Active_Car).Theory.wheel(ModWheel(j)).Offset(1) = New Vector3d(If(ModWheel(j) Mod 2 = 0, Fact(0) * -NumericUpDown3.Value, Fact(0) * NumericUpDown3.Value), _
                                                                          NumericUpDown2.Value, _
                                                                     If(ModWheel(j) < 2, Fact(1) * NumericUpDown1.Value, -Fact(1) * NumericUpDown1.Value))
            cars(Active_Car).models.Wheel(ModWheel(j)).Position = cars(Active_Car).Theory.wheel(ModWheel(j)).Offset(1) * Zoom
        Next
        '  ElseIf ModWheel.Count = 2 Then


        '  End If

    End Sub

    Public Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()
        NumericUpDown3.Minimum = -1000
        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        PrintVecToObjectsV(cars(Active_Car).OriginalTheory.wheel(ModWheel(0)).Offset(1), NumericUpDown3, NumericUpDown2, NumericUpDown1)
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        PrintVecToObjectsV(cars(Active_Car).OriginalTheory.wheel(ModWheel(0)).Offset(2), NumericUpDown6, NumericUpDown5, NumericUpDown4)
    End Sub

    Private Sub Info_Changed() Handles MCheckbox1.CheckedChanged, MCheckbox2.CheckedChanged, MCheckbox3.CheckedChanged
        'present,powered,turnable
        If Loading Then Exit Sub

        For i = 0 To ModWheel.Count - 1
            cars(Active_Car).Theory.wheel(ModWheel(i)).IsPresent = MCheckbox1.Checked
            cars(Active_Car).Theory.wheel(ModWheel(i)).IsPowered = MCheckbox2.Checked
            cars(Active_Car).Theory.wheel(ModWheel(i)).IsTurnable = MCheckbox3.Checked
            '  If cars(Active_Car).models.Wheel(i) IsNot Nothing Then cars(Active_Car).models.Wheel(i).isVisible = cars(Active_Car).Theory.wheel(ModWheel(i)).IsPresent
        Next

        For i = 0 To 3
            cars(Active_Car).models.Wheel(i).isVisible = cars(Active_Car).Theory.wheel(i).IsPresent
        Next
    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click
        Rad.Value = 0.5 * (cars(Active_Car).models.Wheel(ModWheel(0)).BoundingBox.maxY - cars(Active_Car).models.Wheel(ModWheel(0)).BoundingBox.MinY)
    End Sub

    Private Sub Button10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button10.Click
        skidw.Value = (cars(Active_Car).models.Wheel(ModWheel(0)).BoundingBox.maxX - cars(Active_Car).models.Wheel(ModWheel(0)).BoundingBox.MinX)
    End Sub

    Private Sub Offset2_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown6.ValueChanged, NumericUpDown5.ValueChanged, NumericUpDown4.ValueChanged
        Patch(sender)
        If Loading Or Not Initializd Then Exit Sub
        Dim Fact = New Integer() {1, 1}

        Select Case ModWheel(0)
            Case 0 : Fact = New Integer() {-1, 1}
            Case 1 : Fact = New Integer() {1, 1}
            Case 2 : Fact = New Integer() {-1, -1}
            Case 3 : Fact = New Integer() {1, -1}
        End Select


        'If ModWheel.Count = 4 Then
        For j = 0 To ModWheel.Count - 1
            cars(Active_Car).Theory.wheel(ModWheel(j)).Offset(2) = New Vector3d(If(ModWheel(j) Mod 2 = 0, Fact(0) * -NumericUpDown6.Value, Fact(0) * NumericUpDown6.Value), _
                                                                          NumericUpDown5.Value, _
                                                                     If(ModWheel(j) < 2, Fact(1) * NumericUpDown4.Value, -Fact(1) * NumericUpDown4.Value))
            ' cars(Active_Car).models.Wheel(ModWheel(j)).Position = cars(Active_Car).Theory.wheel(ModWheel(j)).Offset(1) * Zoom
            UpdateSplitter(ModWheel(j), cars(Active_Car).models.Wheel(ModWheel(j)), cars(Active_Car).Theory.wheel(ModWheel(j)).Offset(2) * Zoom)
        Next

    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        Tablepad.Label1.Text = "Table Pad (Wheel Offset)"
        Tablepad.Current_Editing_Mode = Tablepad.CurEdMod.wheels
        Panel3.Enabled = False
        Tablepad.Show()

    End Sub

    Private Sub GroupBox2_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GroupBox2.Enter

    End Sub

    Private Sub MCheckbox() Handles MCheckbox4.CheckedChanged

        For i = 0 To 3
            Axis_Grid_Etc.SPL(i).isVisible = MCheckbox4.Checked And ModWheel.Contains(i)
        Next




    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        PrintVecToObjectsV(cars(Active_Car).models.Wheel(ModWheel(0)).BoundingBox.getCenter, NumericUpDown6, NumericUpDown5, NumericUpDown4)
    End Sub

    Private Sub dynamic_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles steerRat.ValueChanged, engineRat.ValueChanged, Rad.ValueChanged, _
    Mass.ValueChanged, skidw.ValueChanged, toein.ValueChanged, axlefric.ValueChanged, grip.ValueChanged, statfric.ValueChanged, kinefric.ValueChanged
        If Not Initializd Or Loading Then Exit Sub

        For i = 0 To ModWheel.Count - 1
            With cars(Active_Car).Theory.wheel(ModWheel(i))
                .SteerRatio = steerRat.Value
                .EngineRatio = engineRat.Value
                .Radius = Rad.Value
                .Mass = Mass.Value
                .SkidWidth = skidw.Value
                .ToeInn = toein.Value
                .AxleFriction = axlefric.Value
                .Grip = grip.Value
                .StaticFriction = statfric.Value
                .KineticFriction = kinefric.Value
            End With
        Next


    End Sub


    Private Sub MCheckbox1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MCheckbox1.Load

    End Sub

    Private Sub Button9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button9.Click

    End Sub
End Class
