Public Class Singletons

    'SINGLETONS (of parameterstxt)

    Private Shared newStr As String
    Dim path$
    ReadOnly Property RETURN_STRING() 'return all string
        Get
            Return newStr
        End Get
    End Property

    Sub New(ByVal FilePath As String)
        If Not IO.File.Exists(FilePath) Then
            Console_.W("Error:" & FilePath & " couldn't be found")
            Beep()
            Exit Sub
        End If
        newStr = IO.File.ReadAllText(FilePath)

        Clean()
    End Sub
    Sub New(ByVal Str As String, ByVal FLAG_OF_STREAM As Boolean)

        newStr = Str

        Clean()
    End Sub

    'TODO: clean this Clean code
    Sub Clean()
        Dim temp = Split(newStr, vbNewLine)
        Dim CleanStr As String = ""
        For i = 0 To UBound(temp)
            If InStr(temp(i), ";") > 0 Then
                If temp(i).Length > 1 Then
                    If temp(i)(1) = ")" Then
                        CleanStr &= temp(i) & vbNewLine
                        Continue For
                    End If
                End If


                CleanStr &= Split(temp(i), ";")(0) & vbNewLine
            Else

                CleanStr &= temp(i) & vbNewLine
            End If
        Next

        Do Until InStr(CleanStr, vbNewLine & vbNewLine) < 1
            CleanStr = Replace(CleanStr, vbNewLine & vbNewLine, vbNewLine)
        Loop

        newStr = CleanStr
    End Sub
    Public Function getAllSingletons() As String()
        Dim temp() = newStr.Split(vbNewLine)
        Dim header As String = ""
        For i = LBound(temp) To UBound(temp)
            If InStr(temp(i), "{") > 0 Then
                header &= Replace(Replace(Split(temp(i), "{")(0), " " & vbTab, ""), vbTab, "") & ","
            End If
        Next

        Return header.Split(",")
    End Function
    Sub writeHeader(ByRef w As IO.StreamWriter, ByVal Name$)
        w.WriteLine()
        w.WriteLine(";====================")
        w.WriteLine("; {0}", Name)
        w.WriteLine(";====================")
        w.WriteLine()
    End Sub
    Public Sub SaveToFile()
        SaveToFile(cars(Active_Car).Path & "\parameters.txt")
    End Sub
    Public Sub SaveToFile(ByVal path As String)

        'FileCopy(path, path & "x")

        Dim w As New IO.StreamWriter(New IO.FileStream(Replace(path, "\\", "\", , , CompareMethod.Text), IO.FileMode.Create))

        With cars(Active_Car).Theory.MainInfos
            w.WriteLine("{")
            w.WriteLine()
            w.WriteLine(";============================================================")
            w.WriteLine(";============================================================")
            w.WriteLine("; " & .Name)
            w.WriteLine(";============================================================")
            w.WriteLine(";============================================================")
            Dim x$ = .Name
            Do Until x(0) <> " "
                x = Mid(x, 2)
            Loop
            w.WriteLine("Name      	""{0}""", x)
            w.WriteLine()

            writeHeader(w, "Model Filepaths")

            For i = 0 To 18
                w.WriteLine("MODEL 	{0} 	""{1}""", i, .Model(i))
            Next

            w.WriteLine("TPAGE 	""{0}""", .Tpage)
            w.WriteLine("COLL 	""{0}""", .CollFile)
            w.WriteLine(";)TCARBOX 	""{0}""", .TCarBox)
            w.WriteLine("EnvRGB 	{0} {1} {2}", .EnvRGB.R * 255, .EnvRGB.G * 255, .EnvRGB.B * 255)



            writeHeader(w, "Stuff mainly for frontend display and car selectability")



            w.WriteLine("BestTime   	{0}", UCase(.BESTTIME.ToString))
            w.WriteLine("Selectable   	{0}", UCase(.SELECTABLE.ToString))

            w.WriteLine("Statistics    	{0} 			; Can be included in statistics", UCase(.Statistics.ToString))
            w.WriteLine("Class      	{0} 			; Engine type (0=Elec, 1=Glow, 2=Other)", .car_class)
            w.WriteLine("Obtain     	{0} 			; Obtain method", .obtain)
            w.WriteLine("Rating     	{0} 			; Skill level (rookie, amateur, ...)", .Rating)
            w.WriteLine("TopEnd     	{0} 			; Actual top speed (mph) for frontend bars", .TopEnd)
            w.WriteLine("Acc        	{0} 			; Acceleration rating (empirical)", Strings.Format(.Acceleration, "0.000000"))
            w.WriteLine("Weight     	{0} 			; Scaled weight (for frontend bars)", Strings.Format(.Weight, "0.000000"))
            w.WriteLine("Handling   	{0} 			; Handling ability (empirical and totally subjective)", Strings.Format(.Handling, "0.000000"))
            w.WriteLine("Trans      	{0} 			; Transmission type (calculate in game anyway...)", Strings.Format(.Trans, "0.000000"))
            w.WriteLine("MaxRevs    	{0} 			; Max Revs (for rev counter)", Strings.Format(.MaxRev, "0.000000"))

        End With

        With cars(Active_Car).Theory.RealInfos
            writeHeader(w, "Handling related stuff")
            w.WriteLine("SteerRate  	{0} 			; Rate at which steer angle approaches value from input", Strings.Format(.SteerRate, "0.000000"))
            w.WriteLine("SteerMod   	{0} 			; ", Strings.Format(.SteerMode, "0.000000"))
            w.WriteLine("EngineRate 	{0} 			; Rate at which Engine voltage approaches set value", Strings.Format(.EngineRate, "0.000000"))
            w.WriteLine("TopSpeed   	{0} 			; Car's theoretical top speed (not including friction...)", Strings.Format(.TopSpeed, "0.000000"))
            w.WriteLine("DownForceMod	{0} 			; Down force modifier when car on floor", Strings.Format(.DownForceModifier, "0.000000"))
            w.WriteLine("CoM        	{0} {1} {2} 		; Centre of mass relative to model centre", Strings.Format(.COM.X * 1.0, "0.000000"), Strings.Format(.COM.Y * 1.0, "0.000000"), Strings.Format(.COM.Z * 1.0, "0.000000"))
            w.WriteLine("Weapon     	{0} {1} {2} 		; Weapon genration offset", Strings.Format(.WeaponGeneration.X * 1.0, "0.000000"), Strings.Format(.WeaponGeneration.Y * -1.0, "0.000000"), Strings.Format(.WeaponGeneration.Z * 1.0, "0.000000"))
            w.WriteLine(";)Flippable    {0}   ; Rotor car effect", UCase(.Flippable.ToString))
            w.WriteLine(";)Flying       {0}   ; Flying like the UFO car", UCase(.Flying.ToString))
            w.WriteLine(";)ClothFx      {0}   ; Mystery car cloth effect", UCase(.ClothFX.ToString))


        End With

        With cars(Active_Car).Theory.Body
            writeHeader(w, "Car Body details")
            w.WriteLine("BODY {		; Start Body")
            w.WriteLine("ModelNum   	{0} 			; Model Number in above list", .modelNumber)
            w.WriteLine("Offset     	{0}, {1}, {2} 		; Calculated in game", Strings.Format(.Offset.X * 1, "0.000000"), Strings.Format(.Offset.Y * -1, "0.000000"), Strings.Format(.Offset.Z * 1, "0.000000"))
            w.WriteLine("Mass       	{0}", .Mass)
            w.WriteLine("Inertia    	{0} {1} {2}", Strings.Format(.Inertia(0).X * 1, "0.000000"), Strings.Format(.Inertia(0).Y * 1, "0.000000"), Strings.Format(.Inertia(0).Z * 1, "0.000000"))
            w.WriteLine("           	{0} {1} {2}", Strings.Format(.Inertia(1).X * 1, "0.000000"), Strings.Format(.Inertia(1).Y * 1, "0.000000"), Strings.Format(.Inertia(1).Z * 1, "0.000000"))
            w.WriteLine("           	{0} {1} {2}", Strings.Format(.Inertia(2).X * 1, "0.000000"), Strings.Format(.Inertia(2).Y * 1, "0.000000"), Strings.Format(.Inertia(2).Z * 1, "0.000000"))
            w.WriteLine("Gravity		2200 			; No longer used")
            w.WriteLine("Hardness   	{0}", .Hardness)
            w.WriteLine("Resistance 	{0} 			; Linear air esistance", Strings.Format(.Resistance, "0.000000"))
            w.WriteLine("AngRes     	{0} 			; Angular air resistance", Strings.Format(.AngleRes, "0.000000"))
            w.WriteLine("ResMod     	{0} 			; Ang air resistnce scale when in air", Strings.Format(.ResMode, "0.000000"))
            w.WriteLine("Grip       	{0} 			; Converts downforce to friction value", Strings.Format(.Grip, "0.000000"))
            w.WriteLine("StaticFriction	{0}", Strings.Format(.StaticFriction, "0.000000"))
            w.WriteLine("KineticFriction {0}", Strings.Format(.KineticFriction, "0.000000"))
            w.WriteLine("}     		; End Body")

        End With

        writeHeader(w, "Car Wheel details")

        For i = 0 To 3

            With cars(Active_Car).Theory.wheel(i)
                w.WriteLine("WHEEL " & i & " { 	; Start Wheel")
                w.WriteLine("ModelNum 	    " & .modelNumber)
                w.WriteLine("Offset1  	    {0} {1} {2}", .Offset(1).X * 1, .Offset(1).Y * -1, .Offset(1).Z * 1)
                w.WriteLine("Offset2  	    {0} {1} {2}", .Offset(2).X * 1, .Offset(2).Y * -1, .Offset(2).Z * 1)
                w.WriteLine("IsPresent   	{0}", UCase(.IsPresent.ToString))
                w.WriteLine("IsPowered   	{0}", UCase(.IsPowered.ToString))
                w.WriteLine("IsTurnable   	{0}", UCase(.IsTurnable.ToString))
                w.WriteLine("SteerRatio  	{0}", .SteerRatio)
                w.WriteLine("EngineRatio 	{0}", .EngineRatio)
                w.WriteLine("Radius      	{0}", .Radius)
                w.WriteLine("Mass        	{0}", .Mass)
                w.WriteLine("Gravity     	2200")
                w.WriteLine("MaxPos      	{0}", .MaxPos)
                w.WriteLine("SkidWidth   	{0}", .SkidWidth)
                w.WriteLine("ToeIn       	{0}", .ToeInn)
                w.WriteLine("AxleFriction    	{0}", .AxleFriction)
                w.WriteLine("Grip            	{0}", .Grip)
                w.WriteLine("StaticFriction  	{0}", .StaticFriction)
                w.WriteLine("KineticFriction 	{0}", .KineticFriction)
                w.WriteLine("}          	; End Wheel")
                w.WriteLine()
            End With

        Next

        writeHeader(w, "Car Spring details")

        For i = 0 To 3

            With cars(Active_Car).Theory.Spring(i)
                w.WriteLine("SPRING " & i & " { 	; Start Spring")
                w.WriteLine("ModelNum    	{0}", .modelNumber)
                w.WriteLine("Offset  	    {0} {1} {2}", .Offset.X * 1, .Offset.Y * -1, .Offset.Z * 1)
                w.WriteLine("Length      	{0}", .Length)
                w.WriteLine("Stiffness   	{0}", .Stiffness)
                w.WriteLine("Damping     	{0}", .Damping)
                w.WriteLine("Restitution 	{0}", .Restitution)

                w.WriteLine("}          	; End Spring")
                w.WriteLine()
            End With

        Next

        writeHeader(w, "Car Pin details")


        For i = 0 To 3

            With cars(Active_Car).Theory.PIN(i)
                w.WriteLine("PIN " & i & " { 	    ; Start Pin")
                w.WriteLine("ModelNum    	{0}", .modelNumber)
                w.WriteLine("Offset  	    {0} {1} {2}", .offSet.X * 1, .offSet.Y * -1, .offSet.Z * 1)
                w.WriteLine("Length      	{0}", .Length)
                w.WriteLine("}          	; End Pin")
                w.WriteLine()
            End With

        Next


        writeHeader(w, "Car axle details")


        For i = 0 To 3

            With cars(Active_Car).Theory.Axle(i)
                w.WriteLine("AXLE " & i & " { 	; Start Axle")
                w.WriteLine("ModelNum    	{0}", .modelNumber)
                w.WriteLine("Offset  	{0} {1} {2}", .offSet.X * 1, .offSet.Y * -1, .offSet.Z * 1)
                w.WriteLine("Length      	{0}", .Length)
                w.WriteLine("}          	; End Axle")
                w.WriteLine()
            End With

        Next

        writeHeader(w, "Car Spinner details")



        With cars(Active_Car).Theory.Spinner
            w.WriteLine("SPINNER { 	    ; Start spinner")
            w.WriteLine("ModelNum    	{0}", .modelNumber)
            w.WriteLine("Offset      	{0} {1} {2}", .offSet.X * 1, .offSet.Y * -1, .offSet.Z * 1)
            w.WriteLine("Axis        	{0} {1} {2}", .Axis.X * 1, .Axis.Y * -1, .Axis.Z * 1)
            w.WriteLine("AngVel      	{0}", .angVel)
            w.WriteLine("}          	; End Spinner")
            w.WriteLine()
        End With

        writeHeader(w, "Car Aerial details")



        With cars(Active_Car).Theory.Aerial
            w.WriteLine("AERIAL { 	; Start Aerial")
            w.WriteLine("SecModelNum 	{0}", .ModelNumber)
            w.WriteLine("TopModelNum 	{0}", .TopModelNumber)
            w.WriteLine("Offset      	{0} {1} {2}", .offset.X * 1, .offset.Y * -1, .offset.Z * 1)
            w.WriteLine("Direction   	{0} {1} {2}", .Direction.X * 1, .Direction.Y * -1, .Direction.Z * 1)
            w.WriteLine("Length      	{0}", .length)
            w.WriteLine("Stiffness   	{0}", .stiffness)
            w.WriteLine("Damping     	{0}", .damping)

            w.WriteLine("}          	; End Aerial")
            w.WriteLine()
        End With
        Try


            If cars(Active_Car).Theory.carAi IsNot Nothing Then

                With cars(Active_Car).Theory.carAi

                    writeHeader(w, "Car AI details")
                    w.WriteLine("AI { 	; Start AI")
                    w.WriteLine("UnderThresh 	{0}", .UnderThresh)
                    w.WriteLine("UnderRange  	{0}", .UnderRange)
                    w.WriteLine("UnderFront	 	{0}", .UnderFront)
                    w.WriteLine("UnderRear   	{0}", .UnderRear)
                    w.WriteLine("UnderMax    	{0}", .UnderMax)
                    w.WriteLine("OverThresh  	{0}", .OverThresh)
                    w.WriteLine("OverRange   	{0}", .OverRange)
                    w.WriteLine("OverMax     	{0}", .OverMax)
                    w.WriteLine("OverAccThresh  	{0}", .OverAccThresh)
                    w.WriteLine("OverAccRange   	{0}", .OverAccRange)
                    w.WriteLine("PickupBias     	{0}", .PickupBias)
                    w.WriteLine("BlockBias      	{0}", .BlockBias)
                    w.WriteLine("OvertakeBias   	{0}", .OvertakeBias)
                    w.WriteLine("Suspension     	{0}", .Suspension)
                    w.WriteLine("Aggression     	{0}", .Aggression)
                    w.WriteLine("}          	; End AI")
                    w.WriteLine()
                End With

            End If
        Catch

        End Try
        w.WriteLine("}")
        w.Flush()
        w.Close()

        'patching file
        If InStr(Str(CSng(0.2)), ",", CompareMethod.Text) > 0 Then
            Dim mystr = IO.File.ReadAllText(path)
            mystr = Replace(mystr, ",", ".")
            IO.File.WriteAllText(path, mystr)
        End If



    End Sub
    Public Function SaveToStr() As String

        'FileCopy(path, path & "x")
        Dim P As New IO.MemoryStream()
        Dim w As New IO.StreamWriter(P)

        With cars(Active_Car).Theory.MainInfos
            w.WriteLine("{")
            w.WriteLine()
            w.WriteLine(";============================================================")
            w.WriteLine(";============================================================")
            w.WriteLine("; " & .Name)
            w.WriteLine(";============================================================")
            w.WriteLine(";============================================================")
            Dim x$ = .Name
            Do Until x(0) <> " "
                x = Mid(x, 2)
            Loop
            w.WriteLine("Name      	""{0}""", x)
            w.WriteLine()

            writeHeader(w, "Model Filepaths")

            For i = 0 To 18
                w.WriteLine("MODEL 	{0} 	""{1}""", i, .Model(i))
            Next

            w.WriteLine("TPAGE 	""{0}""", .Tpage)
            w.WriteLine("COLL 	""{0}""", .CollFile)
            w.WriteLine(";)TCARBOX 	""{0}""", .TCarBox)
            w.WriteLine("EnvRGB 	{0} {1} {2}", .EnvRGB.R * 255, .EnvRGB.G * 255, .EnvRGB.B * 255)



            writeHeader(w, "Stuff mainly for frontend display and car selectability")



            w.WriteLine("BestTime   	{0}", UCase(.BESTTIME.ToString))
            w.WriteLine("Selectable   	{0}", UCase(.SELECTABLE.ToString))

            w.WriteLine("Statistics    	{0} 			; Can be included in statistics", UCase(.Statistics.ToString))
            w.WriteLine("Class      	{0} 			; Engine type (0=Elec, 1=Glow, 2=Other)", .car_class)
            w.WriteLine("Obtain     	{0} 			; Obtain method", .obtain)
            w.WriteLine("Rating     	{0} 			; Skill level (rookie, amateur, ...)", .Rating)
            w.WriteLine("TopEnd     	{0} 			; Actual top speed (mph) for frontend bars", .TopEnd)
            w.WriteLine("Acc        	{0} 			; Acceleration rating (empirical)", Strings.Format(.Acceleration, "0.000000"))
            w.WriteLine("Weight     	{0} 			; Scaled weight (for frontend bars)", Strings.Format(.Weight, "0.000000"))
            w.WriteLine("Handling   	{0} 			; Handling ability (empirical and totally subjective)", Strings.Format(.Handling, "0.000000"))
            w.WriteLine("Trans      	{0} 			; Transmission type (calculate in game anyway...)", Strings.Format(.Trans, "0.000000"))
            w.WriteLine("MaxRevs    	{0} 			; Max Revs (for rev counter)", Strings.Format(.MaxRev, "0.000000"))

        End With

        With cars(Active_Car).Theory.RealInfos
            writeHeader(w, "Handling related stuff")
            w.WriteLine("SteerRate  	{0} 			; Rate at which steer angle approaches value from input", Strings.Format(.SteerRate, "0.000000"))
            w.WriteLine("SteerMod   	{0} 			; ", Strings.Format(.SteerMode, "0.000000"))
            w.WriteLine("EngineRate 	{0} 			; Rate at which Engine voltage approaches set value", Strings.Format(.EngineRate, "0.000000"))
            w.WriteLine("TopSpeed   	{0} 			; Car's theoretical top speed (not including friction...)", Strings.Format(.TopSpeed, "0.000000"))
            w.WriteLine("DownForceMod	{0} 			; Down force modifier when car on floor", Strings.Format(.DownForceModifier, "0.000000"))
            w.WriteLine("CoM        	{0} {1} {2} 		; Centre of mass relative to model centre", Strings.Format(.COM.X * 1.0, "0.000000"), Strings.Format(.COM.Y * 1.0, "0.000000"), Strings.Format(.COM.Z * 1.0, "0.000000"))
            w.WriteLine("Weapon     	{0} {1} {2} 		; Weapon genration offset", Strings.Format(.WeaponGeneration.X * 1.0, "0.000000"), Strings.Format(.WeaponGeneration.Y * -1.0, "0.000000"), Strings.Format(.WeaponGeneration.Z * 1.0, "0.000000"))
            w.WriteLine(";)Flippable    {0}   ; Rotor car effect", UCase(.Flippable.ToString))
            w.WriteLine(";)Flying       {0}   ; Flying like the UFO car", UCase(.Flying.ToString))
            w.WriteLine(";)ClothFx      {0}   ; Mystery car cloth effect", UCase(.ClothFX.ToString))


        End With

        With cars(Active_Car).Theory.Body
            writeHeader(w, "Car Body details")
            w.WriteLine("BODY {		; Start Body")
            w.WriteLine("ModelNum   	{0} 			; Model Number in above list", .modelNumber)
            w.WriteLine("Offset     	{0}, {1}, {2} 		; Calculated in game", Strings.Format(.Offset.X * 1, "0.000000"), Strings.Format(.Offset.Y * -1, "0.000000"), Strings.Format(.Offset.Z * 1, "0.000000"))
            w.WriteLine("Mass       	{0}", .Mass)
            w.WriteLine("Inertia    	{0} {1} {2}", Strings.Format(.Inertia(0).X * 1, "0.000000"), Strings.Format(.Inertia(0).Y * 1, "0.000000"), Strings.Format(.Inertia(0).Z * 1, "0.000000"))
            w.WriteLine("           	{0} {1} {2}", Strings.Format(.Inertia(1).X * 1, "0.000000"), Strings.Format(.Inertia(1).Y * 1, "0.000000"), Strings.Format(.Inertia(1).Z * 1, "0.000000"))
            w.WriteLine("           	{0} {1} {2}", Strings.Format(.Inertia(2).X * 1, "0.000000"), Strings.Format(.Inertia(2).Y * 1, "0.000000"), Strings.Format(.Inertia(2).Z * 1, "0.000000"))
            w.WriteLine("Gravity		2200 			; No longer used")
            w.WriteLine("Hardness   	{0}", .Hardness)
            w.WriteLine("Resistance 	{0} 			; Linear air esistance", Strings.Format(.Resistance, "0.000000"))
            w.WriteLine("AngRes     	{0} 			; Angular air resistance", Strings.Format(.AngleRes, "0.000000"))
            w.WriteLine("ResMod     	{0} 			; Ang air resistnce scale when in air", Strings.Format(.ResMode, "0.000000"))
            w.WriteLine("Grip       	{0} 			; Converts downforce to friction value", Strings.Format(.Grip, "0.000000"))
            w.WriteLine("StaticFriction	{0}", Strings.Format(.StaticFriction, "0.000000"))
            w.WriteLine("KineticFriction {0}", Strings.Format(.KineticFriction, "0.000000"))
            w.WriteLine("}     		; End Body")

        End With

        writeHeader(w, "Car Wheel details")

        For i = 0 To 3

            With cars(Active_Car).Theory.wheel(i)
                w.WriteLine("WHEEL " & i & " { 	; Start Wheel")
                w.WriteLine("ModelNum 	    " & .modelNumber)
                w.WriteLine("Offset1  	    {0} {1} {2}", .Offset(1).X * 1, .Offset(1).Y * -1, .Offset(1).Z * 1)
                w.WriteLine("Offset2  	    {0} {1} {2}", .Offset(2).X * 1, .Offset(2).Y * -1, .Offset(2).Z * 1)
                w.WriteLine("IsPresent   	{0}", UCase(.IsPresent.ToString))
                w.WriteLine("IsPowered   	{0}", UCase(.IsPowered.ToString))
                w.WriteLine("IsTurnable   	{0}", UCase(.IsTurnable.ToString))
                w.WriteLine("SteerRatio  	{0}", .SteerRatio)
                w.WriteLine("EngineRatio 	{0}", .EngineRatio)
                w.WriteLine("Radius      	{0}", .Radius)
                w.WriteLine("Mass        	{0}", .Mass)
                w.WriteLine("Gravity     	2200")
                w.WriteLine("MaxPos      	{0}", .MaxPos)
                w.WriteLine("SkidWidth   	{0}", .SkidWidth)
                w.WriteLine("ToeIn       	{0}", .ToeInn)
                w.WriteLine("AxleFriction    	{0}", .AxleFriction)
                w.WriteLine("Grip            	{0}", .Grip)
                w.WriteLine("StaticFriction  	{0}", .StaticFriction)
                w.WriteLine("KineticFriction 	{0}", .KineticFriction)
                w.WriteLine("}          	; End Wheel")
                w.WriteLine()
            End With

        Next

        writeHeader(w, "Car Spring details")

        For i = 0 To 3

            With cars(Active_Car).Theory.Spring(i)
                w.WriteLine("SPRING " & i & " { 	; Start Spring")
                w.WriteLine("ModelNum    	{0}", .modelNumber)
                w.WriteLine("Offset  	    {0} {1} {2}", .Offset.X * 1, .Offset.Y * -1, .Offset.Z * 1)
                w.WriteLine("Length      	{0}", .Length)
                w.WriteLine("Stiffness   	{0}", .Stiffness)
                w.WriteLine("Damping     	{0}", .Damping)
                w.WriteLine("Restitution 	{0}", .Restitution)

                w.WriteLine("}          	; End Spring")
                w.WriteLine()
            End With

        Next

        writeHeader(w, "Car Pin details")


        For i = 0 To 3

            With cars(Active_Car).Theory.PIN(i)
                w.WriteLine("PIN " & i & " { 	    ; Start Pin")
                w.WriteLine("ModelNum    	{0}", .modelNumber)
                w.WriteLine("Offset  	    {0} {1} {2}", .offSet.X * 1, .offSet.Y * -1, .offSet.Z * 1)
                w.WriteLine("Length      	{0}", .Length)
                w.WriteLine("}          	; End Pin")
                w.WriteLine()
            End With

        Next


        writeHeader(w, "Car axle details")


        For i = 0 To 3

            With cars(Active_Car).Theory.Axle(i)
                w.WriteLine("AXLE " & i & " { 	; Start Axle")
                w.WriteLine("ModelNum    	{0}", .modelNumber)
                w.WriteLine("Offset  	{0} {1} {2}", .offSet.X * 1, .offSet.Y * -1, .offSet.Z * 1)
                w.WriteLine("Length      	{0}", .Length)
                w.WriteLine("}          	; End Axle")
                w.WriteLine()
            End With

        Next

        writeHeader(w, "Car Spinner details")



        With cars(Active_Car).Theory.Spinner
            w.WriteLine("SPINNER { 	    ; Start spinner")
            w.WriteLine("ModelNum    	{0}", .modelNumber)
            w.WriteLine("Offset      	{0} {1} {2}", .offSet.X * 1, .offSet.Y * -1, .offSet.Z * 1)
            w.WriteLine("Axis        	{0} {1} {2}", .Axis.X * 1, .Axis.Y * -1, .Axis.Z * 1)
            w.WriteLine("AngVel      	{0}", .angVel)
            w.WriteLine("}          	; End Spinner")
            w.WriteLine()
        End With

        writeHeader(w, "Car Aerial details")



        With cars(Active_Car).Theory.Aerial
            w.WriteLine("AERIAL { 	; Start Aerial")
            w.WriteLine("SecModelNum 	{0}", .ModelNumber)
            w.WriteLine("TopModelNum 	{0}", .TopModelNumber)
            w.WriteLine("Offset      	{0} {1} {2}", .offset.X * 1, .offset.Y * -1, .offset.Z * 1)
            w.WriteLine("Direction   	{0} {1} {2}", .Direction.X * 1, .Direction.Y * -1, .Direction.Z * 1)
            w.WriteLine("Length      	{0}", .length)
            w.WriteLine("Stiffness   	{0}", .stiffness)
            w.WriteLine("Damping     	{0}", .damping)

            w.WriteLine("}          	; End Aerial")
            w.WriteLine()
        End With
        Try


            If cars(Active_Car).Theory.carAi IsNot Nothing Then

                With cars(Active_Car).Theory.carAi

                    writeHeader(w, "Car AI details")
                    w.WriteLine("AI { 	; Start AI")
                    w.WriteLine("UnderThresh 	{0}", .UnderThresh)
                    w.WriteLine("UnderRange  	{0}", .UnderRange)
                    w.WriteLine("UnderFront	 	{0}", .UnderFront)
                    w.WriteLine("UnderRear   	{0}", .UnderRear)
                    w.WriteLine("UnderMax    	{0}", .UnderMax)
                    w.WriteLine("OverThresh  	{0}", .OverThresh)
                    w.WriteLine("OverRange   	{0}", .OverRange)
                    w.WriteLine("OverMax     	{0}", .OverMax)
                    w.WriteLine("OverAccThresh  	{0}", .OverAccThresh)
                    w.WriteLine("OverAccRange   	{0}", .OverAccRange)
                    w.WriteLine("PickupBias     	{0}", .PickupBias)
                    w.WriteLine("BlockBias      	{0}", .BlockBias)
                    w.WriteLine("OvertakeBias   	{0}", .OvertakeBias)
                    w.WriteLine("Suspension     	{0}", .Suspension)
                    w.WriteLine("Aggression     	{0}", .Aggression)
                    w.WriteLine("}          	; End AI")
                    w.WriteLine()
                End With

            End If
        Catch

        End Try
        w.WriteLine("}")
        w.Flush()
        'w.Close()
        P.Position = 0
        Dim pstr As New IO.StreamReader(P)
        Dim str = pstr.ReadToEnd()
        pstr.Close()
        pstr = Nothing : w = Nothing
        Return str



    End Function

    Public Function getSingleton(ByVal header) As SingletonItem

        'TODO: Jigebren's 4x4 car
        If InStr(newStr, header) < 1 Then
            Return SingletonItem.Null
        End If


        Dim temp As String = ""
        If header = "" Or header = " " Then
            temp = Split(newStr, "{")(1)

        Else
            temp = Split(Split(newStr, header)(1), "{")(1)
        End If

        If InStr(Split(temp, "}")(0), "{") < 1 Then
            'lucky us!
            Dim l = Split(temp, "}")(0)
            Dim torep = Split(l, vbNewLine).Last

            Return SingletonItem.ToSingletonItem(Replace(l, torep, ""))


        Else
            'how unlucky...
            Dim tmp As String = temp
            Do Until InStr(tmp, "{") = 0
                Dim splt = Split(Split(Split(tmp, "{")(0), vbNewLine)(1), "}")(0)
                tmp = Replace(tmp, splt, "")
            Loop
            Return SingletonItem.ToSingletonItem(tmp)
        End If
        Return SingletonItem.Null
    End Function

End Class
Public Class SingletonItem
    Private Shared items() As String
    Public Shared Null = Nothing

    Public Shared Function ToSingletonItem(ByVal str As String) As SingletonItem
        Dim nSing As New SingletonItem
        Dim splitted() = Split(str, vbNewLine)
        ReDim items(splitted.Length)
        SingletonItem.items = splitted
        SingletonItem.items = splitted
        Return nSing
    End Function

    Public Function getValue(ByVal key As Object, Optional ByVal defaultValue As Object = Nothing, Optional ByVal expected As String = "obj")


        For i = LBound(items) To UBound(items)

            If InStr(items(i), key, CompareMethod.Text) > 0 Then
                Dim tmp = Replace(Split(items(i), key, , CompareMethod.Text)(1), " " & vbTab, "") ', ".", ",")


                If IO.File.Exists(RVPATH & "\" & Replace(tmp, """", "")) Then Return Replace(tmp, """", "")

                If InStr(CSng(2.15), ",") <> 0 Then
                    Dim cnt As Integer, sp As Integer
                    For j = 0 To tmp.Length - 1
                        If CChar(tmp(j)) >= "0" And CChar(tmp(j)) <= "9" Then cnt += 1
                        If CChar(tmp(j)) = " " Or CChar(tmp(j)) = vbTab Then sp += 1
                    Next

                    If cnt / (tmp.Length - sp) * 100 > 40 Then
                        tmp = Replace(tmp, ".", ",")
                    End If

                    If expected = "int" And IsNumeric(tmp) = False Then tmp = defaultValue
                    If expected = "float" And IsNumeric(tmp) = False Then tmp = defaultValue
                    If expected = "bool" Then tmp = Replace(Replace(tmp, " ", ""), "    ", "")

                    
                    If InStr(tmp, ";") > 0 Then tmp = Split(tmp, ";")(0)
                    tmp = LTrim(RTrim(tmp))
                    If tmp Is Nothing Then Return defaultValue

                    Return tmp



                End If

                If tmp Is Nothing Then Return defaultValue
                If expected = "int" And IsNumeric(tmp) = False Then tmp = defaultValue
                If expected = "float" And IsNumeric(tmp) = False Then tmp = defaultValue
                If expected = "bool" Then tmp = Replace(Replace(tmp, " ", ""), "    ", "")


                If InStr(tmp, ";") > 0 Then tmp = Split(tmp, ";")(0)
                tmp = LTrim(RTrim(tmp))
                Return tmp

            End If
        Next
        Return Nothing
    End Function
    'replace RTrim
    Function RTrim(ByVal str$)
        Dim foundChar = False
        Dim i = 0
        For i = str.Length - 1 To 0 Step -1
            If Not (str(i) = " " Or str(i) = vbTab Or str(i) = Chr(13)) Then Exit For
            'If str(i) >= "a" And str(i) <= "z" Or str(i) >= "A" And str(i) <= "Z" Or IsNumeric(str(i)) Or str(i) = "!" Or str(i) = "?" Or str(i) = "é" Or str(i) = "è" Or _
            'str(i) = "$" Or str(i) = "£" Or str(i) = "¤" Or str(i) = "%" Or str(i) = "," Or str(i) = "." Or str(i) = ";" Or str(i) = "#" Or str(i) = "~" Or str(i) = ")" Or str(i) = "(" Or _
            'str(i) = "|" Or str(i) = "+" Or str(i) = "-" Or str(i) = "@" Or str(i) = "à" Or str(i) = "î" Or str(i) = "ï" Or str(i) = "ü" Or str(i) = "û" Or str(i) = "ù" Or str(i) = "'" Then
            'Exit For

        Next
        Return Mid(str, 1, i + 1)
    End Function
    'replace LTrim
    Function LTrim(ByVal str$)
        Dim foundChar = False
        Dim i = 0
        For i = 0 To str.Length - 1
            If Not (str(i) = " " Or str(i) = vbTab Or str(i) = Chr(13)) Then Exit For
            'If str(i) >= "a" And str(i) <= "z" Or str(i) >= "A" And str(i) <= "Z" Or IsNumeric(str(i)) Or str(i) = "!" Or str(i) = "?" Or str(i) = "é" Or str(i) = "è" Or _
            'str(i) = "$" Or str(i) = "£" Or str(i) = "¤" Or str(i) = "%" Or str(i) = "," Or str(i) = "." Or str(i) = ";" Or str(i) = "#" Or str(i) = "~" Or str(i) = ")" Or str(i) = "(" Or _
            'str(i) = "|" Or str(i) = "+" Or str(i) = "-" Or str(i) = "@" Or str(i) = "à" Or str(i) = "î" Or str(i) = "ï" Or str(i) = "ü" Or str(i) = "û" Or str(i) = "ù" Or str(i) = "'" Then
            'Exit For

        Next
        Return Mid(str, i + 1, str.Length - i)
    End Function
    Public Function get3LinesValue(ByVal key)

        For i = LBound(items) To UBound(items)

            If InStr(items(i), key) > 0 Then
                Dim tmp = Replace(Split(items(i), key)(1), " " & vbTab, "") & vbNewLine  ', ".", ",")

                tmp &= Replace(items(i + 1), " " & vbTab, "") & vbNewLine  ', ".", ",")
                tmp &= Replace(items(i + 2), " " & vbTab, "") ', ".", ",")

                If InStr(CSng(2.15), ",") <> 0 Then
                    tmp = Replace(tmp, ".", ",")
                End If

                Return tmp

            End If
        Next
        Return Nothing
    End Function
    Public Function get2LinesValue(ByVal key)

        For i = LBound(items) To UBound(items)

            If InStr(items(i), key) > 0 Then
                Dim tmp = Replace(Split(items(i), key)(1), " " & vbTab, "") & vbNewLine  ', ".", ",") & 

                tmp &= Replace(items(i + 1), " " & vbTab, "") ', ".", ",")

                If InStr(CSng(2.15), ",") <> 0 Then
                    tmp = Replace(tmp, ".", ",")
                End If

                Return tmp

            End If
        Next
        Return Nothing
    End Function
    Public Function getAllKeys()
        Dim allVal(items.Length) As String
        For i = LBound(items) To UBound(items)
            If InStr(items(i), " " & vbTab) > 0 Then
                allVal(i) = Split(items(i), " " & vbTab)(0)
            Else
                allVal(i) = Split(items(i), vbTab)(0)
            End If



        Next
        Return allVal
    End Function
    Public Sub setValue(ByVal key As String, ByVal value As String)
        For i = LBound(items) To UBound(items)

            If InStr(items(i), key) > 0 Then

                items(i) = Replace(items(i), Split(items(i), key)(1), value)

                Exit Sub
            End If
        Next
        Dim nItems(items.Length + 1)

        For i = LBound(items) To UBound(items)
            nItems(i) = items(i)
        Next

        ReDim items(items.Length + 1)

        For i = LBound(items) To UBound(items) - 1
            items(i) = nItems(i)
        Next
        items(UBound(items)) = key & " " & vbTab & value

    End Sub
    Public Function GetEverything() As String()
        Return items
    End Function


End Class