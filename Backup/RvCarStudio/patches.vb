Module patches
    'patch model
    Public Sub Patch(ByRef Sender As NumericUpDown)
        If Sender.Minimum <> -10000D Then

            Sender.Minimum = -10000D
            Sender.Maximum = 10000D
        End If
    End Sub

End Module
