Imports System.Drawing
Imports System.Windows.Forms

Public Class mCheckbox
    ' Inherits Label

    Dim txt$ = Me.Text
    Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Label1.Text = Me.Text


    End Sub

    Sub New(ByVal st$)

        ' This call is required by the Windows Form Designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.
        Me.Text = st
    End Sub
    'properties
    Public Overrides Property Text() As String
        Get
            Return txt
        End Get
        Set(ByVal value$)
            txt = value
            Label1.Text = value
        End Set
    End Property
    Public Property Caption() As String
        Get
            Return txt
        End Get
        Set(ByVal value$)
            txt = value
            Label1.Text = value
        End Set
    End Property
    Public Overrides Property Font() As Font
        Get
            Return Label1.Font
        End Get
        Set(ByVal value As Font)
            Label1.Font = value
        End Set
    End Property
    Public Event CheckedChanged As Action '(ByVal sender As Object, ByVal e As EventArgs)



    Dim chkstat As Boolean = Me.Checked
    Public Property Checked() As Boolean
        Get

            Return chkstat
        End Get
        Set(ByVal value As Boolean)
            chkstat = value
            RaiseEvent CheckedChanged()
        End Set
    End Property

    ' Event CheckedChanged As Action



    Private Sub PictureBox1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PictureBox1.Click, Label1.Click
        Select Case chkstat
            Case True
                PictureBox1.Image = My.Resources.Checkbox_active
                chkstat = CheckState.Unchecked
            Case Else
                PictureBox1.Image = My.Resources.Checkbox_checked_active
                chkstat = CheckState.Checked

        End Select
        RaiseEvent CheckedChanged()
    End Sub




    Private Sub mCheckbox_Resize() Handles Me.Resize, Me.CheckedChanged
        PictureBox1.Width = PictureBox1.Height
        Select Case chkstat
            Case True
                PictureBox1.Image = My.Resources.Checkbox_checked
                ' chkstat = CheckState.Unchecked
            Case Else
                PictureBox1.Image = My.Resources.Checkbox '_active
                ' chkstat = CheckState.Checked

        End Select
    End Sub

    Private Sub Label1_MouseLeave(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label1.MouseLeave, PictureBox1.MouseLeave
        Select Case chkstat
            Case True
                PictureBox1.Image = My.Resources.Checkbox_checked
            Case False
                PictureBox1.Image = My.Resources.Checkbox
        End Select
        Label1.ForeColor = Color.White
    End Sub

    Private Sub Label1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Label1.MouseMove, Label1.MouseMove
        Label1.ForeColor = Color.LightBlue
        Select Case chkstat
            Case CheckState.Checked
                PictureBox1.Image = My.Resources.Checkbox_checked_active
            Case CheckState.Unchecked
                PictureBox1.Image = My.Resources.Checkbox_active
        End Select

    End Sub


End Class
