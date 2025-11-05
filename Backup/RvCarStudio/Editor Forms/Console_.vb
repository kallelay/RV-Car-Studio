Public Class Console_

    Private Sub Console__Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub
    Sub W(ByVal str$)
        TextBox1.AppendText(">> " & str & vbNewLine)
    End Sub
End Class