Imports OpenTK
Imports System.Math

Public Class Spinner_

    Private Sub Spinner__Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub
    Dim Loading = True
    Sub DoLoad()
        PrintVecToObjectsV(cars(Active_Car).Theory.Spinner.offSet, NumericUpDown1, NumericUpDown2, NumericUpDown3)
        PrintVecToObjectsV(cars(Active_Car).Theory.Spinner.Axis, NumericUpDown6, NumericUpDown5, NumericUpDown4)
        NumericUpDown9.Value = cars(Active_Car).Theory.Spinner.angVel
        ' If cars(Active_Car).models.Spinner IsNot Nothing Then ObjectsVToVec(NumericUpDown1, NumericUpDown2, NumericUpDown3, cars(Active_Car).models.Spinner.Position)

        Loading = False

    End Sub
    Sub UnLoad()

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        PrintVecToObjectsV(cars(Active_Car).OriginalTheory.Spinner.offSet, NumericUpDown1, NumericUpDown2, NumericUpDown3)

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        PrintVecToObjectsV(cars(Active_Car).OriginalTheory.Spinner.Axis, NumericUpDown6, NumericUpDown5, NumericUpDown4)

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        NumericUpDown9.Value = cars(Active_Car).OriginalTheory.Spinner.angVel

    End Sub

    Private Sub NumericUpDown1_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown1.ValueChanged, NumericUpDown2.ValueChanged, NumericUpDown3.ValueChanged
        If Loading Then Exit Sub
        ObjectsVToVec(NumericUpDown1, NumericUpDown2, NumericUpDown3, cars(Active_Car).Theory.Spinner.offSet)
        If cars(Active_Car).models.Spinner IsNot Nothing Then _
                  cars(Active_Car).models.Spinner.MATRIX = Matrix4.CreateTranslation(cars(Active_Car).Theory.Spinner.offSet * Zoom)



    End Sub

    Private Sub NumericUpDown6_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown6.ValueChanged, NumericUpDown5.ValueChanged, NumericUpDown4.ValueChanged
        If Loading Then Exit Sub
        ObjectsVToVec(NumericUpDown6, NumericUpDown5, NumericUpDown4, cars(Active_Car).Theory.Spinner.Axis)


    End Sub

    Private Sub NumericUpDown9_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown9.ValueChanged
        If Loading Then Exit Sub
        cars(Active_Car).Theory.Spinner.angVel = NumericUpDown9.Value
    End Sub

    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox1.CheckedChanged
        Timer1.Enabled = CheckBox1.Checked
    End Sub
    Dim t = 0.0F


    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        t += 1 / Min(Max(fps, 30), 75)
        If t >= Abs(cars(Active_Car).Theory.Spinner.angVel * fps) Then t = 0
        If cars(Active_Car).models.Spinner Is Nothing Then Exit Sub

        cars(Active_Car).models.Spinner.MATRIX = Matrix4.CreateFromAxisAngle(cars(Active_Car).Theory.Spinner.Axis, t * cars(Active_Car).Theory.Spinner.angVel)

        cars(Active_Car).models.Spinner.MATRIX *= Matrix4.CreateTranslation(cars(Active_Car).Theory.Spinner.offSet * Zoom)
        '  cars(Active_Car).models.Spinner.Rotation = cars(Active_Car).OriginalTheory.Spinner.Axis * cars(Active_Car).Theory.Spinner.angVel / Min(Max(60, fps), 75)


    End Sub

    Private Sub Button45_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button45.Click
        PICK_PRM.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location

        PICK_PRM.Config.Filler = cars(Active_Car).Theory.Spinner
        PICK_PRM.Show()
    End Sub
End Class