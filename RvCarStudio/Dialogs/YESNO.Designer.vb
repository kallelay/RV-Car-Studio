<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class YESNO
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Panel1 = New System.Windows.Forms.Panel
        Me.No = New System.Windows.Forms.Button
        Me.Question = New System.Windows.Forms.Label
        Me.Panel2 = New System.Windows.Forms.Panel
        Me.Title = New System.Windows.Forms.Label
        Me.Yes = New System.Windows.Forms.Button
        Me.Panel1.SuspendLayout()
        Me.Panel2.SuspendLayout()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVCS2
        Me.Panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Controls.Add(Me.No)
        Me.Panel1.Controls.Add(Me.Question)
        Me.Panel1.Controls.Add(Me.Panel2)
        Me.Panel1.Controls.Add(Me.Yes)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(343, 142)
        Me.Panel1.TabIndex = 7
        '
        'No
        '
        Me.No.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.No.FlatAppearance.BorderColor = System.Drawing.Color.Gray
        Me.No.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.No.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.No.ForeColor = System.Drawing.Color.White
        Me.No.Location = New System.Drawing.Point(12, 104)
        Me.No.Name = "No"
        Me.No.Size = New System.Drawing.Size(131, 28)
        Me.No.TabIndex = 6
        Me.No.Text = "NO"
        Me.No.UseVisualStyleBackColor = False
        '
        'Question
        '
        Me.Question.BackColor = System.Drawing.Color.Transparent
        Me.Question.Font = New System.Drawing.Font("Consolas", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Question.ForeColor = System.Drawing.Color.LightGray
        Me.Question.Location = New System.Drawing.Point(8, 30)
        Me.Question.Name = "Question"
        Me.Question.Size = New System.Drawing.Size(330, 73)
        Me.Question.TabIndex = 1
        Me.Question.Text = "Question"
        Me.Question.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Panel2
        '
        Me.Panel2.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.Panel2.Controls.Add(Me.Title)
        Me.Panel2.Location = New System.Drawing.Point(0, -19)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(342, 41)
        Me.Panel2.TabIndex = 6
        '
        'Title
        '
        Me.Title.BackColor = System.Drawing.Color.Transparent
        Me.Title.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Title.ForeColor = System.Drawing.Color.White
        Me.Title.Location = New System.Drawing.Point(0, 0)
        Me.Title.Margin = New System.Windows.Forms.Padding(3, 30, 30, 30)
        Me.Title.Name = "Title"
        Me.Title.Padding = New System.Windows.Forms.Padding(0, 0, 0, 5)
        Me.Title.Size = New System.Drawing.Size(342, 41)
        Me.Title.TabIndex = 0
        Me.Title.Text = "Question"
        Me.Title.TextAlign = System.Drawing.ContentAlignment.BottomCenter
        '
        'Yes
        '
        Me.Yes.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.Yes.FlatAppearance.BorderColor = System.Drawing.Color.Gray
        Me.Yes.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Yes.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.Yes.ForeColor = System.Drawing.Color.White
        Me.Yes.Location = New System.Drawing.Point(201, 104)
        Me.Yes.Name = "Yes"
        Me.Yes.Size = New System.Drawing.Size(131, 28)
        Me.Yes.TabIndex = 5
        Me.Yes.Text = "YES"
        Me.Yes.UseVisualStyleBackColor = False
        '
        'YESNO
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(343, 142)
        Me.Controls.Add(Me.Panel1)
        Me.Font = New System.Drawing.Font("Calibri", 8.28!)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "YESNO"
        Me.Opacity = 0.95
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "YESNO"
        Me.Panel1.ResumeLayout(False)
        Me.Panel2.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents No As System.Windows.Forms.Button
    Friend WithEvents Yes As System.Windows.Forms.Button
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Panel2 As System.Windows.Forms.Panel
    Friend WithEvents Title As System.Windows.Forms.Label
    Friend WithEvents Question As System.Windows.Forms.Label

End Class
