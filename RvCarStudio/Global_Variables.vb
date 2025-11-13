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
    ''' Validates if RVPATH points to a valid Re-Volt installation directory
    ''' </summary>
    ''' <param name="path">Path to validate</param>
    ''' <param name="errorMessage">Output error message if validation fails</param>
    ''' <returns>True if path is valid, False otherwise</returns>
    Public Function IsValidRVPath(ByVal path As String, ByRef errorMessage As String) As Boolean
        ' Check if path is empty or null
        If String.IsNullOrEmpty(path) Then
            errorMessage = "Re-Volt path is not configured"
            Return False
        End If

        ' Check if path exists and is accessible
        Try
            If Not IO.Directory.Exists(path) Then
                errorMessage = "Re-Volt path does not exist: " & path
                Return False
            End If
        Catch ex As IO.IOException
            ' Device not ready (e.g., disconnected USB drive)
            errorMessage = "Re-Volt path is not accessible (device not ready): " & path & vbNewLine & ex.Message
            Return False
        Catch ex As UnauthorizedAccessException
            ' Permission denied
            errorMessage = "Access denied to Re-Volt path: " & path & vbNewLine & ex.Message
            Return False
        Catch ex As Exception
            ' Other errors
            errorMessage = "Error accessing Re-Volt path: " & path & vbNewLine & ex.Message
            Return False
        End Try

        ' Check if cars directory exists
        Try
            Dim carsPath As String = IO.Path.Combine(path, "cars")
            If Not IO.Directory.Exists(carsPath) Then
                errorMessage = "Invalid Re-Volt installation - 'cars' directory not found in: " & path
                Return False
            End If
        Catch ex As Exception
            errorMessage = "Error validating Re-Volt installation: " & ex.Message
            Return False
        End Try

        ' Path is valid
        errorMessage = ""
        Return True
    End Function

    ''' <summary>
    ''' Validates if RVPATH points to a valid Re-Volt installation directory (overload without error message)
    ''' </summary>
    Public Function IsValidRVPath(ByVal path As String) As Boolean
        Dim dummy As String = ""
        Return IsValidRVPath(path, dummy)
    End Function

    ''' <summary>
    ''' Ensures RVPATH is valid, prompts user to reconfigure if not
    ''' </summary>
    ''' <returns>True if RVPATH is valid (or user successfully reconfigured), False if user cancelled</returns>
    Public Function EnsureValidRVPath() As Boolean
        Dim errorMsg As String = ""

        ' Check if current RVPATH is valid
        If IsValidRVPath(RVPATH, errorMsg) Then
            Return True
        End If

        ' RVPATH is invalid - show error and prompt for reconfiguration
        Dim result As MsgBoxResult = MsgBox(
            "Re-Volt Car Studio cannot access the Re-Volt installation directory." & vbNewLine & vbNewLine &
            "Error: " & errorMsg & vbNewLine & vbNewLine &
            "Would you like to reconfigure the Re-Volt path now?" & vbNewLine & vbNewLine &
            "Click 'Yes' to configure, 'No' to exit application.",
            MsgBoxStyle.Exclamation Or MsgBoxStyle.YesNo,
            "Invalid Re-Volt Path"
        )

        If result = MsgBoxResult.Yes Then
            ' Show configuration dialog
            Config.ShowDialog()

            ' Reload RVPATH from settings
            RVPATH = Sett_get("dir", "")

            ' Validate again
            If IsValidRVPath(RVPATH, errorMsg) Then
                Return True
            Else
                MsgBox("Configuration failed. Path is still invalid: " & errorMsg, MsgBoxStyle.Critical, "Configuration Failed")
                Return False
            End If
        Else
            ' User chose to exit
            Return False
        End If
    End Function

    ''' <summary>
    ''' Safely combines RVPATH with a relative path, with validation
    ''' </summary>
    ''' <param name="relativePath">Relative path to combine with RVPATH</param>
    ''' <param name="resultPath">Output: The combined path if successful</param>
    ''' <returns>True if successful, False if RVPATH is invalid</returns>
    Public Function SafeRVPath(ByVal relativePath As String, ByRef resultPath As String) As Boolean
        If Not EnsureValidRVPath() Then
            resultPath = ""
            Return False
        End If

        Try
            ' Clean up the relative path
            Dim cleanPath As String = relativePath.Replace(Chr(34), "").Replace(",", ".")
            If cleanPath.StartsWith("\") Then cleanPath = cleanPath.Substring(1)

            ' Combine paths properly
            resultPath = IO.Path.Combine(RVPATH, cleanPath)
            Return True
        Catch ex As Exception
            Console_.W("Error building path: " & ex.Message)
            resultPath = ""
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Safely combines RVPATH with a relative path (simplified version)
    ''' Returns empty string if failed
    ''' </summary>
    Public Function SafeRVPath(ByVal relativePath As String) As String
        Dim result As String = ""
        SafeRVPath(relativePath, result)
        Return result
    End Function
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
