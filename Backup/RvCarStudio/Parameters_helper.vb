Module Parameters_helper

    'if more than one model uses same model slot:
    Sub CheckModelNumberAndFix(ByRef N%)
        If N = -1 Then GoTo EditIt
        If FindOccInModels(N) < 2 Then Exit Sub 'no need to proceed... alone

EditIt:
        Dim ns = getFreeSlot()
        If N > -1 Then
            cars(Active_Car).Theory.MainInfos.Model(ns) = cars(Active_Car).Theory.MainInfos.Model(N)
        Else
            cars(Active_Car).Theory.MainInfos.Model(ns) = "NONE"
        End If
        N = ns

    End Sub
    'Occurency of a model slot
    Function FindOccInModels(ByVal N%) As Integer
        Dim fnd = 0
        With cars(Active_Car).Theory


            If .Body.modelNumber = N Then fnd += 1
            For i = 0 To 3
                If .wheel(i).modelNumber = N Then fnd += 1
                If .Axle(i).modelNumber = N Then fnd += 1
                If .Spring(i).modelNumber = N Then fnd += 1
                If .PIN(i).modelNumber = N Then fnd += 1
            Next
            If .Spinner.modelNumber = N Then fnd += 1
            If .Aerial.ModelNumber = N Then fnd += 1
            If .Aerial.TopModelNumber = N Then fnd += 1
        End With
        Return fnd
    End Function
    'get free slot
    Function getFreeSlot() As Integer
        For i = 0 To 18
            '  If cars(Active_Car).Theory.MainInfos.Model(i) = "NONE" Then Return i
            If FindOccInModels(i) = 0 Then Return i

        Next
        Return 18
    End Function

End Module
