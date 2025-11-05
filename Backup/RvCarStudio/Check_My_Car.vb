Imports System.Math
Module Check_My_Car
    'This is not developed, it's for checking whether a car is missing something
    'This module (class) can also score a car !
    Sub CheckMyCar() 'As List(Of String)

        Dim cn$ = cars(Active_Car).Theory.MainInfos.Name

        Dim Err As New List(Of String)
        Dim Warn As New List(Of String)
        Dim Info As New List(Of String)

        'Check there is a body, aerial, wheels
        'Check if files exist

        'Check if it's based on other car (still depending on some)

        'Check if EnvRGB is not 0 0 0

        'Check if it can be best time, selectable, obtain (0)

        'Check if wheel intersect body

        'Check Car size


        'Check Polygon count


        'Check Dynamics

        'COM stuffs
        Info.Add("Center of Mass " & cars(Active_Car).Theory.RealInfos.COM.ToString)


        Info.Add(" Balance:" & Strings.Format(Abs(cars(Active_Car).Theory.RealInfos.COM.X) / 40 * 100, "0.00") & "%")


        If cars(Active_Car).Theory.RealInfos.COM.Y > 0 Then
            Info.Add(" Rollability:" & Strings.Format(cars(Active_Car).Theory.RealInfos.COM.Y / 20 * 100, "0.00") & "%")
        End If
        If cars(Active_Car).Theory.RealInfos.COM.Y < 0 Then
            Info.Add(" Climbability:" & Strings.Format(Abs(cars(Active_Car).Theory.RealInfos.COM.Y) / 20 * 100, "0.00") & "%")
        End If
        If Abs(cars(Active_Car).Theory.RealInfos.COM.Y) - 25 > 0 Then
            Err.Add("Center of Mass's Y coordinate is wrong: " & cars(Active_Car).Theory.RealInfos.COM.Y)
        End If

        ' for Each Stock check if body is same as ...


        'Check Geometry

        'Check Dynamics

        'Check bitmap

        'Check if there is a readme

        'check shading

    End Sub
End Module
