Public Class Parameters_editor

    Dim CurPos As Point = MousePosition
    Private Sub Label4_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Title.MouseMove
        If e.Button = Windows.Forms.MouseButtons.Left Then
            Me.Location += MousePosition - CurPos
            CurPos = MousePosition
        Else
            CurPos = MousePosition
        End If
    End Sub
    Dim WasActive = False
    Dim tptr = 0
    Dim DontReflect = False 'Bug, assign => refresh => assign

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        'update from GUI
        If ActiveForm Is Me Then
            tptr = If(TextBox1.SelectionStart > 1, TextBox1.SelectionStart, tptr)

            If Not WasActive Then
                DontReflect = True
                TextBox1.Text = cars(Active_Car).Sing.SaveToStr()



                TextBox1.Focus()

                TextBox1.SelectionStart = tptr
                TextBox1.ScrollToCaret()

            End If


            WasActive = True
        Else
            tptr = If(TextBox1.SelectionStart > 1, TextBox1.SelectionStart, tptr)


            WasActive = False

        End If
    End Sub


    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged
        Timer2.Start()

    End Sub

    Private Sub Timer2_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer2.Tick
        'update to GUI
        If DontReflect Then DontReflect = False : Exit Sub
        ' MsgBox("here?")
        cars(Active_Car).Sing = New Singletons(TextBox1.Text, True)
        cars(Active_Car).Load(True)
        CarBrowser.ReLoadOneCar()

        CarEditor.ComboBox1_SelectedIndexChanged(sender, e)

        Timer2.Stop()
    End Sub


End Class