Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL
Public Class GL_EXT
    'GL_Extensions 

    Public Shared Function supports(ByVal Str$) As Boolean
        Return GL.GetString(StringName.Extensions).Contains(Str)
    End Function
    Public Shared Function getVendor() As String
        Return GL.GetString(StringName.Vendor)
    End Function
    Public Shared Function getVer() As String
        Return GL.GetString(StringName.Version)
    End Function
    Public Shared Function getRenderer() As String
        Return GL.GetString(StringName.Renderer)
    End Function
End Class
