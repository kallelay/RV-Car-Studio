Imports System
Imports System.Drawing
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL

Public Module GraphicHelpers
    'Capture screenshot: code from OpenTK....
    '// Returns a System.Drawing.Bitmap with the contents of the current framebuffer
    Public Function GrabScreenshot() As Bitmap

        GL.Finish()

        'Return CarEditor.GlControl1.GrabScreenshot()
        Dim screenshot = New System.Drawing.Bitmap(412, 412, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        Dim graph = Graphics.FromImage(screenshot)
        graph.CopyFromScreen( _
        CarEditor.GlControl1.PointToScreen(New Point(63, 0)), New Point(0, 0), New Size(412, 412), CopyPixelOperation.SourceCopy)
        '  graph.ScaleTransform(256 / 412, 256 / 412)
        '   graph.DrawImage(screenshot, New Rectangle(0, 0, 256, 256), New Rectangle(0, 0, 412, 412), GraphicsUnit.Pixel)


        ' graph.Flush()
        graph.Save()
        Dim B As New Bitmap(256, 256)
        Dim g = Graphics.FromImage(B)
        g.DrawImage(screenshot, New Rectangle(0, 0, 256, 256), New Rectangle(0, 0, 412, 412), GraphicsUnit.Pixel)
        graph.Dispose()
        screenshot.Dispose()

        Return b




    End Function
End Module