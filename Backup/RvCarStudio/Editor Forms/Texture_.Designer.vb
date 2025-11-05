<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Texture_
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
        Me.BackgroundWorker1 = New System.ComponentModel.BackgroundWorker
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Panel9 = New System.Windows.Forms.Panel
        Me.Button4 = New System.Windows.Forms.Button
        Me.Button3 = New System.Windows.Forms.Button
        Me.Label10 = New System.Windows.Forms.Label
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label9 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.Label8 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label7 = New System.Windows.Forms.Label
        Me.Label1 = New System.Windows.Forms.Label
        Me.TrackBar8 = New System.Windows.Forms.TrackBar
        Me.TrackBar4 = New System.Windows.Forms.TrackBar
        Me.TrackBar7 = New System.Windows.Forms.TrackBar
        Me.TrackBar3 = New System.Windows.Forms.TrackBar
        Me.TrackBar5 = New System.Windows.Forms.TrackBar
        Me.TrackBar6 = New System.Windows.Forms.TrackBar
        Me.TrackBar2 = New System.Windows.Forms.TrackBar
        Me.TrackBar1 = New System.Windows.Forms.TrackBar
        Me.Button2 = New System.Windows.Forms.Button
        Me.Panel4 = New System.Windows.Forms.Panel
        Me.Button1 = New System.Windows.Forms.Button
        Me.Label5 = New System.Windows.Forms.Label
        Me.Panel8 = New System.Windows.Forms.Panel
        Me.Label3 = New System.Windows.Forms.Label
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.Panel3.SuspendLayout()
        Me.Panel9.SuspendLayout()
        CType(Me.TrackBar8, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TrackBar4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TrackBar7, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TrackBar3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TrackBar5, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TrackBar6, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TrackBar2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel4.SuspendLayout()
        Me.Panel8.SuspendLayout()
        Me.SuspendLayout()
        '
        'BackgroundWorker1
        '
        Me.BackgroundWorker1.WorkerSupportsCancellation = True
        '
        'Timer1
        '
        Me.Timer1.Interval = 500
        '
        'Panel3
        '
        Me.Panel3.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVL
        Me.Panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Panel3.Controls.Add(Me.Panel9)
        Me.Panel3.Controls.Add(Me.Panel8)
        Me.Panel3.Controls.Add(Me.ComboBox1)
        Me.Panel3.Location = New System.Drawing.Point(148, 134)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(304, 350)
        Me.Panel3.TabIndex = 78
        '
        'Panel9
        '
        Me.Panel9.BackColor = System.Drawing.Color.Transparent
        Me.Panel9.Controls.Add(Me.Button4)
        Me.Panel9.Controls.Add(Me.Button3)
        Me.Panel9.Controls.Add(Me.Label10)
        Me.Panel9.Controls.Add(Me.Label6)
        Me.Panel9.Controls.Add(Me.Label9)
        Me.Panel9.Controls.Add(Me.Label4)
        Me.Panel9.Controls.Add(Me.Label8)
        Me.Panel9.Controls.Add(Me.Label2)
        Me.Panel9.Controls.Add(Me.Label7)
        Me.Panel9.Controls.Add(Me.Label1)
        Me.Panel9.Controls.Add(Me.TrackBar8)
        Me.Panel9.Controls.Add(Me.TrackBar4)
        Me.Panel9.Controls.Add(Me.TrackBar7)
        Me.Panel9.Controls.Add(Me.TrackBar3)
        Me.Panel9.Controls.Add(Me.TrackBar5)
        Me.Panel9.Controls.Add(Me.TrackBar6)
        Me.Panel9.Controls.Add(Me.TrackBar2)
        Me.Panel9.Controls.Add(Me.TrackBar1)
        Me.Panel9.Controls.Add(Me.Button2)
        Me.Panel9.Controls.Add(Me.Panel4)
        Me.Panel9.Font = New System.Drawing.Font("Calibri", 8.25!)
        Me.Panel9.Location = New System.Drawing.Point(30, 54)
        Me.Panel9.Name = "Panel9"
        Me.Panel9.Size = New System.Drawing.Size(251, 280)
        Me.Panel9.TabIndex = 76
        '
        'Button4
        '
        Me.Button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button4.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button4.Location = New System.Drawing.Point(198, 33)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(45, 17)
        Me.Button4.TabIndex = 9
        Me.Button4.Text = "reset"
        Me.Button4.UseCompatibleTextRendering = True
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button3.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button3.Location = New System.Drawing.Point(31, 254)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(181, 25)
        Me.Button3.TabIndex = 8
        Me.Button3.Text = "backup,save,reload"
        Me.Button3.UseCompatibleTextRendering = True
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(49, 241)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(38, 13)
        Me.Label10.TabIndex = 7
        Me.Label10.Text = "reBlue"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(54, 134)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(28, 13)
        Me.Label6.TabIndex = 7
        Me.Label6.Text = "Blue"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(41, 223)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(46, 13)
        Me.Label9.TabIndex = 7
        Me.Label9.Text = "reGreen"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(46, 116)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(36, 13)
        Me.Label4.TabIndex = 7
        Me.Label4.Text = "Green"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(47, 204)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(35, 13)
        Me.Label8.TabIndex = 7
        Me.Label8.Text = "reRed"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(57, 97)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(25, 13)
        Me.Label2.TabIndex = 7
        Me.Label2.Text = "Red"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(28, 176)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(49, 13)
        Me.Label7.TabIndex = 7
        Me.Label7.Text = "Vibrance"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(20, 60)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(57, 13)
        Me.Label1.TabIndex = 7
        Me.Label1.Text = "Brightness"
        '
        'TrackBar8
        '
        Me.TrackBar8.AutoSize = False
        Me.TrackBar8.BackColor = System.Drawing.Color.DimGray
        Me.TrackBar8.Location = New System.Drawing.Point(95, 232)
        Me.TrackBar8.Maximum = 255
        Me.TrackBar8.Minimum = -255
        Me.TrackBar8.Name = "TrackBar8"
        Me.TrackBar8.Size = New System.Drawing.Size(143, 22)
        Me.TrackBar8.TabIndex = 6
        '
        'TrackBar4
        '
        Me.TrackBar4.AutoSize = False
        Me.TrackBar4.BackColor = System.Drawing.Color.DimGray
        Me.TrackBar4.Location = New System.Drawing.Point(100, 125)
        Me.TrackBar4.Maximum = 255
        Me.TrackBar4.Minimum = -255
        Me.TrackBar4.Name = "TrackBar4"
        Me.TrackBar4.Size = New System.Drawing.Size(143, 22)
        Me.TrackBar4.TabIndex = 6
        '
        'TrackBar7
        '
        Me.TrackBar7.AutoSize = False
        Me.TrackBar7.BackColor = System.Drawing.Color.DimGray
        Me.TrackBar7.Location = New System.Drawing.Point(95, 214)
        Me.TrackBar7.Maximum = 255
        Me.TrackBar7.Minimum = -255
        Me.TrackBar7.Name = "TrackBar7"
        Me.TrackBar7.Size = New System.Drawing.Size(143, 22)
        Me.TrackBar7.TabIndex = 6
        '
        'TrackBar3
        '
        Me.TrackBar3.AutoSize = False
        Me.TrackBar3.BackColor = System.Drawing.Color.DimGray
        Me.TrackBar3.Location = New System.Drawing.Point(100, 107)
        Me.TrackBar3.Maximum = 255
        Me.TrackBar3.Minimum = -255
        Me.TrackBar3.Name = "TrackBar3"
        Me.TrackBar3.Size = New System.Drawing.Size(143, 22)
        Me.TrackBar3.TabIndex = 6
        '
        'TrackBar5
        '
        Me.TrackBar5.AutoSize = False
        Me.TrackBar5.BackColor = System.Drawing.Color.DimGray
        Me.TrackBar5.Location = New System.Drawing.Point(95, 167)
        Me.TrackBar5.Maximum = 255
        Me.TrackBar5.Minimum = -255
        Me.TrackBar5.Name = "TrackBar5"
        Me.TrackBar5.Size = New System.Drawing.Size(143, 22)
        Me.TrackBar5.TabIndex = 6
        '
        'TrackBar6
        '
        Me.TrackBar6.AutoSize = False
        Me.TrackBar6.BackColor = System.Drawing.Color.DimGray
        Me.TrackBar6.Location = New System.Drawing.Point(95, 195)
        Me.TrackBar6.Maximum = 255
        Me.TrackBar6.Minimum = -255
        Me.TrackBar6.Name = "TrackBar6"
        Me.TrackBar6.Size = New System.Drawing.Size(143, 22)
        Me.TrackBar6.TabIndex = 6
        '
        'TrackBar2
        '
        Me.TrackBar2.AutoSize = False
        Me.TrackBar2.BackColor = System.Drawing.Color.DimGray
        Me.TrackBar2.Location = New System.Drawing.Point(100, 88)
        Me.TrackBar2.Maximum = 255
        Me.TrackBar2.Minimum = -255
        Me.TrackBar2.Name = "TrackBar2"
        Me.TrackBar2.Size = New System.Drawing.Size(143, 22)
        Me.TrackBar2.TabIndex = 6
        '
        'TrackBar1
        '
        Me.TrackBar1.AutoSize = False
        Me.TrackBar1.BackColor = System.Drawing.Color.DimGray
        Me.TrackBar1.Location = New System.Drawing.Point(101, 53)
        Me.TrackBar1.Maximum = 255
        Me.TrackBar1.Minimum = -255
        Me.TrackBar1.Name = "TrackBar1"
        Me.TrackBar1.Size = New System.Drawing.Size(143, 22)
        Me.TrackBar1.TabIndex = 6
        '
        'Button2
        '
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button2.Font = New System.Drawing.Font("Calibri", 7.25!)
        Me.Button2.Location = New System.Drawing.Point(253, 41)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(230, 226)
        Me.Button2.TabIndex = 3
        Me.Button2.Text = "Start Editing "
        Me.Button2.UseCompatibleTextRendering = True
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Panel4
        '
        Me.Panel4.BackColor = System.Drawing.Color.Transparent
        Me.Panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel4.Controls.Add(Me.Button1)
        Me.Panel4.Controls.Add(Me.Label5)
        Me.Panel4.Location = New System.Drawing.Point(3, 1)
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
        'Texture_
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(601, 618)
        Me.Controls.Add(Me.Panel3)
        Me.Name = "Texture_"
        Me.Text = "Texture_"
        Me.Panel3.ResumeLayout(False)
        Me.Panel9.ResumeLayout(False)
        Me.Panel9.PerformLayout()
        CType(Me.TrackBar8, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TrackBar4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TrackBar7, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TrackBar3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TrackBar5, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TrackBar6, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TrackBar2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel4.ResumeLayout(False)
        Me.Panel4.PerformLayout()
        Me.Panel8.ResumeLayout(False)
        Me.Panel8.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents Panel9 As System.Windows.Forms.Panel
    Friend WithEvents Panel8 As System.Windows.Forms.Panel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Panel4 As System.Windows.Forms.Panel
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents TrackBar1 As System.Windows.Forms.TrackBar
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents TrackBar2 As System.Windows.Forms.TrackBar
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents TrackBar4 As System.Windows.Forms.TrackBar
    Friend WithEvents TrackBar3 As System.Windows.Forms.TrackBar
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents TrackBar5 As System.Windows.Forms.TrackBar
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents TrackBar8 As System.Windows.Forms.TrackBar
    Friend WithEvents TrackBar7 As System.Windows.Forms.TrackBar
    Friend WithEvents TrackBar6 As System.Windows.Forms.TrackBar
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents BackgroundWorker1 As System.ComponentModel.BackgroundWorker
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents Button4 As System.Windows.Forms.Button
End Class
