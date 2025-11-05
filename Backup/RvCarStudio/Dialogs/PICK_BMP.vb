Imports OpenTK
Imports OpenTK.Graphics.OpenGL
Imports System.Math

Public Class PICK_BMP
    Dim cf$
    Class SmallConfig
        Public Filler As Object 'Where to fill the results
    End Class
    Public Config As New SmallConfig
    Public Property CurrentFolder() As String

        Get
            Return cf
        End Get
        Set(ByVal value$)
            cf = value
            TextBox1.Text = cf & "\" & ChosenBMP
        End Set
    End Property
    Dim ChosenBMP = ""
    Public VariateTexture As Boolean = False 'Are we choosing carbox or tx?
    Sub DoLoad(ByVal isTexturePicker As Boolean)
        VariateTexture = isTexturePicker
        ChosenBMP = ""
        FlowLayoutPanel1.Controls.Clear()
        CurrentFolder = "cars\" & cars(Active_Car).DirName
        LoadDir(cars(Active_Car).Path)
    End Sub
    Sub LoadDir(ByVal dir$)
        ChosenBMP = ""
        FlowLayoutPanel1.Controls.Clear()

        AddNewBMP_M("NONE")

        Dim Folders = IO.Directory.GetDirectories(dir)
        For Each f In Folders
            AddNewFolder(f)
        Next
        Dim BMPM = IO.Directory.GetFiles(dir, "*.BMP").Union(IO.Directory.GetFiles(dir, "*.BMO"))
        For Each m In BMPM
            AddNewBMP_M(m)
        Next
    End Sub
    Public Sub AddNewFolder(ByVal Name$)
        Dim BTN As New Button

        BTN.FlatAppearance.BorderSize = 0
        BTN.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        BTN.Font = New System.Drawing.Font("Calibri", 8.25!)
        BTN.BackgroundImage = Global.CarStudio.My.Resources.Resources.folder_blue
        BTN.ImageAlign = System.Drawing.ContentAlignment.TopCenter
        ' BTN.Location = New System.Drawing.Point(266, 225)
        'BTN.Name = "Button1"
        BTN.Size = New System.Drawing.Size(53 * 2, 74 * 2)
        BTN.TabIndex = 0
        BTN.Text = Split(Name, "\").Last
        BTN.Tag = Name
        BTN.BackgroundImageLayout = ImageLayout.Center
        BTN.TextAlign = ContentAlignment.BottomCenter '
        BTN.UseVisualStyleBackColor = True

        FlowLayoutPanel1.Controls.Add(BTN)

        AddHandler BTN.Click, AddressOf BTNFLDRCLICKED
    End Sub
    Public Sub AddNewBMP_M(ByVal Name$)

        Dim BTN As New Button


        BTN.FlatAppearance.BorderSize = 0
        BTN.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        BTN.Font = New System.Drawing.Font("Calibri", 8.25!)
        If Name <> "NONE" Then _
             BTN.BackgroundImage = Image.FromFile(Name)
        BTN.ImageAlign = System.Drawing.ContentAlignment.TopCenter

        ' BTN.Location = New System.Drawing.Point(266, 225)
        'BTN.Name = "Button1"
        BTN.Size = New System.Drawing.Size(53 * 2, 74 * 2)
        BTN.TabIndex = 0
        BTN.Text = Split(Name, "\").Last
        BTN.Tag = Name
        BTN.BackgroundImageLayout = ImageLayout.Zoom
        BTN.TextAlign = ContentAlignment.BottomCenter '
        BTN.UseVisualStyleBackColor = True

        FlowLayoutPanel1.Controls.Add(BTN)
        AddHandler BTN.Click, AddressOf BTNBMPCLICKED
    End Sub
    Sub BTNFLDRCLICKED(ByVal sender As Object, ByVal e As EventArgs)
        LoadDir(sender.Tag)
        CurrentFolder &= "\" & sender.text
    End Sub
    Sub BTNBMPCLICKED(ByVal sender As Object, ByVal e As EventArgs)
        ChosenBMP = sender.text
        PreviewBMP(sender.tag)

    End Sub


    Private Zoom = 1
    Private Angle = PI / 4
    Public ThisBMP As Image
    Public Sub PreviewBMP(ByVal BMPPath$)

        TextBox1.Text = cf & "\" & ChosenBMP


        If VariateTexture = True Then 'Change textures

            'While DO_NOT_RENDER

            '  End While
            '   DO_NOT_RENDER = True
            ' If CarEditor.GlControl1.Context.IsCurrent Then CarEditor.GlControl1.Context.MakeCurrent(Nothing)
            '   CarEditor.GlControl1.MakeCurrent()

            TexLib.TexUtil.RemoveTexture(Textures(1))
            If BMPPath <> "NONE" Then _
                Textures(1) = TexLib.TexUtil.CreateTextureFromFile(BMPPath)
            '  DO_NOT_RENDER = False
            '  If CarEditor.GlControl1.Context.IsCurrent Then CarEditor.GlControl1.Context.MakeCurrent(Nothing)
        Else                        'Change TCARBOX
            If BMPPath = "NONE" Then pv_carbox.PictureBox1.Image = getMysteryCarImg() : Exit Sub

            '   If IsStock(cars(Active_Car).DirName) Then TheCarBox.PictureBox1.Image = getStockCarImg(cars(Active_Car).DirName) : Exit Sub
            Dim Str = New IO.FileStream(BMPPath, IO.FileMode.Open, IO.FileAccess.Read)
            pv_carbox.PictureBox1.Image = Image.FromStream(Str)
            Str.Close()



        End If

        ' cars(Active_Car).Theory.MainInfos.Tpage = 

        '   If MyModel.polyl(i).Tpage <> -1 Then GL.BindTexture(TextureTarget.Texture2D, Textures(TextureI)) Else GL.BindTexture(TextureTarget.Texture2D, Textures(0))

    End Sub
    Function getMysteryCarImg() As Image
        If IO.File.Exists(RVPATH & "\levels\frontend\frontendg.bmp") = False Then
            Beep()
            Console_.W("Error! " & RVPATH & "\levels\frontend\frontendg.bmp" & " doesn't exist!")
        End If

    End Function
    Private Sub Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint

    End Sub

    Private Sub FlowLayoutPanel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles FlowLayoutPanel1.Paint

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If CurrentFolder.IndexOf("\") = -1 Then Beep() : Exit Sub
        CurrentFolder = Replace(CurrentFolder, "\" & Split(CurrentFolder, "\").Last, "")
        LoadDir(RVPATH & "\" & CurrentFolder)
    End Sub

    Private Sub PICK_BMP_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        PickingBMP = True

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        LoadDir(RVPATH & "\cars\" & cars(Active_Car).DirName)
        CurrentFolder = "cars\" & cars(Active_Car).DirName
    End Sub


    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        If Strings.Right(TextBox1.Text, 4) <> "NONE" Then _
        If InStr(TextBox1.Text, ".BMP", CompareMethod.Text) + InStr(TextBox1.Text, ".BMO", CompareMethod.Text) = 0 Then Beep() : Exit Sub

        If Strings.Right(TextBox1.Text, 4) <> "NONE" Then _
       Config.Filler.Text = TextBox1.Text _
        Else _
           Config.Filler.Text = "NONE"


        PickingBMP = False

        Me.Close()


    End Sub

    Dim CurPos As Point
    Private Sub Label4_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Label4.MouseMove
        If e.Button = Windows.Forms.MouseButtons.Left Then
            Me.Location += MousePosition - CurPos
            CurPos = MousePosition
        Else
            CurPos = MousePosition
        End If
    End Sub


    Private Sub Panel2_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel2.Paint

    End Sub

    Private Sub Label4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label4.Click

    End Sub
End Class