Imports OpenTK

Public Class body_aider

    Private Sub body_aider_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.GotFocus

        CarEditor.Focus()

    End Sub

    Dim latestScroll! = 1
    Dim c% = 0
    Dim Last%


    Private Sub TrackBar5_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar5.Scroll
        cars(Active_Car).models.BODY.Position += New Vector3(0, (TrackBar5.Value - Last) / 10000, 0)



        Last = TrackBar5.Value
    End Sub

    Private Sub Button28_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button28.Click
        Dim ca! = TrackBar5.Value / 10000
        For i = 0 To cars(Active_Car).models.BODY.MyModel.vertnum - 1

            cars(Active_Car).models.BODY.MyModel.vexl(i).Position -= New Vector3(0, ca / Zoom, 0)

        Next
        TrackBar5.Value = 0
        Last = 0
        cars(Active_Car).models.BODY.Export(cars(Active_Car).models.BODY.Directory & "\" & cars(Active_Car).models.BODY.FileName)

        CarBrowser.ReLoadOneCar()
    End Sub
    Private Sub Button20_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button20.Click

        Dim PRM As New PRM(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Body.modelNumber), True)


        Dim X = (PRM.BoundingBox.maxX + PRM.BoundingBox.MinX) / 2
        ' Dim Y = Abs(cars(Active_Car).models.BODY.BoundingBox.maxY + cars(Active_Car).models.BODY.BoundingBox.MinY) / 2
        Dim Z = (PRM.BoundingBox.maxZ + PRM.BoundingBox.MinZ) / 2

        For i = 0 To cars(Active_Car).models.BODY.MyModel.vertnum - 1
            PRM.MyModel.vexl(i).Position -= New Vector3(X, 0, Z) ' / Zoom

        Next

        PRM.Export(cars(Active_Car).models.BODY.Directory & "\" & cars(Active_Car).models.BODY.FileName)
        CarBrowser.ReLoadOneCar()

    End Sub

    Private Sub Button27_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button27.Click
        Dim PRM As New PRM(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Body.modelNumber), True)


        ' cars(Active_Car).models.BODY.RenderBBOX = True

        YESNO.Question.Text = "continue centering the body?"

        PRM.Export(cars(Active_Car).models.BODY.Directory & "\" & cars(Active_Car).models.BODY.FileName & ".BACKUP")

        If YESNO.ShowDialog = Windows.Forms.DialogResult.No Then cars(Active_Car).models.BODY.RenderBBOX = False : Exit Sub

        Dim X = (PRM.BoundingBox.maxX + PRM.BoundingBox.MinX) / 2
        Dim Y = (PRM.BoundingBox.maxY + PRM.BoundingBox.MinY) / 2
        Dim Z = (PRM.BoundingBox.maxZ + PRM.BoundingBox.MinZ) / 2
        For i = 0 To cars(Active_Car).models.BODY.MyModel.vertnum - 1
            PRM.MyModel.vexl(i).Position -= New Vector3(X, Y, Z) ' / Zoom

        Next
        PRM.Export(cars(Active_Car).models.BODY.Directory & "\" & cars(Active_Car).models.BODY.FileName)
        CarBrowser.ReLoadOneCar()

    End Sub



    Private Sub TrackBar1_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar1.Scroll

        TrackBar2.Value = TrackBar1.Value
        TrackBar3.Value = TrackBar1.Value
        TrackBar4.Value = TrackBar1.Value


    End Sub



    Private Sub Button20_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button20.Click
        cars(Active_Car).models.BODY.ScaleFloat = 1
        For i = 0 To cars(Active_Car).models.BODY.MyModel.vertnum
            cars(Active_Car).models.BODY.MyModel.vexl(i).Position *= TrackBar1.Value / 1000
        Next


        cars(Active_Car).models.BODY.Export(cars(Active_Car).models.BODY.Directory & "\" & cars(Active_Car).models.BODY.FileName)

        TrackBar1.Value = 1000
    End Sub

    Private Sub TrackBar2_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar2.Scroll, TrackBar3.Scroll, TrackBar4.Scroll, TrackBar1.Scroll
        If _Body.Loading Then Exit Sub 'And _Body.Panel9.Visible = False Then Exit Sub



        cars(Active_Car).models.BODY.Scale = New Vector3(TrackBar2.Value / 1000.0F, TrackBar3.Value / 1000.0F, TrackBar4.Value / 1000.0F)
    End Sub

    Private Sub Button26_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button26.Click
        cars(Active_Car).models.BODY.ScaleFloat = 1
        For i = 0 To cars(Active_Car).models.BODY.MyModel.vertnum
            cars(Active_Car).models.BODY.MyModel.vexl(i).Position = Vector3.Multiply(cars(Active_Car).models.BODY.MyModel.vexl(i).Position, New Vector3(TrackBar2.Value / 1000.0F, _
                                                                                                                                                        TrackBar3.Value / 1000.0F, _
                                                                                                                                                        TrackBar4.Value / 1000.0F))
        Next


        cars(Active_Car).models.BODY.Export(cars(Active_Car).models.BODY.Directory & "\" & cars(Active_Car).models.BODY.FileName)
        TrackBar1.Value = 1000
        TrackBar2.Value = 1000
        TrackBar3.Value = 1000
        TrackBar4.Value = 1000
        CarBrowser.ReLoadOneCar()

        'TrackBar3.Value = 1000
        'TrackBar4.Value = 1000
    End Sub

    Private Sub body_aider_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Focus()
        CarEditor.Focus()
    End Sub
End Class