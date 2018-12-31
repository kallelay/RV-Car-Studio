<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class TheCarBox
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
        Me.FontDialog1 = New System.Windows.Forms.FontDialog
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Panel9 = New System.Windows.Forms.Panel
        Me.TabControl1 = New System.Windows.Forms.TabControl
        Me.TabPage1 = New System.Windows.Forms.TabPage
        Me.MCheckbox4 = New CarStudio.mCheckbox
        Me.MCheckbox3 = New CarStudio.mCheckbox
        Me.HScrollBar7 = New System.Windows.Forms.HScrollBar
        Me.Label9 = New System.Windows.Forms.Label
        Me.HScrollBar6 = New System.Windows.Forms.HScrollBar
        Me.Label8 = New System.Windows.Forms.Label
        Me.HScrollBar5 = New System.Windows.Forms.HScrollBar
        Me.Label7 = New System.Windows.Forms.Label
        Me.HScrollBar4 = New System.Windows.Forms.HScrollBar
        Me.Label6 = New System.Windows.Forms.Label
        Me.CheckBox1 = New System.Windows.Forms.CheckBox
        Me.Button5 = New System.Windows.Forms.Button
        Me.Button4 = New System.Windows.Forms.Button
        Me.HScrollBar2 = New System.Windows.Forms.HScrollBar
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.Label1 = New System.Windows.Forms.Label
        Me.HScrollBar3 = New System.Windows.Forms.HScrollBar
        Me.HScrollBar1 = New System.Windows.Forms.HScrollBar
        Me.MCheckbox1 = New CarStudio.mCheckbox
        Me.Button3 = New System.Windows.Forms.Button
        Me.MCheckbox2 = New CarStudio.mCheckbox
        Me.Panel4 = New System.Windows.Forms.Panel
        Me.Button1 = New System.Windows.Forms.Button
        Me.Label5 = New System.Windows.Forms.Label
        Me.Panel8 = New System.Windows.Forms.Panel
        Me.Label3 = New System.Windows.Forms.Label
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.Panel3.SuspendLayout()
        Me.Panel9.SuspendLayout()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        Me.Panel4.SuspendLayout()
        Me.Panel8.SuspendLayout()
        Me.SuspendLayout()
        '
        'FontDialog1
        '
        Me.FontDialog1.AllowVectorFonts = False
        Me.FontDialog1.Font = New System.Drawing.Font("Impact", 40.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.FontDialog1.FontMustExist = True
        '
        'Panel3
        '
        Me.Panel3.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVL
        Me.Panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel3.Controls.Add(Me.Panel9)
        Me.Panel3.Controls.Add(Me.Panel8)
        Me.Panel3.Controls.Add(Me.ComboBox1)
        Me.Panel3.Location = New System.Drawing.Point(159, 79)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(304, 350)
        Me.Panel3.TabIndex = 79
        '
        'Panel9
        '
        Me.Panel9.BackColor = System.Drawing.Color.DimGray
        Me.Panel9.Controls.Add(Me.TabControl1)
        Me.Panel9.Controls.Add(Me.Panel4)
        Me.Panel9.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.Panel9.Location = New System.Drawing.Point(30, 53)
        Me.Panel9.Name = "Panel9"
        Me.Panel9.Size = New System.Drawing.Size(251, 289)
        Me.Panel9.TabIndex = 76
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Location = New System.Drawing.Point(0, 36)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(251, 250)
        Me.TabControl1.TabIndex = 7
        '
        'TabPage1
        '
        Me.TabPage1.AutoScroll = True
        Me.TabPage1.BackColor = System.Drawing.Color.Gray
        Me.TabPage1.Controls.Add(Me.MCheckbox4)
        Me.TabPage1.Controls.Add(Me.MCheckbox3)
        Me.TabPage1.Controls.Add(Me.HScrollBar7)
        Me.TabPage1.Controls.Add(Me.Label9)
        Me.TabPage1.Controls.Add(Me.HScrollBar6)
        Me.TabPage1.Controls.Add(Me.Label8)
        Me.TabPage1.Controls.Add(Me.HScrollBar5)
        Me.TabPage1.Controls.Add(Me.Label7)
        Me.TabPage1.Controls.Add(Me.HScrollBar4)
        Me.TabPage1.Controls.Add(Me.Label6)
        Me.TabPage1.Controls.Add(Me.CheckBox1)
        Me.TabPage1.Controls.Add(Me.Button5)
        Me.TabPage1.Controls.Add(Me.Button4)
        Me.TabPage1.Controls.Add(Me.HScrollBar2)
        Me.TabPage1.Controls.Add(Me.Label2)
        Me.TabPage1.Controls.Add(Me.Label4)
        Me.TabPage1.Controls.Add(Me.Label1)
        Me.TabPage1.Controls.Add(Me.HScrollBar3)
        Me.TabPage1.Controls.Add(Me.HScrollBar1)
        Me.TabPage1.Controls.Add(Me.MCheckbox1)
        Me.TabPage1.Controls.Add(Me.Button3)
        Me.TabPage1.Controls.Add(Me.MCheckbox2)
        Me.TabPage1.ForeColor = System.Drawing.SystemColors.ControlLightLight
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Margin = New System.Windows.Forms.Padding(0)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Size = New System.Drawing.Size(243, 224)
        Me.TabPage1.TabIndex = 0
        '
        'MCheckbox4
        '
        Me.MCheckbox4.BackColor = System.Drawing.Color.Transparent
        Me.MCheckbox4.Caption = "car above text"
        Me.MCheckbox4.Checked = True
        Me.MCheckbox4.Location = New System.Drawing.Point(87, 30)
        Me.MCheckbox4.Name = "MCheckbox4"
        Me.MCheckbox4.Size = New System.Drawing.Size(86, 12)
        Me.MCheckbox4.TabIndex = 26
        '
        'MCheckbox3
        '
        Me.MCheckbox3.BackColor = System.Drawing.Color.Transparent
        Me.MCheckbox3.Caption = "Line Wrap"
        Me.MCheckbox3.Checked = True
        Me.MCheckbox3.Location = New System.Drawing.Point(4, 29)
        Me.MCheckbox3.Name = "MCheckbox3"
        Me.MCheckbox3.Size = New System.Drawing.Size(97, 12)
        Me.MCheckbox3.TabIndex = 25
        '
        'HScrollBar7
        '
        Me.HScrollBar7.Location = New System.Drawing.Point(58, 109)
        Me.HScrollBar7.Maximum = 255
        Me.HScrollBar7.Name = "HScrollBar7"
        Me.HScrollBar7.Size = New System.Drawing.Size(120, 13)
        Me.HScrollBar7.TabIndex = 24
        Me.HScrollBar7.Value = 255
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(5, 107)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(51, 13)
        Me.Label9.TabIndex = 23
        Me.Label9.Text = "1st Alpha"
        '
        'HScrollBar6
        '
        Me.HScrollBar6.Location = New System.Drawing.Point(58, 125)
        Me.HScrollBar6.Maximum = 255
        Me.HScrollBar6.Name = "HScrollBar6"
        Me.HScrollBar6.Size = New System.Drawing.Size(120, 13)
        Me.HScrollBar6.TabIndex = 22
        Me.HScrollBar6.Value = 128
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(5, 123)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(54, 13)
        Me.Label8.TabIndex = 21
        Me.Label8.Text = "2nd Alpha"
        '
        'HScrollBar5
        '
        Me.HScrollBar5.Location = New System.Drawing.Point(59, 142)
        Me.HScrollBar5.Name = "HScrollBar5"
        Me.HScrollBar5.Size = New System.Drawing.Size(120, 13)
        Me.HScrollBar5.TabIndex = 20
        Me.HScrollBar5.Value = 5
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(7, 140)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(50, 13)
        Me.Label7.TabIndex = 19
        Me.Label7.Text = "Numbers"
        '
        'HScrollBar4
        '
        Me.HScrollBar4.Location = New System.Drawing.Point(58, 94)
        Me.HScrollBar4.Maximum = 1000
        Me.HScrollBar4.Name = "HScrollBar4"
        Me.HScrollBar4.Size = New System.Drawing.Size(120, 13)
        Me.HScrollBar4.TabIndex = 18
        Me.HScrollBar4.Value = 250
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(7, 92)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(35, 13)
        Me.Label6.TabIndex = 17
        Me.Label6.Text = "Trans."
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Location = New System.Drawing.Point(177, 29)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(61, 17)
        Me.CheckBox1.TabIndex = 16
        Me.CheckBox1.Text = "inverse"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'Button5
        '
        Me.Button5.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button5.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button5.Location = New System.Drawing.Point(87, 185)
        Me.Button5.Name = "Button5"
        Me.Button5.Size = New System.Drawing.Size(58, 19)
        Me.Button5.TabIndex = 15
        Me.Button5.Text = "Generate"
        Me.Button5.UseCompatibleTextRendering = True
        Me.Button5.UseVisualStyleBackColor = True
        '
        'Button4
        '
        Me.Button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button4.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button4.Location = New System.Drawing.Point(184, 107)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(50, 19)
        Me.Button4.TabIndex = 14
        Me.Button4.Text = "Font"
        Me.Button4.UseCompatibleTextRendering = True
        Me.Button4.UseVisualStyleBackColor = True
        '
        'HScrollBar2
        '
        Me.HScrollBar2.Location = New System.Drawing.Point(59, 79)
        Me.HScrollBar2.Maximum = 200
        Me.HScrollBar2.Minimum = 1
        Me.HScrollBar2.Name = "HScrollBar2"
        Me.HScrollBar2.Size = New System.Drawing.Size(120, 13)
        Me.HScrollBar2.TabIndex = 13
        Me.HScrollBar2.Value = 70
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(7, 77)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(25, 13)
        Me.Label2.TabIndex = 12
        Me.Label2.Text = "Size"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(6, 62)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(51, 13)
        Me.Label4.TabIndex = 11
        Me.Label4.Text = "Spacing V"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(5, 47)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(52, 13)
        Me.Label1.TabIndex = 11
        Me.Label1.Text = "Spacing H"
        '
        'HScrollBar3
        '
        Me.HScrollBar3.Location = New System.Drawing.Point(60, 64)
        Me.HScrollBar3.Maximum = 500
        Me.HScrollBar3.Name = "HScrollBar3"
        Me.HScrollBar3.Size = New System.Drawing.Size(120, 13)
        Me.HScrollBar3.TabIndex = 10
        Me.HScrollBar3.Value = 90
        '
        'HScrollBar1
        '
        Me.HScrollBar1.Location = New System.Drawing.Point(59, 49)
        Me.HScrollBar1.Maximum = 1000
        Me.HScrollBar1.Name = "HScrollBar1"
        Me.HScrollBar1.Size = New System.Drawing.Size(120, 13)
        Me.HScrollBar1.TabIndex = 10
        Me.HScrollBar1.Value = 500
        '
        'MCheckbox1
        '
        Me.MCheckbox1.BackColor = System.Drawing.Color.Transparent
        Me.MCheckbox1.Caption = "Show font"
        Me.MCheckbox1.Checked = False
        Me.MCheckbox1.Location = New System.Drawing.Point(4, 12)
        Me.MCheckbox1.Name = "MCheckbox1"
        Me.MCheckbox1.Size = New System.Drawing.Size(76, 11)
        Me.MCheckbox1.TabIndex = 7
        '
        'Button3
        '
        Me.Button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button3.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button3.Location = New System.Drawing.Point(182, 50)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(55, 48)
        Me.Button3.TabIndex = 9
        Me.Button3.Text = "New Random Color"
        Me.Button3.UseCompatibleTextRendering = True
        Me.Button3.UseVisualStyleBackColor = True
        '
        'MCheckbox2
        '
        Me.MCheckbox2.BackColor = System.Drawing.Color.Transparent
        Me.MCheckbox2.Caption = "White background"
        Me.MCheckbox2.Checked = False
        Me.MCheckbox2.Location = New System.Drawing.Point(86, 12)
        Me.MCheckbox2.Name = "MCheckbox2"
        Me.MCheckbox2.Size = New System.Drawing.Size(123, 12)
        Me.MCheckbox2.TabIndex = 8
        '
        'Panel4
        '
        Me.Panel4.BackColor = System.Drawing.Color.Gray
        Me.Panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel4.Controls.Add(Me.Button1)
        Me.Panel4.Controls.Add(Me.Label5)
        Me.Panel4.Location = New System.Drawing.Point(3, 3)
        Me.Panel4.Name = "Panel4"
        Me.Panel4.Size = New System.Drawing.Size(239, 36)
        Me.Panel4.TabIndex = 5
        '
        'Button1
        '
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button1.Location = New System.Drawing.Point(53, 3)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(181, 25)
        Me.Button1.TabIndex = 2
        Me.Button1.Text = "pick"
        Me.Button1.UseCompatibleTextRendering = True
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(1, 9)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(52, 13)
        Me.Label5.TabIndex = 1
        Me.Label5.Text = "filename:"
        '
        'Panel8
        '
        Me.Panel8.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVLB
        Me.Panel8.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel8.Controls.Add(Me.Label3)
        Me.Panel8.Location = New System.Drawing.Point(0, 1)
        Me.Panel8.Name = "Panel8"
        Me.Panel8.Size = New System.Drawing.Size(304, 16)
        Me.Panel8.TabIndex = 74
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.BackColor = System.Drawing.Color.Transparent
        Me.Label3.Font = New System.Drawing.Font("Calibri", 9.0!)
        Me.Label3.Location = New System.Drawing.Point(110, 1)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(62, 14)
        Me.Label3.TabIndex = 0
        Me.Label3.Text = "Edit Menu"
        '
        'ComboBox1
        '
        Me.ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Items.AddRange(New Object() {"Main", "Texture", "Handling", "Frontend Stuffs", "Body", "Wheels", "Suspensions", "Spinner", "Aerial", "AI", "Camera", "Collision", "Carbox", "Shade", "Shadow", "Misc.", "Readme", "Packing"})
        Me.ComboBox1.Location = New System.Drawing.Point(24, 26)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(258, 21)
        Me.ComboBox1.TabIndex = 0
        '
        'TheCarBox
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(622, 508)
        Me.Controls.Add(Me.Panel3)
        Me.Name = "TheCarBox"
        Me.Text = "theCarBox"
        Me.Panel3.ResumeLayout(False)
        Me.Panel9.ResumeLayout(False)
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        Me.Panel4.ResumeLayout(False)
        Me.Panel4.PerformLayout()
        Me.Panel8.ResumeLayout(False)
        Me.Panel8.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents Panel9 As System.Windows.Forms.Panel
    Friend WithEvents Panel4 As System.Windows.Forms.Panel
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Panel8 As System.Windows.Forms.Panel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents MCheckbox1 As CarStudio.mCheckbox
    Friend WithEvents MCheckbox2 As CarStudio.mCheckbox
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents TabPage1 As System.Windows.Forms.TabPage
    Friend WithEvents HScrollBar1 As System.Windows.Forms.HScrollBar
    Friend WithEvents HScrollBar2 As System.Windows.Forms.HScrollBar
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Button4 As System.Windows.Forms.Button
    Friend WithEvents FontDialog1 As System.Windows.Forms.FontDialog
    Friend WithEvents Button5 As System.Windows.Forms.Button
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents HScrollBar3 As System.Windows.Forms.HScrollBar
    Friend WithEvents HScrollBar4 As System.Windows.Forms.HScrollBar
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents HScrollBar5 As System.Windows.Forms.HScrollBar
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents HScrollBar6 As System.Windows.Forms.HScrollBar
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents HScrollBar7 As System.Windows.Forms.HScrollBar
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents MCheckbox3 As CarStudio.mCheckbox
    Friend WithEvents MCheckbox4 As CarStudio.mCheckbox
End Class
