<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class CarEditor

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
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.Autosave = New System.Windows.Forms.Timer(Me.components)
        Me.BackgroundWorker1 = New System.ComponentModel.BackgroundWorker
        Me.Panel1 = New System.Windows.Forms.Panel
        Me.hider1 = New System.Windows.Forms.Panel
        Me.hider0 = New System.Windows.Forms.Panel
        Me.Label10 = New System.Windows.Forms.Label
        Me.Prv = New System.Windows.Forms.Panel
        Me.pv = New System.Windows.Forms.Label
        Me.pause = New System.Windows.Forms.Label
        Me.Panel10 = New System.Windows.Forms.Panel
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label9 = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.Panel13 = New System.Windows.Forms.Panel
        Me.Panel11 = New System.Windows.Forms.Panel
        Me.Label8 = New System.Windows.Forms.Label
        Me.Label7 = New System.Windows.Forms.Label
        Me.RenderSet = New System.Windows.Forms.Panel
        Me.MCheckbox1 = New CarStudio.mCheckbox
        Me.MCheckbox4 = New CarStudio.mCheckbox
        Me.MCheckbox2 = New CarStudio.mCheckbox
        Me.Pin2_0 = New System.Windows.Forms.PictureBox
        Me.Label11 = New System.Windows.Forms.Label
        Me.Pin1_0 = New System.Windows.Forms.PictureBox
        Me.Aerial_1 = New System.Windows.Forms.PictureBox
        Me.Wheel_4 = New System.Windows.Forms.PictureBox
        Me.Wheel_3 = New System.Windows.Forms.PictureBox
        Me.Wheel_2 = New System.Windows.Forms.PictureBox
        Me.Wheel_1 = New System.Windows.Forms.PictureBox
        Me.Body_0 = New System.Windows.Forms.Panel
        Me.Pin1_1 = New System.Windows.Forms.PictureBox
        Me.Pin2_1 = New System.Windows.Forms.PictureBox
        Me.Axle2_1 = New System.Windows.Forms.PictureBox
        Me.Axle1_1 = New System.Windows.Forms.PictureBox
        Me.Spring2_1 = New System.Windows.Forms.PictureBox
        Me.Spring1_1 = New System.Windows.Forms.PictureBox
        Me.Axle2_0 = New System.Windows.Forms.PictureBox
        Me.Spring2_0 = New System.Windows.Forms.PictureBox
        Me.Axle1_0 = New System.Windows.Forms.PictureBox
        Me.Spring1_0 = New System.Windows.Forms.PictureBox
        Me.Panel5 = New System.Windows.Forms.Panel
        Me.Label2 = New System.Windows.Forms.Label
        Me.Panel6 = New System.Windows.Forms.Panel
        Me.GlControl1 = New OpenTK.GLControl
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Button4 = New System.Windows.Forms.Button
        Me.CheckBox2 = New System.Windows.Forms.CheckBox
        Me.Button3 = New System.Windows.Forms.Button
        Me.Panel9 = New System.Windows.Forms.Panel
        Me.Panel8 = New System.Windows.Forms.Panel
        Me.Label3 = New System.Windows.Forms.Label
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.FlowLayoutPanel1 = New System.Windows.Forms.FlowLayoutPanel
        Me.CheckBox1 = New CarStudio.mCheckbox
        Me.Button1 = New System.Windows.Forms.Button
        Me.Button2 = New System.Windows.Forms.Button
        Me.Panel2 = New System.Windows.Forms.Panel
        Me.Label1 = New System.Windows.Forms.Label
        Me.PictureBox1 = New System.Windows.Forms.PictureBox
        Me.Panel7 = New System.Windows.Forms.Panel
        Me.Panel12 = New System.Windows.Forms.Panel
        Me.Panel1.SuspendLayout()
        Me.hider0.SuspendLayout()
        Me.Panel10.SuspendLayout()
        Me.Panel11.SuspendLayout()
        Me.RenderSet.SuspendLayout()
        CType(Me.Pin2_0, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Pin1_0, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Aerial_1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Wheel_4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Wheel_3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Wheel_2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Wheel_1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Body_0.SuspendLayout()
        CType(Me.Pin1_1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Pin2_1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Axle2_1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Axle1_1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Spring2_1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Spring1_1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Axle2_0, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Spring2_0, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Axle1_0, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Spring1_0, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel5.SuspendLayout()
        Me.Panel3.SuspendLayout()
        Me.Panel8.SuspendLayout()
        Me.FlowLayoutPanel1.SuspendLayout()
        Me.Panel2.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Timer2
        '
        Me.Timer2.Interval = 1
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 15000
        '
        'Autosave
        '
        Me.Autosave.Interval = 400
        '
        'BackgroundWorker1
        '
        Me.BackgroundWorker1.WorkerReportsProgress = True
        Me.BackgroundWorker1.WorkerSupportsCancellation = True
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.Transparent
        Me.Panel1.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVCS2
        Me.Panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Controls.Add(Me.hider1)
        Me.Panel1.Controls.Add(Me.hider0)
        Me.Panel1.Controls.Add(Me.Prv)
        Me.Panel1.Controls.Add(Me.pv)
        Me.Panel1.Controls.Add(Me.pause)
        Me.Panel1.Controls.Add(Me.Panel10)
        Me.Panel1.Controls.Add(Me.Panel13)
        Me.Panel1.Controls.Add(Me.Panel11)
        Me.Panel1.Controls.Add(Me.RenderSet)
        Me.Panel1.Controls.Add(Me.Panel6)
        Me.Panel1.Controls.Add(Me.GlControl1)
        Me.Panel1.Controls.Add(Me.Panel3)
        Me.Panel1.Controls.Add(Me.FlowLayoutPanel1)
        Me.Panel1.Controls.Add(Me.Panel2)
        Me.Panel1.Controls.Add(Me.Panel7)
        Me.Panel1.Controls.Add(Me.Panel12)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.Panel1.ForeColor = System.Drawing.Color.White
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(900, 550)
        Me.Panel1.TabIndex = 64
        '
        'hider1
        '
        Me.hider1.BackColor = System.Drawing.Color.Transparent
        Me.hider1.Location = New System.Drawing.Point(499, 65)
        Me.hider1.Name = "hider1"
        Me.hider1.Size = New System.Drawing.Size(63, 418)
        Me.hider1.TabIndex = 82
        '
        'hider0
        '
        Me.hider0.BackColor = System.Drawing.Color.Transparent
        Me.hider0.Controls.Add(Me.Label10)
        Me.hider0.Location = New System.Drawing.Point(21, 66)
        Me.hider0.Name = "hider0"
        Me.hider0.Size = New System.Drawing.Size(63, 418)
        Me.hider0.TabIndex = 68
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.ForeColor = System.Drawing.Color.Black
        Me.Label10.Location = New System.Drawing.Point(2, 123)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(62, 117)
        Me.Label10.TabIndex = 0
        Me.Label10.Text = "Note:" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "final output" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "will have " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "car over " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "shadow." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "This is " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "only a " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Preview" & _
            "" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10)
        Me.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Prv
        '
        Me.Prv.Location = New System.Drawing.Point(52, 111)
        Me.Prv.Name = "Prv"
        Me.Prv.Size = New System.Drawing.Size(482, 323)
        Me.Prv.TabIndex = 81
        '
        'pv
        '
        Me.pv.AutoSize = True
        Me.pv.BackColor = System.Drawing.Color.SteelBlue
        Me.pv.Enabled = False
        Me.pv.Location = New System.Drawing.Point(29, 79)
        Me.pv.Name = "pv"
        Me.pv.Padding = New System.Windows.Forms.Padding(10, 5, 10, 5)
        Me.pv.Size = New System.Drawing.Size(65, 23)
        Me.pv.TabIndex = 80
        Me.pv.Text = "Preview"
        Me.pv.Visible = False
        '
        'pause
        '
        Me.pause.AutoSize = True
        Me.pause.BackColor = System.Drawing.Color.DarkRed
        Me.pause.Enabled = False
        Me.pause.Location = New System.Drawing.Point(204, 263)
        Me.pause.Name = "pause"
        Me.pause.Padding = New System.Windows.Forms.Padding(10, 5, 10, 5)
        Me.pause.Size = New System.Drawing.Size(173, 23)
        Me.pause.TabIndex = 79
        Me.pause.Text = "Paused, double click to resume"
        Me.pause.Visible = False
        '
        'Panel10
        '
        Me.Panel10.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.Panel10.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel10.Controls.Add(Me.Label6)
        Me.Panel10.Controls.Add(Me.Label9)
        Me.Panel10.Controls.Add(Me.Label5)
        Me.Panel10.Controls.Add(Me.Label4)
        Me.Panel10.Location = New System.Drawing.Point(0, 531)
        Me.Panel10.Name = "Panel10"
        Me.Panel10.Size = New System.Drawing.Size(907, 18)
        Me.Panel10.TabIndex = 74
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.BackColor = System.Drawing.Color.Transparent
        Me.Label6.Font = New System.Drawing.Font("Calibri", 9.0!)
        Me.Label6.Location = New System.Drawing.Point(401, 1)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(79, 14)
        Me.Label6.TabIndex = 2
        Me.Label6.Text = "RV Car studio"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.BackColor = System.Drawing.Color.Transparent
        Me.Label9.Font = New System.Drawing.Font("Calibri", 9.0!)
        Me.Label9.Location = New System.Drawing.Point(726, 2)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(27, 14)
        Me.Label9.TabIndex = 1
        Me.Label9.Text = "LAG"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.BackColor = System.Drawing.Color.Transparent
        Me.Label5.Font = New System.Drawing.Font("Calibri", 9.0!)
        Me.Label5.Location = New System.Drawing.Point(845, 3)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(25, 14)
        Me.Label5.TabIndex = 1
        Me.Label5.Text = "FPS"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.BackColor = System.Drawing.Color.Transparent
        Me.Label4.Font = New System.Drawing.Font("Calibri", 9.0!)
        Me.Label4.Location = New System.Drawing.Point(5, 3)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(38, 14)
        Me.Label4.TabIndex = 0
        Me.Label4.Text = "NAME"
        '
        'Panel13
        '
        Me.Panel13.BackColor = System.Drawing.Color.Transparent
        Me.Panel13.BackgroundImage = Global.CarStudio.My.Resources.Resources.shadow
        Me.Panel13.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel13.Location = New System.Drawing.Point(-68, 526)
        Me.Panel13.Name = "Panel13"
        Me.Panel13.Size = New System.Drawing.Size(1095, 61)
        Me.Panel13.TabIndex = 77
        '
        'Panel11
        '
        Me.Panel11.BackColor = System.Drawing.Color.FromArgb(CType(CType(128, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.Panel11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel11.Controls.Add(Me.Label8)
        Me.Panel11.Controls.Add(Me.Label7)
        Me.Panel11.Location = New System.Drawing.Point(27, 487)
        Me.Panel11.Name = "Panel11"
        Me.Panel11.Size = New System.Drawing.Size(531, 37)
        Me.Panel11.TabIndex = 0
        '
        'Label8
        '
        Me.Label8.Font = New System.Drawing.Font("Arial", 7.0!, System.Drawing.FontStyle.Italic)
        Me.Label8.ForeColor = System.Drawing.Color.Navy
        Me.Label8.Location = New System.Drawing.Point(421, 18)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(108, 17)
        Me.Label8.TabIndex = 1
        Me.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'Label7
        '
        Me.Label7.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Label7.Font = New System.Drawing.Font("Calibri", 9.0!)
        Me.Label7.ForeColor = System.Drawing.Color.Black
        Me.Label7.Location = New System.Drawing.Point(0, 0)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(529, 35)
        Me.Label7.TabIndex = 0
        '
        'RenderSet
        '
        Me.RenderSet.BackColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.RenderSet.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.RenderSet.Controls.Add(Me.MCheckbox1)
        Me.RenderSet.Controls.Add(Me.MCheckbox4)
        Me.RenderSet.Controls.Add(Me.MCheckbox2)
        Me.RenderSet.Controls.Add(Me.Pin2_0)
        Me.RenderSet.Controls.Add(Me.Label11)
        Me.RenderSet.Controls.Add(Me.Pin1_0)
        Me.RenderSet.Controls.Add(Me.Aerial_1)
        Me.RenderSet.Controls.Add(Me.Wheel_4)
        Me.RenderSet.Controls.Add(Me.Wheel_3)
        Me.RenderSet.Controls.Add(Me.Wheel_2)
        Me.RenderSet.Controls.Add(Me.Wheel_1)
        Me.RenderSet.Controls.Add(Me.Body_0)
        Me.RenderSet.Controls.Add(Me.Panel5)
        Me.RenderSet.Location = New System.Drawing.Point(583, 396)
        Me.RenderSet.Name = "RenderSet"
        Me.RenderSet.Size = New System.Drawing.Size(304, 128)
        Me.RenderSet.TabIndex = 72
        '
        'MCheckbox1
        '
        Me.MCheckbox1.BackColor = System.Drawing.Color.Transparent
        Me.MCheckbox1.Caption = "Textured"
        Me.MCheckbox1.Checked = True
        Me.MCheckbox1.Location = New System.Drawing.Point(3, 46)
        Me.MCheckbox1.Name = "MCheckbox1"
        Me.MCheckbox1.Size = New System.Drawing.Size(67, 16)
        Me.MCheckbox1.TabIndex = 84
        '
        'MCheckbox4
        '
        Me.MCheckbox4.BackColor = System.Drawing.Color.Transparent
        Me.MCheckbox4.Caption = "smooth"
        Me.MCheckbox4.Checked = True
        Me.MCheckbox4.Enabled = False
        Me.MCheckbox4.Location = New System.Drawing.Point(3, 70)
        Me.MCheckbox4.Name = "MCheckbox4"
        Me.MCheckbox4.Size = New System.Drawing.Size(67, 16)
        Me.MCheckbox4.TabIndex = 83
        '
        'MCheckbox2
        '
        Me.MCheckbox2.BackColor = System.Drawing.Color.Transparent
        Me.MCheckbox2.Caption = "Shaded"
        Me.MCheckbox2.Checked = True
        Me.MCheckbox2.Location = New System.Drawing.Point(5, 23)
        Me.MCheckbox2.Name = "MCheckbox2"
        Me.MCheckbox2.Size = New System.Drawing.Size(67, 16)
        Me.MCheckbox2.TabIndex = 81
        '
        'Pin2_0
        '
        Me.Pin2_0.BackColor = System.Drawing.Color.Transparent
        Me.Pin2_0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Pin2_0.Location = New System.Drawing.Point(98, 77)
        Me.Pin2_0.Name = "Pin2_0"
        Me.Pin2_0.Size = New System.Drawing.Size(25, 28)
        Me.Pin2_0.TabIndex = 80
        Me.Pin2_0.TabStop = False
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(4, 117)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(178, 13)
        Me.Label11.TabIndex = 83
        Me.Label11.Text = "* Texture not saved, this is a preview"
        Me.Label11.Visible = False
        '
        'Pin1_0
        '
        Me.Pin1_0.BackColor = System.Drawing.Color.Transparent
        Me.Pin1_0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Pin1_0.Location = New System.Drawing.Point(100, 44)
        Me.Pin1_0.Name = "Pin1_0"
        Me.Pin1_0.Size = New System.Drawing.Size(25, 28)
        Me.Pin1_0.TabIndex = 79
        Me.Pin1_0.TabStop = False
        '
        'Aerial_1
        '
        Me.Aerial_1.BackColor = System.Drawing.Color.Transparent
        Me.Aerial_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Aerial_1.Location = New System.Drawing.Point(248, 37)
        Me.Aerial_1.Name = "Aerial_1"
        Me.Aerial_1.Size = New System.Drawing.Size(56, 34)
        Me.Aerial_1.TabIndex = 78
        Me.Aerial_1.TabStop = False
        '
        'Wheel_4
        '
        Me.Wheel_4.BackColor = System.Drawing.Color.Transparent
        Me.Wheel_4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Wheel_4.Location = New System.Drawing.Point(206, 110)
        Me.Wheel_4.Name = "Wheel_4"
        Me.Wheel_4.Size = New System.Drawing.Size(40, 19)
        Me.Wheel_4.TabIndex = 77
        Me.Wheel_4.TabStop = False
        '
        'Wheel_3
        '
        Me.Wheel_3.BackColor = System.Drawing.Color.Transparent
        Me.Wheel_3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Wheel_3.Location = New System.Drawing.Point(206, 19)
        Me.Wheel_3.Name = "Wheel_3"
        Me.Wheel_3.Size = New System.Drawing.Size(40, 19)
        Me.Wheel_3.TabIndex = 76
        Me.Wheel_3.TabStop = False
        '
        'Wheel_2
        '
        Me.Wheel_2.BackColor = System.Drawing.Color.Transparent
        Me.Wheel_2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Wheel_2.Location = New System.Drawing.Point(120, 107)
        Me.Wheel_2.Name = "Wheel_2"
        Me.Wheel_2.Size = New System.Drawing.Size(40, 19)
        Me.Wheel_2.TabIndex = 75
        Me.Wheel_2.TabStop = False
        '
        'Wheel_1
        '
        Me.Wheel_1.BackColor = System.Drawing.Color.Transparent
        Me.Wheel_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Wheel_1.Location = New System.Drawing.Point(123, 19)
        Me.Wheel_1.Name = "Wheel_1"
        Me.Wheel_1.Size = New System.Drawing.Size(40, 19)
        Me.Wheel_1.TabIndex = 75
        Me.Wheel_1.TabStop = False
        '
        'Body_0
        '
        Me.Body_0.BackColor = System.Drawing.Color.Transparent
        Me.Body_0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Body_0.Controls.Add(Me.Pin1_1)
        Me.Body_0.Controls.Add(Me.Pin2_1)
        Me.Body_0.Controls.Add(Me.Axle2_1)
        Me.Body_0.Controls.Add(Me.Axle1_1)
        Me.Body_0.Controls.Add(Me.Spring2_1)
        Me.Body_0.Controls.Add(Me.Spring1_1)
        Me.Body_0.Controls.Add(Me.Axle2_0)
        Me.Body_0.Controls.Add(Me.Spring2_0)
        Me.Body_0.Controls.Add(Me.Axle1_0)
        Me.Body_0.Controls.Add(Me.Spring1_0)
        Me.Body_0.Location = New System.Drawing.Point(112, 34)
        Me.Body_0.Name = "Body_0"
        Me.Body_0.Size = New System.Drawing.Size(143, 80)
        Me.Body_0.TabIndex = 74
        '
        'Pin1_1
        '
        Me.Pin1_1.BackColor = System.Drawing.Color.Transparent
        Me.Pin1_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Pin1_1.Location = New System.Drawing.Point(72, 19)
        Me.Pin1_1.Name = "Pin1_1"
        Me.Pin1_1.Size = New System.Drawing.Size(25, 19)
        Me.Pin1_1.TabIndex = 81
        Me.Pin1_1.TabStop = False
        '
        'Pin2_1
        '
        Me.Pin2_1.BackColor = System.Drawing.Color.Transparent
        Me.Pin2_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Pin2_1.Location = New System.Drawing.Point(71, 43)
        Me.Pin2_1.Name = "Pin2_1"
        Me.Pin2_1.Size = New System.Drawing.Size(25, 18)
        Me.Pin2_1.TabIndex = 81
        Me.Pin2_1.TabStop = False
        '
        'Axle2_1
        '
        Me.Axle2_1.BackColor = System.Drawing.Color.Transparent
        Me.Axle2_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Axle2_1.Location = New System.Drawing.Point(110, 65)
        Me.Axle2_1.Name = "Axle2_1"
        Me.Axle2_1.Size = New System.Drawing.Size(10, 18)
        Me.Axle2_1.TabIndex = 80
        Me.Axle2_1.TabStop = False
        '
        'Axle1_1
        '
        Me.Axle1_1.BackColor = System.Drawing.Color.Transparent
        Me.Axle1_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Axle1_1.Location = New System.Drawing.Point(113, -3)
        Me.Axle1_1.Name = "Axle1_1"
        Me.Axle1_1.Size = New System.Drawing.Size(10, 22)
        Me.Axle1_1.TabIndex = 80
        Me.Axle1_1.TabStop = False
        '
        'Spring2_1
        '
        Me.Spring2_1.BackColor = System.Drawing.Color.Transparent
        Me.Spring2_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Spring2_1.Location = New System.Drawing.Point(96, 53)
        Me.Spring2_1.Name = "Spring2_1"
        Me.Spring2_1.Size = New System.Drawing.Size(14, 33)
        Me.Spring2_1.TabIndex = 79
        Me.Spring2_1.TabStop = False
        '
        'Spring1_1
        '
        Me.Spring1_1.BackColor = System.Drawing.Color.Transparent
        Me.Spring1_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Spring1_1.Location = New System.Drawing.Point(99, -6)
        Me.Spring1_1.Name = "Spring1_1"
        Me.Spring1_1.Size = New System.Drawing.Size(14, 33)
        Me.Spring1_1.TabIndex = 79
        Me.Spring1_1.TabStop = False
        '
        'Axle2_0
        '
        Me.Axle2_0.BackColor = System.Drawing.Color.Transparent
        Me.Axle2_0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Axle2_0.Location = New System.Drawing.Point(29, 50)
        Me.Axle2_0.Name = "Axle2_0"
        Me.Axle2_0.Size = New System.Drawing.Size(10, 27)
        Me.Axle2_0.TabIndex = 78
        Me.Axle2_0.TabStop = False
        '
        'Spring2_0
        '
        Me.Spring2_0.BackColor = System.Drawing.Color.Transparent
        Me.Spring2_0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Spring2_0.Location = New System.Drawing.Point(11, 36)
        Me.Spring2_0.Name = "Spring2_0"
        Me.Spring2_0.Size = New System.Drawing.Size(21, 44)
        Me.Spring2_0.TabIndex = 77
        Me.Spring2_0.TabStop = False
        '
        'Axle1_0
        '
        Me.Axle1_0.BackColor = System.Drawing.Color.Transparent
        Me.Axle1_0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Axle1_0.Location = New System.Drawing.Point(29, 3)
        Me.Axle1_0.Name = "Axle1_0"
        Me.Axle1_0.Size = New System.Drawing.Size(10, 27)
        Me.Axle1_0.TabIndex = 76
        Me.Axle1_0.TabStop = False
        '
        'Spring1_0
        '
        Me.Spring1_0.BackColor = System.Drawing.Color.Transparent
        Me.Spring1_0.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Spring1_0.Location = New System.Drawing.Point(11, -6)
        Me.Spring1_0.Name = "Spring1_0"
        Me.Spring1_0.Size = New System.Drawing.Size(21, 44)
        Me.Spring1_0.TabIndex = 75
        Me.Spring1_0.TabStop = False
        '
        'Panel5
        '
        Me.Panel5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel5.Controls.Add(Me.Label2)
        Me.Panel5.Location = New System.Drawing.Point(0, 0)
        Me.Panel5.Name = "Panel5"
        Me.Panel5.Size = New System.Drawing.Size(304, 16)
        Me.Panel5.TabIndex = 73
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.Font = New System.Drawing.Font("Calibri", 9.0!)
        Me.Label2.Location = New System.Drawing.Point(97, 1)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(91, 14)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "Render Settings"
        '
        'Panel6
        '
        Me.Panel6.BackColor = System.Drawing.Color.Transparent
        Me.Panel6.BackgroundImage = Global.CarStudio.My.Resources.Resources.shadow
        Me.Panel6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel6.Location = New System.Drawing.Point(572, 391)
        Me.Panel6.Name = "Panel6"
        Me.Panel6.Size = New System.Drawing.Size(335, 149)
        Me.Panel6.TabIndex = 73
        '
        'GlControl1
        '
        Me.GlControl1.BackColor = System.Drawing.Color.Black
        Me.GlControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.GlControl1.Location = New System.Drawing.Point(22, 67)
        Me.GlControl1.Name = "GlControl1"
        Me.GlControl1.Size = New System.Drawing.Size(540, 416)
        Me.GlControl1.TabIndex = 75
        Me.GlControl1.VSync = False
        '
        'Panel3
        '
        Me.Panel3.BackColor = System.Drawing.Color.DimGray
        Me.Panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel3.Controls.Add(Me.Button4)
        Me.Panel3.Controls.Add(Me.CheckBox2)
        Me.Panel3.Controls.Add(Me.Button3)
        Me.Panel3.Controls.Add(Me.Panel9)
        Me.Panel3.Controls.Add(Me.Panel8)
        Me.Panel3.Controls.Add(Me.ComboBox1)
        Me.Panel3.ForeColor = System.Drawing.Color.Black
        Me.Panel3.Location = New System.Drawing.Point(584, 34)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(304, 350)
        Me.Panel3.TabIndex = 71
        '
        'Button4
        '
        Me.Button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button4.Location = New System.Drawing.Point(283, 38)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(18, 16)
        Me.Button4.TabIndex = 79
        Me.Button4.Text = "<"
        Me.Button4.UseCompatibleTextRendering = True
        Me.Button4.UseVisualStyleBackColor = True
        '
        'CheckBox2
        '
        Me.CheckBox2.BackColor = System.Drawing.Color.Transparent
        Me.CheckBox2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.CheckBox2.Location = New System.Drawing.Point(234, 336)
        Me.CheckBox2.Name = "CheckBox2"
        Me.CheckBox2.Size = New System.Drawing.Size(77, 16)
        Me.CheckBox2.TabIndex = 77
        Me.CheckBox2.Text = "Auto save parameters"
        Me.CheckBox2.TextAlign = System.Drawing.ContentAlignment.BottomCenter
        Me.CheckBox2.UseVisualStyleBackColor = False
        '
        'Button3
        '
        Me.Button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button3.Location = New System.Drawing.Point(283, 24)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(18, 16)
        Me.Button3.TabIndex = 78
        Me.Button3.Text = ">"
        Me.Button3.UseCompatibleTextRendering = True
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Panel9
        '
        Me.Panel9.BackColor = System.Drawing.Color.Tomato
        Me.Panel9.Location = New System.Drawing.Point(30, 54)
        Me.Panel9.Name = "Panel9"
        Me.Panel9.Size = New System.Drawing.Size(251, 280)
        Me.Panel9.TabIndex = 75
        Me.Panel9.Visible = False
        '
        'Panel8
        '
        Me.Panel8.BackColor = System.Drawing.Color.DarkGray
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
        Me.Label3.ForeColor = System.Drawing.Color.White
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
        Me.ComboBox1.Items.AddRange(New Object() {"Main", "Texture", "Handling", "Frontend Stuffs", "Body", "Wheels", "Suspensions", "Spinner", "Aerial", "AI", "Camera", "Collision", "Carbox", "Shade", "Shadow", "Misc.", "Readme", "Packing", "About", "Console"})
        Me.ComboBox1.Location = New System.Drawing.Point(24, 26)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(258, 21)
        Me.ComboBox1.TabIndex = 0
        '
        'FlowLayoutPanel1
        '
        Me.FlowLayoutPanel1.Controls.Add(Me.CheckBox1)
        Me.FlowLayoutPanel1.Controls.Add(Me.Button1)
        Me.FlowLayoutPanel1.Controls.Add(Me.Button2)
        Me.FlowLayoutPanel1.Location = New System.Drawing.Point(5, 34)
        Me.FlowLayoutPanel1.Name = "FlowLayoutPanel1"
        Me.FlowLayoutPanel1.Size = New System.Drawing.Size(568, 27)
        Me.FlowLayoutPanel1.TabIndex = 70
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.BackColor = System.Drawing.Color.Transparent
        Me.CheckBox1.Caption = Nothing
        Me.CheckBox1.Checked = False
        Me.CheckBox1.ForeColor = System.Drawing.Color.RoyalBlue
        Me.CheckBox1.Location = New System.Drawing.Point(3, 3)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(0, 0)
        Me.CheckBox1.TabIndex = 0
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(46, Byte), Integer), CType(CType(52, Byte), Integer), CType(CType(67, Byte), Integer))
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(9, 3)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(70, 23)
        Me.Button1.TabIndex = 76
        Me.Button1.Text = "Pro editor"
        Me.Button1.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.Button1.UseVisualStyleBackColor = False
        '
        'Button2
        '
        Me.Button2.BackColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(46, Byte), Integer), CType(CType(52, Byte), Integer), CType(CType(67, Byte), Integer))
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button2.ForeColor = System.Drawing.Color.White
        Me.Button2.Location = New System.Drawing.Point(85, 3)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(67, 23)
        Me.Button2.TabIndex = 77
        Me.Button2.Text = "save"
        Me.Button2.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.Button2.UseVisualStyleBackColor = False
        '
        'Panel2
        '
        Me.Panel2.BackColor = System.Drawing.Color.Transparent
        Me.Panel2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel2.Controls.Add(Me.Label1)
        Me.Panel2.Controls.Add(Me.PictureBox1)
        Me.Panel2.Location = New System.Drawing.Point(-23, -3)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(1050, 40)
        Me.Panel2.TabIndex = 69
        '
        'Label1
        '
        Me.Label1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Enabled = False
        Me.Label1.Font = New System.Drawing.Font("Calibri", 12.0!)
        Me.Label1.ForeColor = System.Drawing.Color.White
        Me.Label1.Location = New System.Drawing.Point(422, 5)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(99, 19)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Main Window"
        '
        'PictureBox1
        '
        Me.PictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Right
        Me.PictureBox1.BackColor = System.Drawing.Color.Transparent
        Me.PictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.PictureBox1.Cursor = System.Windows.Forms.Cursors.Hand
        Me.PictureBox1.Image = Global.CarStudio.My.Resources.Resources.window_close
        Me.PictureBox1.Location = New System.Drawing.Point(896, 14)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(14, 12)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox1.TabIndex = 67
        Me.PictureBox1.TabStop = False
        '
        'Panel7
        '
        Me.Panel7.BackColor = System.Drawing.Color.Transparent
        Me.Panel7.BackgroundImage = Global.CarStudio.My.Resources.Resources.shadow
        Me.Panel7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel7.Location = New System.Drawing.Point(573, 20)
        Me.Panel7.Name = "Panel7"
        Me.Panel7.Size = New System.Drawing.Size(331, 393)
        Me.Panel7.TabIndex = 74
        '
        'Panel12
        '
        Me.Panel12.BackColor = System.Drawing.Color.Transparent
        Me.Panel12.BackgroundImage = Global.CarStudio.My.Resources.Resources.shadow
        Me.Panel12.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel12.Location = New System.Drawing.Point(11, 59)
        Me.Panel12.Name = "Panel12"
        Me.Panel12.Size = New System.Drawing.Size(570, 457)
        Me.Panel12.TabIndex = 76
        '
        'CarEditor
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(900, 550)
        Me.ControlBox = False
        Me.Controls.Add(Me.Panel1)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "CarEditor"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Re-Volt Car editor"
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        Me.hider0.ResumeLayout(False)
        Me.hider0.PerformLayout()
        Me.Panel10.ResumeLayout(False)
        Me.Panel10.PerformLayout()
        Me.Panel11.ResumeLayout(False)
        Me.RenderSet.ResumeLayout(False)
        Me.RenderSet.PerformLayout()
        CType(Me.Pin2_0, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Pin1_0, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Aerial_1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Wheel_4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Wheel_3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Wheel_2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Wheel_1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Body_0.ResumeLayout(False)
        CType(Me.Pin1_1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Pin2_1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Axle2_1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Axle1_1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Spring2_1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Spring1_1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Axle2_0, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Spring2_0, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Axle1_0, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Spring1_0, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel5.ResumeLayout(False)
        Me.Panel5.PerformLayout()
        Me.Panel3.ResumeLayout(False)
        Me.Panel8.ResumeLayout(False)
        Me.Panel8.PerformLayout()
        Me.FlowLayoutPanel1.ResumeLayout(False)
        Me.FlowLayoutPanel1.PerformLayout()
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents Panel2 As System.Windows.Forms.Panel
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents FlowLayoutPanel1 As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents RenderSet As System.Windows.Forms.Panel
    Friend WithEvents Panel5 As System.Windows.Forms.Panel
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Panel6 As System.Windows.Forms.Panel
    Friend WithEvents Panel7 As System.Windows.Forms.Panel
    Friend WithEvents GlControl1 As OpenTK.GLControl
    Friend WithEvents Panel12 As System.Windows.Forms.Panel
    Friend WithEvents Timer2 As System.Windows.Forms.Timer
    Friend WithEvents Panel8 As System.Windows.Forms.Panel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Panel9 As System.Windows.Forms.Panel
    Friend WithEvents Panel10 As System.Windows.Forms.Panel
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Panel11 As System.Windows.Forms.Panel
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Panel13 As System.Windows.Forms.Panel
    Friend WithEvents Body_0 As System.Windows.Forms.Panel
    Friend WithEvents Spring1_0 As System.Windows.Forms.PictureBox
    Friend WithEvents Wheel_1 As System.Windows.Forms.PictureBox
    Friend WithEvents Axle1_0 As System.Windows.Forms.PictureBox
    Friend WithEvents Spring2_0 As System.Windows.Forms.PictureBox
    Friend WithEvents Wheel_2 As System.Windows.Forms.PictureBox
    Friend WithEvents Axle2_0 As System.Windows.Forms.PictureBox
    Friend WithEvents Spring1_1 As System.Windows.Forms.PictureBox
    Friend WithEvents Wheel_3 As System.Windows.Forms.PictureBox
    Friend WithEvents Axle2_1 As System.Windows.Forms.PictureBox
    Friend WithEvents Axle1_1 As System.Windows.Forms.PictureBox
    Friend WithEvents Spring2_1 As System.Windows.Forms.PictureBox
    Friend WithEvents Wheel_4 As System.Windows.Forms.PictureBox
    Friend WithEvents Aerial_1 As System.Windows.Forms.PictureBox
    Friend WithEvents Pin1_0 As System.Windows.Forms.PictureBox
    Friend WithEvents Pin2_0 As System.Windows.Forms.PictureBox
    Friend WithEvents Pin1_1 As System.Windows.Forms.PictureBox
    Friend WithEvents Pin2_1 As System.Windows.Forms.PictureBox
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents CheckBox1 As mCheckbox
    Friend WithEvents Autosave As System.Windows.Forms.Timer
    Friend WithEvents pv As System.Windows.Forms.Label
    Friend WithEvents Prv As System.Windows.Forms.Panel
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents hider1 As System.Windows.Forms.Panel
    Friend WithEvents hider0 As System.Windows.Forms.Panel
    Friend WithEvents pause As System.Windows.Forms.Label
    Friend WithEvents MCheckbox2 As CarStudio.mCheckbox
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents MCheckbox4 As CarStudio.mCheckbox
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents CheckBox2 As System.Windows.Forms.CheckBox
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents BackgroundWorker1 As System.ComponentModel.BackgroundWorker
    Friend WithEvents Button4 As System.Windows.Forms.Button
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents MCheckbox1 As CarStudio.mCheckbox
End Class
