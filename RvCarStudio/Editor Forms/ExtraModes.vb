Imports OpenTK
Imports OpenTK.Graphics.OpenGL

'The edit modes that are lists of values: 8 Aerial, 9 AI, 10 Camera, 11 Collision,
'14 Shadow, 15 Misc, 16 Readme, 17 Packing
Module ExtraModes
    Private Pages As New Dictionary(Of Integer, FieldPanel)

    Private ReadOnly Property T() As Car_theory
        Get
            Return cars(Active_Car).Theory
        End Get
    End Property

    Public Function IsExtraMode(ByVal mode As Integer) As Boolean
        Return mode = 8 Or mode = 9 Or mode = 10 Or mode = 11 Or mode = 14 Or mode = 15 Or mode = 16 Or mode = 17
    End Function

    Public Sub ShowMode(ByVal mode As Integer, ByVal host As Control)
        If Not Pages.ContainsKey(mode) Then Pages(mode) = Build(mode)
        host.Controls.Add(Pages(mode))
        Pages(mode).BringToFront()
        Pages(mode).LoadValues()
    End Sub

    Public Sub HideAll(ByVal host As Control)
        For Each p In Pages.Values
            host.Controls.Remove(p)
        Next
    End Sub

    Private Function Build(ByVal mode As Integer) As FieldPanel
        Select Case mode
            Case 8 : Return BuildAerial()
            Case 9 : Return BuildAI()
            Case 10 : Return BuildCamera()
            Case 11 : Return BuildCollision()
            Case 14 : Return BuildShadow()
            Case 15 : Return BuildMisc()
            Case 16 : Return BuildReadme()
            Case Else : Return BuildPacking()
        End Select
    End Function

    Private Function V(ByVal x As Vector3) As Vector3d
        Return New Vector3d(x.X, x.Y, x.Z)
    End Function
    Private Function V(ByVal x As Vector3d) As Vector3
        Return New Vector3(CSng(x.X), CSng(x.Y), CSng(x.Z))
    End Function

    '------------------------ Aerial ----------------------------'
    Private Function BuildAerial() As FieldPanel
        Dim p As New FieldPanel
        p.AddHeader("Aerial (Y goes up here)")
        p.AddNumber("Section model", Function() T.Aerial.ModelNumber, Sub(x) T.Aerial.ModelNumber = CInt(x), -1, 18, 0, True)
        p.AddNumber("Top model", Function() T.Aerial.TopModelNumber, Sub(x) T.Aerial.TopModelNumber = CInt(x), -1, 18, 0, True)
        p.AddVector("Base", Function() V(T.Aerial.offset), Sub(x) T.Aerial.offset = V(x), True)
        p.AddVector("Direction", Function() V(T.Aerial.Direction), Sub(x) T.Aerial.Direction = V(x), True)
        p.AddNumber("Length", Function() T.Aerial.length, Sub(x) T.Aerial.length = x, 0, 10000, 2, True)
        p.AddNumber("Stiffness", Function() T.Aerial.stiffness, Sub(x) T.Aerial.stiffness = x, 0, 100000, 1)
        p.AddNumber("Damping", Function() T.Aerial.damping, Sub(x) T.Aerial.damping = x, 0, 1000, 2)
        p.AddInfo(Function() "Stock cars: stiffness about 2000, damping about 5." & vbNewLine & "0 damping makes it wiggle forever.", 2)
        p.AddButton("Restore aerial", Sub()
                                          T.Aerial = cars(Active_Car).OriginalTheory.Aerial.Clone
                                          CarBrowser.ReLoadOneCar()
                                          p.LoadValues()
                                      End Sub)
        Return p
    End Function

    '------------------------ AI ----------------------------'
    Private Function AiOf() As AI
        If T.carAi Is Nothing Then T.carAi = New AI
        Return T.carAi
    End Function
    Private Function BuildAI() As FieldPanel
        Dim p As New FieldPanel
        p.AddCheck("CPU cars may pick this car", Function() T.MainInfos.CPUSELECTABLE, Sub(x) T.MainInfos.CPUSELECTABLE = x)
        p.AddHeader("Understeer")
        p.AddNumber("Thresh", Function() AiOf().UnderThresh, Sub(x) AiOf().UnderThresh = x)
        p.AddNumber("Range", Function() AiOf().UnderRange, Sub(x) AiOf().UnderRange = x)
        p.AddNumber("Front", Function() AiOf().UnderFront, Sub(x) AiOf().UnderFront = x)
        p.AddNumber("Rear", Function() AiOf().UnderRear, Sub(x) AiOf().UnderRear = x)
        p.AddNumber("Max", Function() AiOf().UnderMax, Sub(x) AiOf().UnderMax = x)
        p.AddHeader("Oversteer")
        p.AddNumber("Thresh", Function() AiOf().OverThresh, Sub(x) AiOf().OverThresh = x)
        p.AddNumber("Range", Function() AiOf().OverRange, Sub(x) AiOf().OverRange = x)
        p.AddNumber("Max", Function() AiOf().OverMax, Sub(x) AiOf().OverMax = x)
        p.AddNumber("Acc thresh", Function() AiOf().OverAccThresh, Sub(x) AiOf().OverAccThresh = x)
        p.AddNumber("Acc range", Function() AiOf().OverAccRange, Sub(x) AiOf().OverAccRange = x)
        p.AddHeader("Behaviour (0 - 32767)")
        p.AddNumber("Pickup bias", Function() AiOf().PickupBias, Sub(x) AiOf().PickupBias = x, 0, 32767, 0)
        p.AddNumber("Block bias", Function() AiOf().BlockBias, Sub(x) AiOf().BlockBias = x, 0, 32767, 0)
        p.AddNumber("Overtake bias", Function() AiOf().OvertakeBias, Sub(x) AiOf().OvertakeBias = x, 0, 32767, 0)
        p.AddNumber("Suspension", Function() AiOf().Suspension, Sub(x) AiOf().Suspension = x, 0, 32767, 0)
        p.AddNumber("Aggression", Function() AiOf().Aggression, Sub(x) AiOf().Aggression = x, 0, 32767, 0)
        Return p
    End Function

    '------------------------ Camera ----------------------------'
    Private Function BuildCamera() As FieldPanel
        Dim p As New FieldPanel
        Dim cam = Function() If(T.Camera, New CamAttached)
        Dim edit = Function()
                       If T.Camera Is Nothing Then T.Camera = New CamAttached
                       Return T.Camera
                   End Function
        p.AddHeader("Hood and rear view (RVGL)")
        p.AddCheck("Use own cameras", Function() T.Camera IsNot Nothing, Sub(x) T.Camera = If(x, New CamAttached, Nothing))
        p.AddVector("Hood offset", Function() cam().HoodOffset, Sub(x) edit().HoodOffset = x)
        p.AddNumber("Hood look", Function() cam().HoodLook, Sub(x) edit().HoodLook = x, -0.25, 0.25, 3)
        p.AddVector("Rear offset", Function() cam().RearOffset, Sub(x) edit().RearOffset = x)
        p.AddNumber("Rear look", Function() cam().RearLook, Sub(x) edit().RearLook = x, -0.25, 0.25, 3)
        p.AddCheck("Fixed offset", Function() cam().FixedOffset, Sub(x) edit().FixedOffset = x)
        p.AddCheck("Fixed look", Function() cam().FixedLook, Sub(x) edit().FixedLook = x)
        p.AddInfo(Function() "Offsets are from the model center. Look goes" & vbNewLine & "from -0.25 to 0.25, 0 is straight ahead.", 2)
        Return p
    End Function

    '------------------------ Collision ----------------------------'
    Private Function BuildCollision() As FieldPanel
        Dim p As New FieldPanel
        p.AddHeader("Collision hull")
        p.AddPath("Hull file", Function() T.MainInfos.CollFile, Sub(x) T.MainInfos.CollFile = x, "Hull files (*.hul)|*.hul|All files|*.*")
        p.AddInfo(Function()
                      Dim f = Car.GamePath(T.MainInfos.CollFile)
                      If f Is Nothing Then Return "File not found: the car would have no hull!"
                      Return "Found, " & New IO.FileInfo(f).Length & " bytes."
                  End Function)
        p.AddInfo(Function() "The hull is made in a 3D editor, this program only" & vbNewLine & "links it. You can borrow the hull of a car of the" & vbNewLine & "same shape.", 3)
        p.AddHeader("Hardness (see Body mode for the rest)")
        p.AddNumber("Body hardness", Function() T.Body.Hardness, Sub(x) T.Body.Hardness = x, 0, 100, 3)
        Return p
    End Function

    '------------------------ Shadow ----------------------------'
    Private Function BuildShadow() As FieldPanel
        Dim p As New FieldPanel
        Dim names = New String() {"Left", "Right", "Front", "Back", "Height"}
        p.AddHeader("Shadow (RVGL)")
        p.AddPath("Texture", Function() T.MainInfos.TShadow, Sub(x) T.MainInfos.TShadow = x, "Images|*.bmp;*.png|All files|*.*")
        p.AddNumber("Stock index", Function() T.MainInfos.ShadowIndex, Sub(x)
                                                                           T.MainInfos.ShadowIndex = CInt(x)
                                                                           T.MainInfos.HasShadowIndex = True
                                                                       End Sub, -1, 27, 0)
        p.AddInfo(Function() "Index is a shadow of cars\misc\shadow.bmp, -1 is a" & vbNewLine & "rectangle. It's ignored if there's an own texture.", 2)
        p.AddHeader("Place, from the model center")
        For i = 0 To 4
            Dim k = i
            p.AddNumber(names(k), Function() T.MainInfos.ShadowTable(k), Sub(x)
                                                                             T.MainInfos.ShadowTable(k) = x
                                                                             T.MainInfos.HasShadowTable = True
                                                                         End Sub, -10000, 10000, 2)
        Next
        p.AddButton("Fit to the car", Sub()
                                          FitShadow()
                                          p.LoadValues()
                                      End Sub)
        p.AddInfo(Function() "All zeros: the game puts it under the body," & vbNewLine & "which won't cover wheels sticking out.", 2)
        Return p
    End Function

    'Shadow table around the body and the wheels
    Public Sub FitShadow()
        Dim minX = 0.0F, maxX = 0.0F, minZ = 0.0F, maxZ = 0.0F
        Dim m = cars(Active_Car).models
        If m.BODY IsNot Nothing Then
            minX = m.BODY.BoundingBox.MinX + T.Body.Offset.X
            maxX = m.BODY.BoundingBox.maxX + T.Body.Offset.X
            minZ = m.BODY.BoundingBox.MinZ + T.Body.Offset.Z
            maxZ = m.BODY.BoundingBox.maxZ + T.Body.Offset.Z
        End If
        For i = 0 To 3
            If m.Wheel(i) Is Nothing OrElse Not T.wheel(i).IsPresent Then Continue For
            Dim o = T.wheel(i).Offset(1)
            minX = Math.Min(minX, CSng(o.X) + m.Wheel(i).BoundingBox.MinX)
            maxX = Math.Max(maxX, CSng(o.X) + m.Wheel(i).BoundingBox.maxX)
            minZ = Math.Min(minZ, CSng(o.Z) + m.Wheel(i).BoundingBox.MinZ)
            maxZ = Math.Max(maxZ, CSng(o.Z) + m.Wheel(i).BoundingBox.maxZ)
        Next
        Dim height = If(T.MainInfos.HasShadowTable, T.MainInfos.ShadowTable(4), -5.0)
        T.MainInfos.ShadowTable = New Double() {minX, maxX, maxZ, minZ, height}
        T.MainInfos.HasShadowTable = True
    End Sub

    '------------------------ Misc ----------------------------'
    Private Function BuildMisc() As FieldPanel
        Dim p As New FieldPanel
        Dim wav = "Sounds (*.wav)|*.wav|All files|*.*"
        p.AddHeader("Sounds (RVGL)")
        p.AddPath("Engine", Function() T.MainInfos.SfxEngine, Sub(x) T.MainInfos.SfxEngine = x, wav)
        p.AddPath("Horn", Function() T.MainInfos.SfxHonk, Sub(x) T.MainInfos.SfxHonk = x, wav)
        p.AddPath("Servo", Function() T.MainInfos.SfxServo, Sub(x) T.MainInfos.SfxServo = x, wav)

        p.AddHeader("Weapons")
        p.AddVector("Shockwave", Function() V(T.RealInfos.WeaponGeneration), Sub(x) T.RealInfos.WeaponGeneration = V(x))
        p.AddVector("Water bomb", Function() V(T.RealInfos.WeaponGeneration2), Sub(x)
                                                                                   T.RealInfos.WeaponGeneration2 = V(x)
                                                                                   T.RealInfos.HasWeapon2 = True
                                                                               End Sub)
        p.AddHeader("Camber, degrees (RVGL)")
        For i = 0 To 3
            Dim k = i
            p.AddNumber("Wheel " & k, Function() T.wheel(k).Camber, Sub(x) T.wheel(k).Camber = x, -90, 90, 1, True)
        Next

        p.AddHeader("Spinner (RVGL)")
        p.AddNumber("Type 0 1 2 4 6", Function() T.Spinner.Type, Sub(x) T.Spinner.Type = CInt(x), 0, 6, 0)
        p.AddVector("Translation", Function() T.Spinner.Trans, Sub(x) T.Spinner.Trans = x)
        p.AddNumber("Trans. speed", Function() T.Spinner.TransVel, Sub(x) T.Spinner.TransVel = x, -100, 100, 4)

        p.AddHeader("Skins found")
        p.AddInfo(Function() String.Join(", ", FindSkins(cars(Active_Car))), 3)
        Return p
    End Function

    'Skins are detected by name: car.bmp gives caror.bmp, car1.bmp, car-bleh.bmp
    Public Function FindSkins(ByVal c As Car) As String()
        Dim tex = Car.GamePath(c.Theory.MainInfos.Tpage)
        If tex Is Nothing Then Return New String() {"(no texture)"}
        Dim stem = IO.Path.GetFileNameWithoutExtension(tex)
        Dim box = If(c.Theory.MainInfos.TCarBox = "", "", IO.Path.GetFileNameWithoutExtension(c.Theory.MainInfos.TCarBox))
        Dim found As New List(Of String)
        For Each f In IO.Directory.GetFiles(IO.Path.GetDirectoryName(tex), stem & "*.bmp")
            Dim n = IO.Path.GetFileNameWithoutExtension(f)
            If n.Length - stem.Length > 11 Then Continue For
            If box <> "" AndAlso n.StartsWith(box, StringComparison.OrdinalIgnoreCase) Then Continue For
            found.Add(n)
        Next
        Return found.ToArray()
    End Function

    '------------------------ Readme ----------------------------'
    Public Class ReadmeInfo
        Public Author As String = ""
        Public Email As String = ""
        Public Site As String = ""
        Public Comments As String = ""
        Public Description As String = ""
        Public Thanks As String = ""
        Public Tools As String = "RV Car Studio"
        Public Links As String = ""
        Public Copyright As String = ""
    End Class

    Public Function RatingName(ByVal r As Integer) As String
        Return Replace(DirectCast(r, CarBrowser.ratingo).ToString(), "_", "-")
    End Function

    Public Function FillReadme(ByVal template As String, ByVal c As Car, ByVal info As ReadmeInfo) As String
        Dim th = c.Theory
        Dim wheels = c.models.Wheel.Where(Function(w) w IsNot Nothing).Sum(Function(w) CInt(w.MyModel.polynum))
        Dim oneWheel = c.models.Wheel.Where(Function(w) w IsNot Nothing).Select(Function(w) CInt(w.MyModel.polynum)).DefaultIfEmpty(0).First()
        Dim body = If(c.models.BODY IsNot Nothing, CInt(c.models.BODY.MyModel.polynum), 0)
        Dim powered = Enumerable.Range(0, 4).Where(Function(i) th.wheel(i).IsPowered).ToArray()
        Dim drive = If(powered.Length = 4, "4WD", If(powered.Length = 0, "none", If(powered(0) < 2, "Front wheels", "Rear wheels")))

        Dim tags As New Dictionary(Of String, String) From {
            {"[CARNAME]", th.MainInfos.Name}, {"[AUTHOR]", info.Author}, {"[FOLDER]", c.DirName},
            {"[TYPE]", DescribeType(c)},
            {"[CLASS]", DirectCast(th.MainInfos.car_class, CarBrowser.classes).ToString()},
            {"[RATING]", RatingName(th.MainInfos.Rating)},
            {"[ENGINE]", drive},
            {"[TOPSPEED]", Format(th.RealInfos.TopSpeed, "0.0") & " mph (" & Format(th.RealInfos.TopSpeed * 1.609, "0.0") & " km/h)"},
            {"[POLYCOUNT]", CStr(body + wheels)}, {"[BODYPOLYCOUNT]", CStr(body)}, {"[WHEELSPOLYCOUNT]", CStr(wheels)}, {"[WHEELONEPOLYCOUNT]", CStr(oneWheel)},
            {"[EMAIL]", info.Email}, {"[WEBSITE]", info.Site}, {"[SITE]", info.Site},
            {"[COMMENTS]", info.Comments}, {"[DESCRIPTION]", info.Description}, {"[THANKS]", info.Thanks},
            {"[TOOLS]", info.Tools}, {"[LINKS]", info.Links}, {"[COPYRIGHT]", info.Copyright}, {"[DATE]", Now.ToString("yyyy-MM-dd")}
        }

        Dim out As New List(Of String)
        For Each line In template.Replace(vbCrLf, vbLf).Split(vbLf)
            'Framed lines: "+ text   +". The frame stays where it is, whatever the text
            Dim framed = line.Length > 4 AndAlso line.StartsWith("+ ") AndAlso line.TrimEnd().EndsWith("+") AndAlso line.Contains("[")
            If Not framed Then
                For Each kv In tags
                    line = line.Replace(kv.Key, kv.Value)
                Next
                out.Add(line)
                Continue For
            End If

            Dim trimmed = line.TrimEnd()
            Dim width = trimmed.Length - 4
            Dim inner = trimmed.Substring(2, width).Trim()
            Dim right = inner.Contains("%right%")
            Dim keep = inner.Contains("%keeplayout%")
            inner = inner.Replace("%right%", "").Replace("%keeplayout%", "").Trim()
            For Each kv In tags
                inner = inner.Replace(kv.Key, kv.Value)
            Next
            For Each part In Wrap(inner.Replace(vbCrLf, vbLf), width, keep)
                out.Add("+ " & If(right, part.PadLeft(width), part.PadRight(width)) & " +")
            Next
        Next
        Return String.Join(vbCrLf, out.ToArray())
    End Function

    Private Function Wrap(ByVal s As String, ByVal width As Integer, ByVal keepLines As Boolean) As List(Of String)
        Dim r As New List(Of String)
        For Each para In s.Split(vbLf)
            Dim p = para
            Do While p.Length > width
                Dim cut = p.LastIndexOf(" "c, width)
                If cut <= 0 Then cut = width
                r.Add(p.Substring(0, cut))
                p = p.Substring(cut).TrimStart()
            Loop
            r.Add(p)
        Next
        Return r
    End Function

    'Repaint or new model? Same body as a stock car means repaint
    Public Function DescribeType(ByVal c As Car) As String
        Dim stockBody = Check_My_Car.SameBodyAsStock(c)
        If stockBody = "" Then Return "Original"
        If stockBody.Equals(c.DirName, StringComparison.OrdinalIgnoreCase) Then Return "Stock"
        Return "Repaint of " & stockBody
    End Function

    Private Function BuildReadme() As FieldPanel
        Dim p As New FieldPanel
        Dim dir = Application.StartupPath & "\data\readme"
        Dim files = If(IO.Directory.Exists(dir), IO.Directory.GetFiles(dir, "*.txt").Select(Function(f) IO.Path.GetFileName(f)).ToArray(), New String() {})
        p.AddHeader("Readme generator")
        Dim tpl = p.AddChoice("Template", files)
        Dim author = p.AddTextBox("Author", Sett_get("author", ""))
        Dim email = p.AddTextBox("E-mail", Sett_get("email", ""))
        Dim site = p.AddTextBox("Site", Sett_get("site", ""))
        Dim thanks = p.AddTextBox("Thanks to", "")
        Dim comments = p.AddTextBox("Comments", "", 3)
        Dim make = Function()
                       Dim info As New ReadmeInfo With {.Author = author.Text, .Email = email.Text, .Site = site.Text, .Comments = comments.Text, .Description = comments.Text, .Thanks = thanks.Text, .Copyright = author.Text}
                       Return FillReadme(IO.File.ReadAllText(dir & "\" & tpl.Text, Singletons.FileEncoding), cars(Active_Car), info)
                   End Function
        p.AddButton("Preview", Sub()
                                   TextViewer.TextBox1.Text = make()
                                   TextViewer.Show()
                               End Sub)
        p.AddButton("Save readme.txt in the car's folder", Sub()
                                                               Dim file = cars(Active_Car).Path & "\readme.txt"
                                                               If IO.File.Exists(file) AndAlso MsgBox("Replace " & file & "?", MsgBoxStyle.YesNo Or MsgBoxStyle.Exclamation) <> MsgBoxResult.Yes Then Exit Sub
                                                               IO.File.WriteAllText(file, make(), Singletons.FileEncoding)
                                                               Sett_set("author", author.Text)
                                                               Sett_set("email", email.Text)
                                                               Sett_set("site", site.Text)
                                                           End Sub)
        Return p
    End Function

    '------------------------ Packing ----------------------------'
    'Files of other folders this car needs: not in its folder and not in cars\misc
    Public Function ExternalFiles(ByVal c As Car) As List(Of String)
        Dim r As New List(Of String)
        Dim own = "cars\" & c.DirName & "\"
        For Each path In c.Theory.MainInfos.Model.Concat(New String() {c.Theory.MainInfos.Tpage, c.Theory.MainInfos.CollFile, c.Theory.MainInfos.TCarBox, c.Theory.MainInfos.TShadow, c.Theory.MainInfos.SfxEngine, c.Theory.MainInfos.SfxHonk, c.Theory.MainInfos.SfxServo})
            If String.IsNullOrEmpty(path) OrElse path.Trim().ToUpperInvariant() = "NONE" Then Continue For
            Dim rel = path.Trim().Replace("/", "\")
            If rel.StartsWith(own, StringComparison.OrdinalIgnoreCase) Then Continue For
            If rel.StartsWith("cars\misc\", StringComparison.OrdinalIgnoreCase) Then Continue For
            If Not r.Contains(rel) Then r.Add(rel)
        Next
        Return r
    End Function

    ''' <summary>Zip of the car, ready to unzip with folder names on the main Re-Volt folder</summary>
    Public Sub PackCar(ByVal c As Car, ByVal zipPath As String, ByVal withExternal As Boolean)
        If IO.File.Exists(zipPath) Then IO.File.Delete(zipPath)
        Using zip = IO.Compression.ZipFile.Open(zipPath, IO.Compression.ZipArchiveMode.Create)
            For Each f In IO.Directory.GetFiles(c.Path, "*", IO.SearchOption.AllDirectories)
                Dim ext = IO.Path.GetExtension(f).ToLowerInvariant()
                If ext = ".bak" Or ext = ".tmp" Then Continue For
                Dim rel = f.Substring(c.Path.Length + 1).Replace("\", "/")
                IO.Compression.ZipFileExtensions.CreateEntryFromFile(zip, f, "cars/" & c.DirName & "/" & rel)
            Next
            If withExternal Then
                For Each rel In ExternalFiles(c)
                    Dim full = Car.GamePath(rel)
                    If full IsNot Nothing Then IO.Compression.ZipFileExtensions.CreateEntryFromFile(zip, full, rel.Replace("\", "/"))
                Next
            End If
        End Using
    End Sub

    Private Function BuildPacking() As FieldPanel
        Dim p As New FieldPanel
        Dim withExternal = True
        p.AddHeader("Pack the car for sharing")
        p.AddInfo(Function() cars(Active_Car).DirName & ": " & IO.Directory.GetFiles(cars(Active_Car).Path, "*", IO.SearchOption.AllDirectories).Length & " files")
        p.AddInfo(Function()
                      Dim ext = ExternalFiles(cars(Active_Car))
                      If ext.Count = 0 Then Return "Doesn't need files of other cars."
                      Return "Needs files of other folders:" & vbNewLine & String.Join(vbNewLine, ext.Take(5).ToArray())
                  End Function, 6)
        p.AddCheck("Include those files in the zip", Function() withExternal, Sub(x) withExternal = x)
        p.AddButton("Check the car first", Sub() Check_My_Car.CheckMyCar())
        p.AddButton("Save the zip...", Sub()
                                           Using d As New SaveFileDialog With {.Filter = "Zip files (*.zip)|*.zip", .FileName = cars(Active_Car).DirName & ".zip"}
                                               If d.ShowDialog() <> DialogResult.OK Then Exit Sub
                                               If cars(Active_Car).Sing.Modified Then cars(Active_Car).Sing.SaveToFile()
                                               PackCar(cars(Active_Car), d.FileName, withExternal)
                                               MsgBox("Packed: " & d.FileName, MsgBoxStyle.Information)
                                           End Using
                                       End Sub)
        Return p
    End Function
End Module
