Public Class _About

    Public Sub DoLoad()
        TextBox1.Text = "Name: " & APPNAME & vbNewLine & _
                      "Made by: " & Maker & vbNewLine & _
                      "Version: " & MajorVersion & "." & MinorVersion & "." & BuildNumber & "." & Revision & "[" & Type.ToString & "]" & vbNewLine & _
                      "COPYRIGHTS:" & vbNewLine & COPY & vbNewLine & _
                        vbNewLine & _
                            FULL_INFO


    End Sub
    Private Sub About_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub
End Class