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

        cBMP = Nothing
        Dim file = Car.GamePath(cars(Active_Car).Theory.MainInfos.Tpage)
        If file Is Nothing Then
            Console_.W("File " & cars(Active_Car).Theory.MainInfos.Tpage & " not found")
            loading = False
            Exit Sub
        End If

        Using ms As New IO.MemoryStream(IO.File.ReadAllBytes(file))
            Using img = Bitmap.FromStream(ms)
                cBMP = New Bitmap(img)
            End Using
        End Using

        'init
        DMatrix = Nothing : AvgMatrix = Nothing : CTable = Nothing
        TrackBar1.Value = 0 : TrackBar2.Value = 0 : TrackBar3.Value = 0 : TrackBar4.Value = 0
        TrackBar5.Value = 0 : TrackBar6.Value = 0 : TrackBar7.Value = 0 : TrackBar8.Value = 0

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
        CarEditor.Label11.Hide()
        'a preview that wasn't saved goes away with the page
        If cBMP IsNot Nothing AndAlso (TrackBar1.Value <> 0 Or TrackBar2.Value <> 0 Or TrackBar3.Value <> 0 Or TrackBar4.Value <> 0 Or TrackBar5.Value <> 0 Or TrackBar6.Value <> 0 Or TrackBar7.Value <> 0 Or TrackBar8.Value <> 0) Then
            Timer1.Stop()
            Timer1.Interval = 500
            BackgroundWorker1.CancelAsync()
            loading = True
            TrackBar1.Value = 0 : TrackBar2.Value = 0 : TrackBar3.Value = 0 : TrackBar4.Value = 0
            TrackBar5.Value = 0 : TrackBar6.Value = 0 : TrackBar7.Value = 0 : TrackBar8.Value = 0
            loading = False
            TexLib.TexUtil.RemoveTexture(Textures(1))
            Textures(1) = TexLib.TexUtil.CreateTextureFromFile(Car.GamePath(cars(Active_Car).Theory.MainInfos.Tpage))
        End If
        CarEditor.Prv.Hide()
        Preview_texture.Controls.Add(Preview_texture.pbmp)
    End Sub
    Public tBMP As Integer(,)
    Sub EnterTexEditMode()

        CarEditor.Prv.Show()


        CarEditor.Prv.Controls.Add(Preview_texture.pbmp)
        Dim file = Car.GamePath(cars(Active_Car).Theory.MainInfos.Tpage)
        If file IsNot Nothing Then Preview_texture.PictureBox1.Image = Image.FromStream(New IO.MemoryStream(IO.File.ReadAllBytes(file)))

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

        If loading OrElse cBMP Is Nothing Then Exit Sub
        CarEditor.Label11.Show()

        CarEditor.Label1.Text = "Please wait..."

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
        Timer1.Interval = 500
        Timer1.Start()

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
        Textures(1) = TexLib.TexUtil.CreateRGBATexture(cBMP.Width, cBMP.Height, CTable)

        '  If CarEditor.GlControl1.Context.IsCurrent Then CarEditor.GlControl1.Context.MakeCurrent(Nothing)
    End Sub


    Private Sub TrackBar5_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar5.Scroll

    End Sub

    Private Sub TrackBar6_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar6.Scroll

    End Sub

    'write the changed texture
    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        CarEditor.Label11.Hide()
        If cBMP Is Nothing OrElse BackgroundWorker1.IsBusy Then Beep() : Exit Sub
        Dim file = Car.GamePath(cars(Active_Car).Theory.MainInfos.Tpage)
        If file Is Nothing Then Exit Sub

        ComputeTable()

        'keep the first version, once. Not as a .bmp: the game takes car*.bmp for a skin
        If Not IO.File.Exists(file & ".bak") Then IO.File.Copy(file, file & ".bak")
        Using out As New Bitmap(cBMP.Width, cBMP.Height, Drawing.Imaging.PixelFormat.Format24bppRgb)
            For j = 0 To cBMP.Height - 1
                For i = 0 To cBMP.Width - 1
                    Dim at = (j * cBMP.Width + i) * 4
                    out.SetPixel(i, j, Color.FromArgb(CTable(at), CTable(at + 1), CTable(at + 2)))
                Next
            Next
            out.Save(file, Drawing.Imaging.ImageFormat.Bmp)
        End Using

        loading = True
        TexLib.TexUtil.RemoveTexture(Textures(1))
        Textures(1) = TexLib.TexUtil.CreateTextureFromFile(file)
        DOLOAD()
    End Sub


    Dim TrackBar1value, TrackBar2value, TrackBar3value, TrackBar4value, TrackBar5value, TrackBar6value, TrackBar7value, TrackBar8value As Integer
    Dim Progress = 0.0F
    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        ComputeTable()
    End Sub

    'The texture with the sliders applied. Pure black stays black: the game shows it as see-through
    Private Sub ComputeTable()
        Dim w = cBMP.Width
        For j = 0 To cBMP.Height - 1
            For i = 0 To w - 1
                If BackgroundWorker1.CancellationPending Then Exit Sub

                Dim at = (j * w + i) * 4
                Dim d = DMatrix(i, j)
                If d.R = 0 And d.G = 0 And d.B = 0 Then
                    CTable(at) = 0 : CTable(at + 1) = 0 : CTable(at + 2) = 0 : CTable(at + 3) = 0
                    Continue For
                End If

                Dim av = AvgMatrix(i, j)
                CTable(at + 0) = Max(Min(d.R + TrackBar1value + TrackBar2value - (av - d.R) * TrackBar5value / 255 - (av - d.R) * TrackBar6value / 255 + (av - d.R) * TrackBar7value / 255 + (av - d.R) * TrackBar8value / 255, 255), 1)
                CTable(at + 1) = Max(Min(d.G + TrackBar1value + TrackBar3value - (av - d.G) * TrackBar5value / 255 - (av - d.G) * TrackBar7value / 255 + (av - d.G) * TrackBar6value / 255 + (av - d.G) * TrackBar8value / 255, 255), 1)
                CTable(at + 2) = Max(Min(d.B + TrackBar1value + TrackBar4value - (av - d.B) * TrackBar5value / 255 - (av - d.B) * TrackBar8value / 255 + (av - d.B) * TrackBar6value / 255 + (av - d.B) * TrackBar7value / 255, 255), 1)
                CTable(at + 3) = 255
            Next
            Progress = Int(j / cBMP.Height * 10000) / 100
        Next
        Progress = 100 'force
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        If Timer1.Interval = 500 Then
            'wait for an older run to end before starting the next one
            If BackgroundWorker1.IsBusy Then
                BackgroundWorker1.CancelAsync()
                Exit Sub
            End If

            Progress = 0.0F
            BackgroundWorker1.RunWorkerAsync()
            Timer1.Interval = 50
            Exit Sub
        End If

        If BackgroundWorker1.IsBusy Then
            CarEditor.Label1.Text = "Please wait..." & Progress & "%"
        Else
            If Progress >= 100 Then UpdateBMP()
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