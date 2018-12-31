Imports OpenTK
Imports OpenTK.Graphics.OpenGL
Imports System.Math

Public Class PICK_PRM
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
            TextBox1.Text = cf & "\" & ChosenPRM
        End Set
    End Property
    Dim ChosenPRM = ""
    Sub DoLoad()
        ChosenPRM = ""
        FlowLayoutPanel1.Controls.Clear()
        CurrentFolder = "cars\" & cars(Active_Car).DirName
        LoadDir(cars(Active_Car).Path)
    End Sub
    Sub LoadDir(ByVal dir$)
        ChosenPRM = ""
        FlowLayoutPanel1.Controls.Clear()
        AddNewPRM_M("NONE")
        Dim Folders = IO.Directory.GetDirectories(dir)
        For Each f In Folders
            AddNewFolder(f)
        Next
        Dim PRMM = IO.Directory.GetFiles(dir, "*.prm").Union(IO.Directory.GetFiles(dir, "*.m"))
        For Each m In PRMM
            AddNewPRM_M(m)
        Next
    End Sub
    Public Sub AddNewFolder(ByVal Name$)
        Dim BTN As New Button

        BTN.FlatAppearance.BorderSize = 0
        BTN.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        BTN.Font = New System.Drawing.Font("Calibri", 8.25!)
        BTN.Image = Global.CarStudio.My.Resources.Resources.folder_blue
        BTN.ImageAlign = System.Drawing.ContentAlignment.TopCenter
        ' BTN.Location = New System.Drawing.Point(266, 225)
        'BTN.Name = "Button1"
        BTN.Size = New System.Drawing.Size(63, 74)
        BTN.TabIndex = 0
        BTN.Text = Split(Name, "\").Last
        BTN.Tag = Name
        BTN.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        BTN.UseVisualStyleBackColor = True

        FlowLayoutPanel1.Controls.Add(BTN)

        AddHandler BTN.Click, AddressOf BTNFLDRCLICKED
    End Sub
    Public Sub AddNewPRM_M(ByVal Name$)
        Dim BTN As New Button

        BTN.FlatAppearance.BorderSize = 0
        BTN.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        BTN.Font = New System.Drawing.Font("Calibri", 8.25!)
        BTN.Image = Global.CarStudio.My.Resources.Resources.cache
        BTN.ImageAlign = System.Drawing.ContentAlignment.TopCenter
        ' BTN.Location = New System.Drawing.Point(266, 225)
        'BTN.Name = "Button1"
        BTN.Size = New System.Drawing.Size(63, 74)
        BTN.TabIndex = 0
        BTN.Text = Split(Name, "\").Last
        BTN.Tag = Name
        BTN.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        BTN.UseVisualStyleBackColor = True

        FlowLayoutPanel1.Controls.Add(BTN)
        AddHandler BTN.Click, AddressOf BTNPRMCLICKED
    End Sub
    Sub BTNFLDRCLICKED(ByVal sender As Object, ByVal e As EventArgs)
        LoadDir(sender.Tag)
        CurrentFolder &= "\" & sender.text
    End Sub
    Sub BTNPRMCLICKED(ByVal sender As Object, ByVal e As EventArgs)
        ChosenPRM = sender.text
        PreviewPRM(sender.tag)

    End Sub


    Private Zoom = 1
    Private Angle = PI / 4
    Public ThisPRM As PRM
    Public Sub PreviewPRM(ByVal prmPath$)
        '  While DO_NOT_RENDER

        ' End While
        ' DO_NOT_RENDER = True
        ' 'If CarEditor.GlControl1.Context.IsCurrent Then CarEditor.GlControl1.Context.MakeCurrent(Nothing)
        '  CarEditor.GlControl1.MakeCurrent()


        '   If Not PreviewMode Then GoTo analogPVM


        '   If Strings.Right(prmPath, 4) = "NONE" Then TextBox1.Text = "NONE" : Exit Sub
        '   TextBox1.Text = cf & "\" & ChosenPRM
        '  ThisPRM = New PRM(prmPath, True)
        '  ThisPRM.TextureI = 1
        ' If CarEditor.GlControl1.Context.IsCurrent Then CarEditor.GlControl1.Context.MakeCurrent(Nothing)
        'DO_NOT_RENDER = False

        '        Exit Sub
analogPVM:
        ThisPRM = Nothing
        If Strings.Right(prmPath, 4) = "NONE" Then TextBox1.Text = "NONE" Else _
        TextBox1.Text = cf & "\" & ChosenPRM

        If Strings.Right(prmPath, 4) = "NONE" Then
            TextBox1.Text = "NONE"
            '   GoTo finish_this

        End If
        Dim N = Config.Filler.modelNumber



        If cars(Active_Car).Theory.Body.modelNumber = -1 Or N = -1 Then
            N = getFreeSlot()
        End If

        If N = -1 Then Exit Sub

        cars(Active_Car).Theory.MainInfos.Model(N) = TextBox1.Text
        Config.Filler.modelNumber = N
        CarBrowser.ReLoadOneCar()

finish_this:
        '   If CarEditor.GlControl1.Context.IsCurrent Then CarEditor.GlControl1.Context.MakeCurrent(Nothing)
        DO_NOT_RENDER = False



    End Sub
   
    Private Sub Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint

    End Sub

    Private Sub PICK_PRM_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        DoLoad()
        CarEditor.pv.Show()
    End Sub

    Private Sub FlowLayoutPanel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles FlowLayoutPanel1.Paint

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If CurrentFolder.IndexOf("\") = -1 Then Beep() : Exit Sub
        CurrentFolder = Replace(CurrentFolder, "\" & Split(CurrentFolder, "\").Last, "")
        LoadDir(RVPATH & "\" & CurrentFolder)
    End Sub

    Private Sub PICK_PRM_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        ' If PreviewMode Then PickingPRM = True

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        LoadDir(RVPATH & "\cars\" & cars(Active_Car).DirName)
        CurrentFolder = "cars\" & cars(Active_Car).DirName
    End Sub


    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        If Strings.Right(TextBox1.Text, 4) <> "NONE" Then _
    If InStr(TextBox1.Text, ".prm", CompareMethod.Text) + InStr(TextBox1.Text, ".m", CompareMethod.Text) = 0 Then Beep() : Exit Sub
        ' If PreviewMode Then
        ' Config.Filler.Text = TextBox1.Text
        '  If Strings.Right(TextBox1.Text, 4) = "NONE" Then Config.Filler.text = "NONE"

        '    End If



        PickingPRM = False

        CarEditor.pv.Hide()
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