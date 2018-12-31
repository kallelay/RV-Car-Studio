Imports System.Math
Imports OpenTK
Public Class shade
    Enum usemode
        vx = 0
        ply = 1
    End Enum
    Dim USE As usemode
    Dim SAVED = True
    Dim MODIFIED = False
    Dim LOADING = True
    Private Sub RadioButton1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton1.CheckedChanged, RadioButton2.CheckedChanged
        If RadioButton1.Checked Then USE = usemode.vx Else USE = usemode.ply
    End Sub
    Dim BodyModel As PRM
    Sub DoLoad()
        BodyModel = cars(Active_Car).models.BODY.Clone
        LOADING = False
        NumericUpDown1_ValueChanged_1(Nothing, Nothing)
        SAVED = False
    End Sub
    Sub Unload()
        If Not MODIFIED Then Exit Sub 'HACK: ERROR on laod

        Dim p = cars(Active_Car).models.BODY.MyModel.polyl.Clone
        cars(Active_Car).models.BODY = BodyModel
        If SAVED Then cars(Active_Car).models.BODY.MyModel.polyl = p.Clone
        p = Nothing

    End Sub

    Private Sub NumericUpDown1_ValueChanged_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown1.ValueChanged, _
    NumericUpDown2.ValueChanged, NumericUpDown3.ValueChanged, NumericUpDown4.ValueChanged, _
    _x.ValueChanged, _y.ValueChanged, _z.ValueChanged, _
     NumericUpDown9.ValueChanged, _
     CheckBox1.CheckedChanged, RadioButton2.CheckedChanged, RadioButton1.CheckedChanged
        If LOADING Then Exit Sub
        SAVED = False
        MODIFIED = True
        For Each PRM In Models


            Select Case USE
                Case usemode.vx



                    For k = 0 To PRM.MyModel.polynum - 1
                        PRM.MyModel.polyl(k).c(0).Gray = ctr(NumericUpDown4.Value * Sqrt((1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi0).normal.X * NumericUpDown1.Value) ^ 2 / _x.Value + _
                                                       (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi0).normal.Y * NumericUpDown2.Value) ^ 2 / _y.Value + _
                                                       (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi0).normal.Z * NumericUpDown3.Value) ^ 2 / _z.Value) / Sqrt(3))

                        PRM.MyModel.polyl(k).c(1).Gray = ctr(NumericUpDown4.Value * Sqrt((1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi1).normal.X * NumericUpDown1.Value) ^ 2 / _x.Value + _
                                                (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi1).normal.Y * NumericUpDown2.Value) ^ 2 / _y.Value + _
                                                (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi1).normal.Z * NumericUpDown3.Value) ^ 2 / _z.Value) / Sqrt(3))

                        PRM.MyModel.polyl(k).c(2).Gray = ctr(NumericUpDown4.Value * Sqrt((1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi2).normal.X * NumericUpDown1.Value) ^ 2 / _x.Value + _
                                                (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi2).normal.Y * NumericUpDown2.Value) ^ 2 / _y.Value + _
                                                (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi2).normal.Z * NumericUpDown3.Value) ^ 2 / _z.Value) / Sqrt(3))


                        PRM.MyModel.polyl(k).c(3).Gray = ctr(NumericUpDown4.Value * Sqrt((1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi3).normal.X * NumericUpDown1.Value) ^ 2 / _x.Value + _
                                                (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi3).normal.Y * NumericUpDown2.Value) ^ 2 / _y.Value + _
                                                (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi3).normal.Z * NumericUpDown3.Value) ^ 2 / _z.Value) / Sqrt(3))



                    Next





                Case usemode.ply







                    For k = 0 To PRM.MyModel.polynum - 1
                        Dim fc0 = NumericUpDown4.Value * Sqrt((1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi0).normal.X * NumericUpDown1.Value) ^ 2 / _x.Value + _
                                                       (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi0).normal.Y * NumericUpDown2.Value) ^ 2 / _y.Value + _
                                                       (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi0).normal.Z * NumericUpDown3.Value) ^ 2 / _z.Value) / Sqrt(3)

                        Dim fc1 = NumericUpDown4.Value * Sqrt((1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi1).normal.X * NumericUpDown1.Value) ^ 2 / _x.Value + _
                                               (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi1).normal.Y * NumericUpDown2.Value) ^ 2 / _y.Value + _
                                               (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi1).normal.Z * NumericUpDown3.Value) ^ 2 / _z.Value) / Sqrt(3)

                        Dim fc2 = NumericUpDown4.Value * Sqrt((1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi2).normal.X * NumericUpDown1.Value) ^ 2 / _x.Value + _
                                               (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi2).normal.Y * NumericUpDown2.Value) ^ 2 / _y.Value + _
                                               (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi2).normal.Z * NumericUpDown3.Value) ^ 2 / _z.Value) / Sqrt(3)

                        Dim fc3 = NumericUpDown4.Value * Sqrt((1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi3).normal.X * NumericUpDown1.Value) ^ 2 / _x.Value + _
                                               (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi3).normal.Y * NumericUpDown2.Value) ^ 2 / _y.Value + _
                                               (1 + PRM.MyModel.vexl(PRM.MyModel.polyl(k).vi3).normal.Z * NumericUpDown3.Value) ^ 2 / _z.Value) / Sqrt(3)






                        PRM.MyModel.polyl(k).c(0).Gray = ctr((fc1 + fc2 + fc3) / 3)

                        PRM.MyModel.polyl(k).c(1).Gray = ctr((fc1 + fc2 + fc3) / 3)

                        PRM.MyModel.polyl(k).c(2).Gray = ctr((fc1 + fc2 + fc3) / 3)


                        PRM.MyModel.polyl(k).c(3).Gray = ctr((fc1 + fc2 + fc3) / 3)

                    Next

            End Select

        Next



    End Sub
    Function ctr(ByVal s!) As Single
        If CheckBox1.Checked = False Then Return s
        Dim C = NumericUpDown9.Value
        Return (s + 0.5) * C - 0.5
    End Function

    Private Sub NumericUpDown12_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown12.ValueChanged, NumericUpDown11.ValueChanged, NumericUpDown10.ValueChanged
        If LOADING Then Exit Sub
        POLY.BakedColor.BaseColor = New OpenTK.Graphics.Color4(NumericUpDown12.Value / 255, _
    NumericUpDown11.Value / 255, _
    NumericUpDown10.Value / 255, 1)

        'update
        NumericUpDown1_ValueChanged_1(sender, e)



    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        ColorDialog1.Color = Color.FromArgb(NumericUpDown12.Value, NumericUpDown11.Value, NumericUpDown10.Value)
        If ColorDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            NumericUpDown12.Value = ColorDialog1.Color.R
            NumericUpDown11.Value = ColorDialog1.Color.G
            NumericUpDown10.Value = ColorDialog1.Color.B
        End If
    End Sub

    Private Sub Button9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button9.Click

        CarEditor.Timer2.Stop()
        
        FORCE_DO_NOT_RENDER = True


        If SaveFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            Dim o = New IO.FileStream(SaveFileDialog1.FileName, IO.FileMode.Create, IO.FileAccess.Write)
            Dim p = New IO.BinaryWriter(o)
            Try
                p.Write("RIKO SAVE 11")
                p.Write(Decimal.ToSingle(NumericUpDown1.Value))
                p.Write(Decimal.ToSingle(NumericUpDown2.Value))
                p.Write(Decimal.ToSingle(NumericUpDown3.Value))


                p.Write(Decimal.ToSingle(_x.Value))
                p.Write(Decimal.ToSingle(_y.Value))
                p.Write(Decimal.ToSingle(_z.Value))

                p.Write(Decimal.ToSingle(NumericUpDown4.Value))

                p.Write(Convert.ToBoolean(CheckBox1.Checked))
                p.Write(Decimal.ToSingle(NumericUpDown9.Value))

                p.Write(Decimal.ToSingle(NumericUpDown12.Value))
                p.Write(Decimal.ToSingle(NumericUpDown11.Value))
                p.Write(Decimal.ToSingle(NumericUpDown10.Value))
                If RadioButton2.Checked Then
                    p.Write(Decimal.ToInt16(0))
                Else
                    p.Write(Decimal.ToInt16(1))
                End If


            Catch ex As Exception

            Finally
                p.Close()
                p = Nothing
                o = Nothing


            End Try
        End If
        FORCE_DO_NOT_RENDER = False
        CarEditor.Timer2.Start()
    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click
        FORCE_DO_NOT_RENDER = True


        CarEditor.Timer2.Stop()
        If OpenFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            Dim o = New IO.FileStream(OpenFileDialog1.FileName, IO.FileMode.Open, IO.FileAccess.Read)
            Dim p = New IO.BinaryReader(o)
            Try
                p.ReadBytes(13)
                PrintVecToObjectsV(New Vector3(p.ReadSingle(), p.ReadSingle(), p.ReadSingle()), NumericUpDown1, NumericUpDown2, NumericUpDown3)
                PrintVecToObjectsV(New Vector3(p.ReadSingle(), p.ReadSingle(), p.ReadSingle()), _x, _y, _z)
                NumericUpDown4.Value = p.ReadSingle()
                CheckBox1.Checked = p.ReadBoolean()
                NumericUpDown9.Value = p.ReadSingle
                PrintVecToObjectsV(New Vector3(p.ReadSingle(), p.ReadSingle(), p.ReadSingle()), NumericUpDown12, NumericUpDown11, NumericUpDown10)
                If p.ReadInt16 = 0 Then
                    RadioButton2.Checked = True
                Else
                    RadioButton2.Checked = False
                End If



            Catch ex As Exception

            Finally
                p.Close()
                p = Nothing
                o = Nothing


            End Try
        End If
        CarEditor.Timer2.Start()
        FORCE_DO_NOT_RENDER = False

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        For Each PRM In Models
            PRM.Export()
        Next

    End Sub

    Private Sub shade_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub
End Class