<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class shade
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
        Me.Timer2 = New System.Windows.Forms.Timer(Me.components)
        Me.Button3 = New System.Windows.Forms.Button
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog
        Me.TextBox3 = New System.Windows.Forms.TextBox
        Me.Button2 = New System.Windows.Forms.Button
        Me.TextBox2 = New System.Windows.Forms.TextBox
        Me.Button1 = New System.Windows.Forms.Button
        Me.GlControl1 = New OpenTK.GLControl
        Me.ColorDialog1 = New System.Windows.Forms.ColorDialog
        Me.Button7 = New System.Windows.Forms.Button
        Me.Button6 = New System.Windows.Forms.Button
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Panel9 = New System.Windows.Forms.Panel
        Me.Button9 = New System.Windows.Forms.Button
        Me.Button8 = New System.Windows.Forms.Button
        Me.RadioButton2 = New System.Windows.Forms.RadioButton
        Me.RadioButton1 = New System.Windows.Forms.RadioButton
        Me.GroupBox5 = New System.Windows.Forms.GroupBox
        Me.Button4 = New System.Windows.Forms.Button
        Me.NumericUpDown10 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown11 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown12 = New System.Windows.Forms.NumericUpDown
        Me.GroupBox4 = New System.Windows.Forms.GroupBox
        Me.CheckBox1 = New System.Windows.Forms.CheckBox
        Me.Label3 = New System.Windows.Forms.Label
        Me.NumericUpDown9 = New System.Windows.Forms.NumericUpDown
        Me.Label2 = New System.Windows.Forms.Label
        Me.NumericUpDown4 = New System.Windows.Forms.NumericUpDown
        Me.GroupBox3 = New System.Windows.Forms.GroupBox
        Me._y = New System.Windows.Forms.NumericUpDown
        Me._x = New System.Windows.Forms.NumericUpDown
        Me._z = New System.Windows.Forms.NumericUpDown
        Me.GroupBox2 = New System.Windows.Forms.GroupBox
        Me.NumericUpDown2 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown1 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown3 = New System.Windows.Forms.NumericUpDown
        Me.Panel8 = New System.Windows.Forms.Panel
        Me.Label32 = New System.Windows.Forms.Label
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog
        Me.Panel3.SuspendLayout()
        Me.Panel9.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        CType(Me.NumericUpDown10, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown11, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown12, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox4.SuspendLayout()
        CType(Me.NumericUpDown9, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox3.SuspendLayout()
        CType(Me._y, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me._x, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me._z, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel8.SuspendLayout()
        Me.SuspendLayout()
        '
        'Timer2
        '
        Me.Timer2.Interval = 5
        '
        'Button3
        '
        Me.Button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button3.Location = New System.Drawing.Point(12, 245)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(71, 33)
        Me.Button3.TabIndex = 81
        Me.Button3.Text = "Shade!"
        Me.Button3.UseCompatibleTextRendering = True
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 10000
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.Filter = "RIKO save file|*.riko"
        '
        'TextBox3
        '
        Me.TextBox3.BackColor = System.Drawing.Color.AliceBlue
        Me.TextBox3.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TextBox3.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.TextBox3.Location = New System.Drawing.Point(647, 46)
        Me.TextBox3.Multiline = True
        Me.TextBox3.Name = "TextBox3"
        Me.TextBox3.ReadOnly = True
        Me.TextBox3.Size = New System.Drawing.Size(253, 221)
        Me.TextBox3.TabIndex = 61
        '
        'Button2
        '
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button2.Location = New System.Drawing.Point(549, 387)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(37, 24)
        Me.Button2.TabIndex = 59
        Me.Button2.Text = "..."
        Me.Button2.UseVisualStyleBackColor = True
        '
        'TextBox2
        '
        Me.TextBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.TextBox2.Font = New System.Drawing.Font("Calibri", 10.0!)
        Me.TextBox2.Location = New System.Drawing.Point(324, 387)
        Me.TextBox2.Name = "TextBox2"
        Me.TextBox2.Size = New System.Drawing.Size(219, 24)
        Me.TextBox2.TabIndex = 60
        '
        'Button1
        '
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Location = New System.Drawing.Point(730, 273)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(160, 43)
        Me.Button1.TabIndex = 58
        Me.Button1.Text = "&Render"
        Me.Button1.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.Button1.UseVisualStyleBackColor = True
        '
        'GlControl1
        '
        Me.GlControl1.BackColor = System.Drawing.Color.AliceBlue
        Me.GlControl1.Location = New System.Drawing.Point(324, 46)
        Me.GlControl1.Name = "GlControl1"
        Me.GlControl1.Size = New System.Drawing.Size(336, 317)
        Me.GlControl1.TabIndex = 72
        Me.GlControl1.VSync = False
        '
        'ColorDialog1
        '
        Me.ColorDialog1.Color = System.Drawing.Color.White
        '
        'Button7
        '
        Me.Button7.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button7.Location = New System.Drawing.Point(730, 293)
        Me.Button7.Name = "Button7"
        Me.Button7.Size = New System.Drawing.Size(103, 23)
        Me.Button7.TabIndex = 66
        Me.Button7.Text = "Color by Poly"
        Me.Button7.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.Button7.UseVisualStyleBackColor = True
        '
        'Button6
        '
        Me.Button6.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button6.Location = New System.Drawing.Point(730, 273)
        Me.Button6.Name = "Button6"
        Me.Button6.Size = New System.Drawing.Size(103, 23)
        Me.Button6.TabIndex = 65
        Me.Button6.Text = "Color by Vertex"
        Me.Button6.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.Button6.UseVisualStyleBackColor = True
        '
        'Panel3
        '
        Me.Panel3.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVL
        Me.Panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel3.Controls.Add(Me.Panel9)
        Me.Panel3.Controls.Add(Me.Panel8)
        Me.Panel3.Controls.Add(Me.ComboBox1)
        Me.Panel3.Location = New System.Drawing.Point(14, 25)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(304, 350)
        Me.Panel3.TabIndex = 87
        '
        'Panel9
        '
        Me.Panel9.Controls.Add(Me.Button9)
        Me.Panel9.Controls.Add(Me.Button8)
        Me.Panel9.Controls.Add(Me.RadioButton2)
        Me.Panel9.Controls.Add(Me.Button3)
        Me.Panel9.Controls.Add(Me.RadioButton1)
        Me.Panel9.Controls.Add(Me.GroupBox5)
        Me.Panel9.Controls.Add(Me.GroupBox4)
        Me.Panel9.Controls.Add(Me.Label2)
        Me.Panel9.Controls.Add(Me.NumericUpDown4)
        Me.Panel9.Controls.Add(Me.GroupBox3)
        Me.Panel9.Controls.Add(Me.GroupBox2)
        Me.Panel9.Location = New System.Drawing.Point(12, 53)
        Me.Panel9.Name = "Panel9"
        Me.Panel9.Size = New System.Drawing.Size(270, 285)
        Me.Panel9.TabIndex = 78
        '
        'Button9
        '
        Me.Button9.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button9.Location = New System.Drawing.Point(84, 262)
        Me.Button9.Name = "Button9"
        Me.Button9.Size = New System.Drawing.Size(71, 16)
        Me.Button9.TabIndex = 83
        Me.Button9.Text = "Save"
        Me.Button9.UseCompatibleTextRendering = True
        Me.Button9.UseVisualStyleBackColor = True
        '
        'Button8
        '
        Me.Button8.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button8.Location = New System.Drawing.Point(84, 245)
        Me.Button8.Name = "Button8"
        Me.Button8.Size = New System.Drawing.Size(71, 16)
        Me.Button8.TabIndex = 82
        Me.Button8.Text = "Load"
        Me.Button8.UseCompatibleTextRendering = True
        Me.Button8.UseVisualStyleBackColor = True
        '
        'RadioButton2
        '
        Me.RadioButton2.AutoSize = True
        Me.RadioButton2.Location = New System.Drawing.Point(182, 239)
        Me.RadioButton2.Name = "RadioButton2"
        Me.RadioButton2.Size = New System.Drawing.Size(74, 17)
        Me.RadioButton2.TabIndex = 63
        Me.RadioButton2.Text = "by polygon"
        Me.RadioButton2.UseVisualStyleBackColor = True
        '
        'RadioButton1
        '
        Me.RadioButton1.AutoSize = True
        Me.RadioButton1.Checked = True
        Me.RadioButton1.Location = New System.Drawing.Point(182, 261)
        Me.RadioButton1.Name = "RadioButton1"
        Me.RadioButton1.Size = New System.Drawing.Size(66, 17)
        Me.RadioButton1.TabIndex = 63
        Me.RadioButton1.TabStop = True
        Me.RadioButton1.Text = "by vertex"
        Me.RadioButton1.UseVisualStyleBackColor = True
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.Button4)
        Me.GroupBox5.Controls.Add(Me.NumericUpDown10)
        Me.GroupBox5.Controls.Add(Me.NumericUpDown11)
        Me.GroupBox5.Controls.Add(Me.NumericUpDown12)
        Me.GroupBox5.Location = New System.Drawing.Point(15, 183)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(235, 56)
        Me.GroupBox5.TabIndex = 62
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "Main Color"
        '
        'Button4
        '
        Me.Button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button4.Location = New System.Drawing.Point(192, 26)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(37, 24)
        Me.Button4.TabIndex = 32
        Me.Button4.Text = "..."
        Me.Button4.UseVisualStyleBackColor = True
        '
        'NumericUpDown10
        '
        Me.NumericUpDown10.Location = New System.Drawing.Point(134, 28)
        Me.NumericUpDown10.Maximum = New Decimal(New Integer() {255, 0, 0, 0})
        Me.NumericUpDown10.Name = "NumericUpDown10"
        Me.NumericUpDown10.Size = New System.Drawing.Size(40, 21)
        Me.NumericUpDown10.TabIndex = 34
        Me.NumericUpDown10.Value = New Decimal(New Integer() {255, 0, 0, 0})
        '
        'NumericUpDown11
        '
        Me.NumericUpDown11.Location = New System.Drawing.Point(75, 28)
        Me.NumericUpDown11.Maximum = New Decimal(New Integer() {255, 0, 0, 0})
        Me.NumericUpDown11.Name = "NumericUpDown11"
        Me.NumericUpDown11.Size = New System.Drawing.Size(40, 21)
        Me.NumericUpDown11.TabIndex = 33
        Me.NumericUpDown11.Value = New Decimal(New Integer() {255, 0, 0, 0})
        '
        'NumericUpDown12
        '
        Me.NumericUpDown12.Location = New System.Drawing.Point(18, 28)
        Me.NumericUpDown12.Maximum = New Decimal(New Integer() {255, 0, 0, 0})
        Me.NumericUpDown12.Name = "NumericUpDown12"
        Me.NumericUpDown12.Size = New System.Drawing.Size(40, 21)
        Me.NumericUpDown12.TabIndex = 32
        Me.NumericUpDown12.Value = New Decimal(New Integer() {255, 0, 0, 0})
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.CheckBox1)
        Me.GroupBox4.Controls.Add(Me.Label3)
        Me.GroupBox4.Controls.Add(Me.NumericUpDown9)
        Me.GroupBox4.Location = New System.Drawing.Point(15, 130)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(235, 51)
        Me.GroupBox4.TabIndex = 61
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Contrast"
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Location = New System.Drawing.Point(25, 23)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(81, 17)
        Me.CheckBox1.TabIndex = 60
        Me.CheckBox1.Text = "use contrast"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(40, 27)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(46, 13)
        Me.Label3.TabIndex = 59
        Me.Label3.Text = "Contrast"
        '
        'NumericUpDown9
        '
        Me.NumericUpDown9.DecimalPlaces = 2
        Me.NumericUpDown9.Increment = New Decimal(New Integer() {1, 0, 0, 131072})
        Me.NumericUpDown9.Location = New System.Drawing.Point(134, 20)
        Me.NumericUpDown9.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericUpDown9.Name = "NumericUpDown9"
        Me.NumericUpDown9.Size = New System.Drawing.Size(62, 21)
        Me.NumericUpDown9.TabIndex = 58
        Me.NumericUpDown9.Value = New Decimal(New Integer() {7, 0, 0, 65536})
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(21, 114)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(52, 13)
        Me.Label2.TabIndex = 49
        Me.Label2.Text = "Multiplier:"
        '
        'NumericUpDown4
        '
        Me.NumericUpDown4.DecimalPlaces = 2
        Me.NumericUpDown4.Increment = New Decimal(New Integer() {1, 0, 0, 131072})
        Me.NumericUpDown4.Location = New System.Drawing.Point(166, 112)
        Me.NumericUpDown4.Maximum = New Decimal(New Integer() {5, 0, 0, 0})
        Me.NumericUpDown4.Name = "NumericUpDown4"
        Me.NumericUpDown4.Size = New System.Drawing.Size(62, 21)
        Me.NumericUpDown4.TabIndex = 48
        Me.NumericUpDown4.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me._y)
        Me.GroupBox3.Controls.Add(Me._x)
        Me.GroupBox3.Controls.Add(Me._z)
        Me.GroupBox3.Location = New System.Drawing.Point(15, 55)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(241, 51)
        Me.GroupBox3.TabIndex = 0
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Light position"
        '
        '_y
        '
        Me._y.DecimalPlaces = 2
        Me._y.Increment = New Decimal(New Integer() {1, 0, 0, 131072})
        Me._y.Location = New System.Drawing.Point(86, 20)
        Me._y.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me._y.Minimum = New Decimal(New Integer() {1, 0, 0, 131072})
        Me._y.Name = "_y"
        Me._y.Size = New System.Drawing.Size(54, 21)
        Me._y.TabIndex = 54
        Me._y.Value = New Decimal(New Integer() {5, 0, 0, 0})
        '
        '_x
        '
        Me._x.DecimalPlaces = 2
        Me._x.Increment = New Decimal(New Integer() {1, 0, 0, 131072})
        Me._x.Location = New System.Drawing.Point(6, 20)
        Me._x.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me._x.Minimum = New Decimal(New Integer() {1, 0, 0, 131072})
        Me._x.Name = "_x"
        Me._x.Size = New System.Drawing.Size(62, 21)
        Me._x.TabIndex = 53
        Me._x.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        '_z
        '
        Me._z.DecimalPlaces = 2
        Me._z.Increment = New Decimal(New Integer() {1, 0, 0, 131072})
        Me._z.Location = New System.Drawing.Point(165, 20)
        Me._z.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me._z.Minimum = New Decimal(New Integer() {1, 0, 0, 131072})
        Me._z.Name = "_z"
        Me._z.Size = New System.Drawing.Size(70, 21)
        Me._z.TabIndex = 52
        Me._z.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.NumericUpDown2)
        Me.GroupBox2.Controls.Add(Me.NumericUpDown1)
        Me.GroupBox2.Controls.Add(Me.NumericUpDown3)
        Me.GroupBox2.Location = New System.Drawing.Point(15, -2)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(241, 51)
        Me.GroupBox2.TabIndex = 0
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Light normal"
        '
        'NumericUpDown2
        '
        Me.NumericUpDown2.DecimalPlaces = 2
        Me.NumericUpDown2.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.NumericUpDown2.Location = New System.Drawing.Point(86, 20)
        Me.NumericUpDown2.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericUpDown2.Minimum = New Decimal(New Integer() {1, 0, 0, -2147483648})
        Me.NumericUpDown2.Name = "NumericUpDown2"
        Me.NumericUpDown2.Size = New System.Drawing.Size(54, 21)
        Me.NumericUpDown2.TabIndex = 48
        Me.NumericUpDown2.Value = New Decimal(New Integer() {1, 0, 0, -2147483648})
        '
        'NumericUpDown1
        '
        Me.NumericUpDown1.DecimalPlaces = 2
        Me.NumericUpDown1.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.NumericUpDown1.Location = New System.Drawing.Point(6, 20)
        Me.NumericUpDown1.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericUpDown1.Minimum = New Decimal(New Integer() {1, 0, 0, -2147483648})
        Me.NumericUpDown1.Name = "NumericUpDown1"
        Me.NumericUpDown1.Size = New System.Drawing.Size(62, 21)
        Me.NumericUpDown1.TabIndex = 47
        '
        'NumericUpDown3
        '
        Me.NumericUpDown3.DecimalPlaces = 2
        Me.NumericUpDown3.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.NumericUpDown3.Location = New System.Drawing.Point(165, 20)
        Me.NumericUpDown3.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericUpDown3.Minimum = New Decimal(New Integer() {1, 0, 0, -2147483648})
        Me.NumericUpDown3.Name = "NumericUpDown3"
        Me.NumericUpDown3.Size = New System.Drawing.Size(70, 21)
        Me.NumericUpDown3.TabIndex = 46
        '
        'Panel8
        '
        Me.Panel8.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVLB
        Me.Panel8.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel8.Controls.Add(Me.Label32)
        Me.Panel8.Location = New System.Drawing.Point(0, 1)
        Me.Panel8.Name = "Panel8"
        Me.Panel8.Size = New System.Drawing.Size(304, 16)
        Me.Panel8.TabIndex = 74
        '
        'Label32
        '
        Me.Label32.AutoSize = True
        Me.Label32.BackColor = System.Drawing.Color.Transparent
        Me.Label32.Font = New System.Drawing.Font("Calibri", 9.0!)
        Me.Label32.Location = New System.Drawing.Point(110, 1)
        Me.Label32.Name = "Label32"
        Me.Label32.Size = New System.Drawing.Size(62, 14)
        Me.Label32.TabIndex = 0
        Me.Label32.Text = "Edit Menu"
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
        'SaveFileDialog1
        '
        Me.SaveFileDialog1.Filter = "RIKO save file|*.riko"
        '
        'shade
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.DimGray
        Me.ClientSize = New System.Drawing.Size(921, 556)
        Me.Controls.Add(Me.Panel3)
        Me.Controls.Add(Me.TextBox3)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.TextBox2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.GlControl1)
        Me.Controls.Add(Me.Button7)
        Me.Controls.Add(Me.Button6)
        Me.Font = New System.Drawing.Font("Calibri Light", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Name = "shade"
        Me.Text = "shade"
        Me.Panel3.ResumeLayout(False)
        Me.Panel9.ResumeLayout(False)
        Me.Panel9.PerformLayout()
        Me.GroupBox5.ResumeLayout(False)
        CType(Me.NumericUpDown10, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown11, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown12, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        CType(Me.NumericUpDown9, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox3.ResumeLayout(False)
        CType(Me._y, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me._x, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me._z, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel8.ResumeLayout(False)
        Me.Panel8.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Timer2 As System.Windows.Forms.Timer
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents TextBox3 As System.Windows.Forms.TextBox
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents TextBox2 As System.Windows.Forms.TextBox
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents GlControl1 As OpenTK.GLControl
    Friend WithEvents ColorDialog1 As System.Windows.Forms.ColorDialog
    Friend WithEvents Button7 As System.Windows.Forms.Button
    Friend WithEvents Button6 As System.Windows.Forms.Button
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents Panel8 As System.Windows.Forms.Panel
    Friend WithEvents Label32 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Panel9 As System.Windows.Forms.Panel
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents _y As System.Windows.Forms.NumericUpDown
    Friend WithEvents _x As System.Windows.Forms.NumericUpDown
    Friend WithEvents _z As System.Windows.Forms.NumericUpDown
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents NumericUpDown2 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown1 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown3 As System.Windows.Forms.NumericUpDown
    Friend WithEvents RadioButton2 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton1 As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents Button4 As System.Windows.Forms.Button
    Friend WithEvents NumericUpDown10 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown11 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown12 As System.Windows.Forms.NumericUpDown
    Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents NumericUpDown9 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents NumericUpDown4 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Button9 As System.Windows.Forms.Button
    Friend WithEvents Button8 As System.Windows.Forms.Button
    Friend WithEvents SaveFileDialog1 As System.Windows.Forms.SaveFileDialog
End Class
