Imports OpenTK
Imports System.Math
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL
Imports QuickFont

'The 3D view: an orbit camera around the car, and the draw order of a scene.
'
'World axes: Y up, Z towards the front of the car. Model files have Y pointing down, see PRM.Vert.
Module Render
    'One Re-Volt unit in world units (a car is about 2 world units long)
    Public Zoom As Single = 0.01

    'Camera. The angles are PRM.Phi (around the car) and PRM.Theta (height), in degrees
    Public CamDistance As Single = 2.0F
    Public CamPan As New Vector2(0, 0)

    ' Rendering control flags
    Public RENDER_SHADE As Boolean = True
    Public FORCE_DO_NOT_RENDER As Boolean = False ' Used to pause rendering during dialogs
    Public LIGHTS_ON As Boolean = False

    ' Performance metrics
    Public fps As Single, lag As String

    ''' <summary>One time setup of a GL control</summary>
    Public Sub initGL(ByVal Control As GLControl)
        If Control Is Nothing Then Throw New ArgumentNullException("Control", "GLControl cannot be null")
        Control.MakeCurrent()

        GL.ClearColor(Color.AliceBlue)
        GL.Enable(EnableCap.DepthTest)
        GL.DepthFunc(DepthFunction.Lequal)
        GL.Enable(EnableCap.Normalize)
        GL.ShadeModel(ShadingModel.Smooth)

        GL.Enable(EnableCap.ColorMaterial)
        GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse)
        GL.Light(LightName.Light0, LightParameter.Ambient, New Color4(0.55F, 0.55F, 0.55F, 1.0F))
        GL.Light(LightName.Light0, LightParameter.Diffuse, New Color4(0.75F, 0.75F, 0.75F, 1.0F))
        GL.Light(LightName.Light0, LightParameter.Specular, New Color4(0.0F, 0.0F, 0.0F, 1.0F))
        GL.Enable(EnableCap.Light0)

        TexLib.TexUtil.InitTexturing()

        Randomize()
    End Sub

    ''' <summary>Start a frame in this control: projection, camera, clean buffers</summary>
    Public Sub BeginFrame(ByVal Control As GLControl)
        Control.MakeCurrent()

        Dim w = Max(Control.ClientSize.Width, 1)
        Dim h = Max(Control.ClientSize.Height, 1)
        GL.Viewport(0, 0, w, h)

        Perspective = Matrix4.CreatePerspectiveFieldOfView(CSng(PI / 4), w / CSng(h), 0.05F, 200.0F)
        GL.MatrixMode(MatrixMode.Projection)
        GL.LoadMatrix(Perspective)

        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit)

        GL.MatrixMode(MatrixMode.Modelview)
        Dim pitch = PRM.Theta * PI / 180
        Dim yaw = PRM.Phi * PI / 180
        Dim dir As New Vector3(CSng(Cos(pitch) * Sin(yaw)), CSng(Sin(pitch)), CSng(Cos(pitch) * Cos(yaw)))
        Dim right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dir))
        Dim up = Vector3.Cross(dir, right)
        Dim target = right * CamPan.X + up * CamPan.Y
        Dim view = Matrix4.LookAt(target + dir * CamDistance, target, Vector3.UnitY)
        GL.LoadMatrix(view)

        'light from the camera's side
        GL.Light(LightName.Light0, LightParameter.Position, New Vector4(dir.X + 0.3F, dir.Y + 0.6F, dir.Z, 0))
        If LIGHTS_ON Then GL.Enable(EnableCap.Lighting) Else GL.Disable(EnableCap.Lighting)
    End Sub

    Public Sub FrameCar(ByVal c As Car)
        Dim radius = 0.0F
        If c.models.BODY IsNot Nothing Then
            With c.models.BODY.BoundingBox
                radius = Max(Max(Abs(.MinZ), Abs(.maxZ)), Max(Abs(.MinX), Abs(.maxX))) * 1.15F
            End With
        End If
        For i = 0 To 3
            If c.Theory.wheel(i).IsPresent Then
                radius = Max(radius, CSng(Sqrt(c.Theory.wheel(i).Offset(1).X ^ 2 + c.Theory.wheel(i).Offset(1).Z ^ 2) + c.Theory.wheel(i).Radius))
            End If
        Next
        If radius > 0 Then CamDistance = Max(1.0F, Min(150.0F, radius * Zoom * 2.8F))
        CamPan = Vector2.Zero
    End Sub

    Public Function CameraEye() As Vector3
        Dim pitch = PRM.Theta * PI / 180
        Dim yaw = PRM.Phi * PI / 180
        Return New Vector3(CSng(Cos(pitch) * Sin(yaw)), CSng(Sin(pitch)), CSng(Cos(pitch) * Cos(yaw))) * CamDistance
    End Function

    ''' <summary>Solid things first, then see-through ones, which must not hide what's behind them</summary>
    Public Sub DrawModels(ByVal list As IEnumerable(Of PRM))
        GL.Enable(EnableCap.Blend)
        GL.Enable(EnableCap.AlphaTest)
        GL.AlphaFunc(AlphaFunction.Greater, 0)

        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha)
        GL.DepthMask(True)
        For Each m In list
            m.Render(0)
        Next

        GL.DepthMask(False)
        For Each m In list
            m.Render(1)
        Next

        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One)
        For Each m In list
            m.Render(2)
        Next

        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha)
        GL.DepthMask(True)
    End Sub

    ''' <summary>Floor grid, to see how the car stands</summary>
    Public Sub DrawFloor(ByVal height As Single)
        GL.Disable(EnableCap.Lighting)
        GL.BindTexture(TextureTarget.Texture2D, 0)
        GL.Color4(0.55F, 0.62F, 0.72F, 0.6F)
        GL.Begin(BeginMode.Lines)
        For i = -10 To 10
            GL.Vertex3(i * 0.5F, height, -5.0F) : GL.Vertex3(i * 0.5F, height, 5.0F)
            GL.Vertex3(-5.0F, height, i * 0.5F) : GL.Vertex3(5.0F, height, i * 0.5F)
        Next
        GL.End()
        If LIGHTS_ON Then GL.Enable(EnableCap.Lighting)
    End Sub

    ''' <summary>Where the wheels touch the floor, in world units</summary>
    Public Function FloorHeight(ByVal c As Car) As Single
        Dim low = Single.MaxValue
        If c Is Nothing OrElse c.Theory Is Nothing Then Return -0.3F
        For i = 0 To 3
            If c.Theory.wheel(i).IsPresent Then low = Min(low, CSng((c.Theory.wheel(i).Offset(1).Y - c.Theory.wheel(i).Radius) * Zoom))
        Next
        If low = Single.MaxValue Then
            If c.models.BODY IsNot Nothing Then Return -c.models.BODY.BoundingBox.maxY * Zoom + c.models.BODY.Position.Y
            Return -0.3F
        End If
        Return low
    End Function

End Module
