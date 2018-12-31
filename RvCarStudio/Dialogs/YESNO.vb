Imports System.Windows.Forms

Public Class YESNO

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        CarEditor.Timer1.Start()
        Me.Close()
    End Sub

    Public Sub OnHover(ByVal sender As Object, ByVal e As Object) Handles Yes.MouseMove, No.MouseMove ', Pick.MouseMove
        ' If GetType(Object).ToString <> "Button" Then Exit Sub
        ' If Not (sender.BackgroundImage Is My.Resources.button2) Then sender.BackgroundImage = My.Resources.button2




    End Sub
    Public Sub OnExit(ByVal sender As Object, ByVal e As Object) Handles Yes.MouseLeave, No.MouseLeave ', Pick.MouseLeave
        '  If GetType(Object).ToString <> "Button" Then Exit Sub
        '  If Not (sender.BackgroundImage Is My.Resources.Button) Then sender.BackgroundImage = My.Resources.Button

    End Sub
    Public Sub OnDown(ByVal sender As Object, ByVal e As Object) Handles Yes.MouseDown, No.MouseDown ', Pick.MouseDown
        ' If GetType(Object).ToString <> "Button" Then Exit Sub
        '  If Not (sender.BackgroundImage Is My.Resources.button3) Then sender.BackgroundImage = My.Resources.button3

    End Sub

    Private Sub Yes_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Yes.Click
        CarEditor.Timer2.Start()
        FORCE_DO_NOT_RENDER = False
        DialogResult = Windows.Forms.DialogResult.Yes

        Me.Close()


    End Sub

    Private Sub No_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles No.Click
        DialogResult = Windows.Forms.DialogResult.No
        FORCE_DO_NOT_RENDER = False
        CarEditor.Timer2.Start()
        Me.Close()

    End Sub

    Private Sub OnDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Yes.MouseDown, No.MouseDown

    End Sub

    Private Sub OnHover(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Yes.MouseMove, No.MouseMove

    End Sub

    Private Sub OnExit(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Yes.MouseLeave, No.MouseLeave

    End Sub

    Private Sub Panel2_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel2.Paint

    End Sub
    Dim CurPos As Point = MousePosition
    Private Sub Label4_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Title.MouseMove
        If e.Button = Windows.Forms.MouseButtons.Left Then
            Me.Location += MousePosition - CurPos
            CurPos = MousePosition
        Else
            CurPos = MousePosition
        End If
    End Sub
    Public Sub Fill(ByVal title$, ByVal ques$)
        Me.Title.Text = title
        Me.Question.Text = ques

    End Sub
    Private Sub Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint

    End Sub

    Private Sub YESNO_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        FORCE_DO_NOT_RENDER = True
        CarEditor.Timer2.Stop()
    End Sub

    Private Sub Title_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Title.Click

    End Sub
End Class
