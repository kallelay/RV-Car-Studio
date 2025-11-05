Imports OpenTK
''' <summary>
''' Initialize additional models (PRMs) for helping the user pick the correct positionning of CoM, Wheels etc
''' </summary>
''' <remarks>
''' Code init by Kallel Ahmed Yahia
''' GNU General Public License version 3
''' </remarks>


Module Axis_Grid_Etc
    Public AXIS, COM, WG, SPL(3) As PRM ' New PRM("data\models\axis.prm")

    '------------------------ Init ----------------------------'

    'inits all the car components
    Sub AllInit()
        initAxis()
        InitCOM()
        InitWG()
        initSplitter()
    End Sub

    'Init "splitters" model used
    Sub InitSplitter()
        For i = 0 To 3
            SPL(i) = New PRM(Application.StartupPath & "\data\models\splitter.prm", True, True)
            SPL(i).isVisible = False
        Next

    End Sub

    'Center of Mass model
    Sub InitCOM()
        COM = New PRM(Application.StartupPath & "\data\models\COM.prm", True, True)
        COM.isVisible = False
    End Sub

    'WG
    Sub InitWG()
        WG = New PRM(Application.StartupPath & "\data\models\weagen.prm", True, True)
        WG.isVisible = False
    End Sub

    'Axis
    Sub initAxis()
        AXIS = New PRM(Application.StartupPath & "\data\models\axis.prm", True, True)

        AXIS.isVisible = False
    End Sub

    '------------------------ Show ----------------------------'
    Sub ShowAxis()
        AXIS.isVisible = True
    End Sub
    Sub ShowWG()
        WG.isVisible = True
    End Sub
    Sub HideWG()
        If Not Initializd Then Exit Sub
        WG.isVisible = False
    End Sub

    '------------------------ Hide ----------------------------'
    Sub HideAxis()
        If Not Initializd Then Exit Sub
        AXIS.isVisible = False
    End Sub

    Sub ShowCOM()
        COM.isVisible = True
    End Sub
    Sub HideCOM()
        If Not Initializd Then Exit Sub
        COM.isVisible = False
    End Sub

    '------------------------ Update Positions ----------------------------'
    Sub UpdateAxis(ByVal Follower As PRM)

        AXIS.Position = Follower.Position
        AXIS.Rotation = Follower.Rotation
    End Sub
    Sub UpdateSplitter(ByVal index%, ByVal Follower As PRM, ByVal Offset As Vector3)
        If Follower Is Nothing Then SPL(index).Position = New Vector3 : Exit Sub
        SPL(index).Position = Follower.Position + Offset
    End Sub

    Sub UpdateCOM(ByVal vec As Vector3)
        'vec = Vector3.Multiply(vec, New Vector3(1, -1, 1))
        COM.Position = vec * Zoom
    End Sub
    Sub UpdateWG(ByVal vec As Vector3)
        ' vec = Vector3.Multiply(vec, New Vector3(1, -1, 1))
        WG.Position = vec * Zoom
    End Sub

End Module

