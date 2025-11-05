<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class CarBrowser
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
        Me.Label20 = New System.Windows.Forms.Label
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.memory = New System.Windows.Forms.Label
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.GlControl1 = New OpenTK.GLControl
        Me.Timer2 = New System.Windows.Forms.Timer(Me.components)
        Me.Timer3 = New System.Windows.Forms.Timer(Me.components)
        Me.Panel6 = New System.Windows.Forms.Panel
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Button1 = New System.Windows.Forms.Button
        Me.Pick = New System.Windows.Forms.Button
        Me.pick_a_car = New System.Windows.Forms.Panel
        Me.Label1 = New System.Windows.Forms.Label
        Me.Label23 = New System.Windows.Forms.Label
        Me.Panel2 = New System.Windows.Forms.Panel
        Me.Panel4 = New System.Windows.Forms.Panel
        Me.Search = New System.Windows.Forms.TextBox
        Me.ListBox2 = New System.Windows.Forms.ListBox
        Me.Panel12 = New System.Windows.Forms.Panel
        Me.Panel10 = New System.Windows.Forms.Panel
        Me.Label2 = New System.Windows.Forms.Label
        Me.PictureBox1 = New System.Windows.Forms.PictureBox
        Me.Car_selection = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.PANEL1 = New System.Windows.Forms.Panel
        Me.Panel9 = New System.Windows.Forms.Panel
        Me.Panel8 = New System.Windows.Forms.Panel
        Me.Label22 = New System.Windows.Forms.Label
        Me.made_by = New System.Windows.Forms.Label
        Me.Label17 = New System.Windows.Forms.Label
        Me.top_speed = New System.Windows.Forms.Label
        Me.Label15 = New System.Windows.Forms.Label
        Me.rating = New System.Windows.Forms.Label
        Me.Label13 = New System.Windows.Forms.Label
        Me.obtain = New System.Windows.Forms.Label
        Me.Label11 = New System.Windows.Forms.Label
        Me.rank = New System.Windows.Forms.Label
        Me.Label9 = New System.Windows.Forms.Label
        Me.timetrial = New System.Windows.Forms.Label
        Me.Label7 = New System.Windows.Forms.Label
        Me.selectable = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.Wheel_poly_count = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.body_poly_count = New System.Windows.Forms.Label
        Me.poly_score = New System.Windows.Forms.Label
        Me.Panel5 = New System.Windows.Forms.Panel
        Me.view_readme = New System.Windows.Forms.Button
        Me.VisitRVL = New System.Windows.Forms.LinkLabel
        Me.Auto_Rotate_car = New CarStudio.mCheckbox
        Me.Panel7 = New System.Windows.Forms.Panel
        Me.Panel11 = New System.Windows.Forms.Panel
        Me.start_new_car = New System.Windows.Forms.Button
        Me.light = New CarStudio.mCheckbox
        Me.Panel6.SuspendLayout()
        Me.Panel3.SuspendLayout()
        Me.pick_a_car.SuspendLayout()
        Me.Panel2.SuspendLayout()
        Me.Panel12.SuspendLayout()
        Me.Panel10.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.PANEL1.SuspendLayout()
        Me.Panel5.SuspendLayout()
        Me.Panel11.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label20
        '
        Me.Label20.AutoSize = True
        Me.Label20.Font = New System.Drawing.Font("Calibri", 16.25!)
        Me.Label20.Location = New System.Drawing.Point(302, 17)
        Me.Label20.Name = "Label20"
        Me.Label20.Size = New System.Drawing.Size(0, 27)
        Me.Label20.TabIndex = 60
        '
        'memory
        '
        Me.memory.BackColor = System.Drawing.Color.Transparent
        Me.memory.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.memory.Location = New System.Drawing.Point(146, 317)
        Me.memory.Name = "memory"
        Me.memory.Size = New System.Drawing.Size(109, 13)
        Me.memory.TabIndex = 59
        Me.memory.Text = "memory"
        Me.memory.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.ToolTip1.SetToolTip(Me.memory, "memory")
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 2000
        '
        'GlControl1
        '
        Me.GlControl1.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.GlControl1.BackColor = System.Drawing.Color.Black
        Me.GlControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.GlControl1.Location = New System.Drawing.Point(13, 47)
        Me.GlControl1.Name = "GlControl1"
        Me.GlControl1.Size = New System.Drawing.Size(277, 301)
        Me.GlControl1.TabIndex = 61
        Me.GlControl1.VSync = False
        '
        'Timer2
        '
        Me.Timer2.Interval = 1
        '
        'Timer3
        '
        Me.Timer3.Interval = 1
        '
        'Panel6
        '
        Me.Panel6.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVCS2
        Me.Panel6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel6.Controls.Add(Me.Panel3)
        Me.Panel6.Controls.Add(Me.Panel12)
        Me.Panel6.Controls.Add(Me.Panel10)
        Me.Panel6.Controls.Add(Me.PictureBox1)
        Me.Panel6.Controls.Add(Me.Car_selection)
        Me.Panel6.Controls.Add(Me.Label3)
        Me.Panel6.Controls.Add(Me.PANEL1)
        Me.Panel6.Controls.Add(Me.Panel5)
        Me.Panel6.Controls.Add(Me.Panel7)
        Me.Panel6.Controls.Add(Me.Panel11)
        Me.Panel6.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel6.ForeColor = System.Drawing.Color.Black
        Me.Panel6.Location = New System.Drawing.Point(0, 0)
        Me.Panel6.Name = "Panel6"
        Me.Panel6.Size = New System.Drawing.Size(872, 367)
        Me.Panel6.TabIndex = 64
        '
        'Panel3
        '
        Me.Panel3.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.Panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel3.Controls.Add(Me.Button1)
        Me.Panel3.Controls.Add(Me.Pick)
        Me.Panel3.Controls.Add(Me.pick_a_car)
        Me.Panel3.Controls.Add(Me.Label23)
        Me.Panel3.Controls.Add(Me.Panel2)
        Me.Panel3.Controls.Add(Me.Search)
        Me.Panel3.Controls.Add(Me.ListBox2)
        Me.Panel3.Location = New System.Drawing.Point(295, 40)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(291, 314)
        Me.Panel3.TabIndex = 53
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(46, Byte), Integer), CType(CType(52, Byte), Integer), CType(CType(67, Byte), Integer))
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(230, 16)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(51, 18)
        Me.Button1.TabIndex = 68
        Me.Button1.Text = "Random"
        Me.Button1.UseCompatibleTextRendering = True
        Me.Button1.UseVisualStyleBackColor = False
        '
        'Pick
        '
        Me.Pick.BackColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(46, Byte), Integer), CType(CType(52, Byte), Integer), CType(CType(67, Byte), Integer))
        Me.Pick.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Pick.ForeColor = System.Drawing.Color.White
        Me.Pick.Location = New System.Drawing.Point(12, 287)
        Me.Pick.Name = "Pick"
        Me.Pick.Size = New System.Drawing.Size(77, 23)
        Me.Pick.TabIndex = 67
        Me.Pick.Text = "Pick"
        Me.Pick.UseVisualStyleBackColor = False
        '
        'pick_a_car
        '
        Me.pick_a_car.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.pick_a_car.Controls.Add(Me.Label1)
        Me.pick_a_car.Location = New System.Drawing.Point(-1, 0)
        Me.pick_a_car.Name = "pick_a_car"
        Me.pick_a_car.Size = New System.Drawing.Size(290, 20)
        Me.pick_a_car.TabIndex = 65
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.ForeColor = System.Drawing.Color.White
        Me.Label1.Location = New System.Drawing.Point(104, 3)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(51, 13)
        Me.Label1.TabIndex = 66
        Me.Label1.Text = "pick a car"
        '
        'Label23
        '
        Me.Label23.BackColor = System.Drawing.Color.Transparent
        Me.Label23.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.Label23.Location = New System.Drawing.Point(103, 290)
        Me.Label23.Name = "Label23"
        Me.Label23.Size = New System.Drawing.Size(81, 15)
        Me.Label23.TabIndex = 49
        Me.Label23.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'Panel2
        '
        Me.Panel2.BackColor = System.Drawing.SystemColors.ControlLightLight
        Me.Panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel2.Controls.Add(Me.Panel4)
        Me.Panel2.Location = New System.Drawing.Point(10, 279)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(271, 5)
        Me.Panel2.TabIndex = 46
        Me.Panel2.Visible = False
        '
        'Panel4
        '
        Me.Panel4.BackColor = System.Drawing.Color.DodgerBlue
        Me.Panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel4.Location = New System.Drawing.Point(-1, -2)
        Me.Panel4.Name = "Panel4"
        Me.Panel4.Size = New System.Drawing.Size(16, 5)
        Me.Panel4.TabIndex = 2
        '
        'Search
        '
        Me.Search.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
        Me.Search.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Search.ForeColor = System.Drawing.Color.Silver
        Me.Search.Location = New System.Drawing.Point(191, 289)
        Me.Search.Name = "Search"
        Me.Search.Size = New System.Drawing.Size(90, 21)
        Me.Search.TabIndex = 48
        Me.Search.Text = "Search"
        '
        'ListBox2
        '
        Me.ListBox2.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.ListBox2.Font = New System.Drawing.Font("Calibri", 10.0!)
        Me.ListBox2.ForeColor = System.Drawing.Color.White
        Me.ListBox2.ItemHeight = 15
        Me.ListBox2.Location = New System.Drawing.Point(10, 28)
        Me.ListBox2.Name = "ListBox2"
        Me.ListBox2.Size = New System.Drawing.Size(271, 244)
        Me.ListBox2.Sorted = True
        Me.ListBox2.TabIndex = 43
        '
        'Panel12
        '
        Me.Panel12.BackColor = System.Drawing.Color.Transparent
        Me.Panel12.BackgroundImage = Global.CarStudio.My.Resources.Resources.shadow
        Me.Panel12.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel12.Controls.Add(Me.memory)
        Me.Panel12.Location = New System.Drawing.Point(-2, 36)
        Me.Panel12.Name = "Panel12"
        Me.Panel12.Size = New System.Drawing.Size(304, 344)
        Me.Panel12.TabIndex = 68
        '
        'Panel10
        '
        Me.Panel10.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.Panel10.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.Panel10.Controls.Add(Me.Label2)
        Me.Panel10.Location = New System.Drawing.Point(601, 35)
        Me.Panel10.Name = "Panel10"
        Me.Panel10.Size = New System.Drawing.Size(261, 20)
        Me.Panel10.TabIndex = 67
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.ForeColor = System.Drawing.Color.White
        Me.Label2.Location = New System.Drawing.Point(104, 3)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(67, 13)
        Me.Label2.TabIndex = 66
        Me.Label2.Text = "Informations"
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.Color.Transparent
        Me.PictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.PictureBox1.Cursor = System.Windows.Forms.Cursors.Hand
        Me.PictureBox1.Image = Global.CarStudio.My.Resources.Resources.window_close
        Me.PictureBox1.Location = New System.Drawing.Point(840, 7)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(22, 19)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox1.TabIndex = 66
        Me.PictureBox1.TabStop = False
        '
        'Car_selection
        '
        Me.Car_selection.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.Car_selection.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Car_selection.Font = New System.Drawing.Font("Consolas", 10.25!)
        Me.Car_selection.ForeColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.Car_selection.Location = New System.Drawing.Point(-8, -2)
        Me.Car_selection.Name = "Car_selection"
        Me.Car_selection.Size = New System.Drawing.Size(870, 32)
        Me.Car_selection.TabIndex = 51
        Me.Car_selection.Text = "Cars Selection"
        Me.Car_selection.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(353, 17)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(0, 13)
        Me.Label3.TabIndex = 65
        '
        'PANEL1
        '
        Me.PANEL1.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.PANEL1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.PANEL1.Controls.Add(Me.Panel9)
        Me.PANEL1.Controls.Add(Me.Panel8)
        Me.PANEL1.Controls.Add(Me.Label22)
        Me.PANEL1.Controls.Add(Me.made_by)
        Me.PANEL1.Controls.Add(Me.Label17)
        Me.PANEL1.Controls.Add(Me.top_speed)
        Me.PANEL1.Controls.Add(Me.Label15)
        Me.PANEL1.Controls.Add(Me.rating)
        Me.PANEL1.Controls.Add(Me.Label13)
        Me.PANEL1.Controls.Add(Me.obtain)
        Me.PANEL1.Controls.Add(Me.Label11)
        Me.PANEL1.Controls.Add(Me.rank)
        Me.PANEL1.Controls.Add(Me.Label9)
        Me.PANEL1.Controls.Add(Me.timetrial)
        Me.PANEL1.Controls.Add(Me.Label7)
        Me.PANEL1.Controls.Add(Me.selectable)
        Me.PANEL1.Controls.Add(Me.Label5)
        Me.PANEL1.Controls.Add(Me.Wheel_poly_count)
        Me.PANEL1.Controls.Add(Me.Label4)
        Me.PANEL1.Controls.Add(Me.body_poly_count)
        Me.PANEL1.Controls.Add(Me.poly_score)
        Me.PANEL1.Font = New System.Drawing.Font("Consolas", 8.25!)
        Me.PANEL1.Location = New System.Drawing.Point(603, 49)
        Me.PANEL1.Name = "PANEL1"
        Me.PANEL1.Size = New System.Drawing.Size(259, 236)
        Me.PANEL1.TabIndex = 55
        '
        'Panel9
        '
        Me.Panel9.BackgroundImage = Global.CarStudio.My.Resources.Resources.grad
        Me.Panel9.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.Panel9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel9.Location = New System.Drawing.Point(11, 57)
        Me.Panel9.Name = "Panel9"
        Me.Panel9.Size = New System.Drawing.Size(0, 5)
        Me.Panel9.TabIndex = 67
        '
        'Panel8
        '
        Me.Panel8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel8.Location = New System.Drawing.Point(11, 57)
        Me.Panel8.Name = "Panel8"
        Me.Panel8.Size = New System.Drawing.Size(239, 5)
        Me.Panel8.TabIndex = 66
        '
        'Label22
        '
        Me.Label22.BackColor = System.Drawing.Color.Transparent
        Me.Label22.Location = New System.Drawing.Point(78, 207)
        Me.Label22.Name = "Label22"
        Me.Label22.Size = New System.Drawing.Size(170, 13)
        Me.Label22.TabIndex = 39
        Me.Label22.Text = "?"
        Me.Label22.Visible = False
        '
        'made_by
        '
        Me.made_by.AutoSize = True
        Me.made_by.BackColor = System.Drawing.Color.Transparent
        Me.made_by.Location = New System.Drawing.Point(8, 207)
        Me.made_by.Name = "made_by"
        Me.made_by.Size = New System.Drawing.Size(55, 13)
        Me.made_by.TabIndex = 38
        Me.made_by.Text = "Made by:"
        Me.made_by.Visible = False
        '
        'Label17
        '
        Me.Label17.AutoSize = True
        Me.Label17.BackColor = System.Drawing.Color.Transparent
        Me.Label17.Location = New System.Drawing.Point(154, 175)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(13, 13)
        Me.Label17.TabIndex = 37
        Me.Label17.Text = "0"
        '
        'top_speed
        '
        Me.top_speed.AutoSize = True
        Me.top_speed.BackColor = System.Drawing.Color.Transparent
        Me.top_speed.Location = New System.Drawing.Point(8, 175)
        Me.top_speed.Name = "top_speed"
        Me.top_speed.Size = New System.Drawing.Size(67, 13)
        Me.top_speed.TabIndex = 36
        Me.top_speed.Text = "Top speed:"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.BackColor = System.Drawing.Color.Transparent
        Me.Label15.Location = New System.Drawing.Point(154, 141)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(13, 13)
        Me.Label15.TabIndex = 35
        Me.Label15.Text = "0"
        '
        'rating
        '
        Me.rating.AutoSize = True
        Me.rating.BackColor = System.Drawing.Color.Transparent
        Me.rating.Location = New System.Drawing.Point(8, 141)
        Me.rating.Name = "rating"
        Me.rating.Size = New System.Drawing.Size(49, 13)
        Me.rating.TabIndex = 34
        Me.rating.Text = "Rating:"
        '
        'Label13
        '
        Me.Label13.BackColor = System.Drawing.Color.Transparent
        Me.Label13.Location = New System.Drawing.Point(154, 128)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(80, 13)
        Me.Label13.TabIndex = 33
        Me.Label13.Text = "0"
        Me.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'obtain
        '
        Me.obtain.AutoSize = True
        Me.obtain.BackColor = System.Drawing.Color.Transparent
        Me.obtain.Location = New System.Drawing.Point(8, 128)
        Me.obtain.Name = "obtain"
        Me.obtain.Size = New System.Drawing.Size(49, 13)
        Me.obtain.TabIndex = 32
        Me.obtain.Text = "Obtain:"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.BackColor = System.Drawing.Color.Transparent
        Me.Label11.Location = New System.Drawing.Point(154, 115)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(13, 13)
        Me.Label11.TabIndex = 31
        Me.Label11.Text = "0"
        '
        'rank
        '
        Me.rank.AutoSize = True
        Me.rank.BackColor = System.Drawing.Color.Transparent
        Me.rank.Location = New System.Drawing.Point(8, 115)
        Me.rank.Name = "rank"
        Me.rank.Size = New System.Drawing.Size(43, 13)
        Me.rank.TabIndex = 30
        Me.rank.Text = "Class:"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.BackColor = System.Drawing.Color.Transparent
        Me.Label9.Location = New System.Drawing.Point(154, 79)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(13, 13)
        Me.Label9.TabIndex = 29
        Me.Label9.Text = "0"
        '
        'timetrial
        '
        Me.timetrial.AutoSize = True
        Me.timetrial.BackColor = System.Drawing.Color.Transparent
        Me.timetrial.Location = New System.Drawing.Point(8, 79)
        Me.timetrial.Name = "timetrial"
        Me.timetrial.Size = New System.Drawing.Size(67, 13)
        Me.timetrial.TabIndex = 28
        Me.timetrial.Text = "Timetrial:"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.BackColor = System.Drawing.Color.Transparent
        Me.Label7.Location = New System.Drawing.Point(154, 66)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(13, 13)
        Me.Label7.TabIndex = 27
        Me.Label7.Text = "0"
        '
        'selectable
        '
        Me.selectable.AutoSize = True
        Me.selectable.BackColor = System.Drawing.Color.Transparent
        Me.selectable.Location = New System.Drawing.Point(8, 66)
        Me.selectable.Name = "selectable"
        Me.selectable.Size = New System.Drawing.Size(73, 13)
        Me.selectable.TabIndex = 26
        Me.selectable.Text = "Selectable:"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.BackColor = System.Drawing.Color.Transparent
        Me.Label5.Location = New System.Drawing.Point(154, 21)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(13, 13)
        Me.Label5.TabIndex = 25
        Me.Label5.Text = "0"
        '
        'Wheel_poly_count
        '
        Me.Wheel_poly_count.AutoSize = True
        Me.Wheel_poly_count.BackColor = System.Drawing.Color.Transparent
        Me.Wheel_poly_count.Location = New System.Drawing.Point(8, 21)
        Me.Wheel_poly_count.Name = "Wheel_poly_count"
        Me.Wheel_poly_count.Size = New System.Drawing.Size(121, 13)
        Me.Wheel_poly_count.TabIndex = 24
        Me.Wheel_poly_count.Text = "Wheels Polys Count:"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.BackColor = System.Drawing.Color.Transparent
        Me.Label4.Location = New System.Drawing.Point(154, 8)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(13, 13)
        Me.Label4.TabIndex = 23
        Me.Label4.Text = "0"
        '
        'body_poly_count
        '
        Me.body_poly_count.AutoSize = True
        Me.body_poly_count.BackColor = System.Drawing.Color.Transparent
        Me.body_poly_count.Location = New System.Drawing.Point(8, 8)
        Me.body_poly_count.Name = "body_poly_count"
        Me.body_poly_count.Size = New System.Drawing.Size(109, 13)
        Me.body_poly_count.TabIndex = 22
        Me.body_poly_count.Text = "Body Polys Count:"
        '
        'poly_score
        '
        Me.poly_score.AutoSize = True
        Me.poly_score.BackColor = System.Drawing.Color.Transparent
        Me.poly_score.Location = New System.Drawing.Point(9, 46)
        Me.poly_score.Name = "poly_score"
        Me.poly_score.Size = New System.Drawing.Size(67, 13)
        Me.poly_score.TabIndex = 68
        Me.poly_score.Text = "poly score"
        '
        'Panel5
        '
        Me.Panel5.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.Panel5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.Panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel5.Controls.Add(Me.view_readme)
        Me.Panel5.Controls.Add(Me.VisitRVL)
        Me.Panel5.Controls.Add(Me.Auto_Rotate_car)
        Me.Panel5.Location = New System.Drawing.Point(601, 259)
        Me.Panel5.Name = "Panel5"
        Me.Panel5.Size = New System.Drawing.Size(264, 76)
        Me.Panel5.TabIndex = 54
        '
        'view_readme
        '
        Me.view_readme.BackColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(46, Byte), Integer), CType(CType(52, Byte), Integer), CType(CType(67, Byte), Integer))
        Me.view_readme.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.view_readme.ForeColor = System.Drawing.Color.White
        Me.view_readme.Location = New System.Drawing.Point(152, 31)
        Me.view_readme.Name = "view_readme"
        Me.view_readme.Size = New System.Drawing.Size(103, 23)
        Me.view_readme.TabIndex = 67
        Me.view_readme.Text = "View readme"
        Me.view_readme.UseVisualStyleBackColor = False
        '
        'VisitRVL
        '
        Me.VisitRVL.ActiveLinkColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.VisitRVL.AutoSize = True
        Me.VisitRVL.LinkColor = System.Drawing.Color.Gray
        Me.VisitRVL.Location = New System.Drawing.Point(175, 62)
        Me.VisitRVL.Name = "VisitRVL"
        Me.VisitRVL.Size = New System.Drawing.Size(85, 13)
        Me.VisitRVL.TabIndex = 52
        Me.VisitRVL.TabStop = True
        Me.VisitRVL.Text = "Visit Re-Volt Live"
        Me.VisitRVL.VisitedLinkColor = System.Drawing.Color.Gray
        '
        'Auto_Rotate_car
        '
        Me.Auto_Rotate_car.BackColor = System.Drawing.Color.Transparent
        Me.Auto_Rotate_car.Caption = "Auto rotate car"
        Me.Auto_Rotate_car.Checked = False
        Me.Auto_Rotate_car.Location = New System.Drawing.Point(0, 38)
        Me.Auto_Rotate_car.Name = "Auto_Rotate_car"
        Me.Auto_Rotate_car.Size = New System.Drawing.Size(119, 24)
        Me.Auto_Rotate_car.TabIndex = 57
        '
        'Panel7
        '
        Me.Panel7.BackColor = System.Drawing.Color.Transparent
        Me.Panel7.BackgroundImage = Global.CarStudio.My.Resources.Resources.shadow
        Me.Panel7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel7.Location = New System.Drawing.Point(290, 39)
        Me.Panel7.Name = "Panel7"
        Me.Panel7.Size = New System.Drawing.Size(311, 337)
        Me.Panel7.TabIndex = 67
        '
        'Panel11
        '
        Me.Panel11.BackColor = System.Drawing.Color.Transparent
        Me.Panel11.BackgroundImage = Global.CarStudio.My.Resources.Resources.shadow
        Me.Panel11.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel11.Controls.Add(Me.start_new_car)
        Me.Panel11.Controls.Add(Me.light)
        Me.Panel11.Location = New System.Drawing.Point(596, 23)
        Me.Panel11.Name = "Panel11"
        Me.Panel11.Size = New System.Drawing.Size(280, 337)
        Me.Panel11.TabIndex = 68
        '
        'start_new_car
        '
        Me.start_new_car.BackColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(46, Byte), Integer), CType(CType(52, Byte), Integer), CType(CType(67, Byte), Integer))
        Me.start_new_car.Enabled = False
        Me.start_new_car.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.start_new_car.ForeColor = System.Drawing.Color.White
        Me.start_new_car.Location = New System.Drawing.Point(152, 314)
        Me.start_new_car.Name = "start_new_car"
        Me.start_new_car.Size = New System.Drawing.Size(103, 23)
        Me.start_new_car.TabIndex = 67
        Me.start_new_car.Text = "Start new car"
        Me.start_new_car.UseVisualStyleBackColor = False
        '
        'light
        '
        Me.light.AutoSize = True
        Me.light.BackColor = System.Drawing.Color.Transparent
        Me.light.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.light.Caption = "light"
        Me.light.Checked = False
        Me.light.ForeColor = System.Drawing.Color.White
        Me.light.Location = New System.Drawing.Point(0, 311)
        Me.light.Name = "light"
        Me.light.Size = New System.Drawing.Size(75, 31)
        Me.light.TabIndex = 57
        '
        'CarBrowser
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.AliceBlue
        Me.ClientSize = New System.Drawing.Size(872, 367)
        Me.Controls.Add(Me.GlControl1)
        Me.Controls.Add(Me.Label20)
        Me.Controls.Add(Me.Panel6)
        Me.DoubleBuffered = True
        Me.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "CarBrowser"
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Car selection"
        Me.Panel6.ResumeLayout(False)
        Me.Panel6.PerformLayout()
        Me.Panel3.ResumeLayout(False)
        Me.Panel3.PerformLayout()
        Me.pick_a_car.ResumeLayout(False)
        Me.pick_a_car.PerformLayout()
        Me.Panel2.ResumeLayout(False)
        Me.Panel12.ResumeLayout(False)
        Me.Panel10.ResumeLayout(False)
        Me.Panel10.PerformLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.PANEL1.ResumeLayout(False)
        Me.PANEL1.PerformLayout()
        Me.Panel5.ResumeLayout(False)
        Me.Panel5.PerformLayout()
        Me.Panel11.ResumeLayout(False)
        Me.Panel11.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label22 As System.Windows.Forms.Label
    Friend WithEvents made_by As System.Windows.Forms.Label
    Friend WithEvents Label17 As System.Windows.Forms.Label
    Friend WithEvents Label15 As System.Windows.Forms.Label
    Friend WithEvents rating As System.Windows.Forms.Label
    Friend WithEvents top_speed As System.Windows.Forms.Label
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents obtain As System.Windows.Forms.Label
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents rank As System.Windows.Forms.Label
    Friend WithEvents Label20 As System.Windows.Forms.Label
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents memory As System.Windows.Forms.Label
    Friend WithEvents ToolTip1 As System.Windows.Forms.ToolTip
    Friend WithEvents Panel2 As System.Windows.Forms.Panel
    Friend WithEvents Panel4 As System.Windows.Forms.Panel
    Friend WithEvents body_poly_count As System.Windows.Forms.Label
    Public WithEvents Search As System.Windows.Forms.TextBox
    Friend WithEvents VisitRVL As System.Windows.Forms.LinkLabel
    Friend WithEvents Label23 As System.Windows.Forms.Label
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents ListBox2 As System.Windows.Forms.ListBox
    Friend WithEvents Car_selection As System.Windows.Forms.Label
    Friend WithEvents PANEL1 As System.Windows.Forms.Panel
    Friend WithEvents timetrial As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents selectable As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Wheel_poly_count As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Panel5 As System.Windows.Forms.Panel
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents GlControl1 As OpenTK.GLControl
    Friend WithEvents Timer2 As System.Windows.Forms.Timer
    Friend WithEvents Timer3 As System.Windows.Forms.Timer
    Friend WithEvents Panel6 As System.Windows.Forms.Panel
    Friend WithEvents Panel9 As System.Windows.Forms.Panel
    Friend WithEvents Panel8 As System.Windows.Forms.Panel
    Friend WithEvents poly_score As System.Windows.Forms.Label
    Public WithEvents light As CarStudio.mCheckbox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents pick_a_car As System.Windows.Forms.Panel
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Panel7 As System.Windows.Forms.Panel
    Friend WithEvents Panel10 As System.Windows.Forms.Panel
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Panel11 As System.Windows.Forms.Panel
    Friend WithEvents Panel12 As System.Windows.Forms.Panel
    Friend WithEvents Auto_Rotate_car As CarStudio.mCheckbox
    Friend WithEvents Pick As System.Windows.Forms.Button
    Friend WithEvents view_readme As System.Windows.Forms.Button
    Friend WithEvents start_new_car As System.Windows.Forms.Button
    Friend WithEvents Button1 As System.Windows.Forms.Button

End Class
