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
            Using x = New IO.BinaryReader(New IO.FileStream(file, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                Return x.ReadInt16()
            End Using

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
    Public Camera As CamAttached





    'clone car theory (for backup)
    Function Clone() As Car_theory
        Dim J As New Car_theory
        J.MainInfos = MainInfos.Clone
        J.Body = Body.Clone
        J.RealInfos = RealInfos.Clone
        For i = 0 To J.wheel.Count - 1 : J.wheel(i) = wheel(i).Clone : Next
        For i = 0 To J.Spring.Count - 1 : J.Spring(i) = Spring(i).Clone : Next
        For i = 0 To J.PIN.Count - 1 : J.PIN(i) = PIN(i).Clone : Next
        For i = 0 To J.Axle.Count - 1 : J.Axle(i) = Axle(i).Clone : Next

        J.Spinner = Spinner.Clone
        J.Aerial = Aerial.Clone
        If carAi IsNot Nothing Then J.carAi = carAi.Clone
        If Camera IsNot Nothing Then J.Camera = Camera.Clone
        Return J

    End Function
End Class
Public Class Main
    Public Name As String = ""
    Public Model(19) As String
    Public Tpage As String = ""
    Public CollFile As String = ""
    Public EnvRGB As Color4
    Public TCarBox As String = ""
    Public TShadow As String = ""
    Public ShadowIndex As Integer = -1
    Public HasShadowIndex As Boolean = False
    Public ShadowTable(4) As Double
    Public HasShadowTable As Boolean = False
    Public SfxEngine As String = ""
    Public SfxHonk As String = ""
    Public SfxServo As String = ""

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

    Public Statistics As Boolean = False

    Function Clone() As Main
        Dim J = DirectCast(MemberwiseClone(), Main)
        J.Model = DirectCast(Model.Clone, String())
        J.ShadowTable = DirectCast(ShadowTable.Clone, Double())
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
    Public WeaponGeneration2 As Vector3
    Public HasWeapon2 As Boolean = False
    Public ClothFX As Boolean = False
    Public Flying As Boolean = False
    Public Flippable As Boolean = False

    Function Clone() As Real_Inf
        Return DirectCast(MemberwiseClone(), Real_Inf)
    End Function
End Class

Public Class Body
    Public modelNumber As Integer = -1
    Public Offset As Vector3
    Public Mass As Double
    Public Inertia(3) As Vector3
    Public Hardness As Double
    Public Resistance As Double
    Public AngleRes As Double
    Public ResMode As Double
    Public Grip As Double
    Public StaticFriction As Double
    Public KineticFriction As Double

    Function Clone() As Body
        Dim j = DirectCast(MemberwiseClone(), Body)
        j.Inertia = DirectCast(Inertia.Clone, Vector3())
        Return j
    End Function
End Class

Public Class Wheel
    Public modelNumber As Integer = -1
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
    Public Camber As Double

    Function Clone() As Wheel
        Dim N = DirectCast(MemberwiseClone(), Wheel)
        N.Offset = DirectCast(Offset.Clone, Vector3d())
        Return N
    End Function
    Public ReadOnly Property m_x_g() As Vector3
        Get
            Return New Vector3(Offset(1).X * Mass, Offset(1).Y * Mass, Offset(1).Z * Mass)
        End Get
    End Property
End Class

Public Class Spring
    Public modelNumber As Integer = -1
    Public Offset As Vector3d
    Public Length As Double
    Public Stiffness As Double
    Public Damping As Double
    Public Restitution As Double
    Function Clone() As Spring
        Return DirectCast(MemberwiseClone(), Spring)
    End Function
End Class
Public Class PIN
    Public modelNumber As Integer = -1
    Public offSet As Vector3d
    Public Length As Double
    Function Clone() As PIN
        Return DirectCast(MemberwiseClone(), PIN)
    End Function
End Class
Public Class Axle
    Public modelNumber As Integer = -1
    Public offSet As Vector3d
    Public Length As Double
    Function Clone() As Axle
        Return DirectCast(MemberwiseClone(), Axle)
    End Function
End Class
Public Class Spinner
    Public modelNumber As Integer = -1
    Public offSet As Vector3d
    Public Axis As Vector3d
    Public angVel As Double
    Public Type As Integer = 1
    Public Trans As Vector3d
    Public TransVel As Double

    Function Clone() As Spinner
        Return DirectCast(MemberwiseClone(), Spinner)
    End Function
End Class
Public Class Aerial
    Public ModelNumber As Integer = -1
    Public TopModelNumber As Integer = -1
    Public offset As Vector3
    Public Direction As Vector3
    Public length As Double
    Public stiffness As Double
    Public damping As Double
    Function Clone() As Aerial
        Return DirectCast(MemberwiseClone(), Aerial)
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
    Function Clone() As AI
        Return DirectCast(MemberwiseClone(), AI)
    End Function
End Class

'RVGL: hood and rear view cameras
Public Class CamAttached
    Public HoodOffset As Vector3d
    Public HoodLook As Double
    Public RearOffset As Vector3d
    Public RearLook As Double
    Public FixedOffset As Boolean = True
    Public FixedLook As Boolean = True
    Function Clone() As CamAttached
        Return DirectCast(MemberwiseClone(), CamAttached)
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

    'Vector of a file line. In memory Y points up, in the file it points down
    Private Shared Function Vec(ByVal item As SingletonItem, ByVal key As String) As Vector3d
        Dim n = item.Numbers(key, 3)
        Return New Vector3d(n(0), -n(1), n(2))
    End Function
    Public Shared Function VecNoFlip(ByVal item As SingletonItem, ByVal key As String) As Vector3
        Dim n = item.Numbers(key, 3)
        Return New Vector3(CSng(n(0)), CSng(n(1)), CSng(n(2)))
    End Function
    Private Shared Function ToV3(ByVal v As Vector3d) As Vector3
        Return New Vector3(CSng(v.X), CSng(v.Y), CSng(v.Z))
    End Function
    Private Function Section(ByVal header As String) As SingletonItem
        Dim it = Sing.getSingleton(header)
        Return If(it, SingletonItem.Empty)
    End Function

    '------------------------parameters.txt--------------------
    Sub Load(Optional ByVal LoadFromCurrentSingleton = False)
        'load car's parameters.txt
        If Not LoadFromCurrentSingleton Then 'load, simply
            Sing = New Singletons(Path & "\parameters.txt")
        End If
        Sing.Owner = Me

        Dim Main = Section("")
        Me.Theory = New Car_theory
        With Me.Theory.MainInfos
            .Name = Main.getValue("Name", "")
            Console_.W("Loading Car:" & .Name)
            For t = 0 To 18
                .Model(t) = Main.getValue("MODEL " & t, "NONE")
            Next t

            .Tpage = Main.getValue("TPAGE", "")
            .CollFile = Main.getValue("COLL", "")
            Dim rgb = Main.Numbers("EnvRGB", 3)
            .EnvRGB = New Color4(CSng(rgb(0) / 255), CSng(rgb(1) / 255), CSng(rgb(2) / 255), 1)

            .TCarBox = Main.getValue("TCARBOX", "")
            .TShadow = Main.getValue("TSHADOW", "")
            .HasShadowIndex = Main.Has("SHADOWINDEX")
            .ShadowIndex = Main.Int("SHADOWINDEX", -1)
            .HasShadowTable = Main.Has("SHADOWTABLE")
            .ShadowTable = Main.Numbers("SHADOWTABLE", 5)
            .SfxEngine = Main.getValue("SFXENGINE", "")
            .SfxHonk = Main.getValue("SFXHONK", "")
            .SfxServo = Main.getValue("SFXSERVO", "")

            .Statistics = Main.Bool("Statistics", IsStock(Me.DirName))
            .BESTTIME = Main.Bool("BestTime")
            .SELECTABLE = Main.Bool("Selectable")
            .CPUSELECTABLE = Main.Bool("CPUSelectable", True)

            .car_class = Main.Int("Class", 2)
            .obtain = Main.Int("Obtain", -1)
            .Rating = Main.Int("Rating", 4)
            .TopEnd = CSng(Main.Num("TopEnd"))
            .Acceleration = CSng(Main.Num("Acc"))
            .Weight = CSng(Main.Num("Weight"))
            .Handling = CSng(Main.Num("Handling"))
            .Trans = Main.Int("Trans")
            .MaxRev = CSng(Main.Num("MaxRevs"))
        End With

        With Me.Theory.RealInfos
            .ClothFX = Main.Bool("ClothFx")
            .Flippable = Main.Bool("Flippable")
            .Flying = Main.Bool("Flying")

            .SteerRate = Main.Num("SteerRate")
            .SteerMode = Main.Num("SteerMod")
            .EngineRate = Main.Num("EngineRate")
            .TopSpeed = Main.Num("TopSpeed")
            .DownForceModifier = Main.Num("DownForceMod")

            .COM = VecNoFlip(Main, "CoM")
            .WeaponGeneration = ToV3(Vec(Main, If(Main.Has("WeaponOffset1"), "WeaponOffset1", "Weapon")))
            .HasWeapon2 = Main.Has("WeaponOffset2")
            .WeaponGeneration2 = ToV3(Vec(Main, "WeaponOffset2"))
        End With

        Dim body = Section("BODY")
        With Me.Theory.Body
            .modelNumber = body.Int("ModelNum", -1)
            .Offset = ToV3(Vec(body, "Offset"))
            .Mass = body.Num("Mass")
            .Inertia(0) = VecNoFlip(body, "Inertia")
            .Inertia(1) = VecNoFlip(body, "Inertia#1")
            .Inertia(2) = VecNoFlip(body, "Inertia#2")
            .Hardness = body.Num("Hardness")
            .Resistance = body.Num("Resistance")
            .AngleRes = body.Num("AngRes")
            .ResMode = body.Num("ResMod")
            .Grip = body.Num("Grip")
            .StaticFriction = body.Num("StaticFriction")
            .KineticFriction = body.Num("KineticFriction")
        End With

        For u = 0 To 3
            Dim Wheel = Section("WHEEL " & u)
            Me.Theory.wheel(u) = New Wheel
            With Me.Theory.wheel(u)
                .modelNumber = Wheel.Int("ModelNum", -1)
                .Offset(1) = Vec(Wheel, "Offset1")
                .Offset(2) = Vec(Wheel, "Offset2")
                .IsPresent = Wheel.Bool("IsPresent")
                .IsPowered = Wheel.Bool("IsPowered")
                .IsTurnable = Wheel.Bool("IsTurnable")
                .SteerRatio = Wheel.Num("SteerRatio")
                .EngineRatio = Wheel.Num("EngineRatio")
                .Radius = Wheel.Num("Radius")
                .Mass = Wheel.Num("Mass")
                .Gravity = Wheel.Num("Gravity")
                .MaxPos = Wheel.Num("MaxPos")
                .SkidWidth = Wheel.Num("SkidWidth")
                .ToeInn = Wheel.Num("ToeIn")
                .AxleFriction = Wheel.Num("AxleFriction")
                .Grip = Wheel.Num("Grip")
                .StaticFriction = Wheel.Num("StaticFriction")
                .KineticFriction = Wheel.Num("KineticFriction")
                .Camber = Wheel.Num("Camber")
            End With
        Next

        For u = 0 To 3
            Dim Spring = Section("SPRING " & u)
            Me.Theory.Spring(u) = New Spring
            With Me.Theory.Spring(u)
                .modelNumber = Spring.Int("ModelNum", -1)
                .Offset = Vec(Spring, "Offset")
                .Length = Spring.Num("Length")
                .Stiffness = Spring.Num("Stiffness")
                .Damping = Spring.Num("Damping")
                .Restitution = Spring.Num("Restitution")
            End With
        Next

        For u = 0 To 3
            Dim PIN = Section("PIN " & u)
            Me.Theory.PIN(u) = New PIN
            With Me.Theory.PIN(u)
                .modelNumber = PIN.Int("ModelNum", -1)
                .offSet = Vec(PIN, "Offset")
                .Length = PIN.Num("Length")
            End With
        Next

        For u = 0 To 3
            Dim Axle = Section("AXLE " & u)
            Me.Theory.Axle(u) = New Axle
            With Me.Theory.Axle(u)
                .modelNumber = Axle.Int("ModelNum", -1)
                .offSet = Vec(Axle, "Offset")
                .Length = Axle.Num("Length")
            End With
        Next

        Dim Spinner = Section("SPINNER")
        Me.Theory.Spinner = New Spinner
        With Me.Theory.Spinner
            .modelNumber = Spinner.Int("ModelNum", -1)
            .offSet = Vec(Spinner, "Offset")
            .Axis = Vec(Spinner, "Axis")
            .angVel = Spinner.Num("AngVel")
            .Type = Spinner.Int("Type", 1)
            .Trans = Vec(Spinner, "Trans")
            .TransVel = Spinner.Num("TransVel")
        End With

        Dim Aerial = Section("AERIAL")
        Me.Theory.Aerial = New Aerial
        With Me.Theory.Aerial
            .ModelNumber = Aerial.Int("SecModelNum", -1)
            .TopModelNumber = Aerial.Int("TopModelNum", -1)
            .offset = ToV3(Vec(Aerial, "Offset"))
            .Direction = ToV3(Vec(Aerial, "Direction"))
            .length = Aerial.Num("Length")
            .stiffness = Aerial.Num("Stiffness")
            .damping = Aerial.Num("Damping")
        End With

        Dim Ai = Sing.getSingleton("AI")
        If Ai IsNot Nothing Then
            Me.Theory.carAi = New AI
            With Me.Theory.carAi
                .UnderThresh = Ai.Num("UnderThresh")
                .UnderRange = Ai.Num("UnderRange")
                .UnderFront = Ai.Num("UnderFront")
                .UnderRear = Ai.Num("UnderRear")
                .UnderMax = Ai.Num("UnderMax")
                .OverThresh = Ai.Num("OverThresh")
                .OverRange = Ai.Num("OverRange")
                .OverMax = Ai.Num("OverMax")
                .OverAccThresh = Ai.Num("OverAccThresh")
                .OverAccRange = Ai.Num("OverAccRange")
                .PickupBias = Ai.Num("PickupBias")
                .BlockBias = Ai.Num("BlockBias")
                .OvertakeBias = Ai.Num("OvertakeBias")
                .Suspension = Ai.Num("Suspension")
                .Aggression = Ai.Num("Aggression")
            End With
        End If

        Dim Cam = Sing.getSingleton("CAMATTACHED")
        If Cam IsNot Nothing Then
            Me.Theory.Camera = New CamAttached
            With Me.Theory.Camera
                .HoodOffset = Vec(Cam, "HoodOffset")
                .HoodLook = Cam.Num("HoodLook")
                .RearOffset = Vec(Cam, "RearOffset")
                .RearLook = Cam.Num("RearLook")
                .FixedOffset = Cam.Bool("FixedOffset", True)
                .FixedLook = Cam.Bool("FixedLook", True)
            End With
        End If

        Sing.Rebase(Me.Theory)
        OriginalTheory = Theory.Clone
    End Sub

    'Full path of a path written in parameters.txt, Nothing if there's no such file
    Public Shared Function GamePath(ByVal relative As String) As String
        If String.IsNullOrEmpty(relative) OrElse relative.Trim().ToUpperInvariant() = "NONE" Then Return Nothing
        Dim full = RVPATH & "\" & relative.Trim().Replace("/", "\")
        Return If(IO.File.Exists(full), full, Nothing)
    End Function
End Class
