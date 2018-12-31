Imports CarStudio.CarBrowser

Public Class Frontend

    Private Sub TabPage2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TabPage2.Click

    End Sub

    Private Sub TabPage2_MouseEnter(ByVal sender As Object, ByVal e As System.EventArgs) Handles TabPage2.MouseEnter

    End Sub
    Sub DoLoad()
        CarEditor.Prv.Show()
        CarEditor.Prv.Controls.Add(Preview_Frontend.fte)
        DrawAllOfThem()

        Selectable.Checked = cars(Active_Car).Theory.MainInfos.SELECTABLE
        CPUselectable.Checked = cars(Active_Car).Theory.MainInfos.CPUSELECTABLE
        Besttime.Checked = cars(Active_Car).Theory.MainInfos.BESTTIME
        Statistics.Checked = cars(Active_Car).Theory.MainInfos.Statistics

        Select Case cars(Active_Car).Theory.MainInfos.car_class
            Case classes.Electric
                RadioButton1.Checked = True
            Case classes.Glow
                RadioButton2.Checked = True
            Case Else
                RadioButton3.Checked = True
        End Select


        Select Case cars(Active_Car).Theory.MainInfos.Rating
            Case ratingo.Rookie
                Rookie.Checked = True
            Case ratingo.Amateur
                Amateur.Checked = True
            Case ratingo.Advanced
                Advanced.Checked = True
            Case ratingo.Semi_Pro
                Semi_Pro.Checked = True
            Case ratingo.Pro
                Pro.Checked = True

        End Select

        MaxRev.Value = cars(Active_Car).Theory.MainInfos.MaxRev


        Select Case cars(Active_Car).Theory.MainInfos.obtain
            Case Obtaino.Anytime
                _always.Checked = True
            Case Obtaino.Carnival_Only
                _Never.Checked = True
            Case Obtaino.Championship
                _champ.Checked = True
            Case Obtaino.Practice
                _practice.Checked = True
            Case Obtaino.Timetrial
                _timetrial.Checked = True
            Case Obtaino.Winning_Single_Races
                _single.Checked = True
        End Select

        NumericUpDown1.Value = cars(Active_Car).Theory.MainInfos.TopEnd
        NumericUpDown2.Value = cars(Active_Car).Theory.MainInfos.Acceleration
        NumericUpDown3.Value = cars(Active_Car).Theory.MainInfos.Weight
        NumericUpDown4.Value = cars(Active_Car).Theory.MainInfos.Handling


        isLoading = False
    End Sub
    Sub UnLoad()
        CarEditor.Prv.Hide()
        Preview_Frontend.Controls.Add(Preview_Frontend.fte)
    End Sub

    'Translated directly from the game
    Const MPH2OGU_SPEED = 1 / 0.01118
    Const MIN_CAR_TOPEND = MPH2OGU_SPEED * 26
    Const MAX_CAR_TOPEND = MPH2OGU_SPEED * 42
    Const MIN_CAR_WEIGHT = 0.6
    Const MAX_CAR_WEIGHT = 2.8
    Const MIN_CAR_ACC = 4
    Const MAX_CAR_ACC = 12
    Const MIN_CAR_HANDLING = 0
    Const MAX_CAR_HANDLING = 100
    Dim isLoading = True

    Sub DrawAllOfThem()
        With Preview_Frontend
            'name 
            .carname.Text = cars(Active_Car).Theory.MainInfos.Name

            .carclass.Text = DirectCast(cars(Active_Car).Theory.MainInfos.car_class, classes).ToString
            .carrating.Text = Replace(DirectCast(cars(Active_Car).Theory.MainInfos.Rating, ratingo).ToString, "_", " ")
            .cartrans.Text = cars(Active_Car).Theory.MainInfos.Trans
            Select Case .cartrans.Text
                Case 0
                    .cartrans.Text = "4WD"
                Case 1
                    .cartrans.Text = "RWD"
                Case 2
                    .cartrans.Text = "FWD"
            End Select





            If cars(Active_Car).Theory.MainInfos.Statistics Then
                .carspeed.Hide()
                .caracc.Hide()
                .carweight.Hide()
                .speedpanel.Show()
                .accpanel.Show()
                .weightpanel.Show()

                Dim constSize = 135

                ' speed = cars(Active_Car).Theory.MainInfos.TopEnd - speed ') '/ 10
                ' .speedpanel.Width = 100 * (speed - MIN_CAR_TOPEND) / (MAX_CAR_TOPEND - MIN_CAR_TOPEND)
                .speedpanel.Width = constSize * (cars(Active_Car).Theory.MainInfos.TopEnd - MIN_CAR_TOPEND) / (MAX_CAR_TOPEND - MIN_CAR_TOPEND) '- speed) / (MAX_CAR_TOPEND - MIN_CAR_TOPEND)
                .accpanel.Width = constSize * (MAX_CAR_ACC - cars(Active_Car).Theory.MainInfos.Acceleration) / (MAX_CAR_ACC - MIN_CAR_ACC) '- speed) / (MAX_CAR_TOPEND - MIN_CAR_TOPEND)
                .weightpanel.Width = constSize * (cars(Active_Car).Theory.MainInfos.Weight - MIN_CAR_WEIGHT) / (MAX_CAR_WEIGHT - MIN_CAR_WEIGHT)
                ' acc += (cars(Active_Car).Theory.MainInfos.Acceleration - acc) / 10
                ' .accpanel.Width = 100 * (MAX_CAR_ACC - acc) / (MAX_CAR_ACC - MIN_CAR_ACC)

                'weight += (cars(Active_Car).Theory.MainInfos.Weight - weight) / 10
                ' .weightpanel.Width = 100 * (weight - MIN_CAR_WEIGHT) / (MAX_CAR_WEIGHT - MIN_CAR_WEIGHT)
            Else
                .carspeed.Show()
                .caracc.Show()
                .carweight.Show()
                .speedpanel.Hide()
                .accpanel.Hide()
                .weightpanel.Hide()

            End If




        End With


    End Sub

    Private Sub RadioButton4_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Rookie.CheckedChanged, Amateur.CheckedChanged, Advanced.CheckedChanged, Semi_Pro.CheckedChanged, Pro.CheckedChanged

    End Sub

    Private Sub RadioButton12_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Frontend_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Private Sub MCheckbox2_CheckedChanged() Handles Selectable.CheckedChanged, Besttime.CheckedChanged, Statistics.CheckedChanged, _Never.CheckedChanged, _always.CheckedChanged, _champ.CheckedChanged, _timetrial.CheckedChanged, _single.CheckedChanged, _
    _practice.CheckedChanged, RadioButton1.CheckedChanged, RadioButton2.CheckedChanged, RadioButton3.CheckedChanged, _
    MaxRev.ValueChanged, Rookie.CheckedChanged, Amateur.CheckedChanged, Advanced.CheckedChanged, Semi_Pro.CheckedChanged, _
    Pro.CheckedChanged, CPUselectable.CheckedChanged, NumericUpDown1.ValueChanged, NumericUpDown2.ValueChanged, NumericUpDown3.ValueChanged, NumericUpDown4.ValueChanged
        If isLoading Then Exit Sub

        With cars(Active_Car).Theory.MainInfos
            .SELECTABLE = Selectable.Checked
            .CPUSELECTABLE = CPUselectable.Checked
            .Statistics = Statistics.Checked
            .BESTTIME = Besttime.Checked
            If RadioButton1.Checked Then .car_class = classes.Electric
            If RadioButton2.Checked Then .car_class = classes.Glow
            If RadioButton3.Checked Then .car_class = classes.Special

            If Rookie.Checked Then
                .Rating = ratingo.Rookie
            ElseIf Amateur.Checked Then
                .Rating = ratingo.Amateur
            ElseIf Advanced.Checked Then
                .Rating = ratingo.Advanced
            ElseIf Semi_Pro.Checked Then
                .Rating = ratingo.Semi_Pro
            Else
                .Rating = ratingo.Pro
            End If

            .MaxRev = MaxRev.Value


            If _Never.Checked Then
                .obtain = Obtaino.Carnival_Only
            ElseIf _always.Checked Then
                .obtain = Obtaino.Anytime
            ElseIf _champ.Checked Then
                .obtain = Obtaino.Championship
            ElseIf _single.Checked Then
                .obtain = Obtaino.Winning_Single_Races
            ElseIf _practice.Checked Then
                .obtain = Obtaino.Practice
            ElseIf _timetrial.Checked Then
                .obtain = Obtaino.Timetrial
            End If

            .TopEnd = NumericUpDown1.Value
            .Acceleration = NumericUpDown2.Value
            .Weight = NumericUpDown3.Value
            .Handling = NumericUpDown4.Value

        End With


        With cars(Active_Car).Theory.RealInfos

        End With

        DrawAllOfThem()

    End Sub

    Private Sub MCheckbox2_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Selectable.Load

    End Sub

    Private Sub Panel3_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel3.Paint

    End Sub

    Private Sub MCheckbox2_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

End Class