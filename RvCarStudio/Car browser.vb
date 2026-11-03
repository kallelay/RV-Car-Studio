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
    Public CarFolderOf As New Dictionary(Of String, String) 'list item -> folder of that car
  
    Private Sub CarBrowser_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
#If DEBUG Then
        Auto_Rotate_car.Checked = False
#End If
    End Sub
    Public PolyCount = 0 '$kay: requires a LONG cast! this is for calculating the polygon count


    'for showing car statistics (most of it is just text rendering)
    Sub Stats()
        If cars.Count = 0 OrElse cars(0).Theory Is Nothing Then Exit Sub
        Dim m = cars(Active_Car).models
        Dim t = cars(Active_Car).Theory

        Label4.Text = If(m.BODY IsNot Nothing, CStr(m.BODY.MyModel.polynum), "-")

        Label5.Text = ""
        For i = 0 To 3
            Label5.Text &= If(m.Wheel(i) IsNot Nothing, CStr(m.Wheel(i).MyModel.polynum), "-")
            If i < 3 Then Label5.Text &= If(i = 1, "," & vbNewLine, ", ")
        Next

        'selectable, besttime etc...
        Label7.Text = t.MainInfos.SELECTABLE
        Label9.Text = t.MainInfos.BESTTIME

        Label11.Text = DirectCast(t.MainInfos.car_class, classes).ToString
        Label13.Text = Replace(DirectCast(t.MainInfos.obtain, Obtaino).ToString, "_", " ")
        Label15.Text = Replace(DirectCast(t.MainInfos.Rating, ratingo).ToString, "_", "-")

        Label17.Text = Format(t.RealInfos.TopSpeed, "0.0") & " mph" & vbNewLine & Int(t.RealInfos.TopSpeed * 1.6) & " km/h"

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
        Super_Pro = 5
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
        ' Validate RVPATH before attempting to access it
        If Not EnsureValidRVPath() Then
            ' User cancelled reconfiguration or path is still invalid
            MsgBox("Cannot load cars without a valid Re-Volt installation path. Application will now exit.",
                   MsgBoxStyle.Critical, "Cannot Continue")
            Application.Exit()
            Return
        End If

        ' Car path (use Path.Combine for proper path handling)
        Dim carPath As String = IO.Path.Combine(RVPATH, "cars")

        ' Remove all cars
        ListBox2.Items.Clear()
        carList.Clear()

        ' Get all cars folders with error handling
        Dim cars() As String
        Try
            cars = IO.Directory.GetDirectories(carPath)
        Catch ex As IO.IOException
            MsgBox("Error accessing cars directory: " & ex.Message & vbNewLine & vbNewLine &
                   "The device may not be ready. Please check your Re-Volt installation path.",
                   MsgBoxStyle.Exclamation, "Cannot Access Cars")
            ' Try to reconfigure
            If EnsureValidRVPath() Then
                ' Retry after reconfiguration
                Try
                    carPath = IO.Path.Combine(RVPATH, "cars")
                    cars = IO.Directory.GetDirectories(carPath)
                Catch retryEx As Exception
                    MsgBox("Still cannot access cars directory after reconfiguration: " & retryEx.Message,
                           MsgBoxStyle.Critical, "Fatal Error")
                    Application.Exit()
                    Return
                End Try
            Else
                Application.Exit()
                Return
            End If
        Catch ex As Exception
            MsgBox("Unexpected error loading cars: " & ex.Message, MsgBoxStyle.Critical, "Error")
            Application.Exit()
            Return
        End Try

        'for each car...
        CarFolderOf.Clear()
        For i = LBound(cars) To UBound(cars)
            If IO.File.Exists(cars(i) & "\parameters.txt") Then 'get parameters
                Dim sing As New Singletons(cars(i) & "\parameters.txt") 'load it into singletons
                Dim Pname = cars(i).Split("\").Last
                Dim name = sing.getSingleton("").getValue("Name", "")
                If name = "" Then name = "(no name)"

                Dim item = " " & vbTab & name & "   (" & Pname & ")" 'name & folder(path) name
                CarFolderOf(item) = Pname
                ListBox2.Items.Add(item)
            End If
        Next

        Application.DoEvents()
        If Not Initializd Then SplashScreen.Progress("Activating Search function")

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
        ' Validate RVPATH before attempting to load car
        If Not EnsureValidRVPath() Then
            MsgBox("Cannot load car without a valid Re-Volt installation path.", MsgBoxStyle.Exclamation, "Cannot Load Car")
            CarIsLoading = False
            Exit Sub
        End If

        'get one car, main code
        PolyCount = 0
        Timer2.Stop()
        If ListBox2.SelectedIndex = -1 Then Exit Sub 'if selected&clicked exit


        'car->loading
        CarIsLoading = True

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

        Dim mycar = CarFolderOf(ListBox2.SelectedItem) 'get carfolder

        ' Use Path.Combine for proper path handling and wrap in Try-Catch
        Try
            Dim carPath As String = IO.Path.Combine(RVPATH & "\cars", mycar)
            cars.Add(New Car(carPath)) 'add new car

            cars(Active_Car).Load() 'load car
        Catch ex As IO.IOException
            MsgBox("Error loading car. The Re-Volt path may not be accessible." & vbNewLine & vbNewLine &
                   "Error: " & ex.Message, MsgBoxStyle.Exclamation, "Cannot Load Car")
            CarIsLoading = False
            Exit Sub
        Catch ex As Exception
            MsgBox("Unexpected error loading car: " & ex.Message, MsgBoxStyle.Critical, "Error")
            CarIsLoading = False
            Exit Sub
        End Try


        '----------------------- get author name-------------------
        Dim auth$ = ""
        Label22.Text = ""

        Try
            Dim carDirPath As String = IO.Path.Combine(RVPATH & "\cars", mycar)
            If Not IO.Directory.Exists(carDirPath) Then Exit Sub

            ' Check for author name from car name
            If InStr(cars(Active_Car).Theory.MainInfos.Name, "Halogaland", CompareMethod.Text) > 0 Then
                auth = "Halogaland"
                Label22.Text = auth
            End If

            ' Try to get author from readme file
            Dim readmeFiles() As String = IO.Directory.GetFiles(carDirPath, "*read*me*")
            If readmeFiles.Length > 0 Then
                made_by.Visible = True
                Label22.Visible = True
                view_readme.Visible = True
                auth = DetectAuthor(IO.File.ReadAllText(readmeFiles(0), Singletons.FileEncoding))
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

        Catch ex As IO.IOException
            ' Error reading author info - not critical, continue loading car
            Console_.W("Warning: Could not read car author info - " & ex.Message)
            made_by.Visible = False
            Label22.Visible = False
            view_readme.Visible = False
        Catch ex As Exception
            ' Other errors in author detection - not critical
            Console_.W("Warning: Error detecting car author - " & ex.Message)
            made_by.Visible = False
            Label22.Visible = False
            view_readme.Visible = False
        End Try





        Application.DoEvents()
        setProgress(20)

        'get texture
        Application.DoEvents()
        setProgress(22)
        InitAllTextures(Car.GamePath(cars(Active_Car).Theory.MainInfos.Tpage))


        ReLoadOneCar() 'load all car models goes here
        FrameCar(cars(Active_Car))

        setProgress(100)
        Panel2.Hide()
        Timer2.Start()

    End Sub
    'Author of a car, from its readme
    Public Shared Function DetectAuthor(ByVal rm As String) As String
        Dim lines = rm.Split(New Char() {vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        If lines.Length > 0 AndAlso lines.Last.TrimStart().StartsWith("Burner", StringComparison.OrdinalIgnoreCase) Then Return "Burner94"
        If InStr(rm, "---") > 0 And InStr(rm, "citywalker", CompareMethod.Text) > 0 Then Return "Citywalker"

        For Each pattern In New String() {"author\s*name", "author", "created\s+by", "creator", "made\s+by", "\bby"}
            Dim m = System.Text.RegularExpressions.Regex.Match(rm, "(?:" & pattern & ")\b[^A-Za-z0-9\r\n]*([A-Za-z0-9].*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If m.Success Then
                Dim a = m.Groups(1).Value.Trim(" "c, vbTab, vbCr, "+"c, "-"c, "="c, "*"c, "#"c, "."c)
                If a <> "" Then Return If(a.Length > 40, a.Substring(0, 40), a)
            End If
        Next
        Return "Unknown"
    End Function

    'One part of the car: the model in this slot, or Nothing if there isn't any
    Private Function LoadPart(ByVal slot As Integer, Optional ByVal texture As Integer = 1) As PRM
        If slot < 0 OrElse slot > 19 Then Return Nothing
        Dim file = Car.GamePath(cars(Active_Car).Theory.MainInfos.Model(slot))
        If file Is Nothing Then Return Nothing
        Try
            Dim p As New PRM(file)
            p.TextureI = texture
            Return p
        Catch ex As Exception
            Return Nothing 'already in the console, and the car checker will report it
        End Try
    End Function

    'Point a model from one place to another. Y is the long side of springs and pins, Z of axles
    Private Shared Function SafeDown(ByVal from As Vector3, ByVal target As Vector3) As Matrix4
        If (target - from).LengthSquared < 0.0000001F Then Return Matrix4.Identity
        Return BuildLookMatrixDown(from, target)
    End Function
    Private Shared Function SafeForward(ByVal from As Vector3, ByVal target As Vector3) As Matrix4
        If (target - from).LengthSquared < 0.0000001F Then Return Matrix4.Identity
        Return BuildLookMatrixForward(from, target)
    End Function

    Sub ReLoadOneCar(Optional ByVal KeepTextures = True, Optional ByVal ReloadParams = False)
        'This is the sub responsible for loading one car's models
        PolyCount = 0

        'make sure initialisation goes fine
        CarIsLoading = True
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
        If Not KeepTextures Then InitAllTextures(Car.GamePath(cars(Active_Car).Theory.MainInfos.Tpage))

        Dim t = cars(Active_Car).Theory
        Dim m As New MODELSET
        cars(Active_Car).models = m

        'body
        m.BODY = LoadPart(t.Body.modelNumber)
        If m.BODY IsNot Nothing Then m.BODY.Position = t.Body.Offset * Zoom

        Application.DoEvents()
        setProgress(35)

        For i = 0 To 3
            Dim wheelPos As Vector3 = t.wheel(i).Offset(1)

            'wheels
            m.Wheel(i) = LoadPart(t.wheel(i).modelNumber)
            If m.Wheel(i) IsNot Nothing Then
                m.Wheel(i).Position = wheelPos * Zoom
                m.Wheel(i).isVisible = t.wheel(i).IsPresent
            End If

            'springs hang from their mount, stretched down to the wheel
            Dim mount As Vector3 = t.Spring(i).Offset
            m.Spring(i) = LoadPart(t.Spring(i).modelNumber)
            If m.Spring(i) IsNot Nothing Then
                Dim stretch = If(t.Spring(i).Length <> 0, (mount - wheelPos).Length / CSng(t.Spring(i).Length), 1.0F)
                m.Spring(i).MATRIX = Matrix4.Scale(1, stretch, 1) * SafeDown(wheelPos * Zoom, mount * Zoom) * Matrix4.CreateTranslation(mount * Zoom)
            End If

            'axles go from their mount to the wheel, stretched as well
            Dim axleMount As Vector3 = t.Axle(i).offSet
            m.axle(i) = LoadPart(t.Axle(i).modelNumber)
            If m.axle(i) IsNot Nothing Then
                Dim stretch = If(t.Axle(i).Length <> 0, (axleMount - wheelPos).Length / CSng(t.Axle(i).Length), 1.0F)
                m.axle(i).MATRIX = Matrix4.Scale(1, 1, stretch) * SafeForward(axleMount * Zoom, wheelPos * Zoom) * Matrix4.CreateTranslation(axleMount * Zoom)
            End If

            'pins are stiff: they hang from the base of the spring towards the wheel, without stretching
            'Length multiplies the model, the usual negative one makes it point down
            m.Pin(i) = LoadPart(t.PIN(i).modelNumber)
            If m.Pin(i) IsNot Nothing Then
                m.Pin(i).MATRIX = Matrix4.Scale(1, CSng(t.PIN(i).Length), 1) * SafeDown(wheelPos * Zoom, mount * Zoom) * Matrix4.CreateTranslation(mount * Zoom)
            End If

            'where wheels turn
            If m.Wheel(i) IsNot Nothing AndAlso t.wheel(i).Camber <> 0 Then
                Dim side = If(wheelPos.X < 0, 1, -1)
                m.Wheel(i).MATRIX = Matrix4.CreateRotationZ(CSng(side * t.wheel(i).Camber * PI / 180)) * m.Wheel(i).MATRIX
            End If
        Next

        'spinner
        m.Spinner = LoadPart(t.Spinner.modelNumber)
        If m.Spinner IsNot Nothing Then m.Spinner.Position = t.Spinner.offSet * Zoom

        'aerial: straight sections and the top model at the end. Length is the length of 2 of
        'the 4 parts in the game. These models are 1 unit long, going up from their origin
        If t.Aerial.ModelNumber <> -1 OrElse t.Aerial.TopModelNumber <> -1 Then
            Dim dir = t.Aerial.Direction
            If dir.LengthSquared < 0.0001F Then dir = Vector3.UnitY
            dir.Normalize()
            Dim partLen = CSng(t.Aerial.length * 2 / AerialParts)

            For s = 0 To AerialParts - 1
                Dim part = LoadPart(If(s < AerialParts - 1, t.Aerial.ModelNumber, t.Aerial.TopModelNumber), 2)
                If part Is Nothing Then Continue For
                If s = 0 Then m.Aerial = part
                Dim from = t.Aerial.offset + dir * (partLen * s)
                part.MATRIX = Matrix4.Scale(1, partLen, 1) * SafeDown(from * Zoom, (from + dir) * Zoom) * Matrix4.CreateTranslation(from * Zoom)
            Next
        End If

        CarIsLoading = False

    End Sub
    Const AerialParts = 4
    Dim kp As Point
    Dim delta As Integer
    Public PANNING_ALLOWED = True
    Public Sub GlControl1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles GlControl1.MouseMove
        'Viewport controlling

        'left -> rotate
        If e.Button = Windows.Forms.MouseButtons.Left Then
            'myw.Position += New Vector3((e.Location - kp).X * 0.5, -(e.Location - kp).Y * 0.5, 0)

            PRM.Theta = Max(-89, Min(89, PRM.Theta + (e.Location - kp).Y * 0.5))
            PRM.Phi -= (e.Location - kp).X * 0.5

            'Invalidate()

        End If

        'right -> (if panning_allowed) PANNING
        If PANNING_ALLOWED And e.Button = Windows.Forms.MouseButtons.Right Then
            'myw.Position += New Vector3((e.Location - kp).X * 0.5, -(e.Location - kp).Y * 0.5, 0)

            CamPan.X -= (e.Location - kp).X * 0.0025F
            CamPan.Y += (e.Location - kp).Y * 0.0025F

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
        If Not Me.Visible OrElse GlControl1.IsDisposed Then Exit Sub

        BeginFrame(GlControl1)
        If cars.Count > 0 Then DrawFloor(FloorHeight(cars(Active_Car)))
        DrawModels(Models)
        GlControl1.SwapBuffers()

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
        If e.Delta = 0 Then Exit Sub
        CamDistance = Max(0.3F, Min(30.0F, CamDistance * If(e.Delta > 0, 0.9F, 1.1F)))

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
        PRM.Phi += 1
    End Sub

    Private Sub Auto_Rotate_car_CheckedChanged() Handles Auto_Rotate_car.CheckedChanged
        'again, auto rotation
        Timer3.Enabled = Auto_Rotate_car.Checked

    End Sub
    '--------------------------------------SEARCH------------------------------------
    Dim customSearchMode = False
    Dim SearchKeywords As New List(Of String)
    Dim HashKeyWords As New List(Of String)
    Private tmpKeys$
    Private Sub Search_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Search.TextChanged
        ListBox2.BeginUpdate()
        ListBox2.Items.Clear()

        'the empty box says "Search"
        If Search.Text = "Search" Or Search.Text = "" Then
            ListBox2.Items.AddRange(carList.ToArray())
            ListBox2.EndUpdate()
            Exit Sub
        End If

        SearchKeywords.Clear()
        HashKeyWords.Clear()

        'words starting with # are filters, the rest are names separated by commas
        tmpKeys = Search.Text
        For Each word In Split(Search.Text, " ")
            If word.StartsWith("#") Then
                HashKeyWords.Add(UCase(word.Substring(1)))
                tmpKeys = Replace(tmpKeys, word, "")
            End If
        Next
        For Each word In Split(tmpKeys, ",")
            If Trim(word) <> "" Then SearchKeywords.Add(CleanStrRet(Trim(word)))
        Next

        For Each item In carList
            Dim folder = CarFolderOf(item)
            If HashKeyWords.Contains("STOCK") And Not IsStock(folder) Then Continue For
            If HashKeyWords.Contains("CUSTOM") And IsStock(folder) Then Continue For
            If SearchKeywords.Count = 0 OrElse SearchKeywords.Any(Function(k) InStr(CleanStrRet(item), k, CompareMethod.Text) > 0) Then
                ListBox2.Items.Add(item)
            End If
        Next
        ListBox2.EndUpdate()

        If SearchKeywords.Count = 0 Or ListBox2.Items.Count = 0 Then Exit Sub

        'pick the closest name
        Dim best = 0, bestScore = -1.0F
        For i = 0 To ListBox2.Items.Count - 1
            Dim score = CompScore(CleanStrRet(Trim(ListBox2.Items(i).ToString.Replace(vbTab, ""))), SearchKeywords(0))
            If score > bestScore Then bestScore = score : best = i
        Next
        ListBox2.SelectedIndex = best
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

            '   search.Font = New Drawing.Font(Drawing.FontFamily.GenericSansSerif, 8.25, FontStyle.Italic, GraphicsUnit.Point)

            Search.ForeColor = Drawing.Color.Gray


        End If
    End Sub
  
    'lights on?
    Private Sub CheckBox1_CheckedChanged() Handles light.CheckedChanged
        LIGHTS_ON = light.Checked
    End Sub

    'quit
    Private Sub PictureBox1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PictureBox1.Click
        End
    End Sub

 
    'go to RVL
    Private Sub VisitRVL_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles VisitRVL.LinkClicked
        Process.Start("https://www.revoltworld.net/")
    End Sub

    'Resize scene
    Private Sub GlControl1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles GlControl1.Resize
        'BeginFrame takes the new size
    End Sub

    Dim EditorStarted As Boolean = False
    'picked a car!
    Public Sub Pick_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Pick.Click
        If cars.Count = 0 Then Exit Sub
        Timer2.Stop() 'stop rendering


        Me.Hide()
        Dim firstTime = Not CarEditor.Visible AndAlso Not EditorStarted
        CarEditor.Show()                 ' show editor window
        If firstTime Then initGL(CarEditor.GlControl1) 'initGL of the car editor
        EditorStarted = True
        CarEditor.Label4.Text = cars(Active_Car).Theory.MainInfos.Name
        CarEditor.ComboBox1_SelectedIndexChanged(sender, e)
        CarEditor.Timer2.Start()         'start editor's rendering


        Auto_Rotate_car.Checked = False 'no need to rotate

    End Sub

    'show the readme, the modder's own one if there are several
    Private Sub view_readme_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles view_readme.Click
        Dim allreadmes = IO.Directory.GetFiles(cars(0).Path, "*read*me*")
        If allreadmes.Length = 0 Then Exit Sub
        Dim pick = allreadmes(0)
        For Each f In allreadmes
            If InStr(f, "original", CompareMethod.Text) = 0 Then pick = f : Exit For
        Next

        TextViewer.TextBox1.Text = IO.File.ReadAllText(pick, Singletons.FileEncoding).Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
        TextViewer.Text = "Readme"
        TextViewer.TextBox1.Select(0, 0)

        TextViewer.Show()
    End Sub

    'start new car: a copy of the selected car (or of the stock Genghis Kar), under a new folder
    Private Sub start_new_car_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles start_new_car.Click
        Dim template = If(ListBox2.SelectedItem IsNot Nothing, CarFolderOf(ListBox2.SelectedItem), "gencar")

        Dim folder = InputBox("Folder of the new car, inside cars\." & vbNewLine & "It starts as a copy of '" & template & "'.", APPNAME)
        If folder = "" Then Exit Sub
        Dim name = InputBox("Name of the new car:", APPNAME, folder)
        If name = "" Then Exit Sub

        Try
            StartNewCar(template, folder, name)
        Catch ex As Exception
            MsgBox("Can't make the car: " & ex.Message, MsgBoxStyle.Exclamation, APPNAME)
            Exit Sub
        End Try

        LoadAllCarsIntoList()
        Search.Text = "Search"
        For Each item In carList
            If CarFolderOf(item).Equals(folder, StringComparison.OrdinalIgnoreCase) Then ListBox2.SelectedItem = item : Exit For
        Next
    End Sub

    ''' <summary>Copy a car to a new folder, its parameters pointing to that folder</summary>
    Public Shared Sub StartNewCar(ByVal templateFolder As String, ByVal newFolder As String, ByVal newName As String)
        If Not _main.ValidFolderName(newFolder) Then Throw New ArgumentException("That's not a good folder name")
        Dim from = RVPATH & "\cars\" & templateFolder
        Dim dest = RVPATH & "\cars\" & newFolder
        If Not IO.Directory.Exists(from) Then Throw New IO.DirectoryNotFoundException(from)
        If IO.Directory.Exists(dest) Then Throw New IO.IOException("There already is a folder " & dest)

        IO.Directory.CreateDirectory(dest)
        For Each f In IO.Directory.GetFiles(from)
            If IO.Path.GetExtension(f).ToLowerInvariant() = ".bak" Then Continue For
            IO.File.Copy(f, dest & "\" & IO.Path.GetFileName(f))
        Next

        'the copy still says it's the old car, which is why it can be repointed like a renamed one
        Dim c As New Car(dest)
        c.DirName = templateFolder
        c.Load()
        c.Theory.MainInfos.Name = newName
        c.Path = dest
        _main.RepointCar(c, newFolder)
        c.Sing.SaveToFile(dest & "\parameters.txt")
        If IO.File.Exists(dest & "\parameters.txt.bak") Then IO.File.Delete(dest & "\parameters.txt.bak")
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