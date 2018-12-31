Public Class mButton
    Public Property Caption() As String
        Get
            Return Label1.Text
        End Get
        Set(ByVal value As String)
            Label1.Text = value
        End Set
    End Property
    Public Shadows Event MouseClick()

    Private Sub Label1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label1.Click
        RaiseEvent MouseClick()
    End Sub
    Private Sub Label1_MouseLeave(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label1.MouseLeave
        Me.BackgroundImage = My.Resources.button
        Label1.ForeColor = Drawing.Color.Navy
    End Sub

    Private Sub Label1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Label1.MouseMove
        If e.Button = Windows.Forms.MouseButtons.None Then

            Me.BackgroundImage = My.Resources.button_hover
            Label1.ForeColor = Drawing.Color.RoyalBlue
        Else

            Me.BackgroundImage = My.Resources.button_down
            Label1.ForeColor = Drawing.Color.LightBlue
        End If
    End Sub
End Class
