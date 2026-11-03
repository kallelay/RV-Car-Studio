Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions

'parameters.txt reader and writer.
'
'The file is kept as text, line by line. Saving only touches the lines whose value was changed
'in the editor, so comments, ;) RVGL extensions, sections we don't know about and the checksum
'at the end of stock files all stay exactly as they were.
Public Class Singletons

    'Latin-1 maps every byte to one char, so car names and stock checksums survive a round trip
    Public Shared ReadOnly FileEncoding As Encoding = Encoding.GetEncoding(28591)

    Private Lines As New List(Of String)
    Private Eols As New List(Of String)
    Private MainItem As SingletonItem
    Private Blocks As New Dictionary(Of String, SingletonItem)(StringComparer.OrdinalIgnoreCase)
    Private MainCloseLine As Integer = -1

    'The car this file belongs to, and its values as they are in Lines
    Public Owner As Car
    Private BaseEntries As List(Of ParamEntry)

    Sub New(ByVal FilePath As String)
        Dim txt = ""
        If IO.File.Exists(FilePath) Then
            txt = IO.File.ReadAllText(FilePath, FileEncoding)
        Else
            Console_.W("Error:" & FilePath & " couldn't be found")
        End If
        SetText(txt)
    End Sub
    Sub New(ByVal Str As String, ByVal FLAG_OF_STREAM As Boolean)
        SetText(Str)
    End Sub

    '------------------------parse--------------------
    Private Sub SetText(ByVal txt As String)
        Lines.Clear()
        Eols.Clear()
        Dim st = 0
        For i = 0 To txt.Length - 1
            If txt(i) = vbLf Then
                Dim hasCr = i > st AndAlso txt(i - 1) = vbCr
                Lines.Add(txt.Substring(st, i - st - If(hasCr, 1, 0)))
                Eols.Add(If(hasCr, vbCrLf, vbLf))
                st = i + 1
            End If
        Next
        If st < txt.Length Then
            Lines.Add(txt.Substring(st))
            Eols.Add("")
        End If
        Parse()
    End Sub

    Private Shared ReadOnly ModelLine As New Regex("^(?:MODEL\s*)?(\d+)\s*(""[^""]*""?.*)$", RegexOptions.IgnoreCase)
    Private Shared ReadOnly KeyLine As New Regex("^([A-Za-z_][A-Za-z0-9_]*)\s*(.*)$")

    Private Sub Parse()
        MainItem = New SingletonItem("")
        Blocks.Clear()
        MainCloseLine = -1

        Dim depth = 0
        Dim cur = MainItem
        Dim last As ParamLine = Nothing
        Dim cont = 0

        For i = 0 To Lines.Count - 1
            Dim ext = False
            Dim codeStart = 0
            Dim code = CodeOf(Lines(i), ext, codeStart)
            Dim trimmed = code.Trim()
            If trimmed = "" Then Continue For

            Dim brace = trimmed.IndexOf("{"c)
            If brace >= 0 AndAlso trimmed(0) <> """"c Then
                Dim hdr = Regex.Replace(trimmed.Substring(0, brace).Trim(), "\s+", " ")
                If hdr = "" AndAlso depth = 0 Then
                    depth = 1
                    Continue For
                End If
                'header on the line before the brace
                If hdr = "" AndAlso last IsNot Nothing AndAlso last.Value = "" Then
                    hdr = last.Key
                    cur.Items.Remove(last)
                End If
                If depth = 0 Then depth = 1
                cur = New SingletonItem(hdr)
                cur.OpenLine = i
                Blocks(hdr) = cur
                depth = 2
                last = Nothing
                Continue For
            End If

            If trimmed(0) = "}"c Then
                depth -= 1
                If depth = 1 Then cur.CloseLine = i
                cur = MainItem
                last = Nothing
                If depth <= 0 Then
                    MainCloseLine = i
                    Exit For
                End If
                Continue For
            End If

            If depth = 0 Then depth = 1

            Dim item As New ParamLine With {.Line = i, .Ext = ext}
            Dim mm = If(cur Is MainItem, ModelLine.Match(trimmed), Match.Empty)
            If mm.Success Then
                item.Key = "MODEL " & Integer.Parse(mm.Groups(1).Value)
                SetValueRange(item, Lines(i), codeStart, code, mm.Groups(2).Value)
            ElseIf IsNumberStart(trimmed) AndAlso last IsNot Nothing Then
                'continuation of the value above (Inertia takes 3 lines)
                cont += 1
                item.Key = BaseKey(last.Key) & "#" & cont
                SetValueRange(item, Lines(i), codeStart, code, trimmed)
                cur.Items.Add(item)
                last = item
                Continue For
            Else
                Dim km = KeyLine.Match(trimmed)
                If Not km.Success Then Continue For
                item.Key = km.Groups(1).Value
                SetValueRange(item, Lines(i), codeStart, code, km.Groups(2).Value)
            End If
            cont = 0
            cur.Items.Add(item)
            last = item
        Next

        MainItem.OpenLine = -1
    End Sub

    Private Shared Function BaseKey(ByVal k As String) As String
        Dim p = k.IndexOf("#"c)
        Return If(p < 0, k, k.Substring(0, p))
    End Function

    Private Shared Function IsNumberStart(ByVal s As String) As Boolean
        Return Char.IsDigit(s(0)) OrElse ((s(0) = "-"c OrElse s(0) = "."c OrElse s(0) = "+"c) AndAlso s.Length > 1 AndAlso (Char.IsDigit(s(1)) OrElse s(1) = "."c))
    End Function

    Private Shared Sub SetValueRange(ByVal item As ParamLine, ByVal line As String, ByVal codeStart As Integer, ByVal code As String, ByVal value As String)
        value = value.Trim()
        item.Value = value
        Dim codeEnd = codeStart + code.TrimEnd().Length
        item.ValueEnd = codeEnd
        item.ValueStart = codeEnd - value.Length
    End Sub

    'The part of a line that isn't a comment. ";)" at the start is RVGL's marker for lines old versions skip
    Private Shared Function CodeOf(ByVal line As String, ByRef ext As Boolean, ByRef codeStart As Integer) As String
        Dim i = 0
        Do While i < line.Length AndAlso (line(i) = " "c OrElse line(i) = vbTab)
            i += 1
        Loop
        codeStart = 0
        If i + 1 < line.Length AndAlso line(i) = ";"c AndAlso line(i + 1) = ")"c Then
            ext = True
            codeStart = i + 2
        End If
        Dim inQuote = False
        For j = codeStart To line.Length - 1
            If line(j) = """"c Then inQuote = Not inQuote
            If line(j) = ";"c AndAlso Not inQuote Then Return line.Substring(codeStart, j - codeStart)
        Next
        Return line.Substring(codeStart)
    End Function

    '------------------------read--------------------
    ''' <summary>The section with this header ("" = top level), or Nothing if the file doesn't have it</summary>
    Public Function getSingleton(ByVal header As String) As SingletonItem
        If header Is Nothing OrElse header.Trim() = "" Then Return MainItem
        Dim b As SingletonItem = Nothing
        If Blocks.TryGetValue(header.Trim(), b) Then Return b
        Return Nothing
    End Function

    Public Function getAllSingletons() As String()
        Return Blocks.Keys.ToArray()
    End Function

    '------------------------write--------------------
    ''' <summary>Tell the file its text now matches this car</summary>
    Public Sub Rebase(ByVal theory As Car_theory)
        BaseEntries = ParamEntry.Emit(theory)
    End Sub

    Public ReadOnly Property Text() As String
        Get
            Dim sb As New StringBuilder
            For i = 0 To Lines.Count - 1
                sb.Append(Lines(i)).Append(Eols(i))
            Next
            Return sb.ToString()
        End Get
    End Property

    ''' <summary>Is the car different from the file?</summary>
    Public ReadOnly Property Modified() As Boolean
        Get
            If Owner Is Nothing OrElse BaseEntries Is Nothing Then Return False
            Dim now = ParamEntry.Emit(Owner.Theory)
            If now.Count <> BaseEntries.Count Then Return True
            For i = 0 To now.Count - 1
                If now(i).Value <> BaseEntries(i).Value Then Return True
            Next
            Return False
        End Get
    End Property

    Private Function Find(ByVal block As String, ByVal key As String) As ParamLine
        Dim it = getSingleton(block)
        Return If(it Is Nothing, Nothing, it.Find(key))
    End Function

    'Write the car's changes into the text
    Private Sub ApplyChanges()
        If Owner Is Nothing OrElse BaseEntries Is Nothing OrElse Not Modified Then Exit Sub
        Dim now = ParamEntry.Emit(Owner.Theory)
        Dim was As New Dictionary(Of String, String)

        'an empty file gets everything, otherwise only what changed
        If Lines.Count = 0 Then
            Lines.Add("{") : Eols.Add(vbCrLf)
            Lines.Add("}") : Eols.Add(vbCrLf)
            Parse()
        Else
            For Each e In BaseEntries
                was(e.Block & "|" & e.Key) = e.Value
            Next
        End If

        For Each e In now
            Dim old As String = Nothing
            If was.TryGetValue(e.Block & "|" & e.Key, old) AndAlso old = e.Value Then Continue For
            'a new optional value that was never set: nothing to write
            If old Is Nothing AndAlso e.Value = "" Then Continue For

            Dim p = Find(e.Block, e.Key)
            If p Is Nothing AndAlso e.Key = "Weapon" Then p = Find(e.Block, "WeaponOffset1")
            If p IsNot Nothing Then
                Dim l = Lines(p.Line)
                Dim value = e.Value
                If p.ValueEnd = p.ValueStart AndAlso p.ValueEnd > 0 AndAlso l(p.ValueEnd - 1) <> " "c AndAlso l(p.ValueEnd - 1) <> vbTab Then value = vbTab & value
                Lines(p.Line) = l.Substring(0, p.ValueStart) & value & l.Substring(p.ValueEnd)
            Else
                InsertEntry(e)
            End If
            Parse()
        Next
    End Sub

    Private Sub InsertEntry(ByVal e As ParamEntry)
        Dim it = getSingleton(e.Block)
        Dim at As Integer
        If it Is Nothing Then
            'new section, before the closing brace of the file
            at = If(MainCloseLine >= 0, MainCloseLine, Lines.Count)
            Lines.Insert(at, e.Block & " {") : Eols.Insert(at, vbCrLf)
            Lines.Insert(at + 1, "}") : Eols.Insert(at + 1, vbCrLf)
            Parse()
            it = getSingleton(e.Block)
        End If
        If it.Items.Count > 0 Then
            at = it.Items.Last.Line + 1
        ElseIf it.CloseLine >= 0 Then
            at = it.CloseLine
        ElseIf MainCloseLine >= 0 Then
            at = MainCloseLine
        Else
            at = Lines.Count
        End If
        'make sure the line above ends
        If at > 0 AndAlso at <= Eols.Count AndAlso Eols(at - 1) = "" Then Eols(at - 1) = vbCrLf
        Dim key = e.Key
        If key.StartsWith("MODEL ") Then key = "MODEL " & vbTab & key.Substring(6)
        If key.Contains("#") Then key = ""
        Lines.Insert(at, If(e.Ext, ";)", "") & key & vbTab & e.Value)
        Eols.Insert(at, vbCrLf)
    End Sub

    Public Function SaveToStr() As String
        ApplyChanges()
        If Owner IsNot Nothing Then Rebase(Owner.Theory)
        Return Text
    End Function

    Public Sub SaveToFile()
        SaveToFile(Owner.Path & "\parameters.txt")
    End Sub

    Public Sub SaveToFile(ByVal path As String)
        Dim content = SaveToStr()
        path = Replace(path, "\\", "\")

        'keep the first version of a car, once
        If IO.File.Exists(path) AndAlso Not IO.File.Exists(path & ".bak") Then IO.File.Copy(path, path & ".bak")

        'a crash in the middle of a save must not cost the car its parameters
        Dim tmp = path & ".tmp"
        IO.File.WriteAllText(tmp, content, FileEncoding)
        If IO.File.Exists(path) Then IO.File.Delete(path)
        IO.File.Move(tmp, path)
    End Sub
End Class

'One "Key  value" line
Public Class ParamLine
    Public Key As String
    Public Value As String = ""
    Public Line As Integer
    Public ValueStart As Integer
    Public ValueEnd As Integer
    Public Ext As Boolean
End Class

'One section of the file
Public Class SingletonItem
    Public Header As String
    Public Items As New List(Of ParamLine)
    Public OpenLine As Integer = -1
    Public CloseLine As Integer = -1

    Sub New(ByVal hdr As String)
        Header = hdr
    End Sub

    'An item that holds nothing: everything falls back to its default
    Public Shared ReadOnly Empty As New SingletonItem("")

    'The game reads top to bottom, so when a key is there twice the last one counts
    Public Function Find(ByVal key As String) As ParamLine
        For i = Items.Count - 1 To 0 Step -1
            If String.Equals(Items(i).Key, key, StringComparison.OrdinalIgnoreCase) Then Return Items(i)
        Next
        Return Nothing
    End Function

    Public Function Has(ByVal key As String) As Boolean
        Return Find(key) IsNot Nothing
    End Function

    'Text value, without quotes
    Public Function getValue(ByVal key As String, Optional ByVal defaultValue As String = Nothing) As String
        Dim p = Find(key)
        If p Is Nothing Then Return defaultValue
        Dim v = p.Value
        If v.StartsWith("""") Then
            Dim e = v.IndexOf(""""c, 1)
            v = If(e > 0, v.Substring(1, e - 1), v.Substring(1))
        End If
        Return v
    End Function

    Public Function Num(ByVal key As String, Optional ByVal defaultValue As Double = 0) As Double
        Dim p = Find(key)
        If p Is Nothing Then Return defaultValue
        Return ToNumber(p.Value, defaultValue)
    End Function

    Public Function Int(ByVal key As String, Optional ByVal defaultValue As Integer = 0) As Integer
        Dim p = Find(key)
        If p Is Nothing Then Return defaultValue
        Return CInt(Math.Truncate(Math.Max(Math.Min(ToNumber(p.Value, defaultValue), Integer.MaxValue), Integer.MinValue)))
    End Function

    Public Function Bool(ByVal key As String, Optional ByVal defaultValue As Boolean = False) As Boolean
        Dim p = Find(key)
        If p Is Nothing Then Return defaultValue
        Select Case p.Value.Trim().ToUpperInvariant()
            Case "TRUE", "1", "ON", "YES" : Return True
            Case "FALSE", "0", "OFF", "NO" : Return False
        End Select
        Return defaultValue
    End Function

    'Numbers of a vector line, separated by spaces, tabs or commas
    Public Function Numbers(ByVal key As String, ByVal count As Integer) As Double()
        Dim r(count - 1) As Double
        Dim p = Find(key)
        If p Is Nothing Then Return r
        Dim parts = Regex.Split(p.Value.Trim(), "[\s,]+")
        For i = 0 To Math.Min(count, parts.Length) - 1
            r(i) = ToNumber(parts(i), 0)
        Next
        Return r
    End Function

    'Car makers write 1.2.3456, 1.0f and worse
    Public Shared Function ToNumber(ByVal s As String, ByVal defaultValue As Double) As Double
        Dim sb As New StringBuilder
        Dim dot = False
        For Each c In s.Trim()
            If Char.IsDigit(c) OrElse c = "-"c OrElse c = "+"c OrElse c = "e"c OrElse c = "E"c Then
                sb.Append(c)
            ElseIf c = "."c AndAlso Not dot Then
                sb.Append(c)
                dot = True
            ElseIf c = " "c OrElse c = vbTab Then
                If sb.Length > 0 Then Exit For
            End If
        Next
        Dim d As Double
        If Double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, d) Then Return d
        Return defaultValue
    End Function
End Class

'One value of the car as it goes into the file
Public Class ParamEntry
    Public Block As String
    Public Key As String
    Public Value As String
    Public Ext As Boolean

    Sub New(ByVal b As String, ByVal k As String, ByVal v As String, Optional ByVal extension As Boolean = False)
        Block = b : Key = k : Value = v : Ext = extension
    End Sub

    Private Shared Function F(ByVal d As Double) As String
        Return d.ToString("0.000000", CultureInfo.InvariantCulture)
    End Function
    Private Shared Function V3(ByVal x As Double, ByVal y As Double, ByVal z As Double) As String
        Return F(x) & " " & F(y) & " " & F(z)
    End Function
    Private Shared Function B(ByVal v As Boolean) As String
        Return If(v, "TRUE", "FALSE")
    End Function
    Private Shared Function Q(ByVal s As String) As String
        Return """" & s & """"
    End Function

    'Every value of the car. Y is flipped in memory, except for CoM and Inertia
    Public Shared Function Emit(ByVal t As Car_theory) As List(Of ParamEntry)
        Dim r As New List(Of ParamEntry)
        Dim add = Sub(blk As String, k As String, v As String) r.Add(New ParamEntry(blk, k, v))
        Dim ext = Sub(blk As String, k As String, v As String) r.Add(New ParamEntry(blk, k, v, True))

        With t.MainInfos
            add("", "Name", Q(.Name))
            For i = 0 To 18
                add("", "MODEL " & i, Q(.Model(i)))
            Next
            add("", "TPAGE", Q(.Tpage))
            add("", "COLL", Q(.CollFile))
            add("", "EnvRGB", CInt(.EnvRGB.R * 255) & " " & CInt(.EnvRGB.G * 255) & " " & CInt(.EnvRGB.B * 255))
            ext("", "TCARBOX", If(.TCarBox = "", "", Q(.TCarBox)))
            ext("", "TSHADOW", If(.TShadow = "", "", Q(.TShadow)))
            ext("", "SHADOWINDEX", If(.HasShadowIndex, CStr(.ShadowIndex), ""))
            ext("", "SHADOWTABLE", If(.HasShadowTable, String.Join(" ", .ShadowTable.Select(Function(d) d.ToString("0.000", CultureInfo.InvariantCulture)).ToArray()), ""))
            ext("", "SFXENGINE", If(.SfxEngine = "", "", Q(.SfxEngine)))
            ext("", "SFXHONK", If(.SfxHonk = "", "", Q(.SfxHonk)))
            ext("", "SFXSERVO", If(.SfxServo = "", "", Q(.SfxServo)))
            add("", "BestTime", B(.BESTTIME))
            add("", "Selectable", B(.SELECTABLE))
            ext("", "CPUSelectable", B(.CPUSELECTABLE))
            ext("", "Statistics", B(.Statistics))
            add("", "Class", CStr(.car_class))
            add("", "Obtain", CStr(.obtain))
            add("", "Rating", CStr(.Rating))
            add("", "TopEnd", F(.TopEnd))
            add("", "Acc", F(.Acceleration))
            add("", "Weight", F(.Weight))
            add("", "Handling", F(.Handling))
            add("", "Trans", CStr(.Trans))
            add("", "MaxRevs", F(.MaxRev))
        End With

        With t.RealInfos
            add("", "SteerRate", F(.SteerRate))
            add("", "SteerMod", F(.SteerMode))
            add("", "EngineRate", F(.EngineRate))
            add("", "TopSpeed", F(.TopSpeed))
            add("", "DownForceMod", F(.DownForceModifier))
            add("", "CoM", V3(.COM.X, .COM.Y, .COM.Z))
            add("", "Weapon", V3(.WeaponGeneration.X, -.WeaponGeneration.Y, .WeaponGeneration.Z))
            ext("", "WeaponOffset2", If(.HasWeapon2, V3(.WeaponGeneration2.X, -.WeaponGeneration2.Y, .WeaponGeneration2.Z), ""))
            ext("", "Flippable", B(.Flippable))
            ext("", "Flying", B(.Flying))
            ext("", "ClothFx", B(.ClothFX))
        End With

        With t.Body
            add("BODY", "ModelNum", CStr(.modelNumber))
            add("BODY", "Offset", V3(.Offset.X, -.Offset.Y, .Offset.Z))
            add("BODY", "Mass", F(.Mass))
            add("BODY", "Inertia", V3(.Inertia(0).X, .Inertia(0).Y, .Inertia(0).Z))
            add("BODY", "Inertia#1", V3(.Inertia(1).X, .Inertia(1).Y, .Inertia(1).Z))
            add("BODY", "Inertia#2", V3(.Inertia(2).X, .Inertia(2).Y, .Inertia(2).Z))
            add("BODY", "Gravity", "2200")
            add("BODY", "Hardness", F(.Hardness))
            add("BODY", "Resistance", F(.Resistance))
            add("BODY", "AngRes", F(.AngleRes))
            add("BODY", "ResMod", F(.ResMode))
            add("BODY", "Grip", F(.Grip))
            add("BODY", "StaticFriction", F(.StaticFriction))
            add("BODY", "KineticFriction", F(.KineticFriction))
        End With

        For i = 0 To 3
            Dim s = "WHEEL " & i
            With t.wheel(i)
                add(s, "ModelNum", CStr(.modelNumber))
                add(s, "Offset1", V3(.Offset(1).X, -.Offset(1).Y, .Offset(1).Z))
                add(s, "Offset2", V3(.Offset(2).X, -.Offset(2).Y, .Offset(2).Z))
                add(s, "IsPresent", B(.IsPresent))
                add(s, "IsPowered", B(.IsPowered))
                add(s, "IsTurnable", B(.IsTurnable))
                add(s, "SteerRatio", F(.SteerRatio))
                add(s, "EngineRatio", F(.EngineRatio))
                add(s, "Radius", F(.Radius))
                add(s, "Mass", F(.Mass))
                add(s, "Gravity", "2200")
                add(s, "MaxPos", F(.MaxPos))
                add(s, "SkidWidth", F(.SkidWidth))
                add(s, "ToeIn", F(.ToeInn))
                add(s, "AxleFriction", F(.AxleFriction))
                add(s, "Grip", F(.Grip))
                add(s, "StaticFriction", F(.StaticFriction))
                add(s, "KineticFriction", F(.KineticFriction))
                ext(s, "Camber", F(.Camber))
            End With
        Next

        For i = 0 To 3
            Dim s = "SPRING " & i
            With t.Spring(i)
                add(s, "ModelNum", CStr(.modelNumber))
                add(s, "Offset", V3(.Offset.X, -.Offset.Y, .Offset.Z))
                add(s, "Length", F(.Length))
                add(s, "Stiffness", F(.Stiffness))
                add(s, "Damping", F(.Damping))
                add(s, "Restitution", F(.Restitution))
            End With
        Next

        For i = 0 To 3
            Dim s = "PIN " & i
            With t.PIN(i)
                add(s, "ModelNum", CStr(.modelNumber))
                add(s, "Offset", V3(.offSet.X, -.offSet.Y, .offSet.Z))
                add(s, "Length", F(.Length))
            End With
        Next

        For i = 0 To 3
            Dim s = "AXLE " & i
            With t.Axle(i)
                add(s, "ModelNum", CStr(.modelNumber))
                add(s, "Offset", V3(.offSet.X, -.offSet.Y, .offSet.Z))
                add(s, "Length", F(.Length))
            End With
        Next

        With t.Spinner
            add("SPINNER", "ModelNum", CStr(.modelNumber))
            add("SPINNER", "Offset", V3(.offSet.X, -.offSet.Y, .offSet.Z))
            add("SPINNER", "Axis", V3(.Axis.X, -.Axis.Y, .Axis.Z))
            add("SPINNER", "AngVel", F(.angVel))
            ext("SPINNER", "Type", CStr(.Type))
            ext("SPINNER", "Trans", V3(.Trans.X, -.Trans.Y, .Trans.Z))
            ext("SPINNER", "TransVel", F(.TransVel))
        End With

        With t.Aerial
            add("AERIAL", "SecModelNum", CStr(.ModelNumber))
            add("AERIAL", "TopModelNum", CStr(.TopModelNumber))
            add("AERIAL", "Offset", V3(.offset.X, -.offset.Y, .offset.Z))
            add("AERIAL", "Direction", V3(.Direction.X, -.Direction.Y, .Direction.Z))
            add("AERIAL", "Length", F(.length))
            add("AERIAL", "Stiffness", F(.stiffness))
            add("AERIAL", "Damping", F(.damping))
        End With

        If t.carAi IsNot Nothing Then
            With t.carAi
                add("AI", "UnderThresh", F(.UnderThresh))
                add("AI", "UnderRange", F(.UnderRange))
                add("AI", "UnderFront", F(.UnderFront))
                add("AI", "UnderRear", F(.UnderRear))
                add("AI", "UnderMax", F(.UnderMax))
                add("AI", "OverThresh", F(.OverThresh))
                add("AI", "OverRange", F(.OverRange))
                add("AI", "OverMax", F(.OverMax))
                add("AI", "OverAccThresh", F(.OverAccThresh))
                add("AI", "OverAccRange", F(.OverAccRange))
                add("AI", "PickupBias", F(.PickupBias))
                add("AI", "BlockBias", F(.BlockBias))
                add("AI", "OvertakeBias", F(.OvertakeBias))
                add("AI", "Suspension", F(.Suspension))
                add("AI", "Aggression", F(.Aggression))
            End With
        End If

        If t.Camera IsNot Nothing Then
            With t.Camera
                add("CAMATTACHED", "HoodOffset", V3(.HoodOffset.X, -.HoodOffset.Y, .HoodOffset.Z))
                add("CAMATTACHED", "HoodLook", F(.HoodLook))
                add("CAMATTACHED", "RearOffset", V3(.RearOffset.X, -.RearOffset.Y, .RearOffset.Z))
                add("CAMATTACHED", "RearLook", F(.RearLook))
                add("CAMATTACHED", "FixedOffset", If(.FixedOffset, "true", "false"))
                add("CAMATTACHED", "FixedLook", If(.FixedLook, "true", "false"))
            End With
        End If

        Return r
    End Function
End Class
