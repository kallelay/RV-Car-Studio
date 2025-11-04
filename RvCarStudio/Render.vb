Imports OpenTK
Imports System.Math
Imports OpenTK.Graphics.OpenGL
Imports QuickFont
Imports System.Threading

Module Render
    ' Graphics engine state
    Public GlobalPosition As New Vector3(-0.85, 2.0, 0.25) 'Global Position
    Public Zoom As Single = 0.01

    ' Rendering control flags
    Public RENDER_SHADE As Boolean = True
    Public DO_NOT_RENDER As Boolean = False
    Public FORCE_DO_NOT_RENDER As Boolean = False ' Used to pause rendering during dialogs

    ' Performance metrics
    Public fps As Single, lag As String 


    ''' <summary>
    ''' Initialize OpenGL rendering context with proper state setup
    ''' </summary>
    ''' <param name="Control">The OpenTK GLControl to initialize</param>
    Public Sub initGL(ByVal Control As GLControl)
        If Control Is Nothing Then
            Throw New ArgumentNullException("Control", "GLControl cannot be null")
        End If

        ' Set clear color
        GL.ClearColor(Color.AliceBlue)

        ' Get viewport dimensions
        Dim Width As Integer = Control.Width
        Dim Height As Integer = Control.Height

        If Width <= 0 OrElse Height <= 0 Then
            Throw New InvalidOperationException("GLControl dimensions must be positive")
        End If

        GL.LoadIdentity()

        ' Enable depth testing for proper 3D rendering
        GL.Enable(EnableCap.DepthTest)
        GL.DepthFunc(DepthFunction.Lequal)

        ' Enable normal vector normalization (important for lighting)
        GL.Enable(EnableCap.Normalize)

        ' Enable scissor test for viewport clipping
        GL.Enable(EnableCap.ScissorTest)

        ' Enable alpha blending for transparency
        GL.Enable(EnableCap.Blend)

        ' Enable anti-aliasing for smoother lines and polygons
        GL.Enable(EnableCap.LineSmooth)
        GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest)
        GL.Enable(EnableCap.PolygonSmooth)
        GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest)

        ' Enable color material for per-vertex coloring
        GL.Enable(EnableCap.ColorMaterial)
        GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.Ambient)

        ' Disable multisampling (handled by anti-aliasing above)
        GL.Disable(EnableCap.Multisample)

        ' Initialize texture system
        TexLib.TexUtil.InitTexturing()

        ' Setup viewport and perspective projection
        GL.Viewport(0, 0, Width, Height)
        Perspective = OpenTK.Matrix4.CreatePerspectiveFieldOfView(PI / 4, Width / Height, 0.001F, 1000)
        GL.MatrixMode(MatrixMode.Projection)
        GL.LoadMatrix(Perspective)

        ' Initialize font configuration for QuickFont
        Dim config As New QFontBuilderConfiguration(True)

        ' Initialize random number generator
        Randomize()
    End Sub

End Module
