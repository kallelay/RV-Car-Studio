Imports OpenTK
Imports System.Math

Public Class Suspens_

    Private Sub RadioButton6_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton6.CheckedChanged

    End Sub

    Private Sub Suspens__Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Private Sub Panel3_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel3.Paint

    End Sub

    Public Sub DoLoad()

        '--------- SPRINGS--------------------
        If ModSpring.Count = 0 Then ModSpring.Add(0) 'init?

        Dim ActiveSpring = cars(Active_Car).Theory.Spring(ModSpring(0))


        ' offset 
        PrintVecToObjectsV(ActiveSpring.Offset, NumericUpDown3, NumericUpDown2, NumericUpDown1)
        HScrollBar1.Value = Int(ActiveSpring.Length)
        TrackBar6.Value = (ActiveSpring.Length - Int(ActiveSpring.Length)) * 1000


        For i = 0 To 3
            SpringsP(i) = cars(Active_Car).Theory.Spring(i).modelNumber 'restore if present

        Next

        'splitters
        For j = 0 To ModSpring.Count - 1

            UpdateSplitter(ModSpring(j), cars(Active_Car).models.Wheel(ModSpring(j)), cars(Active_Car).Theory.wheel(ModSpring(j)).Offset(2) * Zoom)
        Next


        With cars(Active_Car).Theory.Spring(ModSpring(0))
            damping.Value = .Damping
            stiffness.Value = .Stiffness
            Res.Value = .Restitution
        End With

        If SpringsP(ModSpring(0)) > -1 Then CheckBox1.Checked = True Else CheckBox1.Checked = False


        Label7.Text = "Length (" & cars(Active_Car).Theory.Spring(ModSpring(0)).Length & ")"


        '--------- AXLES--------------------
        If ModAxle.Count = 0 Then ModAxle.Add(0) 'init?

        Dim Activeaxle = cars(Active_Car).Theory.Axle(ModAxle(0))


        ' offset 
        PrintVecToObjectsV(Activeaxle.offSet, NumericUpDown6, NumericUpDown5, NumericUpDown4)
        HScrollBar2.Value = Int(Activeaxle.Length)
        TrackBar7.Value = (Activeaxle.Length - Int(Activeaxle.Length)) * 1000


        For i = 0 To 3
            AxlesP(i) = cars(Active_Car).Theory.Axle(i).modelNumber 'restore if present

        Next

        'splitters
        For j = 0 To ModAxle.Count - 1

            UpdateSplitter(ModAxle(j), cars(Active_Car).models.Wheel(ModAxle(j)), cars(Active_Car).Theory.wheel(ModAxle(j)).Offset(2) * Zoom)
        Next




        If AxlesP(ModAxle(0)) > -1 Then CheckBox2.Checked = True Else CheckBox2.Checked = False


        Label12.Text = "Length (" & cars(Active_Car).Theory.Axle(ModAxle(0)).Length & ")"





        Loading = False

    End Sub
    Public Sub UnLoad()

    End Sub
    Dim SpringsP(4) As Integer
    '----------------------------- SPRINGS -----------------------
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.Spring(0).modelNumber)

        ' PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Spring(0)
        PICK_PRM.Show()
        SpringsP(0) = cars(Active_Car).Theory.Spring(0).modelNumber

    End Sub

    Private Sub Button45_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button45.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.Spring(1).modelNumber)

        ' PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Spring(1)
        PICK_PRM.Show()
        SpringsP(1) = cars(Active_Car).Theory.Spring(1).modelNumber
    End Sub

    Private Sub Button46_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button46.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.Spring(2).modelNumber)

        '  PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Spring(2)
        PICK_PRM.Show()
        SpringsP(2) = cars(Active_Car).Theory.Spring(2).modelNumber
    End Sub

    Private Sub Button47_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button47.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.Spring(3).modelNumber)

        ' PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Spring(3)
        PICK_PRM.Show()
        SpringsP(3) = cars(Active_Car).Theory.Spring(3).modelNumber
    End Sub

    Private Sub Button48_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button48.Click

        Dim index(3) As Integer  'index of every
        Dim isset(3) As Boolean

        'init 
        For i = 0 To 3
            isset(i) = False

            CheckModelNumberAndFix(cars(Active_Car).Theory.Spring(i).modelNumber)
            index(i) = cars(Active_Car).Theory.Spring(i).modelNumber
        Next

        'one spring for 'em all!

        If IO.Directory.GetFiles(cars(Active_Car).Path, "spring*prm").Count = 1 Then
            For i = 0 To 3
                cars(Active_Car).Theory.MainInfos.Model(index(i)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*prm")(0), RVPATH, ""), 2)
                cars(Active_Car).Theory.Spring(i).modelNumber = index(i)
            Next
            GoTo finished
        End If


        'APPROACH BY R,L
        'L
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "spring*l*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(0)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*l*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Spring(0).modelNumber = index(0)
            cars(Active_Car).Theory.MainInfos.Model(index(2)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*l*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Spring(2).modelNumber = index(2)
        End If
        'R
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "spring*r*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(1)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*r*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Spring(1).modelNumber = index(1)
            cars(Active_Car).Theory.MainInfos.Model(index(3)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*r*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Spring(2).modelNumber = index(3)
        End If





        'APPROACH BY FR,FL,BR,BL
        'FL
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "spring*fl*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(0)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*fl*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Spring(0).modelNumber = index(0)
        End If


        'FR
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "spring*fr*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(1)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*fr*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Spring(1).modelNumber = index(1)
        End If



        'bl
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "spring*bl*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(2)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*bl*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Spring(2).modelNumber = index(2)
        End If


        'br
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "spring*br*").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(3)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "spring*br*")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Spring(3).modelNumber = index(3)
        End If




finished:
        CarBrowser.ReLoadOneCar()


    End Sub
    Dim ModSpring As New List(Of Integer)
    Dim Loading = True

    Private Sub SPRINGS_CHANGED(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton1.CheckedChanged, RadioButton2.CheckedChanged, _
         RadioButton3.CheckedChanged, RadioButton4.CheckedChanged, RadioButton5.CheckedChanged, RadioButton6.CheckedChanged, RadioButton7.CheckedChanged, _
     RadioButton8.CheckedChanged, RadioButton9.CheckedChanged
        If Loading Then Exit Sub
        ModSpring.Clear()
        If RadioButton1.Checked Then selected.Text = "All" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {0, 1, 2, 3})
        If RadioButton2.Checked Then selected.Text = "Left (Front+Rear)" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {0, 2})
        If RadioButton3.Checked Then selected.Text = "Right (Front+Rear)" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {1, 3})
        If RadioButton4.Checked Then selected.Text = "Forward (Left+Right)" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {0, 1})
        If RadioButton5.Checked Then selected.Text = "Rear (Left+Right)" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {2, 3})
        If RadioButton6.Checked Then selected.Text = "Forward Left" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {0})
        If RadioButton7.Checked Then selected.Text = "Forward Right" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {1})
        If RadioButton8.Checked Then selected.Text = "Rear Left" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {2})
        If RadioButton9.Checked Then selected.Text = "Rear right" : ModSpring.Clear() : ModSpring.AddRange(New Integer() {3})

        'reload
        DoLoad()

        For i = 0 To 3

            '     Axis_Grid_Etc.SPL(i).isVisible = MCheckbox4.Checked And Modspring.Contains(i)
        Next
        If SpringsP(ModSpring(0)) > -1 Then CheckBox1.Checked = True Else CheckBox1.Checked = False


        Label7.Text = "Length (" & cars(Active_Car).Theory.Spring(ModSpring(0)).Length & ")"

    End Sub
    Dim Fact = New Integer() {1, 1}

    Private Sub Offset_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown3.ValueChanged, NumericUpDown2.ValueChanged, NumericUpDown1.ValueChanged
        Patch(sender)
        If Loading Or Not Initializd Then Exit Sub


        Select Case ModSpring(0)
            Case 0 : Fact = New Integer() {-1, 1}
            Case 1 : Fact = New Integer() {1, 1}
            Case 2 : Fact = New Integer() {-1, -1}
            Case 3 : Fact = New Integer() {1, -1}
        End Select


        'If Modspring.Count = 4 Then
        For j = 0 To ModSpring.Count - 1
            cars(Active_Car).Theory.Spring(ModSpring(j)).Offset = New Vector3d(If(ModSpring(j) Mod 2 = 0, Fact(0) * -NumericUpDown3.Value, Fact(0) * NumericUpDown3.Value), _
                                                                          NumericUpDown2.Value, _
                                                                     If(ModSpring(j) < 2, Fact(1) * NumericUpDown1.Value, -Fact(1) * NumericUpDown1.Value))
            If cars(Active_Car).models.Spring(ModSpring(j)) IsNot Nothing Then cars(Active_Car).models.Spring(ModSpring(j)).Position = cars(Active_Car).Theory.Spring(ModSpring(j)).Offset * Zoom
        Next
        '  ElseIf Modspring.Count = 2 Then

        ShowSpringCorrect()

        '  End If

    End Sub

    Private Sub HScrollBar1_Vch() Handles HScrollBar1.ValueChanged, TrackBar6.ValueChanged
        If Loading Or Not Initializd Then Exit Sub


        Select Case ModSpring(0)
            Case 0 : Fact = New Integer() {-1, 1}
            Case 1 : Fact = New Integer() {1, 1}
            Case 2 : Fact = New Integer() {-1, -1}
            Case 3 : Fact = New Integer() {1, -1}
        End Select


        'If Modspring.Count = 4 Then
        For j = 0 To ModSpring.Count - 1
            cars(Active_Car).Theory.Spring(ModSpring(j)).Length = HScrollBar1.Value + TrackBar6.Value / 1000

        Next
        ShowSpringCorrect()
        '  ElseIf Modspring.Count = 2 Then

        Label7.Text = "Length (" & cars(Active_Car).Theory.Spring(ModSpring(0)).Length & ")"

    End Sub



    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Dim v = ModSpring(0)
        Dim ml = cars(Active_Car).models.Spring(v).BoundingBox.maxY - cars(Active_Car).models.Spring(v).BoundingBox.MinY

        Dim tl = (cars(Active_Car).Theory.Spring(v).Offset - cars(Active_Car).Theory.wheel(v).Offset(1)).Length ' * Zoom
        HScrollBar1.Value = Int(ml)
        TrackBar6.Value = Max(0, (ml - HScrollBar1.Value) * 1000 - 500)


    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Dim ActiveSpring = cars(Active_Car).OriginalTheory.Spring(ModSpring(0))


        HScrollBar1.Value = Int(ActiveSpring.Length)
        TrackBar6.Value = (ActiveSpring.Length - Int(ActiveSpring.Length)) * 1000


    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        PrintVecToObjectsV(cars(Active_Car).OriginalTheory.Spring(ModSpring(0)).Offset, NumericUpDown3, NumericUpDown2, NumericUpDown1)
    End Sub

    Private Sub Button13_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button13.Click
        stiffness.Value = cars(Active_Car).OriginalTheory.Spring(ModSpring(0)).Stiffness
    End Sub

    Private Sub Button14_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button14.Click
        damping.Value = cars(Active_Car).OriginalTheory.Spring(ModSpring(0)).Damping
    End Sub

    Private Sub Button15_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button15.Click
        Res.Value = cars(Active_Car).OriginalTheory.Spring(ModSpring(0)).Restitution
    End Sub


    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox1.CheckedChanged
        For i = 0 To ModSpring.Count - 1

            If Not CheckBox1.Checked Then
                cars(Active_Car).Theory.Spring(ModSpring(i)).modelNumber = -1
                If cars(Active_Car).models.Spring(ModSpring(i)) IsNot Nothing Then cars(Active_Car).models.Spring(ModSpring(i)).isVisible = False
            Else
                cars(Active_Car).Theory.Spring(ModSpring(i)).modelNumber = SpringsP(ModSpring(i))
                If cars(Active_Car).models.Spring(ModSpring(i)) IsNot Nothing Then cars(Active_Car).models.Spring(ModSpring(i)).isVisible = True
            End If
        Next
    End Sub

    Private Sub stiffness_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles stiffness.ValueChanged
        For i = 0 To ModSpring.Count - 1
            cars(Active_Car).Theory.Spring(ModSpring(i)).Stiffness = stiffness.Value
        Next
    End Sub

    Private Sub damping_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles damping.ValueChanged
        For i = 0 To ModSpring.Count - 1
            cars(Active_Car).Theory.Spring(ModSpring(i)).Damping = damping.Value
        Next
    End Sub

    Private Sub Res_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Res.ValueChanged
        For i = 0 To ModSpring.Count - 1
            cars(Active_Car).Theory.Spring(ModSpring(i)).Restitution = Res.Value
        Next
    End Sub
    Dim AxlesP(4) As Integer
    '------------- AXLES-----------------
    Private Sub Button11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button11.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.Axle(0).modelNumber)

        ' PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Axle(0)
        PICK_PRM.Show()
        AxlesP(0) = cars(Active_Car).Theory.Axle(0).modelNumber

    End Sub

    Private Sub Button10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button10.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.Axle(1).modelNumber)

        '  PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Axle(1)
        PICK_PRM.Show()
        AxlesP(1) = cars(Active_Car).Theory.Axle(1).modelNumber

    End Sub

    Private Sub Button9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button9.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.Axle(2).modelNumber)

        ' PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Axle(2)
        PICK_PRM.Show()
        AxlesP(2) = cars(Active_Car).Theory.Axle(2).modelNumber

    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click
        CheckModelNumberAndFix(cars(Active_Car).Theory.Axle(3).modelNumber)

        ' PICK_PRM.PreviewMode = False
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Axle(3)
        PICK_PRM.Show()
        AxlesP(3) = cars(Active_Car).Theory.Axle(3).modelNumber

    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click

        Dim index(3) As Integer  'index of every
        Dim isset(3) As Boolean

        'init 
        For i = 0 To 3
            isset(i) = False

            CheckModelNumberAndFix(cars(Active_Car).Theory.Axle(i).modelNumber)
            index(i) = cars(Active_Car).Theory.Axle(i).modelNumber
        Next

        'one axle for 'em all!

        If IO.Directory.GetFiles(cars(Active_Car).Path, "axle*prm").Count = 1 Then
            For i = 0 To 3
                cars(Active_Car).Theory.MainInfos.Model(index(i)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*prm")(0), RVPATH, ""), 2)
                cars(Active_Car).Theory.Axle(i).modelNumber = index(i)
            Next
            GoTo finished
        End If


        'APPROACH BY R,L
        'L
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "axle*l*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(0)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*l*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Axle(0).modelNumber = index(0)
            cars(Active_Car).Theory.MainInfos.Model(index(2)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*l*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Axle(2).modelNumber = index(2)
        End If
        'R
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "axle*r*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(1)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*r*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Axle(1).modelNumber = index(1)
            cars(Active_Car).Theory.MainInfos.Model(index(3)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*r*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Axle(2).modelNumber = index(3)
        End If





        'APPROACH BY FR,FL,BR,BL
        'FL
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "axle*fl*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(0)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*fl*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Axle(0).modelNumber = index(0)
        End If


        'FR
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "axle*fr*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(1)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*fr*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Axle(1).modelNumber = index(1)
        End If



        'bl
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "axle*bl*prm").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(2)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*bl*prm")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Axle(2).modelNumber = index(2)
        End If


        'br
        If Not IO.Directory.GetFiles(cars(Active_Car).Path, "axle*br*").Count = 0 Then
            cars(Active_Car).Theory.MainInfos.Model(index(3)) = Mid(Replace(IO.Directory.GetFiles(cars(Active_Car).Path, "axle*br*")(0), RVPATH, ""), 2)
            cars(Active_Car).Theory.Axle(3).modelNumber = index(3)
        End If




finished:
        CarBrowser.ReLoadOneCar()
    End Sub
    Dim ModAxle As New List(Of Integer)
    Private Sub AXLES_CHANGED(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton18.CheckedChanged, RadioButton17.CheckedChanged, _
           RadioButton16.CheckedChanged, RadioButton15.CheckedChanged, RadioButton14.CheckedChanged, RadioButton13.CheckedChanged, RadioButton12.CheckedChanged, _
       RadioButton11.CheckedChanged, RadioButton10.CheckedChanged
        If Loading Then Exit Sub
        ModAxle.Clear()
        If RadioButton18.Checked Then selected.Text = "All" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {0, 1, 2, 3})
        If RadioButton16.Checked Then selected.Text = "Left (Front+Rear)" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {0, 2})
        If RadioButton15.Checked Then selected.Text = "Right (Front+Rear)" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {1, 3})
        If RadioButton17.Checked Then selected.Text = "Forward (Left+Right)" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {0, 1})
        If RadioButton13.Checked Then selected.Text = "Rear (Left+Right)" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {2, 3})
        If RadioButton12.Checked Then selected.Text = "Forward Left" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {0})
        If RadioButton11.Checked Then selected.Text = "Forward Right" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {1})
        If RadioButton10.Checked Then selected.Text = "Rear Left" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {2})
        If RadioButton14.Checked Then selected.Text = "Rear right" : ModAxle.Clear() : ModAxle.AddRange(New Integer() {3})

        'reload
        DoLoad()

        For i = 0 To 3

            '     Axis_Grid_Etc.SPL(i).isVisible = MCheckbox4.Checked And Modaxle.Contains(i)
        Next
        If AxlesP(ModAxle(0)) > -1 Then CheckBox1.Checked = True Else CheckBox1.Checked = False


        Label7.Text = "Length (" & cars(Active_Car).Theory.Axle(ModAxle(0)).Length & ")"

    End Sub

    Private Sub NumericUpDown6_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown6.ValueChanged, NumericUpDown5.ValueChanged, NumericUpDown4.ValueChanged

        Patch(sender)
        If Loading Or Not Initializd Then Exit Sub


        Select Case ModAxle(0)
            Case 0 : Fact = New Integer() {-1, 1}
            Case 1 : Fact = New Integer() {1, 1}
            Case 2 : Fact = New Integer() {-1, -1}
            Case 3 : Fact = New Integer() {1, -1}
        End Select


        'If Modaxle.Count = 4 Then
        For j = 0 To ModAxle.Count - 1
            cars(Active_Car).Theory.Axle(ModAxle(j)).offSet = New Vector3d(If(ModAxle(j) Mod 2 = 0, Fact(0) * -NumericUpDown6.Value, Fact(0) * NumericUpDown6.Value), _
                                                                          NumericUpDown5.Value, _
                                                                     If(ModAxle(j) < 2, Fact(1) * NumericUpDown4.Value, -Fact(1) * NumericUpDown4.Value))
            If cars(Active_Car).models.axle(ModAxle(j)) IsNot Nothing Then cars(Active_Car).models.axle(ModAxle(j)).Position = cars(Active_Car).Theory.Axle(ModAxle(j)).offSet * Zoom
        Next
        '  ElseIf Modaxle.Count = 2 Then

        ShowAxleCorrect()

        '  End If
    End Sub




    '-------------SHOW CORRECT----------------
    Sub ShowSpringCorrect()
        For i = 0 To 3
            Dim Scale! = (cars(Active_Car).Theory.Spring(i).Offset - cars(Active_Car).Theory.wheel(i).Offset(1)).Length / cars(Active_Car).Theory.Spring(i).Length

            If cars(Active_Car).models.Spring(i) Is Nothing Then Continue For
            cars(Active_Car).models.Spring(i).MATRIX = Matrix4.Scale(1, Scale, 1)

            cars(Active_Car).models.Spring(i).MATRIX *= BuildLookMatrixDown( _
                        cars(Active_Car).Theory.wheel(i).Offset(1) * Zoom, _
                        cars(Active_Car).Theory.Spring(i).Offset * Zoom)


            cars(Active_Car).models.Spring(i).MATRIX *= Matrix4.CreateTranslation(cars(Active_Car).Theory.Spring(i).Offset * Zoom)
        Next i

    End Sub

    Sub ShowAxleCorrect()
        For i = 0 To 3
            Dim Scale! = (cars(Active_Car).Theory.Axle(i).offSet - cars(Active_Car).Theory.wheel(i).Offset(1)).LengthFast / cars(Active_Car).Theory.Axle(i).Length


            If cars(Active_Car).models.axle(i) Is Nothing Then Continue For
            cars(Active_Car).models.axle(i).MATRIX = Matrix4.Scale(1, 1, Scale)

            cars(Active_Car).models.axle(i).MATRIX *= BuildLookMatrixForward( _
                                                cars(Active_Car).Theory.Axle(i).offSet * Zoom, _
                                                  cars(Active_Car).Theory.wheel(i).Offset(1) * Zoom)


            cars(Active_Car).models.axle(i).MATRIX *= Matrix4.CreateTranslation(cars(Active_Car).Theory.Axle(i).offSet * Zoom)


        Next i

    End Sub

    Private Sub Button17_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button17.Click
        PrintVecToObjectsV(cars(Active_Car).OriginalTheory.Axle(ModAxle(0)).offSet, NumericUpDown6, NumericUpDown5, NumericUpDown4)

    End Sub

    Private Sub HScrollBar1_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar1.Scroll

    End Sub

    Private Sub HScrollBar2_Vch() Handles HScrollBar2.ValueChanged, TrackBar7.ValueChanged
        If Loading Or Not Initializd Then Exit Sub


        Select Case ModAxle(0)
            Case 0 : Fact = New Integer() {-1, 1}
            Case 1 : Fact = New Integer() {1, 1}
            Case 2 : Fact = New Integer() {-1, -1}
            Case 3 : Fact = New Integer() {1, -1}
        End Select


        'If Modaxle.Count = 4 Then
        For j = 0 To ModAxle.Count - 1
            cars(Active_Car).Theory.Axle(ModAxle(j)).Length = HScrollBar2.Value + TrackBar7.Value / 1000

        Next
        ShowAxleCorrect()
        '  ElseIf Modaxle.Count = 2 Then

        Label12.Text = "Length (" & cars(Active_Car).Theory.Axle(ModAxle(0)).Length & ")"

    End Sub

    Private Sub Button12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button12.Click
        Dim Activeaxle = cars(Active_Car).OriginalTheory.Axle(ModAxle(0))


        HScrollBar2.Value = Int(Activeaxle.Length)
        TrackBar7.Value = (Activeaxle.Length - Int(Activeaxle.Length)) * 1000


    End Sub

    Private Sub Button16_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button16.Click
        Dim v = ModAxle(0)
        ' Dim ml = cars(Active_Car).models.axle(v).BoundingBox.maxZ - cars(Active_Car).models.axle(v).BoundingBox.MinZ

        Dim tl = (cars(Active_Car).Theory.Axle(v).offSet - cars(Active_Car).Theory.wheel(v).Offset(1)).Length ' * Zoom
        HScrollBar2.Value = Int(tl)
        TrackBar7.Value = (tl - HScrollBar2.Value) * 1000

    End Sub

    Private Sub CheckBox2_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox2.CheckedChanged
        For i = 0 To ModAxle.Count - 1

            If Not CheckBox2.Checked Then
                cars(Active_Car).Theory.Axle(ModAxle(i)).modelNumber = -1
                If cars(Active_Car).models.axle(ModAxle(i)) IsNot Nothing Then cars(Active_Car).models.axle(ModAxle(i)).isVisible = False
            Else
                cars(Active_Car).Theory.Axle(ModAxle(i)).modelNumber = AxlesP(ModAxle(i))
                If cars(Active_Car).models.axle(ModAxle(i)) IsNot Nothing Then cars(Active_Car).models.axle(ModAxle(i)).isVisible = True
            End If
        Next
    End Sub

    Private Sub HScrollBar2_Scroll(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ScrollEventArgs) Handles HScrollBar2.Scroll

    End Sub
End Class