Imports OpenTK
Imports System.Math
Imports OpenTK.Graphics.OpenGL
Imports QuickFont
Imports System.Threading

Module Render
    Public GlobalPosition As New Vector3(-0.85, 2.0, 0.25) 'Global Position
    Public Zoom As Single = 0.01
    Public Const N_THREADS = 3
    Public Th() As Thread  'Rendering Threads

    Public RENDER_SHADE = True
    Public DO_NOT_RENDER = False
    Public FORCE_DO_NOT_RENDER = False
    Public DO_NOT_RENDER_ACCEPETED = False
    Public DO_NOT_THREAD = True 'use software rendering
    Public TH_STARTED = False

    Public fps As Single, lag$ 'For international ! 


    Public Sub initGL(ByVal Control As GLControl)
        'initializing openGL
        GL.ClearColor(Color.AliceBlue)


        Dim Width% = Control.Width
        Dim Height% = Control.Height

        '  GL.Viewport(New System.Drawing.Size(Width, Height))
        'glDepthRange(0.0001, 1280)

        GL.LoadIdentity()

        GL.Enable(EnableCap.DepthTest)

        '  GL.Enable(EnableCap.DepthClamp)
        '  GL.Enable(EnableCap.Histogram)
        GL.Enable(EnableCap.Normalize)
        GL.Enable(EnableCap.ScissorTest)
        GL.Enable(EnableCap.Blend)

        'GL.DepthFunc(DepthFunction.Less)
        GL.DepthFunc(DepthFunction.Lequal)

        GL.Enable(EnableCap.LineSmooth)
        GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest)

        GL.Enable(EnableCap.PolygonSmooth)
        GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest)

        GL.Enable(EnableCap.ColorMaterial)
        GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.Ambient)


        GL.Disable(EnableCap.Multisample)


        TexLib.TexUtil.InitTexturing()


        'Resizing 
        GL.Viewport(0, 0, Width, Height)
        Perspective = OpenTK.Matrix4.CreatePerspectiveFieldOfView(PI / 4, Width / Height, 0.001F, 1000)
        GL.MatrixMode(MatrixMode.Projection)
        GL.LoadMatrix(Perspective)

        Dim config As New QFontBuilderConfiguration(True)

        ReDim Th(N_THREADS - 1)
        For i = 0 To N_THREADS - 1
            Th(i) = New Threading.Thread(AddressOf VOID )

        Next

        'FBO (openTK)
        'makeFBO()
        'Fonts
        Randomize() 'pick random color

        ' CarEditor.BackgroundWorker1.RunWorkerAsync() 'start rendering

      
    End Sub
    Dim MadeFBO = False 'buffer of textures
    ' Public mFont As QFont '(New Font("Microsoft Sans Serif", "20", FontStyle.Regular, GraphicsUnit.Point, New Byte))
    Sub VOID()

    End Sub
    Public ColorTexture, DepthTexture, FBOHandle As UInt32

    Sub makeFBO()
        If MadeFBO = False Then
            ' // Create Color Tex
            GL.GenTextures(1, ColorTexture)
            GL.BindTexture(TextureTarget.Texture2D, ColorTexture)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, 512, 512, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero)

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, TextureMinFilter.Linear)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, TextureWrapMode.ClampToBorder)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, TextureWrapMode.ClampToBorder)
            '         // GL.Ext.GenerateMipmap( GenerateMipmapTarget.Texture2D );

            '        // Create Depth Tex
            GL.GenTextures(1, DepthTexture)
            GL.BindTexture(TextureTarget.Texture2D, DepthTexture)
            GL.TexImage2D(TextureTarget.Texture2D, 0, All.DepthComponent32, 512, 512, 0, PixelFormat.DepthComponent, PixelType.UnsignedInt, IntPtr.Zero)
            ' // things go horribly wrong if DepthComponent's Bitcount does not match the main Framebuffer's Dept
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, TextureMinFilter.Linear)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, TextureWrapMode.ClampToBorder)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, TextureWrapMode.ClampToBorder)
            '  // GL.Ext.GenerateMipmap( GenerateMipmapTarget.Texture2D );

            ' // Create a FBO and attach the textures

            GL.Ext.GenFramebuffers(1, FBOHandle)
        End If
        MadeFBO = True
        GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, FBOHandle)
        GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, ColorTexture, 0)
        GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.DepthAttachmentExt, TextureTarget.Texture2D, DepthTexture, 0)

    End Sub

    Declare Auto Sub glDepthRange Lib "Opengl32.dll" Alias "glDepthRange" (ByVal Near As Double, ByVal far As Double)

    Public PlyToRender& = 100
    Public RemToRender& = 0

    Public Function Encode(ByVal nvex As Long, ByVal npoly As Long, _
                             ByVal vexs() As VERTEX, ByVal Polys() As POLY, _
                             ByVal texturei As Integer, _
                             ByVal MATRIX As Matrix4, ByVal RenderType As BeginMode, ByVal GlobalAlpha As Single, _
                            ByVal BeginFrom%, ByVal FinishTo%) As Object()


        Return New Object() {nvex, npoly, texturei, MATRIX, RenderType, GlobalAlpha, BeginFrom%, FinishTo%}
    End Function


    Public Sub CallMeOnce(ByVal npoly%)
        PlyToRender = npoly \ N_THREADS
        RemToRender = npoly Mod (PlyToRender * N_THREADS)
    End Sub




    Public Sub StartRendering(ByVal nvex As Long, ByVal npoly As Long, _
                              ByVal vexs() As VERTEX, ByVal Polys() As POLY, _
                              ByVal texturei As Integer, _
                              ByVal MATRIX As Matrix4, ByVal RenderType As BeginMode, ByVal GlobalAlpha As Single)
        '   
        For i = 0 To npoly

            If texturei <> -1 Then GL.BindTexture(TextureTarget.Texture2D, Textures(texturei)) Else GL.BindTexture(TextureTarget.Texture2D, -1)


            GL.PushMatrix()



            GL.MultMatrix(MATRIX)
            GL.Scale(Zoom, Zoom, Zoom)

            GL.Begin(RenderType)
            If RENDER_SHADE Then GL.Color4(Polys(i).c(0).ReturnBaked(GlobalAlpha))
            GL.TexCoord2(Polys(i).u0, Polys(i).v0)
            GL.Normal3(vexs(Polys(i).vi0).normal.X, vexs(Polys(i).vi0).normal.Y, vexs(Polys(i).vi0).normal.Z)
            GL.Vertex3(vexs(Polys(i).vi0).Position.X, -vexs(Polys(i).vi0).Position.Y, vexs(Polys(i).vi0).Position.Z)




            If RENDER_SHADE Then GL.Color4(Polys(i).c(2).ReturnBaked(GlobalAlpha))
            GL.TexCoord2(New Vector2d(Polys(i).u2, Polys(i).v2))
            GL.Normal3(vexs(Polys(i).vi2).normal.X, vexs(Polys(i).vi2).normal.Y, vexs(Polys(i).vi2).normal.Z)
            GL.Vertex3(vexs(Polys(i).vi2).Position.X, -vexs(Polys(i).vi2).Position.Y, vexs(Polys(i).vi2).Position.Z)



            If RENDER_SHADE Then GL.Color4(Polys(i).c(1).ReturnBaked(GlobalAlpha))
            GL.TexCoord2(New Vector2d(Polys(i).u1, Polys(i).v1))
            GL.Normal3(vexs(Polys(i).vi1).normal.X, vexs(Polys(i).vi1).normal.Y, vexs(Polys(i).vi1).normal.Z)
            GL.Vertex3(vexs(Polys(i).vi1).Position.X, -vexs(Polys(i).vi1).Position.Y, vexs(Polys(i).vi1).Position.Z)


            '   GoTo SkipDBLSD
            If (Polys(i).type And PolyType.DOUBLE_SIDED) Then


                If RENDER_SHADE Then GL.Color4(Polys(i).c(0).ReturnBaked(GlobalAlpha))
                GL.TexCoord2(Polys(i).u0, Polys(i).v0)
                GL.Normal3(vexs(Polys(i).vi0).normal.X, vexs(Polys(i).vi0).normal.Y, vexs(Polys(i).vi0).normal.Z)
                GL.Vertex3(vexs(Polys(i).vi0).Position.X, -vexs(Polys(i).vi0).Position.Y, vexs(Polys(i).vi0).Position.Z)


                If RENDER_SHADE Then GL.Color4(Polys(i).c(1).ReturnBaked(GlobalAlpha))
                GL.Normal3(vexs(Polys(i).vi1).normal.X, vexs(Polys(i).vi1).normal.Y, vexs(Polys(i).vi1).normal.Z)
                GL.TexCoord2(New Vector2d(Polys(i).u1, Polys(i).v1))
                GL.Vertex3(vexs(Polys(i).vi1).Position.X, -vexs(Polys(i).vi1).Position.Y, vexs(Polys(i).vi1).Position.Z)


                If RENDER_SHADE Then GL.Color4(Polys(i).c(2).ReturnBaked(GlobalAlpha))
                GL.TexCoord2(New Vector2d(Polys(i).u2, Polys(i).v2))
                GL.Normal3(vexs(Polys(i).vi2).normal.X, vexs(Polys(i).vi2).normal.Y, vexs(Polys(i).vi2).normal.Z)
                GL.Vertex3(vexs(Polys(i).vi2).Position.X, -vexs(Polys(i).vi2).Position.Y, vexs(Polys(i).vi2).Position.Z)



            End If

SkipDBLSD:

            If (Polys(i).type And PolyType.QUAD) Then


                If RENDER_SHADE Then GL.Color4(Polys(i).c(0).ReturnBaked(GlobalAlpha))
                GL.TexCoord2(Polys(i).u0, Polys(i).v0)
                GL.Normal3(vexs(Polys(i).vi0).normal.X, vexs(Polys(i).vi0).normal.Y, vexs(Polys(i).vi0).normal.Z)
                GL.Vertex3(vexs(Polys(i).vi0).Position.X, -vexs(Polys(i).vi0).Position.Y, vexs(Polys(i).vi0).Position.Z)




                If RENDER_SHADE Then GL.Color4(Polys(i).c(3).ReturnBaked(GlobalAlpha))
                GL.TexCoord2(Polys(i).u3, Polys(i).v3)
                GL.Normal3(vexs(Polys(i).vi3).normal.X, vexs(Polys(i).vi3).normal.Y, vexs(Polys(i).vi3).normal.Z)
                GL.Vertex3(vexs(Polys(i).vi3).Position.X, -vexs(Polys(i).vi3).Position.Y, vexs(Polys(i).vi3).Position.Z)


                If RENDER_SHADE Then GL.Color4(Polys(i).c(2).ReturnBaked(GlobalAlpha))
                GL.TexCoord2(Polys(i).u2, Polys(i).v2)
                GL.Normal3(vexs(Polys(i).vi2).normal.X, vexs(Polys(i).vi2).normal.Y, vexs(Polys(i).vi2).normal.Z)
                GL.Vertex3(vexs(Polys(i).vi2).Position.X, -vexs(Polys(i).vi2).Position.Y, vexs(Polys(i).vi2).Position.Z)




                '  GoTo SkipDBLSD_Q
                If Polys(i).type And PolyType.DOUBLE_SIDED Then

                    If RENDER_SHADE Then GL.Color4(Polys(i).c(0).ReturnBaked(GlobalAlpha))
                    GL.TexCoord2(Polys(i).u0, Polys(i).v0)
                    GL.Normal3(vexs(Polys(i).vi0).normal.X, vexs(Polys(i).vi0).normal.Y, vexs(Polys(i).vi0).normal.Z)
                    GL.Vertex3(vexs(Polys(i).vi0).Position.X, -vexs(Polys(i).vi0).Position.Y, vexs(Polys(i).vi0).Position.Z)





                    If RENDER_SHADE Then GL.Color4(Polys(i).c(2).ReturnBaked(GlobalAlpha))
                    GL.TexCoord2(Polys(i).u2, Polys(i).v2)
                    GL.Normal3(vexs(Polys(i).vi2).normal.X, vexs(Polys(i).vi2).normal.Y, vexs(Polys(i).vi2).normal.Z)
                    GL.Vertex3(vexs(Polys(i).vi2).Position.X, -vexs(Polys(i).vi2).Position.Y, vexs(Polys(i).vi2).Position.Z)


                    If RENDER_SHADE Then GL.Color4(Polys(i).c(3).ReturnBaked(GlobalAlpha))
                    GL.TexCoord2(Polys(i).u3, Polys(i).v3)
                    GL.Normal3(vexs(Polys(i).vi3).normal.X, vexs(Polys(i).vi3).normal.Y, vexs(Polys(i).vi3).normal.Z)
                    GL.Vertex3(vexs(Polys(i).vi3).Position.X, -vexs(Polys(i).vi3).Position.Y, vexs(Polys(i).vi3).Position.Z)


                End If
SkipDBLSD_Q:
            End If


            GL.End()
            GL.PopMatrix()
        Next

    End Sub

End Module
