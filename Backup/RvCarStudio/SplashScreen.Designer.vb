<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SplashScreen
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
        Me.loading = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.Vers = New System.Windows.Forms.Label
        Me.info = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.SuspendLayout()
        '
        'loading
        '
        Me.loading.AccessibleRole = System.Windows.Forms.AccessibleRole.None
        Me.loading.AutoEllipsis = True
        Me.loading.BackColor = System.Drawing.Color.Transparent
        Me.loading.CausesValidation = False
        Me.loading.Font = New System.Drawing.Font("Consolas", 11.25!)
        Me.loading.ForeColor = System.Drawing.Color.Silver
        Me.loading.Location = New System.Drawing.Point(12, 425)
        Me.loading.Name = "loading"
        Me.loading.Size = New System.Drawing.Size(263, 23)
        Me.loading.TabIndex = 20
        Me.loading.Text = "Loading..."
        Me.loading.TextAlign = System.Drawing.ContentAlignment.TopCenter
        '
        'Label2
        '
        Me.Label2.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.Label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label2.Location = New System.Drawing.Point(12, 448)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(17, 22)
        Me.Label2.TabIndex = 19
        '
        'Vers
        '
        Me.Vers.BackColor = System.Drawing.Color.Transparent
        Me.Vers.Font = New System.Drawing.Font("Lucida Sans Typewriter", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Vers.ForeColor = System.Drawing.Color.Black
        Me.Vers.Location = New System.Drawing.Point(386, 43)
        Me.Vers.Name = "Vers"
        Me.Vers.Size = New System.Drawing.Size(190, 24)
        Me.Vers.TabIndex = 24
        Me.Vers.Text = "{0}.{1}"
        '
        'info
        '
        Me.info.BackColor = System.Drawing.Color.Transparent
        Me.info.Font = New System.Drawing.Font("Lucida Sans Typewriter", 7.0!, CType((System.Drawing.FontStyle.Bold Or System.Drawing.FontStyle.Italic), System.Drawing.FontStyle))
        Me.info.ForeColor = System.Drawing.Color.Black
        Me.info.Location = New System.Drawing.Point(319, 67)
        Me.info.Name = "info"
        Me.info.Size = New System.Drawing.Size(275, 342)
        Me.info.TabIndex = 25
        Me.info.Text = "version:"
        Me.info.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.info.UseCompatibleTextRendering = True
        '
        'Label3
        '
        Me.Label3.AccessibleRole = System.Windows.Forms.AccessibleRole.None
        Me.Label3.AutoEllipsis = True
        Me.Label3.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.Label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label3.CausesValidation = False
        Me.Label3.Font = New System.Drawing.Font("Calibri", 11.25!)
        Me.Label3.ForeColor = System.Drawing.Color.Black
        Me.Label3.Location = New System.Drawing.Point(12, 448)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(607, 22)
        Me.Label3.TabIndex = 18
        Me.Label3.TextAlign = System.Drawing.ContentAlignment.TopCenter
        Me.Label3.Visible = False
        '
        'SplashScreen
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackgroundImage = Global.CarStudio.My.Resources.Resources.RVCS
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.ClientSize = New System.Drawing.Size(638, 479)
        Me.ControlBox = False
        Me.Controls.Add(Me.info)
        Me.Controls.Add(Me.Vers)
        Me.Controls.Add(Me.loading)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label3)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "SplashScreen"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents loading As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Vers As System.Windows.Forms.Label
    Friend WithEvents info As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label

End Class
