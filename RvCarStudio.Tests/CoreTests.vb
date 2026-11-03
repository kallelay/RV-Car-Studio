Imports CarStudio
Imports OpenTK
Imports System.IO.Compression

Module CoreTests
    Sub Run()
        Parser()
        For Each folder In IO.Directory.GetDirectories(RVPATH & "\cars")
            If IO.File.Exists(folder & "\parameters.txt") Then OneCar(folder)
        Next
        Features()
        For Each file In IO.Directory.GetFiles(RVPATH & "\cars", "*.*", IO.SearchOption.AllDirectories)
            Dim e = IO.Path.GetExtension(file).ToLowerInvariant()
            If e = ".prm" Or e = ".m" Then OneModel(file)
        Next
    End Sub

    'Nothing is written here, safe on the real game folder
    Sub ReadOnlyPass()
        Dim files = 0
        For Each folder In IO.Directory.GetDirectories(RVPATH & "\cars")
            Dim path = folder & "\parameters.txt"
            If Not IO.File.Exists(path) Then Continue For
            files += 1
            Try
                Dim bytes = IO.File.ReadAllBytes(path)
                Dim c As New Car(folder)
                c.Load()
                Check(Singletons.FileEncoding.GetBytes(c.Sing.SaveToStr()).SequenceEqual(bytes), IO.Path.GetFileName(folder) & ": unchanged save is byte identical")
                Dim r = Check_My_Car.Check(c)
                Log(IO.Path.GetFileName(folder) & ": score " & r.Score & ", " & r.Errors.Count & " errors, " & r.Warnings.Count & " warnings")
                For Each line In r.Errors
                    Log("    E " & line)
                Next
                For Each line In r.Warnings
                    Log("    W " & line)
                Next
            Catch ex As Exception
                Check(False, folder & ": " & ex.ToString())
            End Try
        Next
        Log(files & " cars")
    End Sub

    'Small cases real cars don't have
    Sub Parser()
        Dim t = "{" & vbCrLf & "Name ""Air AI (BODY)""" & vbCrLf & "Acc 1.5.2" & vbCrLf & ";)CPUSelectable FALSE" & vbCrLf &
                "BODY {" & vbCrLf & "ModelNum 3 ; {comment}" & vbCrLf & "Offset 1.0, -2.0, 3.0" & vbCrLf & "Inertia 1 2 3" & vbCrLf & "4 5 6" & vbCrLf & "7 8 9" & vbCrLf & "}" & vbCrLf &
                "}" & vbCrLf & "ABCDEF12"
        Dim s As New Singletons(t, True)
        Check(s.Text = t, "text kept")
        Dim m = s.getSingleton("")
        Check(m.getValue("Name") = "Air AI (BODY)", "name")
        Check(m.Num("Acc") = 1.52, "double dot number")
        Check(m.Bool("CPUSelectable", True) = False, ";) line read")
        Dim b = s.getSingleton("BODY")
        Check(b.Int("ModelNum") = 3, "block value, comment ignored")
        Check(b.Numbers("Offset", 3)(1) = -2, "commas in vector")
        Check(b.Numbers("Inertia#2", 3)(2) = 9, "continuation line")
        Check(s.getSingleton("AI") Is Nothing, "name doesn't make a section")
        Check(s.getSingleton("WHEEL 0") Is Nothing, "missing section")

        'LF only, no main brace
        Dim s2 As New Singletons("Name ""x""" & vbLf & "WHEEL 0 {" & vbLf & "Radius 2" & vbLf & "}" & vbLf, True)
        Check(s2.getSingleton("WHEEL 0").Num("Radius") = 2, "LF only file")
        Check(s2.Text = "Name ""x""" & vbLf & "WHEEL 0 {" & vbLf & "Radius 2" & vbLf & "}" & vbLf, "LF only kept")

        Check(CarBrowser.DetectAuthor("Name: x" & vbCrLf & "Author : Mr Foo" & vbCrLf) = "Mr Foo", "author")
        Check(CarBrowser.DetectAuthor("car made by Bar." & vbCrLf) = "Bar", "made by")
        Check(CarBrowser.DetectAuthor("") = "Unknown", "no author")
    End Sub

    'Checker on every car, new car, rename, zip, readme
    Sub Features()
        For Each folder In IO.Directory.GetDirectories(RVPATH & "\cars")
            If Not IO.File.Exists(folder & "\parameters.txt") Then Continue For
            Try
                Dim c As New Car(folder)
                c.Load()
                Dim r = Check_My_Car.Check(c)
                Log(IO.Path.GetFileName(folder) & ": score " & r.Score & ", " & r.Errors.Count & " errors, " & r.Warnings.Count & " warnings" & If(r.Errors.Count > 0, " | " & r.Errors(0), ""))
                Check(r.Infos.Count > 0, folder & ": checker says something")
            Catch ex As Exception
                Check(False, folder & ": checker crashed " & ex.ToString())
            End Try
        Next

        Dim empty As New Car(RVPATH & "\cars\wincar3")
        empty.Load()
        Check(Check_My_Car.Check(empty).Errors.Count >= 3, "empty car has errors")

        'new car from a template
        Dim fresh = RVPATH & "\cars\testnew"
        If IO.Directory.Exists(fresh) Then IO.Directory.Delete(fresh, True)
        CarBrowser.StartNewCar("adeon", "testnew", "My, New Car")
        Dim n As New Car(fresh)
        n.Load()
        Check(n.Theory.MainInfos.Name = "My, New Car", "new car name")
        Check(n.Theory.MainInfos.Model(0).Equals("cars\testnew\body.prm", StringComparison.OrdinalIgnoreCase), "new car model path " & n.Theory.MainInfos.Model(0))
        Check(n.Theory.MainInfos.Tpage.StartsWith("cars\testnew\", StringComparison.OrdinalIgnoreCase), "new car texture path")
        Check(Car.GamePath(n.Theory.MainInfos.Model(0)) IsNot Nothing, "new car model exists")
        Check(n.Theory.MainInfos.Model(17).Equals("cars\misc\Aerial.m", StringComparison.OrdinalIgnoreCase), "shared aerial stays")
        Check(Not IO.File.Exists(fresh & "\parameters.txt.bak"), "no backup in a new car")
        Dim r2 = Check_My_Car.Check(n)
        Check(r2.Errors.Count = 0, "new car checks fine: " & String.Join("; ", r2.Errors.ToArray()))
        Check(ExtraModes.DescribeType(n) = "Repaint of adeon", "new car seen as repaint: " & ExtraModes.DescribeType(n))
        Check(ExtraModes.ExternalFiles(n).Count = 0, "new car depends on nothing")

        'readme
        For Each tpl In IO.Directory.GetFiles(Application.StartupPath & "\data\readme", "*.txt")
            Dim info As New ExtraModes.ReadmeInfo With {.Author = "Tester", .Comments = "line one" & vbLf & "line two", .Email = "a@b.c"}
            Dim txt = ExtraModes.FillReadme(IO.File.ReadAllText(tpl, Singletons.FileEncoding), n, info)
            Check(txt.Contains("My, New Car") AndAlso txt.Contains("Tester"), "readme filled " & tpl)
            Check(Not System.Text.RegularExpressions.Regex.IsMatch(txt, "\[[A-Z]+\]"), "no tag left in " & tpl)
            Check(Not txt.Contains("%right%") AndAlso Not txt.Contains("%keeplayout%"), "no marker left in " & tpl)
        Next

        'zip
        Dim zip = IO.Path.GetTempFileName() & ".zip"
        ExtraModes.PackCar(n, zip, True)
        Using a = IO.Compression.ZipFile.OpenRead(zip)
            Check(a.Entries.Any(Function(e) e.FullName = "cars/testnew/parameters.txt"), "zip has parameters")
            Check(a.Entries.Any(Function(e) e.FullName = "cars/testnew/body.prm"), "zip has the body")
        End Using
        IO.File.Delete(zip)

        'rename
        Dim again = RVPATH & "\cars\testren"
        If IO.Directory.Exists(again) Then IO.Directory.Delete(again, True)
        _main.RenameCarFolder(n, "testren")
        Check(IO.Directory.Exists(again) And Not IO.Directory.Exists(fresh), "folder renamed")
        Dim n2 As New Car(again)
        n2.Load()
        Check(n2.Theory.MainInfos.Tpage.StartsWith("cars\testren\", StringComparison.OrdinalIgnoreCase), "renamed texture path")
        Check(n2.Theory.MainInfos.CollFile.StartsWith("cars\testren\", StringComparison.OrdinalIgnoreCase), "renamed hull path")
        Check(Car.GamePath(n2.Theory.MainInfos.Model(1)) IsNot Nothing, "renamed model found")
        IO.Directory.Delete(again, True)
    End Sub

    Sub OneCar(ByVal folder As String)
        Dim name = IO.Path.GetFileName(folder)
        Dim path = folder & "\parameters.txt"
        Dim bytes = IO.File.ReadAllBytes(path)
        Try
            'load and save, nothing changed
            Dim c As New Car(folder)
            c.Load()
            Check(Not c.Sing.Modified, name & ": fresh car isn't modified")
            Dim same = Singletons.FileEncoding.GetBytes(c.Sing.SaveToStr())
            Check(same.SequenceEqual(bytes), name & ": unchanged save is byte identical")

            If bytes.Length = 0 Then
                'an empty file has to give a full parameters file
                c.Theory.MainInfos.Name = "Made from nothing"
                c.Theory.Body.Mass = 3
                Dim made = New Singletons(c.Sing.SaveToStr(), True)
                Check(made.getSingleton("").getValue("Name") = "Made from nothing", name & ": name in new file")
                Check(made.getSingleton("BODY").Num("Mass") = 3, name & ": section in new file")
                Return
            End If

            'change some things
            Dim before = ParamEntry.Emit(c.Theory)
            c.Theory.RealInfos.TopSpeed = 123.5
            c.Theory.Body.Mass = 2.25
            c.Theory.wheel(2).Offset(1) = New Vector3d(1, 2, 3)
            c.Theory.Body.Inertia(1) = New Vector3(7, 8, 9)
            c.Theory.MainInfos.Model(5) = "cars\x\y.prm"
            c.Theory.MainInfos.TCarBox = "cars\x\box.bmp"
            If c.Theory.carAi IsNot Nothing Then c.Theory.carAi.Aggression = 5
            c.Theory.Camera = New CamAttached With {.HoodLook = 0.1}
            Check(c.Sing.Modified, name & ": changed car is modified")
            Dim text = c.Sing.SaveToStr()
            Check(Not c.Sing.Modified, name & ": not modified after save")

            'another reader sees the changes
            Dim r As New Car(folder)
            r.Sing = New Singletons(text, True)
            r.Load(True)
            Check(r.Theory.RealInfos.TopSpeed = 123.5, name & ": TopSpeed")
            Check(r.Theory.Body.Mass = 2.25, name & ": Mass")
            Check(r.Theory.wheel(2).Offset(1).Y = 2, name & ": wheel offset, Y sign")
            Check(r.Theory.Body.Inertia(1).Y = 8, name & ": inertia line")
            Check(r.Theory.MainInfos.Model(5) = "cars\x\y.prm", name & ": model path")
            Check(r.Theory.MainInfos.TCarBox = "cars\x\box.bmp", name & ": new value")
            Check(r.Theory.Camera IsNot Nothing AndAlso Math.Abs(r.Theory.Camera.HoodLook - 0.1) < 0.00001, name & ": new section")

            'and nothing else moved
            Dim after = ParamEntry.Emit(r.Theory)
            Dim diff = 0
            For Each e In before
                Dim f = after.FirstOrDefault(Function(x) x.Block = e.Block And x.Key = e.Key)
                If f Is Nothing OrElse f.Value <> e.Value Then diff += 1
            Next
            Check(diff <= 8, name & ": " & diff & " values differ, expected 8 at most")

            'lines we didn't change are all still there, in order
            Dim oldLines = Singletons.FileEncoding.GetString(bytes).Split(vbLf)
            Dim newLines = text.Split(vbLf)
            Dim kept = oldLines.Intersect(newLines).Count()
            Check(oldLines.Distinct().Count() - kept <= 8, name & ": " & (oldLines.Distinct().Count() - kept) & " lines lost")
            Dim tailOld = oldLines.Last()
            Check(tailOld = newLines.Last(), name & ": end of the file kept")

            'save to disk, the backup keeps the original
            c.Sing.SaveToFile()
            Check(IO.File.ReadAllBytes(path & ".bak").SequenceEqual(bytes), name & ": backup")
            Check(Singletons.FileEncoding.GetString(IO.File.ReadAllBytes(path)) = text, name & ": saved text")
        Catch ex As Exception
            Check(False, name & ": " & ex.ToString())
        Finally
            IO.File.WriteAllBytes(path, bytes)
            If IO.File.Exists(path & ".bak") Then IO.File.Delete(path & ".bak")
        End Try
    End Sub

    Sub OneModel(ByVal file As String)
        Dim rel = file.Substring(RVPATH.Length)
        Dim bytes As Byte()
        Try
            bytes = IO.File.ReadAllBytes(file)
        Catch ex As UnauthorizedAccessException
            Log("skip (no access) " & rel)
            Return
        End Try
        If bytes.Length = 0 Then Return
        Try
            Dim p As New PRM(file, True)
            Dim tmp = IO.Path.GetTempFileName()
            p.Export(tmp)
            Check(IO.File.ReadAllBytes(tmp).SequenceEqual(bytes), rel & ": model round trip")
            IO.File.Delete(tmp)
        Catch ex As Exception
            Check(False, rel & ": " & ex.Message)
        End Try
    End Sub
End Module
