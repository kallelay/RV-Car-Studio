Imports OpenTK
Imports OpenTK.Graphics

Public Module Global_
    'Stock (order random, order by Carbox index in frontend)
    Public Const Stock = "*tc6*moss*rc*mite*phat*moss*mud*beatall*Volken*dino*candy*gencar*pcxl*mouse*flag*tc2*r5*tc5*sgt*tc3*tc4*Adeon*fone*tc1*rotor*cougar*Toyeca*amw*wincar*wincar2*wincar3*cougar*panga*sugo*tc4*trolley*ufo*wincar4*q*"
    Public Const stock1 = "*rc*moss*volken*mite*mud*tc6*phat*beatall*dino*candy*mouse*r5*gencar*flag*tc5*tc4*tc2*sgt*tc3*tc1*sugo*adeon*rotor*toyeca*fone*cougar*amw*"

    'read poly count
    Function readPolys(ByVal file$) As Single
        If IO.File.Exists(file) = False Then Return Single.NaN
        Try
            Dim x = New IO.BinaryReader(New IO.FileStream(file, IO.FileMode.Open))
            Dim y = x.ReadInt16()
            x.Close()
            Return y

        Catch ex As Exception
            Return 0
        End Try

    End Function

    'is car stock?
    Function IsStock(ByVal carfolder$)
        Return InStr(Stock, "*" & carfolder & "*", CompareMethod.Text) > 0
    End Function
End Module

'Responsible for everything theory
'It's split between main, real, body, wheel, etc... and kept the same since Car::Load code
Public Class Car_theory
    Public MainInfos As New Main        'infos
    Public RealInfos As New Real_Inf    'backup
    Public ReadOnly Property Weight() 'get total weight? kay (Jan 18th 2014: erm? did I use this? O_o)
        Get
            Return Body.Mass + wheel(0).Mass + wheel(1).Mass + wheel(2).Mass + wheel(3).Mass
        End Get
    End Property
    'our stuffs
    Public Body As New Body
    Public wheel(3) As Wheel
    Public Spring(3) As Spring
    Public PIN(3) As PIN
    Public Axle(3) As Axle
    Public Spinner As Spinner
    Public Aerial As Aerial
    Public carAi As AI





    'clone car theory (for backup)
    Function Clone() As Car_theory
        Dim J As New Car_theory
        J.MainInfos = MainInfos.Clone
        J.Body = Body.Clone
        J.RealInfos = RealInfos.Clone
        For i = 0 To J.wheel.Count - 1 : J.wheel(i) = wheel(i).Clone : Next
        For i = 0 To J.Spring.Count - 1 : J.Spring(i) = Spring(i).clone : Next
        For i = 0 To J.PIN.Count - 1 : J.PIN(i) = PIN(i).clone : Next
        For i = 0 To J.Axle.Count - 1 : J.Axle(i) = Axle(i).clone : Next

        J.Spinner = Spinner.Clone
        J.Aerial = Aerial.Clone
        If J.carAi IsNot Nothing Then J.carAi = carAi.Clone
        Return J

    End Function
End Class
Public Class Main
    Public Name As String
    Public Model(19) As String
    Public Tpage As String
    Public CollFile As String
    Public EnvRGB As Color4
    Public TCarBox As String = "NONE"
    ' Public TSHADOW As String = 

    Public BESTTIME As Boolean
    Public CPUSELECTABLE As Boolean = True
    Public SELECTABLE As Boolean
    Public car_class As Integer
    Public obtain As Integer
    Public Rating As Integer

    Public TopEnd As Single
    Public Acceleration As Single
    Public Weight As Single
    Public Handling As Single

    Public Trans As Integer
    Public MaxRev As Single

    Public Statistics = False

    Function Clone() As Main
        Dim J As New Main()
        With J
            .Name = Replace(Name, ",", ".")

            .Model = Model
            .Tpage = Tpage
            .CollFile = CollFile
            .EnvRGB = EnvRGB
            .TCarBox = TCarBox

            .Model = Model.Clone

            .BESTTIME = BESTTIME
            .SELECTABLE = SELECTABLE
            .CPUSELECTABLE = CPUSELECTABLE
            .car_class = car_class
            .obtain = obtain
            .Rating = Rating

            .TopEnd = TopEnd
            .Acceleration = Acceleration
            .Weight = Weight
            .Handling = Handling
            .Trans = Trans
            .MaxRev = MaxRev


        End With

        Return J


    End Function
End Class

Public Class Real_Inf
    Public SteerRate As Double
    Public SteerMode As Double
    Public EngineRate As Double
    Public TopSpeed As Double
    Public DownForceModifier As Double
    Public COM As Vector3
    Public WeaponGeneration As Vector3
    Public ClothFX As Boolean = False
    Public Flying As Boolean = False
    Public Flippable As Boolean = False


    Function Clone() As Real_Inf
        Dim j As New Real_Inf
        With j
            .SteerRate = SteerRate
            .SteerMode = SteerMode
            .EngineRate = EngineRate
            .TopSpeed = TopSpeed
            .DownForceModifier = DownForceModifier
            .COM = COM
            .WeaponGeneration = WeaponGeneration
        End With
        Return j
    End Function
End Class

Public Class Body
    Public modelNumber As Integer
    Public Offset As Vector3
    Public Mass As Double
    Public Inertia(3) As Vector3
    '  Public Gravity As Double
    Public Hardness As Double
    Public Resistance As Double
    Public AngleRes As Double
    Public ResMode As Double
    Public Grip As Double
    Public StaticFriction As Double
    Public KineticFriction As Double


    Function Clone() As Body
        Dim j As New Body
        With j
            .modelNumber = modelNumber
            .Mass = Mass
            .Inertia(0) = Inertia(0)
            .Inertia(1) = Inertia(1)
            .Inertia(2) = Inertia(2)
            .Hardness = Hardness
            .Resistance = Resistance
            .AngleRes = AngleRes
            .ResMode = ResMode
            .Grip = Grip
            .StaticFriction = StaticFriction
            .KineticFriction = KineticFriction
        End With

        Return j


    End Function
End Class

Public Class Wheel
    Public modelNumber As Integer
    Public Offset(2) As Vector3d
    Public IsPresent As Boolean
    Public IsPowered As Boolean
    Public IsTurnable As Boolean
    Public SteerRatio As Double
    Public EngineRatio As Double
    Public Radius As Double
    Public Mass As Double
    Public Gravity As Double
    Public MaxPos As Double
    Public SkidWidth As Double
    Public ToeInn As Double
    Public AxleFriction As Double
    Public Grip As Double
    Public StaticFriction As Double
    Public KineticFriction As Double
    Function Clone() As Wheel
        Dim N As New Wheel
        With N
            .modelNumber = Me.modelNumber
            .Offset(1) = Me.Offset(1)
            .Offset(2) = Me.Offset(2)
            .IsPresent = Me.IsPresent
            .IsTurnable = Me.IsTurnable
            .IsPowered = Me.IsPowered
            .SteerRatio = Me.SteerRatio
            .EngineRatio = Me.EngineRatio
            .Radius = Me.Radius
            .Mass = Me.Mass
            .Gravity = Me.Gravity
            .MaxPos = Me.MaxPos
            .ToeInn = Me.ToeInn
            .Grip = Me.Grip
            .StaticFriction = Me.StaticFriction
            .KineticFriction = Me.KineticFriction



        End With
        Return N
    End Function
    Public ReadOnly Property m_x_g() As Vector3
        Get
            Return New Vector3(Offset(1).X * Mass, Offset(1).Y * Mass, Offset(1).Z * Mass)
        End Get
    End Property
End Class

Public Class Spring
    Public modelNumber As Integer
    Public Offset As Vector3d
    Public Length As Double
    Public Stiffness As Double
    Public Damping As Double
    Public Restitution As Double
    Function Clone()
        Dim N As New Spring
        N.modelNumber = Me.modelNumber
        N.Offset = Me.Offset
        N.Length = Me.Length
        N.Stiffness = Me.Stiffness
        N.Damping = Me.Damping
        N.Restitution = Me.Restitution
        Return N
    End Function
End Class
Public Class PIN
    Public modelNumber As Integer
    Public offSet As Vector3d
    Public Length As Double
    Function Clone()
        Dim N As New PIN
        N.modelNumber = Me.modelNumber
        N.offSet = Me.offSet
        N.Length = Me.Length
        Return N
    End Function
End Class
Public Class Axle
    Public modelNumber As Integer
    Public offSet As Vector3d
    Public Length As Double
    Function Clone()
        Dim N As New Axle
        N.modelNumber = Me.modelNumber
        N.offSet = Me.offSet
        N.Length = Length
        Return N
    End Function
End Class
Public Class Spinner
    Public modelNumber As Integer
    Public offSet As Vector3d
    Public Axis As Vector3d
    Public angVel As Double

    Function Clone() As Spinner
        Dim j As New Spinner
        With j
            .modelNumber = modelNumber
            .offSet = offSet
            .Axis = Axis
            .angVel = angVel
        End With

        Return j
    End Function
End Class
Public Class Aerial
    Public ModelNumber As Integer
    Public TopModelNumber As Integer
    Public offset As Vector3
    Public Direction As Vector3
    Public length As Double
    Public stiffness As Double
    Public damping As Double
    Function Clone() As Aerial
        Dim j As New Aerial
        With j
            .ModelNumber = ModelNumber
            .TopModelNumber = TopModelNumber
            .offset = offset
            .Direction = Direction
            .length = length
            .stiffness = stiffness
            .damping = damping
        End With
        Return j
    End Function
End Class

Public Class AI
    Public UnderThresh As Double
    Public UnderRange As Double
    Public UnderFront As Double
    Public UnderRear As Double
    Public UnderMax As Double
    Public OverThresh As Double
    Public OverRange As Double
    Public OverMax As Double
    Public OverAccThresh As Double
    Public OverAccRange As Double
    Public PickupBias As Double
    Public BlockBias As Double
    Public OvertakeBias As Double
    Public Suspension As Double
    Public Aggression As Double
    Public CPUSelectable As Boolean = True
    Function Clone() As AI
        Dim j As New AI
        With j
            .UnderFront = UnderFront
            .UnderMax = UnderMax
            .UnderRange = UnderRange
            .UnderRear = UnderRear
            .UnderThresh = UnderThresh

            .OverAccRange = OverAccRange
            .OverAccThresh = OverAccThresh
            .OverMax = OverMax
            .OverRange = OverRange
            .OvertakeBias = OvertakeBias
            .OverThresh = OverThresh

            .PickupBias = PickupBias
            .BlockBias = BlockBias
            .Suspension = Suspension
            .Aggression = Aggression
        End With

        Return j
    End Function

End Class


'Model set! for each car
Public Class MODELSET

    Public BODY As PRM
    Public Wheel(4) As PRM
    Public Spring(4) As PRM
    Public Pin(4) As PRM
    Public axle(4) As PRM
    Public Spinner As PRM
    Public Aerial As PRM

End Class
Public Class Car
    Public DirName As String = ""       'foldername
    Public Path As String                'path
    Public Theory As Car_theory         'theory
    Public OriginalTheory As Car_theory 'backup theory
    Public isLoading As Boolean = False 'being loading?
    Public models As New MODELSET       'models

    Sub New(ByVal Path_ As String) 'new
        Me.DirName = Split(Path_, "\").Last
        Path = Path_
    End Sub
    Public Sing As Singletons
    Public Sub Treat(ByRef str$) 'treat string
        'TODO: update this code
        Do Until str(0) <> vbTab And str(0) <> " "
            str = Mid(str, 2)
        Loop
    End Sub
    '------------------------parameters.txt--------------------
    Sub Load(Optional ByVal LoadFromCurrentSingleton = False)
        'load car's parameters.txt

        On Error Resume Next 'in case error, go next


        'THIS IS DARNED!!!! TOOK ME 6 HOURS TO LOAD THE CAR WHAT THE HACKING WORLD ??!!
        'Kay Jan'18th... yeah this was my comment 2-3 years ago -_-...

        If Not LoadFromCurrentSingleton Then 'load, simply 
            Sing = New Singletons(Path & "\parameters.txt")
        End If


        Dim Main = Sing.getSingleton("")
        Me.Theory = New Car_theory
        With Me.Theory.MainInfos

            .Name = Replace(Main.getValue("Name", Chr(0)), Chr(34), "")
            Treat(.Name)
            Console_.W("Loading Car:" & .Name)
            For t = 0 To 18

                .Model(t) = Replace(Main.getValue("MODEL " & vbTab & t), Chr(34), "")

                'incompetent car makers... goes here
                If .Model(t) = Nothing Then
                    .Model(t) = Main.getValue(" " & t)

                    'hotfix: KR car
                    If .Model(t) = Nothing Then .Model(t) = """NONE"""


                    Do Until .Model(t)(0) = Chr(34)
                        .Model(t) = Mid(.Model(t), 2)
                        If .Model(t) = "" Then .Model(t) = """NONE"""
                    Loop
                    .Model(t) = Replace(.Model(t), Chr(34), "")

                End If
            Next t

            .Tpage = Replace(Main.getValue("TPAGE"), Chr(34), "")
            If .Tpage.Length > 4 Then
                Do Until .Tpage(0) = "c" Or .Tpage(0) = "C"
                    .Tpage = Mid(.Tpage, 2)
                Loop
            End If


            .TCarBox = Replace(Main.getValue("TCARBOX"), Chr(34), "")


            If .TCarBox = Nothing Then GoTo skipme
            Do Until LCase(.TCarBox(0)) = "c"  ' Or .TCarBox(0) = "C"
                .TCarBox = Mid(.TCarBox, 2)
                If .TCarBox = Nothing Then Exit Do
            Loop

skipme:





            .CollFile = Replace(Main.getValue("COLL"), Chr(34), "")
            .EnvRGB = StrToColor(Replace(Main.getValue("EnvRGB"), Chr(34), ""))


            If IsStock(Me.DirName) Then
                .Statistics = True
            Else
                .Statistics = StrToBool(Main.getValue("Statistics", , "bool"), False)

            End If

            .BESTTIME = StrToBool(Main.getValue("BestTime", , "bool"))
            .SELECTABLE = StrToBool(Main.getValue("Selectable", , "bool"), False)
            .SELECTABLE = StrToBool(Main.getValue("CPUSelectable", , "bool"), True)



            .car_class = Int(Main.getValue("Class", 2))

            .obtain = Int(Main.getValue("Obtain", -1, "int"))


            .Rating = Main.getValue("Rating", 4)
            .TopEnd = Main.getValue("TopEnd", 0, "float")

            If InStr(.Name, "Acc") > 0 Then
                .Acceleration = Main.getValue("Acc ", 0)
            Else
                .Acceleration = Main.getValue("Acc", 0)
            End If

            .Weight = Main.getValue("Weight", 0)
            .Handling = Main.getValue("Handling", 0)
            .Trans = Main.getValue("Trans", 0, "int")
            .MaxRev = Main.getValue("MaxRevs")
        End With

        With Me.Theory.RealInfos

            .ClothFX = Main.getValue("clothFX", False, "bool")
            .Flippable = Main.getValue("Flippable", False, "bool")
            .Flying = Main.getValue("Flying", False, "bool")

            .SteerRate = Main.getValue("SteerRate")
            .SteerMode = Main.getValue("SteerMod")
            .EngineRate = Main.getValue("EngineRate")
            .TopSpeed = Main.getValue("TopSpeed")
            .DownForceModifier = Main.getValue("DownForceMod")

            .COM = StrToVector(Main.getValue("CoM")) ' * New Vector3d(1, -1, 1)
            .COM.Y *= -1

            .WeaponGeneration = StrToVector(Main.getValue("Weapon"))


        End With

        Dim body As SingletonItem = Sing.getSingleton("BODY")
        With Me.Theory.Body
            .modelNumber = body.getValue("ModelNum")
            .Offset = StrToVector(body.getValue("Offset"))
            .Mass = body.getValue("Mass")

            Dim _inertia$ = body.get3LinesValue("Inertia")
            .Inertia(0) = StrToVector(Split(_inertia, vbNewLine)(0))
            .Inertia(0).Y *= -1
            .Inertia(1) = StrToVector(Split(_inertia, vbNewLine)(1))
            .Inertia(1).Y *= -1
            .Inertia(2) = StrToVector(Split(_inertia, vbNewLine)(2))
            .Inertia(2).Y *= -1


            '.Inertia(1) = StrToVector(body.getValue(" " & vbTab))
            ' MsgBox(.Inertia(1).X)


            '.Gravity = body.getValue("Gravity		2200"))
            .Hardness = body.getValue("Hardness")
            .Resistance = body.getValue("Resistance")
            .AngleRes = body.getValue("AngRes")
            .ResMode = body.getValue("ResMod")
            .Grip = body.getValue("Grip")
            .StaticFriction = body.getValue("StaticFriction")
            .KineticFriction = body.getValue("KineticFriction")
        End With

        Dim Wheel As SingletonItem
        For u = 0 To 3
            Wheel = Sing.getSingleton("WHEEL " & u)
            Me.Theory.wheel(u) = New Wheel
            With Me.Theory.wheel(u)
                .modelNumber = Wheel.getValue("ModelNum")
                .Offset(1) = StrToVector(Wheel.getValue("Offset1"))
                .Offset(2) = StrToVector(Wheel.getValue("Offset2"))
                .IsPresent = StrToBool(Wheel.getValue("IsPresent", , "bool"))
                .IsPowered = StrToBool(Wheel.getValue("IsPowered", , "bool"))
                .IsTurnable = StrToBool(Wheel.getValue("IsTurnable", , "bool"))
                .SteerRatio = Wheel.getValue("SteerRatio")
                .EngineRatio = Wheel.getValue("EngineRatio")
                .Radius = Wheel.getValue("Radius")
                .Mass = Wheel.getValue("Mass")
                .Gravity = Wheel.getValue("Gravity")
                .MaxPos = Wheel.getValue("MaxPos")
                .SkidWidth = Wheel.getValue("SkidWidth")
                .ToeInn = Wheel.getValue("ToeIn")
                .AxleFriction = Wheel.getValue("AxleFriction")
                .Grip = Wheel.getValue("Grip")
                .StaticFriction = Wheel.getValue("StaticFriction")
                .KineticFriction = Wheel.getValue("KineticFriction")

            End With
        Next

        Dim Spring As SingletonItem
        For u = 0 To 3
            Spring = Sing.getSingleton("SPRING " & u)
            Me.Theory.Spring(u) = New Spring
            With Me.Theory.Spring(u)
                .modelNumber = Spring.getValue("ModelNum")
                .Offset = StrToVector(Spring.getValue("Offset"))
                .Length = Spring.getValue("Length")
                .Stiffness = Spring.getValue("Stiffness")
                .Damping = Spring.getValue("Damping")
                .Restitution = Spring.getValue("Restitution")
            End With
        Next

        Dim PIN As SingletonItem
        For u = 0 To 3
            PIN = Sing.getSingleton("PIN " & u)
            Me.Theory.PIN(u) = New PIN
            With Me.Theory.PIN(u)
                .modelNumber = PIN.getValue("ModelNum")
                .offSet = StrToVector(PIN.getValue("Offset"))
                .Length = PIN.getValue("Length")
            End With
        Next

        Dim Axle As SingletonItem
        For u = 0 To 3
            Axle = Sing.getSingleton("AXLE " & u)
            Me.Theory.Axle(u) = New Axle
            With Me.Theory.Axle(u)
                .modelNumber = Axle.getValue("ModelNum")
                .offSet = StrToVector(Axle.getValue("Offset"))
                .Length = Axle.getValue("Length")
            End With
        Next

        Dim Spinner = Sing.getSingleton("SPINNER")
        Me.Theory.Spinner = New Spinner
        With Me.Theory.Spinner
            .modelNumber = Spinner.getValue("ModelNum")
            .offSet = StrToVector(Spinner.getValue("Offset"))
            .Axis = StrToVector(Spinner.getValue("Axis"))
            .angVel = Spinner.getValue("AngVel")

        End With

        Dim Aerial = Sing.getSingleton("AERIAL")
        Me.Theory.Aerial = New Aerial
        With Me.Theory.Aerial
            .ModelNumber = Aerial.getValue("SecModelNum")
            .TopModelNumber = Aerial.getValue("TopModelNum")
            .offset = StrToVector(Aerial.getValue("Offset"))
            .Direction = StrToVector(Aerial.getValue("Direction"))
            .length = Aerial.getValue("Length")
            .stiffness = Aerial.getValue("Stiffness")
            .damping = Aerial.getValue("Damping")
        End With

        Dim Ai = Sing.getSingleton("AI")
        If Ai Is Nothing Then Exit Sub
        Me.Theory.carAi = New AI
        With Me.Theory.carAi
            .CPUSelectable = Ai.getValue("CPUSelectable")
            .UnderThresh = Ai.getValue("UnderThresh")
            .UnderRange = Ai.getValue("UnderRange")
            .UnderFront = Ai.getValue("UnderFront	")
            .UnderRear = Ai.getValue("UnderRear")
            .UnderMax = Ai.getValue("UnderMax")
            .OverThresh = Ai.getValue("OverThresh")
            .OverRange = Ai.getValue("OverRange")
            .OverMax = Ai.getValue("OverMax")
            .OverAccThresh = Ai.getValue("OverAccThresh")
            .OverAccRange = Ai.getValue("OverAccRange")
            .PickupBias = Ai.getValue("PickupBias")
            .BlockBias = Ai.getValue("BlockBias")
            .OvertakeBias = Ai.getValue("OvertakeBias")
            .Suspension = Ai.getValue("Suspension")
            .Aggression = Ai.getValue("Aggression")
        End With

        OriginalTheory = Theory.Clone

        'Debugger.Break()
        'MsgBox(Me.Theory.RealInfos.TopSpeed)
    End Sub

    'convert STRING to VECTOR
    Public Function StrToVector(ByVal str As String) As Vector3d
        'On Error Resume Next



        If InStr(str, ",") > 0 And InStr(CSng(0.5), ".") > 0 Then str = str.Replace(",", "")




        'Our vector is X Y Z

        str = Replace(Replace(str, Chr(9), " ", , , CompareMethod.Text), Space(2), Space(1), , , CompareMethod.Text)
        str = str.Replace("�", "")
        Do Until str(0) <> " " Or Len(str) = 0
            str = Mid(str, 2)
        Loop
        Dim j = 1
        Do Until j = str.Length - 1
            If str(j) = " " And str(j - 1) = "," Then
                str = str.Remove(j - 1, 1)
                j -= 1
            End If
            j += 1
        Loop





        'If str.Split(" ")(0).Split(".").Length > 2 Then str = Replace(str, str.Split(" ")(0), str.Split(" ")(0).Split(".")(0) & "." & str.Split(" ")(0).Split(".")(1))
        'If str.Split(" ")(1).Split(".").Length > 2 Then str = Replace(str, str.Split(" ")(1), str.Split(" ")(1).Split(".")(0) & "." & str.Split(" ")(1).Split(".")(1))
        'If str.Split(" ")(2).Split(".").Length > 2 Then str = Replace(str, str.Split(" ")(2), str.Split(" ")(2).Split(".")(0) & "." & str.Split(" ")(2).Split(".")(1))

        Dim X As Vector3
        Try

            X.X = CDbl(toDbl(str.Split(" ")(0))) ' 
            X.Y = -CDbl(toDbl(str.Split(" ")(1))) '
            X.Z = CDbl(toDbl(str.Split(" ")(2))) ' 

        Catch ex As Exception

        End Try

        Return X

    End Function

    'If a car has 1.2.3456 => 1.23456 
    Function MakeOnlyOneChar(ByVal str$, ByVal chr As Char) As String
        If InStr(str, chr) = 0 Then Return str

        Dim f = False
        For i = 0 To Len(str) - 1
            If i > Len(str) - 1 Then Exit For
            If str(i) = chr And f = False Then f = True : Continue For
            If str(i) = chr Then str = str.Substring(0, i - 1) & str.Substring(i + 1)


        Next
        Return str
    End Function
    'convert STRING to double
    Public Function toDbl(ByVal v$) As Double
        v = MakeOnlyOneChar(v, ".")
        Return Convert.ToDouble(v)
    End Function
    'convert STRING to color
    Public Function StrToColor(ByVal str As String) As Color4
        'On Error Resume Next



        If InStr(str, ",") > 0 And InStr(CSng(0.5), ".") > 0 Then str = str.Replace(",", "")




        'Our vector is X Y Z

        str = Replace(Replace(str, Chr(9), " ", , , CompareMethod.Text), Space(2), Space(1), , , CompareMethod.Text)
        str = str.Replace("�", "")
        Do Until str(0) <> " " Or Len(str) = 0
            str = Mid(str, 2)
        Loop
        Dim j = 1
        Do Until j = str.Length - 1
            If str(j) = " " And str(j - 1) = "," Then
                str = str.Remove(j - 1, 1)
                j -= 1
            End If
            j += 1
        Loop





        'If str.Split(" ")(0).Split(".").Length > 2 Then str = Replace(str, str.Split(" ")(0), str.Split(" ")(0).Split(".")(0) & "." & str.Split(" ")(0).Split(".")(1))
        'If str.Split(" ")(1).Split(".").Length > 2 Then str = Replace(str, str.Split(" ")(1), str.Split(" ")(1).Split(".")(0) & "." & str.Split(" ")(1).Split(".")(1))
        'If str.Split(" ")(2).Split(".").Length > 2 Then str = Replace(str, str.Split(" ")(2), str.Split(" ")(2).Split(".")(0) & "." & str.Split(" ")(2).Split(".")(1))

        Dim X As Color4
        Try

            X.R = CDbl(str.Split(" ")(0)) / 255 ' 
            X.G = CDbl(Double.Parse(str.Split(" ")(1))) / 255 '
            X.B = CDbl(Convert.ToDouble(str.Split(" ")(2))) / 255 ' 
            X.A = 1
        Catch ex As Exception

        End Try

        Return X

    End Function

    'convert STRING to boolean
    Function StrToBool(ByVal str As String, Optional ByVal defaultv As Boolean = False) As Boolean
        If str = Nothing Then Return defaultv
        Return CBool(Replace(Replace(str, " ", ""), vbTab, ""))
    End Function
    'convert STRING to String
    Function StrToStr(ByVal str As String)
        Return Replace(Split(str, Chr(34))(1), Chr(34), "")
    End Function

End Class