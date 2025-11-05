Public Class pv_carbox

    Private Sub Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint

    End Sub
    Dim DeltaSiz As Point = New Point
    Dim OldSize = New Size(267, 192)
    Private Sub Panel1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Panel1.Resize
        DeltaSiz = Me.Size - OldSize
        ' OldSize = Me.Size
        PictureBox1.Location = New Point(61, 19) + New Point(DeltaSiz.X / 4, DeltaSiz.Y / 4)
        PictureBox1.Size = Me.Size - New Size(New Point(61, 19))
    End Sub

    Private Sub pv_carbox_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        FORCE_DO_NOT_RENDER = True
    End Sub

    Private Sub pv_carbox_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.GotFocus
   
        CarEditor.Focus()
    End Sub

    Private Sub pv_carbox_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Shown
        Focus()

        CarEditor.Focus()

    End Sub

    Private Sub pv_carbox_Load_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Private Sub pv_carbox_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LostFocus
        FORCE_DO_NOT_RENDER = False
    End Sub
End Class