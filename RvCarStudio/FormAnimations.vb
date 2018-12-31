Module FormAnimations
    'Window animations

    Public Sub HideForm(ByVal f As Form)
        For i = 0 To 100 Step 10
            f.Opacity = (100 - i) / 100
            Application.DoEvents()
            Threading.Thread.Sleep(12)
        Next

    End Sub
    Public Sub ShowForm(ByVal f As Form)
        For i = 0 To 100 Step 10
            f.Opacity = i / 100
            Application.DoEvents()
            Threading.Thread.Sleep(12)
        Next

    End Sub
End Module
