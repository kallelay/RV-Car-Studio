<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Handling
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
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Panel9 = New System.Windows.Forms.FlowLayoutPanel
        Me.Panel5 = New System.Windows.Forms.Panel
        Me.Button3 = New System.Windows.Forms.Button
        Me.NumericUpDown3 = New System.Windows.Forms.NumericUpDown
        Me.Label5 = New System.Windows.Forms.Label
        Me.Panel2 = New System.Windows.Forms.Panel
        Me.Button1 = New System.Windows.Forms.Button
        Me.NumericUpDown1 = New System.Windows.Forms.NumericUpDown
        Me.Label2 = New System.Windows.Forms.Label
        Me.Panel1 = New System.Windows.Forms.Panel
        Me.Button13 = New System.Windows.Forms.Button
        Me.EngRat = New System.Windows.Forms.NumericUpDown
        Me.Label1 = New System.Windows.Forms.Label
        Me.Panel4 = New System.Windows.Forms.Panel
        Me.Button2 = New System.Windows.Forms.Button
        Me.NumericUpDown2 = New System.Windows.Forms.NumericUpDown
        Me.Label4 = New System.Windows.Forms.Label
        Me.Panel6 = New System.Windows.Forms.Panel
        Me.Button4 = New System.Windows.Forms.Button
        Me.NumericUpDown4 = New System.Windows.Forms.NumericUpDown
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label7 = New System.Windows.Forms.Label
        Me.Panel8 = New System.Windows.Forms.Panel
        Me.Label3 = New System.Windows.Forms.Label
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.Panel3.SuspendLayout()
        Me.Panel9.SuspendLayout()
        Me.Panel5.SuspendLayout()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel2.SuspendLayout()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        CType(Me.EngRat, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel4.SuspendLayout()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel6.SuspendLayout()
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel8.SuspendLayout()
        Me.SuspendLayout()
        '
        'Panel3
        '
        Me.Panel3.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVL
        Me.Panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel3.Controls.Add(Me.Panel9)
        Me.Panel3.Controls.Add(Me.Panel8)
        Me.Panel3.Controls.Add(Me.ComboBox1)
        Me.Panel3.Location = New System.Drawing.Point(96, 63)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(304, 350)
        Me.Panel3.TabIndex = 80
        '
        'Panel9
        '
        Me.Panel9.AutoScroll = True
        Me.Panel9.BackColor = System.Drawing.Color.Transparent
        Me.Panel9.Controls.Add(Me.Panel5)
        Me.Panel9.Controls.Add(Me.Panel2)
        Me.Panel9.Controls.Add(Me.Panel1)
        Me.Panel9.Controls.Add(Me.Panel4)
        Me.Panel9.Controls.Add(Me.Panel6)
        Me.Panel9.Controls.Add(Me.Label7)
        Me.Panel9.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.Panel9.Location = New System.Drawing.Point(17, 57)
        Me.Panel9.Margin = New System.Windows.Forms.Padding(0)
        Me.Panel9.Name = "Panel9"
        Me.Panel9.Size = New System.Drawing.Size(263, 284)
        Me.Panel9.TabIndex = 77
        '
        'Panel5
        '
        Me.Panel5.Controls.Add(Me.Button3)
        Me.Panel5.Controls.Add(Me.NumericUpDown3)
        Me.Panel5.Controls.Add(Me.Label5)
        Me.Panel5.Location = New System.Drawing.Point(3, 3)
        Me.Panel5.Name = "Panel5"
        Me.Panel5.Size = New System.Drawing.Size(225, 33)
        Me.Panel5.TabIndex = 84
        '
        'Button3
        '
        Me.Button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button3.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button3.Location = New System.Drawing.Point(134, 8)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(43, 17)
        Me.Button3.TabIndex = 3
        Me.Button3.Text = "restore"
        Me.Button3.UseCompatibleTextRendering = True
        Me.Button3.UseVisualStyleBackColor = True
        '
        'NumericUpDown3
        '
        Me.NumericUpDown3.DecimalPlaces = 4
        Me.NumericUpDown3.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.NumericUpDown3.Location = New System.Drawing.Point(71, 6)
        Me.NumericUpDown3.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown3.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown3.Name = "NumericUpDown3"
        Me.NumericUpDown3.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown3.TabIndex = 23
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(7, 8)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(56, 13)
        Me.Label5.TabIndex = 24
        Me.Label5.Text = "steer Rate"
        '
        'Panel2
        '
        Me.Panel2.Controls.Add(Me.Button1)
        Me.Panel2.Controls.Add(Me.NumericUpDown1)
        Me.Panel2.Controls.Add(Me.Label2)
        Me.Panel2.Location = New System.Drawing.Point(3, 42)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(225, 33)
        Me.Panel2.TabIndex = 82
        '
        'Button1
        '
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button1.Location = New System.Drawing.Point(135, 8)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(43, 17)
        Me.Button1.TabIndex = 3
        Me.Button1.Text = "restore"
        Me.Button1.UseCompatibleTextRendering = True
        Me.Button1.UseVisualStyleBackColor = True
        '
        'NumericUpDown1
        '
        Me.NumericUpDown1.DecimalPlaces = 4
        Me.NumericUpDown1.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.NumericUpDown1.Location = New System.Drawing.Point(72, 6)
        Me.NumericUpDown1.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown1.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown1.Name = "NumericUpDown1"
        Me.NumericUpDown1.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown1.TabIndex = 23
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(7, 8)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(55, 13)
        Me.Label2.TabIndex = 24
        Me.Label2.Text = "steer Mod"
        '
        'Panel1
        '
        Me.Panel1.Controls.Add(Me.Button13)
        Me.Panel1.Controls.Add(Me.EngRat)
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Location = New System.Drawing.Point(3, 81)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(225, 33)
        Me.Panel1.TabIndex = 81
        '
        'Button13
        '
        Me.Button13.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button13.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button13.Location = New System.Drawing.Point(133, 8)
        Me.Button13.Name = "Button13"
        Me.Button13.Size = New System.Drawing.Size(43, 17)
        Me.Button13.TabIndex = 3
        Me.Button13.Text = "restore"
        Me.Button13.UseCompatibleTextRendering = True
        Me.Button13.UseVisualStyleBackColor = True
        '
        'EngRat
        '
        Me.EngRat.DecimalPlaces = 4
        Me.EngRat.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.EngRat.Location = New System.Drawing.Point(70, 6)
        Me.EngRat.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.EngRat.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.EngRat.Name = "EngRat"
        Me.EngRat.Size = New System.Drawing.Size(54, 21)
        Me.EngRat.TabIndex = 23
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(7, 8)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(62, 13)
        Me.Label1.TabIndex = 24
        Me.Label1.Text = "Engine Rate"
        '
        'Panel4
        '
        Me.Panel4.Controls.Add(Me.Button2)
        Me.Panel4.Controls.Add(Me.NumericUpDown2)
        Me.Panel4.Controls.Add(Me.Label4)
        Me.Panel4.Location = New System.Drawing.Point(3, 120)
        Me.Panel4.Name = "Panel4"
        Me.Panel4.Size = New System.Drawing.Size(225, 33)
        Me.Panel4.TabIndex = 83
        '
        'Button2
        '
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button2.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button2.Location = New System.Drawing.Point(134, 8)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(43, 17)
        Me.Button2.TabIndex = 3
        Me.Button2.Text = "restore"
        Me.Button2.UseCompatibleTextRendering = True
        Me.Button2.UseVisualStyleBackColor = True
        '
        'NumericUpDown2
        '
        Me.NumericUpDown2.DecimalPlaces = 4
        Me.NumericUpDown2.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.NumericUpDown2.Location = New System.Drawing.Point(71, 6)
        Me.NumericUpDown2.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown2.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown2.Name = "NumericUpDown2"
        Me.NumericUpDown2.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown2.TabIndex = 23
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(7, 8)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(54, 13)
        Me.Label4.TabIndex = 24
        Me.Label4.Text = "Top Speed"
        '
        'Panel6
        '
        Me.Panel6.Controls.Add(Me.Button4)
        Me.Panel6.Controls.Add(Me.NumericUpDown4)
        Me.Panel6.Controls.Add(Me.Label6)
        Me.Panel6.Location = New System.Drawing.Point(3, 159)
        Me.Panel6.Name = "Panel6"
        Me.Panel6.Size = New System.Drawing.Size(225, 33)
        Me.Panel6.TabIndex = 85
        '
        'Button4
        '
        Me.Button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button4.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button4.Location = New System.Drawing.Point(134, 8)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(43, 17)
        Me.Button4.TabIndex = 3
        Me.Button4.Text = "restore"
        Me.Button4.UseCompatibleTextRendering = True
        Me.Button4.UseVisualStyleBackColor = True
        '
        'NumericUpDown4
        '
        Me.NumericUpDown4.DecimalPlaces = 4
        Me.NumericUpDown4.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.NumericUpDown4.Location = New System.Drawing.Point(71, 6)
        Me.NumericUpDown4.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown4.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown4.Name = "NumericUpDown4"
        Me.NumericUpDown4.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown4.TabIndex = 23
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(4, 8)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(69, 13)
        Me.Label6.TabIndex = 24
        Me.Label6.Text = "Dwnfrce Mod"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(3, 195)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(207, 13)
        Me.Label7.TabIndex = 86
        Me.Label7.Text = "CoM and weapon are found in body section"
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
        'Handling
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(497, 477)
        Me.Controls.Add(Me.Panel3)
        Me.Name = "Handling"
        Me.Text = "Handling"
        Me.Panel3.ResumeLayout(False)
        Me.Panel9.ResumeLayout(False)
        Me.Panel9.PerformLayout()
        Me.Panel5.ResumeLayout(False)
        Me.Panel5.PerformLayout()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.EngRat, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel4.ResumeLayout(False)
        Me.Panel4.PerformLayout()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel6.ResumeLayout(False)
        Me.Panel6.PerformLayout()
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel8.ResumeLayout(False)
        Me.Panel8.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents Panel9 As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents EngRat As System.Windows.Forms.NumericUpDown
    Friend WithEvents Button13 As System.Windows.Forms.Button
    Friend WithEvents Panel8 As System.Windows.Forms.Panel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Panel5 As System.Windows.Forms.Panel
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents NumericUpDown3 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Panel2 As System.Windows.Forms.Panel
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents NumericUpDown1 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Panel4 As System.Windows.Forms.Panel
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents NumericUpDown2 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Panel6 As System.Windows.Forms.Panel
    Friend WithEvents Button4 As System.Windows.Forms.Button
    Friend WithEvents NumericUpDown4 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
End Class
