Imports OpenTK.Graphics.OpenGL
Imports OpenTK
Imports System.Math
Imports QuickFont

''' <summary>
''' Used to pick the car 
''' </summary>
''' <remarks>
''' Main code by Kallel Ahmed Yahia
''' Licensed under GNU GPL v3 (see license)
''' </remarks>
Public Class CarBrowser
    Public carList As New List(Of String) 'Cars will be stored as a list of string
  
    Private Sub CarBrowser_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
#If DEBUG Then
        Auto_Rotate_car.Checked = False
#End If
    End Sub
    Public PolyCount = 0 '$kay: requires a LONG cast! this is for calculating the polygon count


    'for showing car statistics (most of it is just text rendering)
    Sub Stats()

        'make sure that nothing goes wrong! calc polies
        Label5.Text = ""
        If cars(Active_Car).models.BODY IsNot Nothing Then Label4.Text = readPolys(Replace(RVPATH & "\" & cars(0).Theory.MainInfos.Model(cars(0).Theory.Body.modelNumber), ",", "."))

        For i = 0 To 3

            If cars(Active_Car).models.Wheel(i) IsNot Nothing Then
                If cars(0).Theory.wheel(0).modelNumber = -1 Then
                    Label5.Text &= "-, "
                    Continue For
                End If
                Dim wpc = readPolys(Replace(RVPATH & "\" & cars(0).Theory.MainInfos.Model(cars(0).Theory.wheel(0).modelNumber), ",", "."))


                Label5.Text &= wpc & ", "
            Else
                Label5.Text &= "-, "
            End If
            If i = 1 Then Label5.Text &= vbNewLine
        Next i

        Label5.Text = Label5.Text.Substring(0, Len(Label5.Text) - 2)


        'selectable, besttime etc...
        Label7.Text = cars(0).Theory.MainInfos.SELECTABLE
        Label9.Text = cars(0).Theory.MainInfos.BESTTIME

        Label11.Text = DirectCast(cars(0).Theory.MainInfos.car_class, classes).ToString
        Label13.Text = Replace(DirectCast(cars(0).Theory.MainInfos.obtain, Obtaino).ToString, "_", " ")
        Label15.Text = Replace(DirectCast(cars(0).Theory.MainInfos.Rating, ratingo).ToString, "_", "-")

        Label17.Text = cars(0).Theory.RealInfos.TopSpeed & " mph" & vbNewLine & Int(cars(0).Theory.RealInfos.TopSpeed * 1.6) & "km/h"

        If Single.IsNaN(PolyCount) Then Panel9.Width = 0 Else _
      Panel9.Width = Min(PolyCount * 240 / 10000, 240)

    End Sub

    '??? on car load
    Sub OnCL()
        'on Car Load
        TrackBar1_Scroll(Nothing, Nothing)
    End Sub

    'Different enums for settings
    Enum Transo

        Four_Wheels_Powered
        Forward_Wheels_Powered
        Back_Wheels_Powered

    End Enum
    Enum classes
        Electric = 0
        Glow = 1
        Special = 2

    End Enum
    Enum ratingo

        Rookie = 0
        Amateur = 1
        Advanced = 2
        Semi_Pro = 3
        Pro = 4
    End Enum
    Enum Obtaino
        Carnival_Only = -1
        Anytime = 0
        Championship = 1
        Timetrial = 2
        Practice = 3
        Winning_Single_Races = 4
        Training = 5



    End Enum
    Sub LoadAllCarsIntoList()
        'Car path
        Dim carPath As String = RVPATH & "\cars"

        'remove all cars
        ListBox2.Items.Clear()
        carList.Clear()


        'get all cars folders
        Dim cars() = IO.Directory.GetDirectories(carPath)

        'for each car...
        For i = LBound(cars) To UBound(cars)
            If IO.File.Exists(cars(i) & "\parameters.txt") Then 'get parameters 
                Dim sing As New Singletons(cars(i) & "\parameters.txt") 'load it into singletons
                Dim name = Replace(Replace(sing.getSingleton("").getValue("Name"), vbTab, ""), ",", ".") 'name
                Do Until name(0) = Chr(34) 'get name
                    name = Mid(name, 2)
                Loop

                name = name.Replace(Chr(34), "") 'fix name $(kay)

                Dim Pname = Replace(cars(i).Split("\").Last, vbTab, "")

                ListBox2.Items.Add(" " & vbTab & name & "   (" & Pname & ")") 'name & folder(path) name
            End If
        Next

        Application.DoEvents()
        SplashScreen.Progress("Activating Search function")

        Application.DoEvents()

        For i = 0 To ListBox2.Items.Count - 1
            Application.DoEvents()

            carList.Add(ListBox2.Items(i).ToString)
        Next i

        Label23.Text = ListBox2.Items.Count & " cars"
    End Sub

    Private Sub ListBox2_DrawItem(ByVal sender As Object, ByVal e As System.Windows.Forms.DrawItemEventArgs) Handles ListBox2.DrawItem
        'CODE FOUND FROM THE INTERNET
        If e.State And DrawItemState.Selected Then

            Dim TextLength As Single = TextRenderer.MeasureText(ListBox2.SelectedItem, ListBox2.Font).Width
            '  If wrong Then
            e.Graphics.FillRectangle(Brushes.LightSkyBlue, New Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height))
            'Else
            '    e.Graphics.FillRectangle(If(e.Index Mod 2 = 1, Brushes.AliceBlue, Brushes.White), New Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height))
            'End If
        Else


            e.Graphics.FillRectangle(If(e.Index Mod 2 = 1, Brushes.AliceBlue, Brushes.White), e.Bounds)
        End If
        'If InStr(Invalid, ListBox2.Items(e.Index).ToString.Split(Chr(9)).First) > 0 Then
        'e.Graphics.DrawString(ListBox2.Items(e.Index).ToString, ListBox2.Font, Brushes.DarkRed, e.Bounds)
        ' Else
        e.Graphics.DrawString(ListBox2.Items(e.Index).ToString, ListBox2.Font, Brushes.Black, e.Bounds)
        'End If

    End Sub
    Public Sub setProgress(ByVal percent)
        Application.DoEvents()


        Panel4.Width = (percent / 100) * Panel2.Width
    End Sub
    Public Shared CarIsLoading = False 'for different car setups
    Sub LoadOneCar()
        'get one car, main code
        PolyCount = 0
        Timer2.Stop()
        If ListBox2.SelectedIndex = -1 Then Exit Sub 'if selected&clicked exit


        'car->loading
        CarIsLoading = True

        'free memory
        For Each m In Models
            If m.Persistent Then Continue For
            m.MyModel.vertnum = Nothing
            m.MyModel.vexl = Nothing
            m.MyModel.polynum = Nothing
            m.MyModel.polyl = Nothing
            m.MyModel = Nothing


        Next
        'free models
        Models.Clear()

        'Allow showing progress
        Application.DoEvents()
        Panel2.Show()
        setProgress(0)


        Application.DoEvents()
        setProgress(5)
        If ListBox2.SelectedIndex = -1 Then Exit Sub 'again, if user has changed car, exit

        cars.Clear() 'remove all cars

        Dim mycar = Split(Split(ListBox2.SelectedItem, "(")(1), ")")(0) 'get carfolder

        cars.Add(New Car(RVPATH & "\cars\" & mycar)) 'add new car 

        cars(Active_Car).Load() 'load car


        '----------------------- get author name-------------------
        Dim auth$ = ""

        If IO.Directory.Exists(RVPATH & "\cars\" & mycar) = False Then Exit Sub


        If InStr(cars(Active_Car).Theory.MainInfos.Name, "Halogaland", CompareMethod.Text) > 0 Then
            auth = "Halogaland"
            Label22.Text = auth
        End If

        If IO.Directory.GetFiles(RVPATH & "\cars\" & mycar, "*read*me*").Length > 0 Then
            made_by.Visible = True
            Label22.Visible = True
            view_readme.Visible = True
            Dim rm = IO.File.ReadAllText(IO.Directory.GetFiles(RVPATH & "\cars\" & mycar, "*read*me*")(0))
            If InStr(rm, "---", CompareMethod.Text) > 0 And InStr(rm, "citywalker", CompareMethod.Text) > 0 Then
                auth = "Citywalker"

            ElseIf InStr(rm, "author name", CompareMethod.Text) > 0 Then
                auth = Split(rm, "author name", -1, CompareMethod.Text)(1).Split(vbNewLine)(0)
            ElseIf InStr(rm, "author", CompareMethod.Text) > 0 Then
                auth = Split(rm, "author", -1, CompareMethod.Text)(1).Split(vbNewLine)(0)
            ElseIf InStr(rm, "created by", CompareMethod.Text) > 0 Then
                auth = Split(rm, "created by", -1, CompareMethod.Text)(1).Split(vbNewLine)(0)
            ElseIf InStr(rm, "creator", CompareMethod.Text) > 0 Then
                auth = Split(rm, "creator", -1, CompareMethod.Text)(1).Split(vbNewLine)(0)
            ElseIf InStr(rm, "by", CompareMethod.Text) > 0 Then
                auth = Split(rm, "by", -1, CompareMethod.Text)(1).Split(vbNewLine)(0)
            Else
                If auth = "" Then auth = "Unknown"
            End If


            'BURNER's Sign

            If InStr(Split(rm, vbNewLine)(UBound(Split(rm, vbNewLine))), "Burner", CompareMethod.Text) = 1 Then
                auth = "Burner94"
            End If


            Do Until auth.Length = 0 Or (Convert.ToInt32(auth(0)) > 64 And Convert.ToInt32(auth(0)) < 123)
                auth = auth.Substring(1)
                Application.DoEvents()
                If auth.Length = 0 Then GoTo xFail
            Loop


            Do Until Convert.ToInt16(CChar(auth(auth.Length - 1))) > 64 And Convert.ToInt16(CChar(auth(auth.Length - 1))) < 123 Or Convert.ToInt16(CChar(auth(auth.Length - 1))) = 41

                auth = auth.Substring(0, auth.Length - 1)

                Application.DoEvents()

            Loop


xFail:
            'author name not found
            Label22.Text = auth




        Else
            made_by.Visible = False
            Label22.Visible = False
            view_readme.Visible = False
        End If

        'is stock? (then auth = Acclaim ) 
        If InStr(Stock, "*" & mycar & "*", CompareMethod.Text) Then
            auth = "Acclaim (STOCK CAR)"
            Label22.Text = "Acclaim (STOCK CAR)"
        End If

        If auth <> "" Then
            made_by.Visible = True
            Label22.Visible = True


        End If





        Application.DoEvents()
        setProgress(20)

        'get texture
        Dim ftex = (Replace(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage, ",", "."))
        Application.DoEvents()
        setProgress(22)
        InitAllTextures(ftex)


        ReLoadOneCar() 'load all car models goes here

        setProgress(100)
        Panel2.Hide()
        Timer2.Start()

    End Sub
    Sub ReLoadOneCar(Optional ByVal KeepTextures = True, Optional ByVal ReloadParams = False)
        'This is the sub responsible for loading one car's models
        PolyCount = 0




        'make sure initialisation goes fine
        CarIsLoading = True

        For Each m In Models
            m.MyModel.vertnum = Nothing
            m.MyModel.vexl = Nothing
            m.MyModel.polynum = Nothing
            m.MyModel.polyl = Nothing
            m.MyModel = Nothing
        Next
        Models.Clear()

        Application.DoEvents()

        'are we reloading parameters?
        If ReloadParams Then

            'remove & load
            Dim mycar = cars(Active_Car).DirName
            cars.Clear()
            cars.Add(New Car(RVPATH & "\cars\" & mycar))
            cars(Active_Car).Load()


        End If



        're-load textures?
        If Not KeepTextures Then
            Dim ftex = (Replace(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Tpage, ",", "."))
            InitAllTextures(ftex)
        End If


        'load body
        If (cars(Active_Car).Theory.Body.modelNumber) <> -1 Then
            If IO.File.Exists(Replace(Replace(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Body.modelNumber), Chr(34), ""), ",", ".")) = True Then
                cars(Active_Car).models.BODY = New PRM(Replace(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Body.modelNumber), Chr(34), ""))
                cars(Active_Car).models.BODY.TextureI = 1
                Try
                    cars(Active_Car).models.BODY.Position = cars(Active_Car).Theory.Body.Offset * Zoom  ' - cars(Active_Car).Theory.RealInfos.COM / 2
                Catch
                End Try



            End If
        Else

        End If

        Application.DoEvents()
        setProgress(35)

        'load wheels
        For i = 0 To 3
            If cars(Active_Car).Theory.wheel(i).modelNumber <> -1 Then
                If IO.File.Exists(Replace(RVPATH & "\" & Replace(cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.wheel(i).modelNumber), Chr(34), ""), ",", ".")) = True Then
                    cars(Active_Car).models.Wheel(i) = New PRM(RVPATH & "\" & Replace(cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.wheel(i).modelNumber), Chr(34), ""))
                    If cars(Active_Car).models.Wheel(i) IsNot Nothing Then
                        cars(Active_Car).models.Wheel(i).TextureI = 1
                        cars(Active_Car).models.Wheel(i).Position = cars(Active_Car).Theory.wheel(i).Offset(1) * Zoom  '+ cars(Active_Car).Theory.RealInfos  '+ cars(Active_Car).Theory.wheel(i).offset  * ZOOM  (2) '- cars(Active_Car).Theory.Body.offset  * ZOOM  
                        cars(Active_Car).models.Wheel(i).isVisible = cars(Active_Car).Theory.wheel(i).IsPresent




                    End If
                Else
                    'Tip.fShow("~~Error: MODEL(" & cars(Active_Car).Theory.wheel(i).modelNumber & ") doesn't exist" & vbNewLine)
                End If

            End If

        Next




        'load spring
        For i = 0 To 3

            If cars(Active_Car).Theory.Spring(i).modelNumber <> -1 Then

                cars(Active_Car).models.Spring(i) = New PRM(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Spring(i).modelNumber).Replace(Chr(34), ""))
                cars(Active_Car).models.Spring(i).TextureI = 1


                'scale
                Dim Scale! = (cars(Active_Car).Theory.Spring(i).Offset - cars(Active_Car).Theory.wheel(i).Offset(1)).Length / cars(Active_Car).Theory.Spring(i).Length


                cars(Active_Car).models.Spring(i).MATRIX = Matrix4.Scale(1, Scale, 1)

                cars(Active_Car).models.Spring(i).MATRIX *= BuildLookMatrixDown( _
                            cars(Active_Car).Theory.wheel(i).Offset(1) * Zoom, _
                            cars(Active_Car).Theory.Spring(i).Offset * Zoom)


                cars(Active_Car).models.Spring(i).MATRIX *= Matrix4.CreateTranslation(cars(Active_Car).Theory.Spring(i).Offset * Zoom)



            End If


        Next


        'load axles
        For i = 0 To 3
            If cars(Active_Car).Theory.Axle(i).modelNumber <> -1 Then


                cars(Active_Car).models.axle(i) = New PRM(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Axle(i).modelNumber).Replace(Chr(34), ""))
                cars(Active_Car).models.axle(i).TextureI = 1

                Dim Scale! = (cars(Active_Car).Theory.Axle(i).offSet - cars(Active_Car).Theory.wheel(i).Offset(1)).LengthFast / cars(Active_Car).Theory.Axle(i).Length


                cars(Active_Car).models.axle(i).MATRIX = Matrix4.Scale(1, 1, Scale)

                cars(Active_Car).models.axle(i).MATRIX *= BuildLookMatrixForward( _
                                                    cars(Active_Car).Theory.Axle(i).offSet * Zoom, _
                                                      cars(Active_Car).Theory.wheel(i).Offset(1) * Zoom)


                cars(Active_Car).models.axle(i).MATRIX *= Matrix4.CreateTranslation(cars(Active_Car).Theory.Axle(i).offSet * Zoom)




            End If
        Next


        'load PINs
        For i = 0 To 3
            If cars(Active_Car).Theory.PIN(i).modelNumber <> -1 Then
                'If _Pin(i) IsNot Nothing Then
                cars(Active_Car).models.Pin(i) = New PRM(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.PIN(i).modelNumber).Replace(Chr(34), ""))
                cars(Active_Car).models.Pin(i).TextureI = 1


                'Dim Scale! = (cars(Active_Car).Theory.Spring(i).offset  * ZOOM   - cars(Active_Car).Theory.wheel(i).offset  * ZOOM (1) ).Length / cars(Active_Car).Theory.PIN(i).Length

                cars(Active_Car).models.Pin(i).MATRIX = Matrix4.Scale(1, -cars(Active_Car).Theory.PIN(i).Length, 1)

                cars(Active_Car).models.Pin(i).MATRIX *= BuildLookMatrixDown( _
                         cars(Active_Car).Theory.wheel(i).Offset(1) * Zoom, _
                         cars(Active_Car).Theory.PIN(i).offSet * Zoom + cars(Active_Car).Theory.Spring(i).Offset * Zoom)

                cars(Active_Car).models.Pin(i).MATRIX *= Matrix4.CreateTranslation(cars(Active_Car).Theory.PIN(i).offSet * Zoom + cars(Active_Car).Theory.Spring(i).Offset * Zoom / 2)




                'End If
            End If
        Next


        'load spinner
        If cars(Active_Car).Theory.Spinner.modelNumber <> -1 Then
            cars(Active_Car).models.Spinner = New PRM(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Spinner.modelNumber).Replace(Chr(34), ""))

            cars(Active_Car).models.Spinner.TextureI = 1
            ' cars(Active_Car).models.spinner.Render()
            '  MsgBox(_Spinner.PolysReadingProgress)
            cars(Active_Car).models.Spinner.Position = cars(Active_Car).Theory.Spinner.offSet * Zoom

            ' cars(Active_Car).Theory.Spinner.Axis()
            '_Spinner.ScnNode.Scale = cars(Active_Car).Theory.Spinner.Axis 

        End If


        'load aerial
        If cars(Active_Car).Theory.Aerial.ModelNumber <> -1 Then
            If IO.File.Exists(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Aerial.ModelNumber).Replace(Chr(34), "")) = False Then
                ' Tip.fShow("~~Error: MODEL(" & cars(Active_Car).Theory.Aerial.ModelNumber & ") doesn't exist" & vbNewLine)
            End If
            cars(Active_Car).models.Aerial = New PRM(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Aerial.ModelNumber).Replace(Chr(34), ""))
            If cars(Active_Car).models.Aerial IsNot Nothing Then

                cars(Active_Car).models.Aerial.TextureI = 2 'RVPATH & "\gfx\fxpage1.bmp"
                '  cars(Active_Car).models.aerial.Render()

                cars(Active_Car).models.Aerial.MATRIX = Matrix4.Scale(1, cars(Active_Car).Theory.Aerial.length * 3 / 2, 1)
                ' cars(Active_Car).models.aerial.MATRIX *= Matrix4.Scale(cars(Active_Car).Theory.Aerial.Direction)
                cars(Active_Car).models.Aerial.RenderBBOX = True

                cars(Active_Car).models.Aerial.Position = cars(Active_Car).Theory.Aerial.offset * Zoom  '+ New Vector3D(0,  cars(Active_Car).models.aerial.BoundingBox.maxY * cars(Active_Car).Theory.Aerial.length, 0)
                ' cars(Active_Car).models.aerial.MATRIX *= Matrix4.Scale(New Vector3d(1, cars(Active_Car).Theory.Aerial.length, 1))
                '   cars(Active_Car).models.aerial.Rotation = cars(Active_Car).Theory.Aerial.Direction
                'cars(Active_Car).models.aerial.ScnNode.Scale.SetLength(cars(Active_Car).Theory.Aerial.length)


            End If
        End If


        'load aerial's top [@TO BE FIXED]
        If cars(Active_Car).Theory.Aerial.TopModelNumber <> -1 Then
            Dim aerialtop As New PRM(RVPATH & "\" & cars(Active_Car).Theory.MainInfos.Model(cars(Active_Car).Theory.Aerial.TopModelNumber).Replace(Chr(34), ""))
            If aerialtop IsNot Nothing Then
                aerialtop.TextureI = 2 ' RVPATH & "\gfx\fxpage1.bmp"
                ' aerialtop.Render()
                '   aerialtop.ScnNode.Scale *= 5
                ' aerialtop.ScnNode.Position *= cars(Active_Car).Theory.Aerial.Direction.Y
                aerialtop.MATRIX = Matrix4.Scale(1, -cars(Active_Car).Theory.Aerial.length / 3, 1)
                aerialtop.Position = cars(Active_Car).Theory.Aerial.offset * Zoom + New Vector3(0, cars(Active_Car).Theory.Aerial.length * 3 / 2, 0) ' * cars(Active_Car).Theory.Aerial.length * 2

                '  aerialtop.Scale = New Vector3d(1, -5, 1)
                '  aerialtop.ScnNode.Position += cars(Active_Car).Theory.Aerial.Direction
                '  aerialtop.ScnNode.Position += cars(Active_Car).Theory.Aerial.offset  * ZOOM   '+  ' ) '+' Aerial.ScnNode.BoundingBox.MaxEdge



            End If
        End If




        CarIsLoading = False

    End Sub
    Dim kp As Point
    Dim delta As Integer
    Public PANNING_ALLOWED = True
    Public Sub GlControl1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles GlControl1.MouseMove
        'Viewport controlling

        'left -> rotate
        If e.Button = Windows.Forms.MouseButtons.Left Then
            'myw.Position += New Vector3((e.Location - kp).X * 0.5, -(e.Location - kp).Y * 0.5, 0)

            PRM.Theta += (e.Location - kp).Y * 0.5
            PRM.Phi -= (e.Location - kp).X * 0.5

            'Invalidate()

        End If

        'right -> (if panning_allowed) PANNING
        If PANNING_ALLOWED And e.Button = Windows.Forms.MouseButtons.Right Then
            'myw.Position += New Vector3((e.Location - kp).X * 0.5, -(e.Location - kp).Y * 0.5, 0)

            GlobalPosition.X += (e.Location - kp).X * 0.0025
            GlobalPosition.Y += -(e.Location - kp).Y * 0.0025

            'Invalidate()

        End If

        'save current location
        kp = e.Location
    End Sub
    Public Shared StartedRendering = False : Public Shared FinishedRendering = True
    Public FrC As Single = 0.0F
    Dim sinceLastFrame = 0
    Dim th() As Threading.Thread
    Private Sub Timer2_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer2.Tick
        'Mastermind of timing 
        If CarIsLoading = True Then Exit Sub 'no need to render a currently being loaded car
        StartedRendering = True 'we have started rendering


        If CarEditor.BackgroundWorker1.IsBusy Then Exit Sub 'currently being rendered at different thread?

        '    If (Not FinishedRendering) and sinceLastFrame < 2  Then Exit Sub

        'glMatrixMode(MODEL_VIEW) of course!
        GL.MatrixMode(MatrixMode.Modelview)
        GL.LoadIdentity() 'GL.MATRIX = IDENTITY
        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit) 'CLEAR Z-Buffer + colors


        Application.DoEvents() 'make sure that other stuffs are being done at the same time



        'lights
        GL.Light(LightName.Light0, LightParameter.Diffuse, New Graphics.Color4(1.0F, 1.0F, 1.0F, 1.0F))
        GL.Light(LightName.Light0, LightParameter.Specular, New Graphics.Color4(1.0F, 1.0F, 1.0F, 1.0F))
        GL.Light(LightName.Light0, LightParameter.Ambient, New Graphics.Color4(0.8F, 0.8F, 0.8F, 1.0F))
        GL.Light(LightName.Light0, LightParameter.Position, New Vector4(-10, -10, 0, 0))

        'if exceeded the global position, reload
        If GlobalPosition.X = Single.NaN Then GlobalPosition.X = -0.85
        If GlobalPosition.Y = Single.NaN Then GlobalPosition.Y = 2
        If GlobalPosition.Z = Single.NaN Then GlobalPosition.Z = 0.25

        'EYE (camera)
        Dim Eye = New Vector3(GlobalPosition.Length * Cos(PRM.Theta * PI / 180) * Sin(PRM.Phi * PI / 180), _
                              GlobalPosition.LengthFast * Sin(PRM.Theta * PI / 180) * Sin(PRM.Phi * PI / 180), _
                              GlobalPosition.LengthFast * Cos(PRM.Phi * PI / 180))


        'Build project matrix
        GL.LoadMatrix(Matrix4.LookAt(Eye, New Vector3, New Vector3(0, 1, 0)))

        'Rotate along Theta (from mousemove event)
        GL.Rotate(2 * PRM.Theta, New Vector3(1, 0, 0))

        'Render each model
        For Each MODEL In Models
            MODEL.Render()

        Next


        sinceLastFrame = 0 'since last render, 0

        Try

            If Not GlControl1.IsDisposed Then GlControl1.SwapBuffers() 'swap buffer if didn't quit
            FinishedRendering = True
            StartedRendering = False
        Catch ex As Exception

        End Try


        FinishedRendering = True
        StartedRendering = False
        FrC += 1
    End Sub


    Private Sub ListBox2_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox2.SelectedIndexChanged
        'load car
        LoadOneCar()
        Stats()
        OnCL()

    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        'get memory
        memory.Text = Format(Process.GetCurrentProcess.WorkingSet64 / (1024 * 1024), "0#.#0") & "MB used"
        Dim intense = My.Computer.Info.AvailablePhysicalMemory / My.Computer.Info.TotalPhysicalMemory
        memory.ForeColor = Color.FromArgb(255 * intense, (1 - intense) * 255, 0)
    End Sub
    Public Sub GlControl1_MouseWheel(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles GlControl1.MouseWheel


        'If e.Delta > 0 Then ZOOM *= Math.Abs(e.Delta) * 0.01 Else ZOOM /= Math.Abs(e.Delta) * 0.01

        'If wheel then zoom
        If GlobalPosition.LengthFast = 0 Then GlobalPosition = New Vector3(Single.Epsilon, Single.Epsilon, Single.Epsilon)
        GlobalPosition += Vector3.NormalizeFast(GlobalPosition) * -0.1 * (e.Delta) / Math.Abs(e.Delta)

        ' $oldjunk:    Invalidate()


    End Sub


    Dim CurPos As Point
    Private Sub Label1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Car_selection.MouseMove
        If e.Button = Windows.Forms.MouseButtons.Left Then
            Me.Location += MousePosition - CurPos
            CurPos = MousePosition
        Else
            CurPos = MousePosition
        End If
    End Sub

    '$toberemade: old code, if trackbar scrolled rotate wheels
    Private Sub TrackBar1_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs)
        For i = 0 To 3
            'If cars(Active_Car).models.wheel(i) IsNot Nothing Then If cars(Active_Car).Theory.wheel(i).IsTurnable Then cars(Active_Car).models.wheel(i).Rotation = New Vector3(0, 'TrackBar1.Value, 0)
        Next
    End Sub

    Private Sub Timer3_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer3.Tick
        'auto car rotation
        If Not Initializd Then Exit Sub
        If CarEditor.Visible = True Then Timer3.Enabled = False
        PRM.Phi += 1
    End Sub

    Private Sub Auto_Rotate_car_CheckedChanged() Handles Auto_Rotate_car.CheckedChanged
        'again, auto rotation
        If CarEditor.Visible = False Then Timer3.Enabled = Auto_Rotate_car.Checked

    End Sub
    '--------------------------------------SEARCH------------------------------------
    Dim customSearchMode = False
    Dim SearchKeywords As New List(Of String)
    Dim HashKeyWords As New List(Of String)
    Private tmpKeys$
    Private Sub Search_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Search.TextChanged
        If Search.Text = "Search" Then
            For i = 0 To carList.Count - 1
                ListBox2.Items.Add(carList(i))
            Next
            Exit Sub
        End If


        'init 
        ListBox2.Items.Clear()
        SearchKeywords.Clear()
        HashKeyWords.Clear()
        tmpKeys = Search.Text

        'search by tag
        If InStr(tmpKeys, "#") > 0 Then
            Dim r = Split(tmpKeys, "#")
            For i = 1 To r.Count - 1
                Dim o = If(InStr(r(i), " ") > 0, Split(r(i), " ")(1), r(i))
                HashKeyWords.Add(UCase(o))
                o = Nothing
                tmpKeys = Replace(tmpKeys, "#" & r(i), "")
            Next
        End If


        'keyword: One or more?
        If InStr(tmpKeys$, ",") > 0 Then
            Dim r = Split(tmpKeys, ",")
            For i = 0 To r.Count - 1
                SearchKeywords.Add(Replace(r(i), ".", ""))
            Next

        Else
            SearchKeywords.Add(Replace(tmpKeys, ".", ""))
        End If


        'search each!
        For j = 0 To SearchKeywords.Count - 1
            For i = 0 To carList.Count - 1
                If InStr(CleanStrRet(carList(i)), SearchKeywords(j), CompareMethod.Text) Then

                    ListBox2.Items.Add(carList(i))

                End If
            Next
        Next j

        'hash! let's leave it at STOCK and CUSTOM (#Author:kallel , #Repaint, #original, #verified)
        For j = 0 To carList.Count - 1
            For i = 0 To HashKeyWords.Count - 1
                Select Case HashKeyWords(i)
                    Case "STOCK"
                        If Not IsStock(Split(Split(carList(j), "(")(1), ")")(0)) Then
                            If ListBox2.Items.Contains(carList(j)) Then
                                ListBox2.Items.Remove(carList(j))
                                '  j -= 1
                            End If

                        End If
                    Case "CUSTOM"
                        If IsStock(Split(Split(carList(j), "(")(1), ")")(0)) Then
                            If ListBox2.Items.Contains(carList(j)) Then
                                ListBox2.Items.Remove(carList(j))
                                '  j -= 1
                            End If

                        End If
                End Select
            Next
        Next

        'approach by first goes to first!
        Dim Uindex As New List(Of Integer), CurInstrPos = 5000
        For i = 0 To ListBox2.Items.Count - 1
            Dim foo = CleanStrRet(UCase(RTrim(LTrim(Split(ListBox2.Items(i), "(")(0)))))
            Dim bar = InStr(foo, UCase(SearchKeywords(0)), CompareMethod.Text)
            If bar > 0 Then
                If CurInstrPos > bar Then CurInstrPos = bar : Uindex.Add(i)
            End If


        Next

        If Uindex.Count = 0 Then Uindex = Nothing : Exit Sub
        If Uindex.Count = 1 Then ListBox2.SelectedIndex = Uindex(0) : Uindex = Nothing : Exit Sub

        'approach by length
        Dim CurScr = 0.0F, CurIn As Integer
        For i = 0 To Uindex.Count - 1

            Dim foo = CleanStrRet(UCase(RTrim(LTrim(Split(ListBox2.Items(Uindex(i)), "(")(0)))))
            Dim d! = CompScore(foo, SearchKeywords(0))
            If CurScr < d Then
                CurScr = d
                CurIn = Uindex(i)
            End If
        Next

        ListBox2.SelectedIndex = CurIn

    End Sub
    'score of how equal are two strings
    Function CompScore(ByVal str1$, ByVal str2$) As Single
        Dim Score = 0
        cleanStr(str1)
        cleanStr(str2)
        str1 = LTrim(RTrim(str1))
        str2 = LTrim(RTrim(str2))
        For i = 0 To Min(str1.Length, str2.Length) - 1
            If LCase(str1(i)) = LCase(str2(i)) Then Score += 1
        Next

        Return Score / Max(str1.Length, str2.Length)

    End Function
    'clean strings
    Private Sub CleanStr(ByRef Str$)
        Str = Replace(Str, ".", "")
        Str = Replace(Str, ",", "")
        Str = Replace(Str, "!", "")
    End Sub

    Private Function CleanStrRet(ByVal Str$) As String
        Str = Replace(Str, ".", "")
        Str = Replace(Str, ",", "")
        Str = Replace(Str, "!", "")
        Return Str
    End Function

    'init search textbox
    Private Sub Search_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Search.GotFocus
        If Search.Text = "Search" Then Search.Text = ""

        Search.ForeColor = Drawing.Color.Black
        '    search.Font = New Drawing.Font(Drawing.FontFamily.GenericSansSerif, 8.25, FontStyle.Regular, GraphicsUnit.Point)



        Search.SelectionStart = Len(Search.Text)

    End Sub

    'blur search textbox
    Private Sub search_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Search.KeyDown
        If e.KeyCode = Keys.Escape Then
            ListBox2.Focus()
        End If
    End Sub

    Private Sub search_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles Search.LostFocus
        If Search.Text = "" Then

            Search.Text = "Search"
            For i = 0 To carList.Count - 1
                ListBox2.Items.Add(carList(i))
            Next

            '   search.Font = New Drawing.Font(Drawing.FontFamily.GenericSansSerif, 8.25, FontStyle.Italic, GraphicsUnit.Point)

            Search.ForeColor = Drawing.Color.Gray


        End If
    End Sub
  
    'lights on?
    Private Sub CheckBox1_CheckedChanged() Handles light.CheckedChanged
        If Not Initializd Then Exit Sub
        If light.Checked Then
            GL.Enable(EnableCap.Lighting)
            GL.Enable(EnableCap.Light0)

        Else
            GL.Disable(EnableCap.Lighting)
            GL.Disable(EnableCap.Light0)

        End If

    End Sub

    'quit
    Private Sub PictureBox1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PictureBox1.Click
        End
    End Sub

 
    'go to RVL
    Private Sub VisitRVL_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles VisitRVL.LinkClicked
        Shell("explorer ""http://z3.invisionfree.com/Revolt_Live", AppWinStyle.NormalFocus)
    End Sub

    'Resize scene
    Private Sub GlControl1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles GlControl1.Resize
        If Not Initializd Then Exit Sub
        GL.Viewport(0, 0, GlControl1.ClientSize.Width, GlControl1.ClientSize.Height)
    End Sub

    'picked a car!
    Public Sub Pick_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Pick.Click
        If cars.Count = 0 Then Exit Sub
        Timer2.Stop() 'stop rendering


        Me.Hide()
        CarEditor.Timer2.Start()         'start editor's rendering
        CarEditor.Show()                 ' show editor window
        initGL(CarEditor.GlControl1)     'initGL of the car editor


        Auto_Rotate_car.Checked = False 'no need to rotate
        Me.Close()                      'get disposed!

    End Sub

    'TODO: readme show fix
    Private Sub view_readme_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles view_readme.Click
        Dim allreadmes = IO.Directory.GetFiles(cars(0).Path, "*read*me*")
        If allreadmes.Count > 1 Then
            For i = 0 To allreadmes.Count - 1
                If InStr(allreadmes(i), "original", CompareMethod.Text) > 0 Then Continue For
                TextViewer.TextBox1.Text = IO.File.ReadAllText((IO.Directory.GetFiles(cars(0).Path, "*read*me*")(i)))
            Next
            TextViewer.TextBox1.Text = IO.File.ReadAllText((IO.Directory.GetFiles(cars(0).Path, "*read*me*")(0)))
        Else
            TextViewer.TextBox1.Text = IO.File.ReadAllText((IO.Directory.GetFiles(cars(0).Path, "*read*me*")(0)))
        End If

        TextViewer.TextBox1.SelectionStart = TextViewer.TextBox1.SelectionLength
        TextViewer.TextBox1.Select()

        TextViewer.Show()
    End Sub

    'start new car (originally thought for licence file)
    Private Sub start_new_car_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles start_new_car.Click
        YESNO.Question.Text = "Going to start a new car, will this be alright?"
        If YESNO.ShowDialog <> Windows.Forms.DialogResult.Yes Then
            Exit Sub

        End If
        cars(0) = New Car("")


    End Sub

    'get Random car?
    Private Sub Button1_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If ListBox2.Items.Count = 0 Then Exit Sub
        Randomize()

        ListBox2.SelectedIndex = Int(Rnd() * ListBox2.Items.Count)
    End Sub

    Private Sub Panel7_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel7.Paint

    End Sub
End Class