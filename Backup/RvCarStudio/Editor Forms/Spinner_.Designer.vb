<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Spinner_
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
        Me.components = New System.ComponentModel.Container
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Panel9 = New System.Windows.Forms.FlowLayoutPanel
        Me.Panel4 = New System.Windows.Forms.Panel
        Me.Button45 = New System.Windows.Forms.Button
        Me.Label5 = New System.Windows.Forms.Label
        Me.TabControl1 = New System.Windows.Forms.TabControl
        Me.TabPage1 = New System.Windows.Forms.TabPage
        Me.CheckBox1 = New System.Windows.Forms.CheckBox
        Me.GroupBox2 = New System.Windows.Forms.GroupBox
        Me.Button2 = New System.Windows.Forms.Button
        Me.NumericUpDown4 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown5 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown6 = New System.Windows.Forms.NumericUpDown
        Me.GroupBox3 = New System.Windows.Forms.GroupBox
        Me.Button1 = New System.Windows.Forms.Button
        Me.NumericUpDown9 = New System.Windows.Forms.NumericUpDown
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.Button3 = New System.Windows.Forms.Button
        Me.NumericUpDown3 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown2 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown1 = New System.Windows.Forms.NumericUpDown
        Me.Panel8 = New System.Windows.Forms.Panel
        Me.Label3 = New System.Windows.Forms.Label
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.Panel3.SuspendLayout()
        Me.Panel9.SuspendLayout()
        Me.Panel4.SuspendLayout()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown5, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown6, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox3.SuspendLayout()
        CType(Me.NumericUpDown9, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox1.SuspendLayout()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel8.SuspendLayout()
        Me.SuspendLayout()
        '
        'Timer1
        '
        Me.Timer1.Interval = 1
        '
        'Panel3
        '
        Me.Panel3.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVL
        Me.Panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel3.Controls.Add(Me.Panel9)
        Me.Panel3.Controls.Add(Me.Panel8)
        Me.Panel3.Controls.Add(Me.ComboBox1)
        Me.Panel3.Location = New System.Drawing.Point(168, 38)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(304, 350)
        Me.Panel3.TabIndex = 79
        '
        'Panel9
        '
        Me.Panel9.AutoScroll = True
        Me.Panel9.BackColor = System.Drawing.Color.Transparent
        Me.Panel9.Controls.Add(Me.Panel4)
        Me.Panel9.Controls.Add(Me.TabControl1)
        Me.Panel9.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.Panel9.Location = New System.Drawing.Point(17, 57)
        Me.Panel9.Margin = New System.Windows.Forms.Padding(0)
        Me.Panel9.Name = "Panel9"
        Me.Panel9.Size = New System.Drawing.Size(263, 277)
        Me.Panel9.TabIndex = 77
        '
        'Panel4
        '
        Me.Panel4.BackColor = System.Drawing.Color.Transparent
        Me.Panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel4.Controls.Add(Me.Button45)
        Me.Panel4.Controls.Add(Me.Label5)
        Me.Panel4.Location = New System.Drawing.Point(3, 3)
        Me.Panel4.Name = "Panel4"
        Me.Panel4.Size = New System.Drawing.Size(239, 28)
        Me.Panel4.TabIndex = 4
        '
        'Button45
        '
        Me.Button45.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button45.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button45.Location = New System.Drawing.Point(51, 0)
        Me.Button45.Name = "Button45"
        Me.Button45.Size = New System.Drawing.Size(181, 25)
        Me.Button45.TabIndex = 2
        Me.Button45.Text = "pick"
        Me.Button45.UseCompatibleTextRendering = True
        Me.Button45.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(0, 6)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(52, 13)
        Me.Label5.TabIndex = 1
        Me.Label5.Text = "filename:"
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Location = New System.Drawing.Point(0, 34)
        Me.TabControl1.Margin = New System.Windows.Forms.Padding(0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.Padding = New System.Drawing.Point(0, 0)
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(260, 236)
        Me.TabControl1.TabIndex = 5
        '
        'TabPage1
        '
        Me.TabPage1.BackColor = System.Drawing.Color.Gray
        Me.TabPage1.Controls.Add(Me.CheckBox1)
        Me.TabPage1.Controls.Add(Me.GroupBox2)
        Me.TabPage1.Controls.Add(Me.GroupBox3)
        Me.TabPage1.Controls.Add(Me.GroupBox1)
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(252, 210)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "Positions"
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Location = New System.Drawing.Point(85, 166)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(66, 17)
        Me.CheckBox1.TabIndex = 10
        Me.CheckBox1.Text = "animate"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.Button2)
        Me.GroupBox2.Controls.Add(Me.NumericUpDown4)
        Me.GroupBox2.Controls.Add(Me.NumericUpDown5)
        Me.GroupBox2.Controls.Add(Me.NumericUpDown6)
        Me.GroupBox2.Location = New System.Drawing.Point(2, 71)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(246, 47)
        Me.GroupBox2.TabIndex = 9
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Axis"
        '
        'Button2
        '
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button2.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button2.Location = New System.Drawing.Point(189, 20)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(57, 17)
        Me.Button2.TabIndex = 8
        Me.Button2.Text = "restore"
        Me.Button2.UseCompatibleTextRendering = True
        Me.Button2.UseVisualStyleBackColor = True
        '
        'NumericUpDown4
        '
        Me.NumericUpDown4.BackColor = System.Drawing.Color.DimGray
        Me.NumericUpDown4.DecimalPlaces = 4
        Me.NumericUpDown4.Location = New System.Drawing.Point(133, 16)
        Me.NumericUpDown4.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown4.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown4.Name = "NumericUpDown4"
        Me.NumericUpDown4.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown4.TabIndex = 7
        '
        'NumericUpDown5
        '
        Me.NumericUpDown5.BackColor = System.Drawing.Color.DimGray
        Me.NumericUpDown5.DecimalPlaces = 4
        Me.NumericUpDown5.Location = New System.Drawing.Point(67, 16)
        Me.NumericUpDown5.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown5.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown5.Name = "NumericUpDown5"
        Me.NumericUpDown5.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown5.TabIndex = 6
        '
        'NumericUpDown6
        '
        Me.NumericUpDown6.BackColor = System.Drawing.Color.DimGray
        Me.NumericUpDown6.DecimalPlaces = 4
        Me.NumericUpDown6.Location = New System.Drawing.Point(2, 16)
        Me.NumericUpDown6.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown6.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown6.Name = "NumericUpDown6"
        Me.NumericUpDown6.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown6.TabIndex = 5
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.Button1)
        Me.GroupBox3.Controls.Add(Me.NumericUpDown9)
        Me.GroupBox3.Location = New System.Drawing.Point(0, 124)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(246, 36)
        Me.GroupBox3.TabIndex = 9
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Speed (Angular Velocity)"
        '
        'Button1
        '
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button1.Location = New System.Drawing.Point(186, 13)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(57, 17)
        Me.Button1.TabIndex = 8
        Me.Button1.Text = "restore"
        Me.Button1.UseCompatibleTextRendering = True
        Me.Button1.UseVisualStyleBackColor = True
        '
        'NumericUpDown9
        '
        Me.NumericUpDown9.BackColor = System.Drawing.Color.DimGray
        Me.NumericUpDown9.DecimalPlaces = 4
        Me.NumericUpDown9.Location = New System.Drawing.Point(77, 13)
        Me.NumericUpDown9.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown9.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown9.Name = "NumericUpDown9"
        Me.NumericUpDown9.Size = New System.Drawing.Size(84, 21)
        Me.NumericUpDown9.TabIndex = 5
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.Button3)
        Me.GroupBox1.Controls.Add(Me.NumericUpDown3)
        Me.GroupBox1.Controls.Add(Me.NumericUpDown2)
        Me.GroupBox1.Controls.Add(Me.NumericUpDown1)
        Me.GroupBox1.Location = New System.Drawing.Point(0, 6)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(246, 47)
        Me.GroupBox1.TabIndex = 1
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "position (Offset)"
        '
        'Button3
        '
        Me.Button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button3.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button3.Location = New System.Drawing.Point(189, 20)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(57, 17)
        Me.Button3.TabIndex = 8
        Me.Button3.Text = "restore"
        Me.Button3.UseCompatibleTextRendering = True
        Me.Button3.UseVisualStyleBackColor = True
        '
        'NumericUpDown3
        '
        Me.NumericUpDown3.BackColor = System.Drawing.Color.DimGray
        Me.NumericUpDown3.DecimalPlaces = 4
        Me.NumericUpDown3.Location = New System.Drawing.Point(133, 16)
        Me.NumericUpDown3.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown3.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown3.Name = "NumericUpDown3"
        Me.NumericUpDown3.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown3.TabIndex = 7
        '
        'NumericUpDown2
        '
        Me.NumericUpDown2.BackColor = System.Drawing.Color.DimGray
        Me.NumericUpDown2.DecimalPlaces = 4
        Me.NumericUpDown2.Location = New System.Drawing.Point(67, 16)
        Me.NumericUpDown2.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown2.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown2.Name = "NumericUpDown2"
        Me.NumericUpDown2.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown2.TabIndex = 6
        '
        'NumericUpDown1
        '
        Me.NumericUpDown1.BackColor = System.Drawing.Color.DimGray
        Me.NumericUpDown1.DecimalPlaces = 4
        Me.NumericUpDown1.Location = New System.Drawing.Point(2, 16)
        Me.NumericUpDown1.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown1.Minimum = New Decimal(New Integer() {100000, 0, 0, -2147483648})
        Me.NumericUpDown1.Name = "NumericUpDown1"
        Me.NumericUpDown1.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown1.TabIndex = 5
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
        'Spinner_
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(641, 426)
        Me.Controls.Add(Me.Panel3)
        Me.Name = "Spinner_"
        Me.Text = "Spinner_"
        Me.Panel3.ResumeLayout(False)
        Me.Panel9.ResumeLayout(False)
        Me.Panel4.ResumeLayout(False)
        Me.Panel4.PerformLayout()
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown5, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown6, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox3.ResumeLayout(False)
        CType(Me.NumericUpDown9, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox1.ResumeLayout(False)
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel8.ResumeLayout(False)
        Me.Panel8.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents Panel9 As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents Panel4 As System.Windows.Forms.Panel
    Friend WithEvents Button45 As System.Windows.Forms.Button
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents TabPage1 As System.Windows.Forms.TabPage
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents NumericUpDown3 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown2 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown1 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Panel8 As System.Windows.Forms.Panel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents NumericUpDown4 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown5 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown6 As System.Windows.Forms.NumericUpDown
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents NumericUpDown9 As System.Windows.Forms.NumericUpDown
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
End Class
