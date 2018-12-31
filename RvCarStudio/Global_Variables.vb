Module Global_Variables
    ''' <summary> 
    ''' Headers and application
    ''' </summary>
    ''' <remarks></remarks>
    ''' Versions
    '''  Revision is Auto Incrimented (OnBuild)
    Public Const MajorVersion = "2"
    Public Const MinorVersion = "0"
    Public Const BuildNumber = "0"

    Public Const Revision ="634"
Public Const Type As VerType = VerType.preAlpha


    '' Applications info
    Public Const APPNAME = "RV car Studio"
    Public Const InnerNAME = "RVCarStudio"
    Public Const Maker = "Kallel A.Y"
    Public Const Description = APPNAME & " by " & Maker
    Public Const ACTIVE_YEARS = "2010-2014"
    Public Const COPY = "Copyright © " & APPNAME & " (" & ACTIVE_YEARS & "). Based on Car Load, Shader, PRM, Re-Volt source code..."

    Public Const FULL_INFO = APPNAME & " (" & MajorVersion & "." & MinorVersion & ") by " & Maker & "." & vbNewLine & _
                            "All rights reserved © " & ACTIVE_YEARS & vbNewLine & _
                            "Licensed under GNU GPL." & vbNewLine & _
                            vbNewLine & _
                            "This program uses VoltGL rendering engine based on OpenTK" & vbNewLine & _
                            "VoltGL's full source code is included in " & APPNAME & ". All rights reserved to its maker Kallel A.Y" & vbNewLine & _
                            "VoltGL uses OpenTK: Copyright (c) 2006 - 2012 the Open Toolkit library." & vbNewLine & _
                            vbNewLine & _
                            "This programs uses Car::Load's source code all ported to VoltGL/RvCarStudio. Copyright C::L 2009-2012" & vbNewLine & _
                            vbNewLine & _
                            "The Tips provided by Burner94, Citywalker, Halogaland and MythicMonkey all belong to their owners." & vbNewLine & _
                            "AI Help is mainly provided by CityWalker." & vbNewLine & _
                            "RvCarStudio uses RVL people's suggestions. Refer to Car Load's topic for more info." & vbNewLine & _
                            vbNewLine & _
                            "Re-Volt is a trademark of Acclaim and IP by We Go Interactive. All rights reserved WeGOi 2013" & vbNewLine & _
                            "We Go Interactive IS NOT " & Maker & "." & Maker & " IS NOT We GO Interactive. " & vbNewLine & _
                            "This Program is made by Re-Volt fans." & vbNewLine & _
                            ""


    'picking prm, bmp
    Public PickingPRM As Boolean = False
    Public PickingBMP As Boolean = False
    Public Active_Car = 0


    'Anti-Alias?
    Public SAMPLE_ACCEPTED% = 8
    Enum VerType
        preAlpha = -1
        alpha = 0
        beta = 1
        gamma = 2
        RC = 3
        release = 4
    End Enum

    ''' <summary>
    ''' Applications Variable
    ''' </summary>
    ''' <remarks></remarks>
    ''' RVpath
    Public RVPATH As String
    ''' <summary>
    ''' Polygon types
    ''' </summary>
    ''' <remarks></remarks>
    Enum PolyType
        QUAD = 1
        DOUBLE_SIDED = 2
        SEMI_TRANS = 4
        ADDITIVE_TRANS = 128
        'SEMI_TRANS_ONE = 256
        TEXANIM = 512
        NOENV = 1024
        ENV = 2048

    End Enum

    Public Enum PRMFLAG
        NORMAL = 0
        USEMATRIX = 1

    End Enum
    Public Sub RaiseFlag(ByRef myExp As Boolean)
        myExp = True 'Set true lol
    End Sub
    Public Sub LowerFlag(ByRef myExp As Boolean)
        myExp = False
    End Sub
    Public Sub TheFollowingFlagWillBeLowered(ByRef myexp)
        LowerFlag(myexp)
    End Sub


    Public _t As Double = 0
    Public cars As New List(Of Car)

    Public Car_Init = False


    Public Models As New List(Of PRM)
    Public PermaModels As New List(Of PRM)

End Module
