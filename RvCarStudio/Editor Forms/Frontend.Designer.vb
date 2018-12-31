<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Frontend
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Frontend))
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Panel9 = New System.Windows.Forms.Panel
        Me.TabControl1 = New System.Windows.Forms.TabControl
        Me.TabPage1 = New System.Windows.Forms.TabPage
        Me.CPUselectable = New CarStudio.mCheckbox
        Me.MaxRevs = New System.Windows.Forms.Label
        Me.MaxRev = New System.Windows.Forms.NumericUpDown
        Me.GroupBox2 = New System.Windows.Forms.GroupBox
        Me.RadioButton3 = New System.Windows.Forms.RadioButton
        Me.RadioButton2 = New System.Windows.Forms.RadioButton
        Me.RadioButton1 = New System.Windows.Forms.RadioButton
        Me.Statistics = New CarStudio.mCheckbox
        Me.Selectable = New CarStudio.mCheckbox
        Me.Besttime = New CarStudio.mCheckbox
        Me.TabPage2 = New System.Windows.Forms.TabPage
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.Advanced = New System.Windows.Forms.RadioButton
        Me.Amateur = New System.Windows.Forms.RadioButton
        Me.Pro = New System.Windows.Forms.RadioButton
        Me.Semi_Pro = New System.Windows.Forms.RadioButton
        Me.Rookie = New System.Windows.Forms.RadioButton
        Me.TabPage3 = New System.Windows.Forms.TabPage
        Me._champ = New System.Windows.Forms.RadioButton
        Me._practice = New System.Windows.Forms.RadioButton
        Me._timetrial = New System.Windows.Forms.RadioButton
        Me._always = New System.Windows.Forms.RadioButton
        Me._single = New System.Windows.Forms.RadioButton
        Me._Never = New System.Windows.Forms.RadioButton
        Me.TabPage4 = New System.Windows.Forms.TabPage
        Me.NumericUpDown4 = New System.Windows.Forms.NumericUpDown
        Me.Label5 = New System.Windows.Forms.Label
        Me.NumericUpDown3 = New System.Windows.Forms.NumericUpDown
        Me.Label4 = New System.Windows.Forms.Label
        Me.NumericUpDown2 = New System.Windows.Forms.NumericUpDown
        Me.Label2 = New System.Windows.Forms.Label
        Me.NumericUpDown1 = New System.Windows.Forms.NumericUpDown
        Me.Label1 = New System.Windows.Forms.Label
        Me.Panel8 = New System.Windows.Forms.Panel
        Me.Label3 = New System.Windows.Forms.Label
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.Panel3.SuspendLayout()
        Me.Panel9.SuspendLayout()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        CType(Me.MaxRev, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        Me.TabPage2.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.TabPage3.SuspendLayout()
        Me.TabPage4.SuspendLayout()
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).BeginInit()
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
        Me.Panel3.Location = New System.Drawing.Point(234, 108)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(304, 350)
        Me.Panel3.TabIndex = 78
        '
        'Panel9
        '
        Me.Panel9.BackColor = System.Drawing.Color.DimGray
        Me.Panel9.Controls.Add(Me.TabControl1)
        Me.Panel9.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.Panel9.Location = New System.Drawing.Point(30, 54)
        Me.Panel9.Name = "Panel9"
        Me.Panel9.Size = New System.Drawing.Size(251, 280)
        Me.Panel9.TabIndex = 76
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Controls.Add(Me.TabPage2)
        Me.TabControl1.Controls.Add(Me.TabPage3)
        Me.TabControl1.Controls.Add(Me.TabPage4)
        Me.TabControl1.HotTrack = True
        Me.TabControl1.Location = New System.Drawing.Point(0, 3)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(252, 277)
        Me.TabControl1.TabIndex = 2
        '
        'TabPage1
        '
        Me.TabPage1.BackColor = System.Drawing.Color.DimGray
        Me.TabPage1.Controls.Add(Me.CPUselectable)
        Me.TabPage1.Controls.Add(Me.MaxRevs)
        Me.TabPage1.Controls.Add(Me.MaxRev)
        Me.TabPage1.Controls.Add(Me.GroupBox2)
        Me.TabPage1.Controls.Add(Me.Statistics)
        Me.TabPage1.Controls.Add(Me.Selectable)
        Me.TabPage1.Controls.Add(Me.Besttime)
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(244, 251)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "Main"
        '
        'CPUselectable
        '
        Me.CPUselectable.BackColor = System.Drawing.Color.Transparent
        Me.CPUselectable.Caption = "Selectable by Computer"
        Me.CPUselectable.Checked = False
        Me.CPUselectable.Location = New System.Drawing.Point(3, 26)
        Me.CPUselectable.Name = "CPUselectable"
        Me.CPUselectable.Size = New System.Drawing.Size(203, 29)
        Me.CPUselectable.TabIndex = 9
        '
        'MaxRevs
        '
        Me.MaxRevs.AutoSize = True
        Me.MaxRevs.Location = New System.Drawing.Point(34, 195)
        Me.MaxRevs.Name = "MaxRevs"
        Me.MaxRevs.Size = New System.Drawing.Size(49, 13)
        Me.MaxRevs.TabIndex = 8
        Me.MaxRevs.Text = "MaxRevs"
        '
        'MaxRev
        '
        Me.MaxRev.Location = New System.Drawing.Point(99, 192)
        Me.MaxRev.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.MaxRev.Name = "MaxRev"
        Me.MaxRev.Size = New System.Drawing.Size(70, 21)
        Me.MaxRev.TabIndex = 7
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.RadioButton3)
        Me.GroupBox2.Controls.Add(Me.RadioButton2)
        Me.GroupBox2.Controls.Add(Me.RadioButton1)
        Me.GroupBox2.Location = New System.Drawing.Point(3, 117)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(227, 53)
        Me.GroupBox2.TabIndex = 6
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Engine type"
        '
        'RadioButton3
        '
        Me.RadioButton3.FlatAppearance.BorderColor = System.Drawing.Color.Red
        Me.RadioButton3.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.RadioButton3.ForeColor = System.Drawing.Color.Red
        Me.RadioButton3.Image = Global.CarStudio.My.Resources.Resources.ufo
        Me.RadioButton3.Location = New System.Drawing.Point(159, 16)
        Me.RadioButton3.Name = "RadioButton3"
        Me.RadioButton3.Size = New System.Drawing.Size(44, 30)
        Me.RadioButton3.TabIndex = 3
        Me.RadioButton3.UseVisualStyleBackColor = True
        '
        'RadioButton2
        '
        Me.RadioButton2.FlatAppearance.BorderColor = System.Drawing.Color.Red
        Me.RadioButton2.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.RadioButton2.ForeColor = System.Drawing.Color.Red
        Me.RadioButton2.Image = Global.CarStudio.My.Resources.Resources.glow
        Me.RadioButton2.Location = New System.Drawing.Point(88, 16)
        Me.RadioButton2.Name = "RadioButton2"
        Me.RadioButton2.Size = New System.Drawing.Size(44, 30)
        Me.RadioButton2.TabIndex = 2
        Me.RadioButton2.UseVisualStyleBackColor = True
        '
        'RadioButton1
        '
        Me.RadioButton1.FlatAppearance.BorderColor = System.Drawing.Color.Red
        Me.RadioButton1.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.RadioButton1.ForeColor = System.Drawing.Color.Red
        Me.RadioButton1.Image = Global.CarStudio.My.Resources.Resources.electrique
        Me.RadioButton1.Location = New System.Drawing.Point(17, 16)
        Me.RadioButton1.Name = "RadioButton1"
        Me.RadioButton1.Size = New System.Drawing.Size(44, 30)
        Me.RadioButton1.TabIndex = 1
        Me.RadioButton1.UseVisualStyleBackColor = True
        '
        'Statistics
        '
        Me.Statistics.BackColor = System.Drawing.Color.Transparent
        Me.Statistics.Caption = "Statistics Enabled"
        Me.Statistics.Checked = False
        Me.Statistics.Location = New System.Drawing.Point(3, 83)
        Me.Statistics.Name = "Statistics"
        Me.Statistics.Size = New System.Drawing.Size(236, 29)
        Me.Statistics.TabIndex = 2
        '
        'Selectable
        '
        Me.Selectable.BackColor = System.Drawing.Color.Transparent
        Me.Selectable.Caption = "Selectable by racer"
        Me.Selectable.Checked = False
        Me.Selectable.Location = New System.Drawing.Point(3, 3)
        Me.Selectable.Name = "Selectable"
        Me.Selectable.Size = New System.Drawing.Size(203, 29)
        Me.Selectable.TabIndex = 1
        '
        'Besttime
        '
        Me.Besttime.BackColor = System.Drawing.Color.Transparent
        Me.Besttime.Caption = "includes its Best Time (can be selected in Practice time)"
        Me.Besttime.Checked = False
        Me.Besttime.Location = New System.Drawing.Point(3, 54)
        Me.Besttime.Name = "Besttime"
        Me.Besttime.Size = New System.Drawing.Size(236, 29)
        Me.Besttime.TabIndex = 0
        '
        'TabPage2
        '
        Me.TabPage2.BackColor = System.Drawing.Color.DimGray
        Me.TabPage2.Controls.Add(Me.GroupBox1)
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(244, 251)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "Rating"
        '
        'GroupBox1
        '
        Me.GroupBox1.BackColor = System.Drawing.Color.DimGray
        Me.GroupBox1.Controls.Add(Me.Advanced)
        Me.GroupBox1.Controls.Add(Me.Amateur)
        Me.GroupBox1.Controls.Add(Me.Pro)
        Me.GroupBox1.Controls.Add(Me.Semi_Pro)
        Me.GroupBox1.Controls.Add(Me.Rookie)
        Me.GroupBox1.Location = New System.Drawing.Point(3, 6)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(232, 221)
        Me.GroupBox1.TabIndex = 4
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Rating"
        '
        'Advanced
        '
        Me.Advanced.AutoEllipsis = True
        Me.Advanced.CheckAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Advanced.FlatAppearance.BorderColor = System.Drawing.Color.Red
        Me.Advanced.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Advanced.ForeColor = System.Drawing.Color.Purple
        Me.Advanced.Image = CType(resources.GetObject("Advanced.Image"), System.Drawing.Image)
        Me.Advanced.Location = New System.Drawing.Point(133, 32)
        Me.Advanced.Margin = New System.Windows.Forms.Padding(0)
        Me.Advanced.Name = "Advanced"
        Me.Advanced.Size = New System.Drawing.Size(68, 62)
        Me.Advanced.TabIndex = 1
        Me.Advanced.TextAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Advanced.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage
        Me.Advanced.UseVisualStyleBackColor = True
        '
        'Amateur
        '
        Me.Amateur.AutoEllipsis = True
        Me.Amateur.CheckAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Amateur.FlatAppearance.BorderColor = System.Drawing.Color.Red
        Me.Amateur.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Amateur.ForeColor = System.Drawing.Color.Green
        Me.Amateur.Image = CType(resources.GetObject("Amateur.Image"), System.Drawing.Image)
        Me.Amateur.Location = New System.Drawing.Point(70, 31)
        Me.Amateur.Margin = New System.Windows.Forms.Padding(0)
        Me.Amateur.Name = "Amateur"
        Me.Amateur.Size = New System.Drawing.Size(68, 62)
        Me.Amateur.TabIndex = 1
        Me.Amateur.TextAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Amateur.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage
        Me.Amateur.UseVisualStyleBackColor = True
        '
        'Pro
        '
        Me.Pro.AutoEllipsis = True
        Me.Pro.CheckAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Pro.FlatAppearance.BorderColor = System.Drawing.Color.Red
        Me.Pro.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Pro.ForeColor = System.Drawing.Color.DarkGoldenrod
        Me.Pro.Image = CType(resources.GetObject("Pro.Image"), System.Drawing.Image)
        Me.Pro.Location = New System.Drawing.Point(112, 130)
        Me.Pro.Name = "Pro"
        Me.Pro.Size = New System.Drawing.Size(68, 62)
        Me.Pro.TabIndex = 1
        Me.Pro.TextAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Pro.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage
        Me.Pro.UseVisualStyleBackColor = True
        '
        'Semi_Pro
        '
        Me.Semi_Pro.AutoEllipsis = True
        Me.Semi_Pro.CheckAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Semi_Pro.FlatAppearance.BorderColor = System.Drawing.Color.Red
        Me.Semi_Pro.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Semi_Pro.ForeColor = System.Drawing.Color.Blue
        Me.Semi_Pro.Image = CType(resources.GetObject("Semi_Pro.Image"), System.Drawing.Image)
        Me.Semi_Pro.Location = New System.Drawing.Point(38, 130)
        Me.Semi_Pro.Name = "Semi_Pro"
        Me.Semi_Pro.Size = New System.Drawing.Size(68, 62)
        Me.Semi_Pro.TabIndex = 1
        Me.Semi_Pro.TextAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Semi_Pro.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage
        Me.Semi_Pro.UseVisualStyleBackColor = True
        '
        'Rookie
        '
        Me.Rookie.AutoEllipsis = True
        Me.Rookie.CheckAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Rookie.FlatAppearance.BorderColor = System.Drawing.Color.Red
        Me.Rookie.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Rookie.ForeColor = System.Drawing.Color.Red
        Me.Rookie.Image = CType(resources.GetObject("Rookie.Image"), System.Drawing.Image)
        Me.Rookie.Location = New System.Drawing.Point(6, 31)
        Me.Rookie.Name = "Rookie"
        Me.Rookie.Size = New System.Drawing.Size(68, 62)
        Me.Rookie.TabIndex = 1
        Me.Rookie.TextAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.Rookie.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage
        Me.Rookie.UseVisualStyleBackColor = True
        '
        'TabPage3
        '
        Me.TabPage3.BackColor = System.Drawing.Color.DimGray
        Me.TabPage3.Controls.Add(Me._champ)
        Me.TabPage3.Controls.Add(Me._practice)
        Me.TabPage3.Controls.Add(Me._timetrial)
        Me.TabPage3.Controls.Add(Me._always)
        Me.TabPage3.Controls.Add(Me._single)
        Me.TabPage3.Controls.Add(Me._Never)
        Me.TabPage3.Location = New System.Drawing.Point(4, 22)
        Me.TabPage3.Name = "TabPage3"
        Me.TabPage3.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage3.Size = New System.Drawing.Size(244, 251)
        Me.TabPage3.TabIndex = 2
        Me.TabPage3.Text = "Obtain"
        '
        '_champ
        '
        Me._champ.BackColor = System.Drawing.Color.Transparent
        Me._champ.ForeColor = System.Drawing.Color.White
        Me._champ.Location = New System.Drawing.Point(9, 95)
        Me._champ.Name = "_champ"
        Me._champ.Size = New System.Drawing.Size(210, 23)
        Me._champ.TabIndex = 4
        Me._champ.Text = "After winning championship"
        Me._champ.UseVisualStyleBackColor = False
        '
        '_practice
        '
        Me._practice.BackColor = System.Drawing.Color.Transparent
        Me._practice.ForeColor = System.Drawing.Color.White
        Me._practice.Location = New System.Drawing.Point(9, 182)
        Me._practice.Name = "_practice"
        Me._practice.Size = New System.Drawing.Size(210, 23)
        Me._practice.TabIndex = 4
        Me._practice.Text = "After Collecting stars (Practice mode)"
        Me._practice.UseVisualStyleBackColor = False
        '
        '_timetrial
        '
        Me._timetrial.BackColor = System.Drawing.Color.Transparent
        Me._timetrial.ForeColor = System.Drawing.Color.White
        Me._timetrial.Location = New System.Drawing.Point(9, 124)
        Me._timetrial.Name = "_timetrial"
        Me._timetrial.Size = New System.Drawing.Size(210, 23)
        Me._timetrial.TabIndex = 4
        Me._timetrial.Text = "After winning Time trial"
        Me._timetrial.UseVisualStyleBackColor = False
        '
        '_always
        '
        Me._always.ForeColor = System.Drawing.Color.White
        Me._always.Location = New System.Drawing.Point(9, 46)
        Me._always.Name = "_always"
        Me._always.Size = New System.Drawing.Size(210, 23)
        Me._always.TabIndex = 4
        Me._always.Text = "Always"
        Me._always.UseVisualStyleBackColor = False
        '
        '_single
        '
        Me._single.BackColor = System.Drawing.Color.Transparent
        Me._single.ForeColor = System.Drawing.Color.White
        Me._single.Location = New System.Drawing.Point(9, 153)
        Me._single.Name = "_single"
        Me._single.Size = New System.Drawing.Size(210, 23)
        Me._single.TabIndex = 4
        Me._single.Text = "After winning Single Races"
        Me._single.UseVisualStyleBackColor = False
        '
        '_Never
        '
        Me._Never.BackColor = System.Drawing.Color.Transparent
        Me._Never.ForeColor = System.Drawing.Color.White
        Me._Never.Location = New System.Drawing.Point(9, 17)
        Me._Never.Name = "_Never"
        Me._Never.Size = New System.Drawing.Size(210, 23)
        Me._Never.TabIndex = 4
        Me._Never.Text = "Never (only using Carnival)"
        Me._Never.UseVisualStyleBackColor = False
        '
        'TabPage4
        '
        Me.TabPage4.BackColor = System.Drawing.Color.DimGray
        Me.TabPage4.Controls.Add(Me.NumericUpDown4)
        Me.TabPage4.Controls.Add(Me.Label5)
        Me.TabPage4.Controls.Add(Me.NumericUpDown3)
        Me.TabPage4.Controls.Add(Me.Label4)
        Me.TabPage4.Controls.Add(Me.NumericUpDown2)
        Me.TabPage4.Controls.Add(Me.Label2)
        Me.TabPage4.Controls.Add(Me.NumericUpDown1)
        Me.TabPage4.Controls.Add(Me.Label1)
        Me.TabPage4.Location = New System.Drawing.Point(4, 22)
        Me.TabPage4.Name = "TabPage4"
        Me.TabPage4.Size = New System.Drawing.Size(244, 251)
        Me.TabPage4.TabIndex = 3
        Me.TabPage4.Text = "Frontend Bars"
        '
        'NumericUpDown4
        '
        Me.NumericUpDown4.BackColor = System.Drawing.Color.Gray
        Me.NumericUpDown4.Location = New System.Drawing.Point(102, 178)
        Me.NumericUpDown4.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown4.Name = "NumericUpDown4"
        Me.NumericUpDown4.Size = New System.Drawing.Size(82, 21)
        Me.NumericUpDown4.TabIndex = 7
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(13, 180)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(49, 13)
        Me.Label5.TabIndex = 6
        Me.Label5.Text = "Handling"
        '
        'NumericUpDown3
        '
        Me.NumericUpDown3.BackColor = System.Drawing.Color.Gray
        Me.NumericUpDown3.Location = New System.Drawing.Point(102, 136)
        Me.NumericUpDown3.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown3.Name = "NumericUpDown3"
        Me.NumericUpDown3.Size = New System.Drawing.Size(82, 21)
        Me.NumericUpDown3.TabIndex = 5
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(13, 138)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(41, 13)
        Me.Label4.TabIndex = 4
        Me.Label4.Text = "Weight"
        '
        'NumericUpDown2
        '
        Me.NumericUpDown2.BackColor = System.Drawing.Color.Gray
        Me.NumericUpDown2.Location = New System.Drawing.Point(102, 95)
        Me.NumericUpDown2.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown2.Name = "NumericUpDown2"
        Me.NumericUpDown2.Size = New System.Drawing.Size(82, 21)
        Me.NumericUpDown2.TabIndex = 3
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(13, 97)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(23, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "Acc"
        '
        'NumericUpDown1
        '
        Me.NumericUpDown1.BackColor = System.Drawing.Color.Gray
        Me.NumericUpDown1.Location = New System.Drawing.Point(102, 58)
        Me.NumericUpDown1.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown1.Name = "NumericUpDown1"
        Me.NumericUpDown1.Size = New System.Drawing.Size(82, 21)
        Me.NumericUpDown1.TabIndex = 1
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(13, 60)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(42, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Top End"
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
        'Frontend
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(773, 567)
        Me.Controls.Add(Me.Panel3)
        Me.Name = "Frontend"
        Me.Text = "Frontend"
        Me.Panel3.ResumeLayout(False)
        Me.Panel9.ResumeLayout(False)
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        CType(Me.MaxRev, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.TabPage2.ResumeLayout(False)
        Me.GroupBox1.ResumeLayout(False)
        Me.TabPage3.ResumeLayout(False)
        Me.TabPage4.ResumeLayout(False)
        Me.TabPage4.PerformLayout()
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel8.ResumeLayout(False)
        Me.Panel8.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents Panel9 As System.Windows.Forms.Panel
    Friend WithEvents Panel8 As System.Windows.Forms.Panel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Selectable As CarStudio.mCheckbox
    Friend WithEvents Besttime As CarStudio.mCheckbox
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents TabPage1 As System.Windows.Forms.TabPage
    Friend WithEvents TabPage2 As System.Windows.Forms.TabPage
    Friend WithEvents Statistics As CarStudio.mCheckbox
    Friend WithEvents Rookie As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents Amateur As System.Windows.Forms.RadioButton
    Friend WithEvents Advanced As System.Windows.Forms.RadioButton
    Friend WithEvents Semi_Pro As System.Windows.Forms.RadioButton
    Friend WithEvents Pro As System.Windows.Forms.RadioButton
    Friend WithEvents TabPage3 As System.Windows.Forms.TabPage
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton3 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton2 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton1 As System.Windows.Forms.RadioButton
    Friend WithEvents MaxRevs As System.Windows.Forms.Label
    Friend WithEvents MaxRev As System.Windows.Forms.NumericUpDown
    Friend WithEvents _Never As RadioButton
    Friend WithEvents _always As RadioButton
    Friend WithEvents _champ As RadioButton
    Friend WithEvents _timetrial As RadioButton
    Friend WithEvents _single As RadioButton
    Friend WithEvents _practice As RadioButton
    Friend WithEvents TabPage4 As System.Windows.Forms.TabPage
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents NumericUpDown1 As System.Windows.Forms.NumericUpDown
    Friend WithEvents CPUselectable As CarStudio.mCheckbox
    Friend WithEvents NumericUpDown4 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents NumericUpDown3 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents NumericUpDown2 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label2 As System.Windows.Forms.Label
End Class
