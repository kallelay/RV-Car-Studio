Imports OpenTK
Imports OpenTK.Graphics.OpenGL
Imports System.Math

Public Class PICK_MATRIX




    Dim CurrentFolder$
    Sub DoLoad()
        FlowLayoutPanel1.Controls.Clear()
        CurrentFolder = "cars\"
        LoadDir(RVPATH & "\cars")
    End Sub
    Sub LoadDir(ByVal dir$)
        FlowLayoutPanel1.Controls.Clear()

        Dim Folders = IO.Directory.GetDirectories(dir)
        For Each f In Folders
            AddNewFolder(f)
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
    Dim Param As Singletons
    Dim Inertia(2) As Vector3

    Sub BTNFLDRCLICKED(ByVal sender As Object, ByVal e As EventArgs)
        Param = New Singletons(RVPATH & "\cars\" & sender.text & "\parameters.txt")
        If Param Is Nothing Then Beep() : Exit Sub
        Dim I$ = Param.getSingleton("BODY").get3LinesValue("Inertia")

        Inertia(0) = cars(Active_Car).StrToVector(Split(I, vbNewLine)(0))
        Inertia(0).Y *= -1
        Inertia(1) = cars(Active_Car).StrToVector(Split(I, vbNewLine)(1))
        Inertia(1).Y *= -1
        Inertia(2) = cars(Active_Car).StrToVector(Split(I, vbNewLine)(2))
        Inertia(2).Y *= -1

        PrintVecToObjectsT(Inertia(0), TextBox10, TextBox2, TextBox3)
        PrintVecToObjectsT(Inertia(1), TextBox4, TextBox5, TextBox6)
        PrintVecToObjectsT(Inertia(2), TextBox7, TextBox8, TextBox9)



        Param = Nothing

    End Sub
  

 
    Private Zoom = 1
    Private Angle = PI / 4
    Public ThisPRM As PRM
 
    Private Sub Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint

    End Sub

    Private Sub PICK_PRM_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        DoLoad()
    End Sub

    Private Sub FlowLayoutPanel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles FlowLayoutPanel1.Paint

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If CurrentFolder.IndexOf("\") = -1 Then Beep() : Exit Sub
        CurrentFolder = Replace(CurrentFolder, "\" & Split(CurrentFolder, "\").Last, "")
        LoadDir(RVPATH & "\" & CurrentFolder)
    End Sub

   
    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        LoadDir(RVPATH & "\cars\" & cars(Active_Car).DirName)
        CurrentFolder = "cars\" & cars(Active_Car).DirName
    End Sub


    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        PrintVecToObjectsT(Inertia(0), _Body.TextBox1, _Body.TextBox2, _Body.TextBox3)
        PrintVecToObjectsT(Inertia(1), _Body.TextBox4, _Body.TextBox5, _Body.TextBox6)
        PrintVecToObjectsT(Inertia(2), _Body.TextBox7, _Body.TextBox8, _Body.TextBox9)


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

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Me.Close()
    End Sub
End Class