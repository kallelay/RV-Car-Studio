Imports System.Math
Imports OpenTK.Graphics.OpenGL

Public Class Texture_

    Private Sub Panel3_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel3.Paint

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        CarEditor.Label11.Hide()
        PICK_BMP.DoLoad(True)
        PICK_BMP.Location = CarEditor.Panel9.Location + CarEditor.Panel3.Location + CarEditor.Location
        PICK_BMP.Config.Filler = Button1
        PICK_BMP.Show()
    End Sub
    Dim loading As Boolean = False
    Dim bmpstr As IO.FileStream
    Dim DMatrix(,) As Color
    ' Dim CMatrix(,) As Color
    Dim AvgMatrix(,) As Integer
    Dim CTable() As Byte
    Public cBMP As Bitmap
    Sub DOLOAD(Optional ByVal skipFilling = False)
        loading = True

        If Not skipFilling Then Button1.Text = cars(Active_Car).Theory.MainInfos.Tpage

        If cars(Active_Car).Theory.MainInfos.Tpage = "NONE" Then Exit Sub

        If Not IO.File.Exists(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage) Then
            Console_.W("File " & RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage & " not found")
            Exit Sub
        End If


        bmpstr = New IO.FileStream(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage, IO.FileMode.Open, IO.FileAccess.Read)
        cBMP = Bitmap.FromStream(bmpstr)
        bmpstr.Close()

        'init
        DMatrix = Nothing : AvgMatrix = Nothing : CTable = Nothing
        TrackBar1.Value = 0 : TrackBar2.Value = 0 : TrackBar3.Value = 0 : TrackBar4.Value = 0
        TrackBar5.Value = 0 : TrackBar6.Value = 0 : TrackBar7.Value = 0

        ReDim DMatrix(cBMP.Width, cBMP.Height)
        ReDim AvgMatrix(cBMP.Width, cBMP.Height)
        ReDim CTable(cBMP.Width * cBMP.Height * 4) 'TODO: was 3
        For i = 0 To cBMP.Width - 1
            For j = 0 To cBMP.Height - 1
                DMatrix(i, j) = cBMP.GetPixel(i, j)
                AvgMatrix(i, j) = (DMatrix(i, j).R / 3 + DMatrix(i, j).G / 3 + DMatrix(i, j).B / 3)
                '   CMatrix(i, j) = DMatrix(i, j)
            Next
        Next

        loading = False


    End Sub
    Sub Unload()
        CarEditor.Prv.Hide()
        Preview_texture.Controls.Add(Preview_texture.pbmp)
    End Sub
    Public tBMP As Integer(,)
    Sub EnterTexEditMode()

        CarEditor.Prv.Show()


        CarEditor.Prv.Controls.Add(Preview_texture.pbmp)
        bmpstr = New IO.FileStream(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage, IO.FileMode.Open)
        Preview_texture.PictureBox1.Image = Image.FromStream(bmpstr)
        bmpstr.Close()

    End Sub
    Private Sub Button1_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Button1.TextChanged
        If Not Initializd Then Exit Sub
        If Button1.Text = "pick" Then Exit Sub

        Button2.Show()

        cars(Active_Car).Theory.MainInfos.Tpage = Button1.Text

        DOLOAD(True)
        '   CarBrowser.ReLoadOneCar()


    End Sub


    Private Sub MCheckbox1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Button2.Hide()
    End Sub

    Private Sub Texture__Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Button2.Location = New Point(8, 41)
    End Sub

    Private Sub Panel9_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel9.Paint

    End Sub
    Dim delta1 = 0

    Private Sub TrackBar1_scroll() Handles TrackBar1.ValueChanged, TrackBar2.ValueChanged, TrackBar4.ValueChanged, TrackBar3.ValueChanged, TrackBar5.ValueChanged, TrackBar8.ValueChanged, TrackBar7.ValueChanged, TrackBar6.ValueChanged

        CarEditor.Label11.Show()

        CarEditor.Label1.Text = "Please wait..."
        CarEditor.Timer2.Stop()
        'Application.DoEvents()
        If cars(Active_Car).Theory.MainInfos.Tpage = "NONE" Then CarEditor.Label1.Text = "Main Window" : Exit Sub

        'Share to thread
        TrackBar1value = TrackBar1.Value
        TrackBar2value = TrackBar2.Value
        TrackBar3value = TrackBar3.Value
        TrackBar4value = TrackBar4.Value
        TrackBar5value = TrackBar5.Value
        TrackBar6value = TrackBar6.Value
        TrackBar7value = TrackBar7.Value
        TrackBar8value = TrackBar8.Value

        'start thread
        Timer1.Start()

        CarEditor.Timer2.Start()

    End Sub



    Public Rcom = 0, Gcom = 0, Acom = 0, Bcom = 0
    Public Sub AddToColorMatrix(ByVal r, ByVal g, ByVal b, ByVal a)
        Acom += a
        Bcom += b
        Gcom += g
        Rcom += r
        For j = 0 To cBMP.Height - 1
            For i = 0 To cBMP.Width - 1
                If (DMatrix(i, j) = Color.Black) Then
                    CTable(j * cBMP.Height * 3 + i * 3) = 0
                    CTable(j * cBMP.Height * 3 + i * 3 + 1) = 0
                    CTable(j * cBMP.Height * 3 + i * 3 + 2) = 0
                    Continue For

                End If
                ' CMatrix(i, j) = Color.FromArgb(Max(Min(DMatrix(i, j).A + Acom, 255), 1), _
                '                                Max(Min(DMatrix(i, j).R + Rcom, 255), 1), _
                '                               Max(Min(DMatrix(i, j).G + Gcom, 255), 1), _
                '                               Max(Min(DMatrix(i, j).B + Bcom, 255), 1))

                CTable(j * cBMP.Height * 3 + i * 3) = Max(Min(DMatrix(i, j).R + Rcom, 255), 1)
                CTable(j * cBMP.Height * 3 + i * 3 + 1) = Max(Min(DMatrix(i, j).G + Gcom, 255), 1)
                CTable(j * cBMP.Height * 3 + i * 3 + 2) = Max(Min(DMatrix(i, j).B + Bcom, 255), 1)

            Next
        Next
        '  UpdateBMP()

    End Sub
    Private Sub UpdateBMP()
        ' While DO_NOT_RENDER

        '  End While
        '  DO_NOT_RENDER = True
        If cBMP Is Nothing Then Exit Sub
        ' If CarEditor.GlControl1.Context.IsCurrent Then CarEditor.GlControl1.Context.MakeCurrent(Nothing)
        ' CarEditor.GlControl1.MakeCurrent()

        TexLib.TexUtil.RemoveTexture(Textures(1))
        TexLib.TexUtil.InitTexturing()
        Textures(1) = TexLib.TexUtil.CreateRGBATexture(cBMP.Width, cBMP.Height, CTable)

        '  If CarEditor.GlControl1.Context.IsCurrent Then CarEditor.GlControl1.Context.MakeCurrent(Nothing)
        DO_NOT_RENDER = False
    End Sub


    Private Sub TrackBar5_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar5.Scroll

    End Sub

    Private Sub TrackBar6_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar6.Scroll

    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        CarEditor.Label11.Hide()
        If cars(Active_Car).Theory.MainInfos.Tpage = "NONE" Then Exit Sub

        IO.File.Copy(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage, RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage & Environment.TickCount & ".bmp")
        For j = 0 To cBMP.Height - 1
            For i = 0 To cBMP.Width - 1
                cBMP.SetPixel(i, j, Color.FromArgb(CTable(j * cBMP.Height * 4 + i * 4), CTable(j * cBMP.Height * 4 + i * 4 + 1), CTable(j * cBMP.Height * 4 + i * 4 + 2)))
            Next
        Next
        cBMP.Save(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage, Drawing.Imaging.ImageFormat.Bmp)
        TrackBar1.Value = 0 : TrackBar2.Value = 0 : TrackBar3.Value = 0 : TrackBar4.Value = 0
        TrackBar5.Value = 0 : TrackBar6.Value = 0 : TrackBar7.Value = 0
        Textures(1) = TexLib.TexUtil.CreateTextureFromFile(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage)
        DOLOAD()
    End Sub


    Dim TrackBar1value, TrackBar2value, TrackBar3value, TrackBar4value, TrackBar5value, TrackBar6value, TrackBar7value, TrackBar8value As Integer
    Dim Progress = 0.0F
    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        For j = 0 To cBMP.Height - 1
            For i = 0 To cBMP.Width - 1
                If BackgroundWorker1.CancellationPending Then Exit Sub



                If (DMatrix(i, j).R ^ 2 + DMatrix(i, j).G ^ 2 + DMatrix(i, j).B ^ 2) < 1 Then
                    'CTable(j * cBMP.Height * 3 + i * 3) = 0
                    ' CTable(j * cBMP.Height * 3 + i * 3 + 1) = 0
                    ' CTable(j * cBMP.Height * 3 + i * 3 + 2) = 0
                    Continue For
                End If

                ' CTable(j * cBMP.Height * 3 + i * 3 + 0) = Max(Min(DMatrix(i, j).R + TrackBar1value + TrackBar2value - (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar5value / 255 - (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar6value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar7value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar8value / 255, 255), 1)
                ' CTable(j * cBMP.Height * 3 + i * 3 + 1) = Max(Min(DMatrix(i, j).G + TrackBar1value + TrackBar3value - (AvgMatrix(i, j) - DMatrix(i, j).G) * TrackBar5value / 255 - (AvgMatrix(i, j) - DMatrix(i, j).G) * TrackBar7value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar6value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar8value / 255, 255), 1)
                ' CTable(j * cBMP.Height * 3 + i * 3 + 2) = Max(Min(DMatrix(i, j).B + TrackBar1value + TrackBar4value - (AvgMatrix(i, j) - DMatrix(i, j).B) * TrackBar5value / 255 - (AvgMatrix(i, j) - DMatrix(i, j).G) * TrackBar8value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar6value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar7value / 255, 255), 1)

                CTable(j * cBMP.Height * 4 + i * 4 + 0) = Max(Min(DMatrix(i, j).R + TrackBar1value + TrackBar2value - (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar5value / 255 - (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar6value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar7value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar8value / 255, 255), 1)
                CTable(j * cBMP.Height * 4 + i * 4 + 1) = Max(Min(DMatrix(i, j).G + TrackBar1value + TrackBar3value - (AvgMatrix(i, j) - DMatrix(i, j).G) * TrackBar5value / 255 - (AvgMatrix(i, j) - DMatrix(i, j).G) * TrackBar7value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar6value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar8value / 255, 255), 1)
                CTable(j * cBMP.Height * 4 + i * 4 + 2) = Max(Min(DMatrix(i, j).B + TrackBar1value + TrackBar4value - (AvgMatrix(i, j) - DMatrix(i, j).B) * TrackBar5value / 255 - (AvgMatrix(i, j) - DMatrix(i, j).G) * TrackBar8value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar6value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar7value / 255, 255), 1)

                CTable(j * cBMP.Height * 4 + i * 4 + 3) = 255 ' Max(Min(DMatrix(i, j).R + TrackBar1value + TrackBar2value - (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar5value / 255 - (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar6value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar7value / 255 + (AvgMatrix(i, j) - DMatrix(i, j).R) * TrackBar8value / 255, 255), 1)


                If (j * cBMP.Height + i) Mod 1000 = 0 Then
                    Progress = Int((i + j * cBMP.Height) / (cBMP.Width * cBMP.Height) * 10000) / 100 ' & "%" : Application.DoEvents()
                End If

            Next
        Next
        Progress = 100 'force
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        If fps < 30 Then CarEditor.Timer2.Stop()

        If Timer1.Interval = 500 Then
            If BackgroundWorker1.IsBusy Then
                BackgroundWorker1.CancelAsync()
            End If

            BackgroundWorker1.RunWorkerAsync()

            Timer1.Interval = 1 : Exit Sub

        End If

        If Progress < 100 Then
            CarEditor.Label1.Text = Strings.Format("Please wait...{0}%", Progress) ' : Application.DoEvents()
        Else
            UpdateBMP()
            CarEditor.Timer2.Start()
            CarEditor.Label1.Text = "Main Window"
            Timer1.Stop()
            Timer1.Interval = 500
        End If
    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click

    End Sub

    Private Sub Label1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label1.DoubleClick
        TrackBar1.Value = 0
    End Sub
    Private Sub Label2_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label2.DoubleClick
        TrackBar2.Value = 0
    End Sub
    Private Sub Label4_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label4.DoubleClick
        TrackBar3.Value = 0
    End Sub
    Private Sub Label6_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label6.DoubleClick
        TrackBar4.Value = 0
    End Sub
    Private Sub Label7_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label7.DoubleClick
        TrackBar5.Value = 0
    End Sub
    Private Sub Label8_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label8.DoubleClick
        TrackBar6.Value = 0
    End Sub
    Private Sub Label9_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label9.DoubleClick
        TrackBar7.Value = 0
    End Sub
    Private Sub Label10_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Label10.DoubleClick
        TrackBar8.Value = 0
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        TrackBar1.Value = 0
        TrackBar2.Value = 0
        TrackBar3.Value = 0
        TrackBar4.Value = 0
        TrackBar5.Value = 0
        TrackBar6.Value = 0
        TrackBar7.Value = 0
        TrackBar8.Value = 0

    End Sub

    Private Sub TrackBar1_scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar8.ValueChanged, TrackBar7.ValueChanged, TrackBar6.ValueChanged, TrackBar5.ValueChanged, TrackBar4.ValueChanged, TrackBar3.ValueChanged, TrackBar2.ValueChanged, TrackBar1.ValueChanged

    End Sub
End Class