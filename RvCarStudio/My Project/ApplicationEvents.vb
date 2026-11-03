Namespace My
    Partial Friend Class MyApplication
        'A car missing some part must not take the whole program down
        Private Sub MyApplication_UnhandledException(ByVal sender As Object, ByVal e As Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs) Handles Me.UnhandledException
            e.ExitApplication = False
            Dim msg = e.Exception.GetType().Name & ": " & e.Exception.Message
            Console_.W("Error: " & e.Exception.ToString())
            If TypeOf e.Exception Is NullReferenceException Then msg = "This car doesn't have the part needed for this action." & vbNewLine & vbNewLine & msg
            MsgBox(msg, MsgBoxStyle.Exclamation, APPNAME)
        End Sub
    End Class
End Namespace
