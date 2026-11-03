Imports System.Math
Imports OpenTK

'Checks whether a car is missing something, and scores it
Module Check_My_Car
    Public Class Report
        Public Errors As New List(Of String)
        Public Warnings As New List(Of String)
        Public Infos As New List(Of String)
        Public ReadOnly Property Score() As Integer
            Get
                Return Max(0, 100 - Errors.Count * 15 - Warnings.Count * 4)
            End Get
        End Property
        Public Overrides Function ToString() As String
            Dim s As New Text.StringBuilder
            s.AppendLine("Score: " & Score & " / 100")
            s.AppendLine()
            s.AppendLine("ERRORS (" & Errors.Count & ")")
            For Each line In Errors : s.AppendLine("  " & line) : Next
            s.AppendLine()
            s.AppendLine("WARNINGS (" & Warnings.Count & ")")
            For Each line In Warnings : s.AppendLine("  " & line) : Next
            s.AppendLine()
            s.AppendLine("INFORMATION")
            For Each line In Infos : s.AppendLine("  " & line) : Next
            Return s.ToString()
        End Function
    End Class

    'The button: check the active car and show the result
    Sub CheckMyCar()
        TextViewer.TextBox1.Text = cars(Active_Car).Theory.MainInfos.Name & vbNewLine & vbNewLine & Check(cars(Active_Car)).ToString()
        TextViewer.Text = "Check my car"
        TextViewer.TextBox1.Select(0, 0)
        TextViewer.Show()
    End Sub

    Private Function IsNone(ByVal path As String) As Boolean
        Return String.IsNullOrEmpty(path) OrElse path.Trim().ToUpperInvariant() = "NONE"
    End Function

    'A model file that opens and makes sense, or the reason why not
    Private Function ReadModel(ByVal path As String, ByRef why As String) As PRM
        Dim full = Car.GamePath(path)
        If full Is Nothing Then why = "file not found" : Return Nothing
        Try
            Dim p As New PRM(full, True)
            If p.MyModel.polynum = 0 Then why = "empty model" : Return Nothing
            For i = 0 To p.MyModel.polynum - 1
                Dim q = p.MyModel.polyl(i)
                Dim last = If((q.type And PolyType.QUAD) <> 0, q.vi3, q.vi2)
                If Max(Max(q.vi0, q.vi1), Max(q.vi2, last)) >= p.MyModel.vertnum Or Min(Min(q.vi0, q.vi1), Min(q.vi2, last)) < 0 Then
                    why = "polygon " & i & " uses a vertex that isn't there" : Return Nothing
                End If
            Next
            Return p
        Catch ex As UnauthorizedAccessException
            why = "no access to the file (check its security settings)"
        Catch ex As Exception
            why = "can't be read: " & ex.Message
        End Try
        Return Nothing
    End Function

    Public Function Check(ByVal c As Car) As Report
        Dim r As New Report
        Dim t = c.Theory
        Dim M = t.MainInfos
        Dim saved = CarBrowser.PolyCount

        If Trim(M.Name) = "" Then r.Errors.Add("The car has no name")

        'Check there is a body, wheels; check if files exist
        Dim slot = Function(n As Integer, what As String) As PRM
                       If n < 0 Then Return Nothing
                       If n > 18 Then r.Errors.Add(what & " uses model " & n & ", the last one is 18") : Return Nothing
                       If IsNone(M.Model(n)) Then r.Errors.Add(what & " uses model " & n & ", which is NONE") : Return Nothing
                       Dim why = ""
                       Dim p = ReadModel(M.Model(n), why)
                       If p Is Nothing Then r.Errors.Add(what & ": " & M.Model(n) & ": " & why)
                       Return p
                   End Function

        Dim body As PRM = Nothing
        If t.Body.modelNumber < 0 Then
            r.Errors.Add("The body has no model")
        Else
            body = slot(t.Body.modelNumber, "Body")
        End If

        Dim wheels(3) As PRM
        For i = 0 To 3
            If t.wheel(i).IsPresent Then
                If t.wheel(i).modelNumber < 0 Then r.Warnings.Add("Wheel " & i & " is present but has no model")
                wheels(i) = slot(t.wheel(i).modelNumber, "Wheel " & i)
            End If
            slot(t.Spring(i).modelNumber, "Spring " & i)
            slot(t.Axle(i).modelNumber, "Axle " & i)
            slot(t.PIN(i).modelNumber, "Pin " & i)
            If t.Spring(i).modelNumber >= 0 AndAlso t.Spring(i).Length = 0 Then r.Errors.Add("Spring " & i & " has a model but length 0")
            If t.Axle(i).modelNumber >= 0 AndAlso t.Axle(i).Length = 0 Then r.Errors.Add("Axle " & i & " has a model but length 0")
        Next
        slot(t.Spinner.modelNumber, "Spinner")
        If t.Aerial.ModelNumber < 0 Then r.Infos.Add("No aerial")
        slot(t.Aerial.ModelNumber, "Aerial")
        slot(t.Aerial.TopModelNumber, "Aerial top")

        'texture
        Dim tex = Car.GamePath(M.Tpage)
        If tex Is Nothing Then
            r.Errors.Add("Texture not found: " & M.Tpage)
        Else
            Try
                Using b = New Bitmap(New IO.MemoryStream(IO.File.ReadAllBytes(tex)))
                    r.Infos.Add("Texture " & b.Width & " x " & b.Height)
                    If b.Width <> b.Height Or (b.Width And (b.Width - 1)) <> 0 Then r.Errors.Add("Texture must be square, power of 2 (it's " & b.Width & " x " & b.Height & ")")
                    If b.Width > 256 Then r.Infos.Add("Texture is over 256: only RVGL can use it")
                End Using
            Catch ex As Exception
                r.Errors.Add("Texture can't be read: " & M.Tpage)
            End Try
        End If

        'other files
        If IsNone(M.CollFile) OrElse Car.GamePath(M.CollFile) Is Nothing Then
            r.Errors.Add("Collision hull not found: " & M.CollFile)
        ElseIf New IO.FileInfo(Car.GamePath(M.CollFile)).Length = 0 Then
            r.Errors.Add("Collision hull is empty: " & M.CollFile)
        End If
        If Not IsNone(M.TCarBox) AndAlso Car.GamePath(M.TCarBox) Is Nothing Then r.Warnings.Add("Carbox not found: " & M.TCarBox)
        If IsNone(M.TCarBox) Then r.Infos.Add("No carbox")
        If Not IsNone(M.TShadow) AndAlso Car.GamePath(M.TShadow) Is Nothing Then r.Warnings.Add("Shadow texture not found: " & M.TShadow)
        For Each sfx In New String() {M.SfxEngine, M.SfxHonk, M.SfxServo}
            If Not IsNone(sfx) AndAlso Car.GamePath(sfx) Is Nothing Then r.Warnings.Add("Sound not found: " & sfx)
        Next

        'Check if it's based on other car (still depending on some)
        For Each f In ExtraModes.ExternalFiles(c)
            r.Warnings.Add("Depends on another folder: " & f)
        Next
        Dim stockBody = SameBodyAsStock(c)
        If stockBody <> "" AndAlso Not stockBody.Equals(c.DirName, StringComparison.OrdinalIgnoreCase) Then r.Infos.Add("Body is the one of stock car '" & stockBody & "'")

        'Check if EnvRGB is not 0 0 0
        If M.EnvRGB.R = 0 And M.EnvRGB.G = 0 And M.EnvRGB.B = 0 Then r.Warnings.Add("EnvRGB is 0 0 0: no reflections at all")

        'Check if it can be best time, selectable, obtain (0)
        If Not M.SELECTABLE Then r.Warnings.Add("Selectable is FALSE: players can't pick it")
        If Not M.BESTTIME Then r.Infos.Add("BestTime is FALSE: its times don't count")
        If M.obtain <> 0 Then r.Infos.Add("Obtain is " & M.obtain & ": not available from the start")

        'Polygon count
        Dim bodyPolys = If(body IsNot Nothing, CInt(body.MyModel.polynum), 0)
        Dim wheelPolys = wheels.Where(Function(w) w IsNot Nothing).Sum(Function(w) CInt(w.MyModel.polynum))
        r.Infos.Add("Polygons: body " & bodyPolys & ", wheels " & wheelPolys)
        If bodyPolys > 5000 Then r.Warnings.Add("Body has a lot of polygons (" & bodyPolys & "), stock cars have a few hundred")

        'Car size, wheels against the body
        If body IsNot Nothing Then
            Dim w = body.BoundingBox.maxX - body.BoundingBox.MinX
            Dim l = body.BoundingBox.maxZ - body.BoundingBox.MinZ
            Dim h = body.BoundingBox.maxY - body.BoundingBox.MinY
            r.Infos.Add("Body size " & Format(w, "0") & " x " & Format(h, "0") & " x " & Format(l, "0") & " (W x H x L)")
            If l < 30 Or l > 600 Then r.Warnings.Add("Unusual body length: " & Format(l, "0") & " (stock cars: 100 to 300)")
            If w > l * 2 Then r.Warnings.Add("Body is much wider than long, is it turned the wrong way?")

            'shading: all white means it never was shaded
            Dim allWhite = True
            For i = 0 To body.MyModel.polynum - 1
                For k = 0 To 3
                    If (body.MyModel.polyl(i).c(k).ReturnLong() And &HFFFFFF) <> &HFFFFFF Then allWhite = False : Exit For
                Next
                If Not allWhite Then Exit For
            Next
            If allWhite Then r.Warnings.Add("Body has no shading at all (see the Shade mode)")
        End If

        'wheels
        Dim present = Enumerable.Range(0, 4).Where(Function(i) t.wheel(i).IsPresent).ToArray()
        If present.Length > 0 Then
            If Not present.Any(Function(i) t.wheel(i).IsPowered) Then r.Errors.Add("No wheel is powered: the car won't move")
            If Not present.Any(Function(i) t.wheel(i).IsTurnable) Then r.Warnings.Add("No wheel can steer")
            For Each i In present
                If t.wheel(i).Radius <= 0 Then r.Errors.Add("Wheel " & i & " has no radius")
                If wheels(i) IsNot Nothing AndAlso t.wheel(i).Radius > 0 Then
                    Dim size = (wheels(i).BoundingBox.maxY - wheels(i).BoundingBox.MinY) / 2
                    If size > 0 AndAlso Abs(size - t.wheel(i).Radius) / size > 0.5 Then r.Warnings.Add("Wheel " & i & ": radius is " & Format(t.wheel(i).Radius, "0.0") & " but the model's is about " & Format(size, "0.0"))
                End If
            Next
            'left and right should match
            For Each pair In New Integer()() {New Integer() {0, 1}, New Integer() {2, 3}}
                If t.wheel(pair(0)).IsPresent And t.wheel(pair(1)).IsPresent Then
                    If Abs(Abs(t.wheel(pair(0)).Offset(1).X) - Abs(t.wheel(pair(1)).Offset(1).X)) > 1 Or Abs(t.wheel(pair(0)).Offset(1).Z - t.wheel(pair(1)).Offset(1).Z) > 1 Then r.Warnings.Add("Wheels " & pair(0) & " and " & pair(1) & " aren't symmetrical")
                End If
            Next
        End If

        'Dynamics
        If t.Body.Mass <= 0 Then r.Errors.Add("Body mass must be above 0")
        If t.RealInfos.TopSpeed <= 0 Then r.Errors.Add("TopSpeed must be above 0")
        If t.RealInfos.EngineRate <= 0 Then r.Warnings.Add("EngineRate is 0")
        If t.RealInfos.SteerRate <= 0 Then r.Warnings.Add("SteerRate is 0")
        For i = 0 To 2
            If i = 0 Then If t.Body.Inertia(0).X <= 0 Then r.Errors.Add("Inertia matrix: first value must be above 0")
            If i = 1 Then If t.Body.Inertia(1).Y <= 0 Then r.Errors.Add("Inertia matrix: middle value must be above 0")
            If i = 2 Then If t.Body.Inertia(2).Z <= 0 Then r.Errors.Add("Inertia matrix: last value must be above 0")
        Next
        If t.carAi Is Nothing Then r.Warnings.Add("No AI section: CPU cars can't use it well")

        'COM stuffs
        Dim com = t.RealInfos.COM
        r.Infos.Add("Center of Mass " & Format(com.X, "0.00") & " " & Format(com.Y, "0.00") & " " & Format(com.Z, "0.00"))
        r.Infos.Add("Balance: " & Format(Abs(com.X) / 40 * 100, "0.00") & "%")
        If com.Y > 0 Then r.Infos.Add("Rollability: " & Format(com.Y / 20 * 100, "0.00") & "%")
        If com.Y < 0 Then r.Infos.Add("Climbability: " & Format(Abs(com.Y) / 20 * 100, "0.00") & "%")
        If Abs(com.Y) > 25 Then r.Warnings.Add("Center of Mass's Y coordinate is wrong: " & com.Y)
        If Abs(com.X) > 40 Then r.Warnings.Add("Center of Mass is far to one side: " & com.X)

        'readme
        If Not IO.Directory.Exists(c.Path) OrElse IO.Directory.GetFiles(c.Path, "*read*me*").Length = 0 Then
            If Not IsStock(c.DirName) Then r.Warnings.Add("No readme (see the Readme mode)")
        End If

        CarBrowser.PolyCount = saved
        Return r
    End Function

    Private BodyHashes As Dictionary(Of String, String) = Nothing

    Private Function HashOf(ByVal file As String) As String
        Try
            Using md = Security.Cryptography.MD5.Create()
                Return Convert.ToBase64String(md.ComputeHash(IO.File.ReadAllBytes(file)))
            End Using
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>The stock car with exactly this body, or ""</summary>
    Public Function SameBodyAsStock(ByVal c As Car) As String
        If BodyHashes Is Nothing Then
            BodyHashes = New Dictionary(Of String, String)
            For Each dirPath In IO.Directory.GetDirectories(RVPATH & "\cars")
                Dim folder = IO.Path.GetFileName(dirPath)
                If Not IsStock(folder) Then Continue For
                Dim f = dirPath & "\body.prm"
                If Not IO.File.Exists(f) OrElse New IO.FileInfo(f).Length = 0 Then Continue For
                Dim h = HashOf(f)
                If h <> "" AndAlso Not BodyHashes.ContainsKey(h) Then BodyHashes(h) = folder
            Next
        End If
        If c.Theory.Body.modelNumber < 0 OrElse c.Theory.Body.modelNumber > 18 Then Return ""
        Dim own = Car.GamePath(c.Theory.MainInfos.Model(c.Theory.Body.modelNumber))
        If own Is Nothing OrElse New IO.FileInfo(own).Length = 0 Then Return ""
        Dim found As String = Nothing
        Return If(BodyHashes.TryGetValue(HashOf(own), found), found, "")
    End Function
End Module
