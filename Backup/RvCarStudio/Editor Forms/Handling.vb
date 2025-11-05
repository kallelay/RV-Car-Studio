Public Class Handling
    Dim isLoading As Boolean = True
  
    Sub DOLOAD()
        isLoading = True

        NumericUpDown3.Value = cars(Active_Car).Theory.RealInfos.SteerRate
        NumericUpDown1.Value = cars(Active_Car).Theory.RealInfos.SteerMode
        EngRat.Value = cars(Active_Car).Theory.RealInfos.EngineRate
        NumericUpDown2.Value = cars(Active_Car).Theory.RealInfos.TopSpeed
        NumericUpDown4.Value = cars(Active_Car).Theory.RealInfos.DownForceModifier
        isLoading = False
    End Sub
    Sub Unload()

    End Sub
    Private Sub Handling_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub
    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        NumericUpDown3.Value = cars(Active_Car).OriginalTheory.RealInfos.SteerRate
    End Sub
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        NumericUpDown1.Value = cars(Active_Car).OriginalTheory.RealInfos.SteerMode
    End Sub

    Private Sub Button13_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button13.Click
        EngRat.Value = cars(Active_Car).OriginalTheory.RealInfos.EngineRate
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        NumericUpDown2.Value = cars(Active_Car).OriginalTheory.RealInfos.TopSpeed
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        NumericUpDown4.Value = cars(Active_Car).OriginalTheory.RealInfos.DownForceModifier
    End Sub

    Private Sub NumericUpDown3_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NumericUpDown3.ValueChanged, NumericUpDown2.ValueChanged, NumericUpDown1.ValueChanged, NumericUpDown4.ValueChanged, EngRat.ValueChanged
        If isLoading Then Exit Sub

        cars(Active_Car).Theory.RealInfos.SteerRate = NumericUpDown3.Value
        cars(Active_Car).Theory.RealInfos.SteerMode = NumericUpDown1.Value
        cars(Active_Car).Theory.RealInfos.EngineRate = EngRat.Value
        cars(Active_Car).Theory.RealInfos.TopSpeed = NumericUpDown2.Value
        cars(Active_Car).Theory.RealInfos.DownForceModifier = NumericUpDown4.Value
    End Sub
End Class