Imports OpenTK.Graphics
Imports System.Diagnostics
Imports System.Drawing
Imports Img = System.Drawing.Imaging
Imports OpenTK.Graphics.OpenGL

''' The methods in this namespace assume you are familiar with the follow
''' texture concepts of OpenGL:
''' * their integer names
''' * their sizes (power of 2)
''' * the currently bound texture
''' * how pixels are colored based on the texture fragment + the current color
''' * how alpha blending works
''' If you are feeling unsure on any of these concepts, do some googling!
Namespace TexLib
    ''' <summary>
    ''' The TexUtil class is released under the MIT-license.
    ''' /Olof Bjarnason
    ''' </summary>
    Public NotInheritable Class TexUtil
        Private Sub New()
        End Sub
#Region "Public"

        ''' <summary>
        ''' Initialize OpenGL state to enable alpha-blended texturing.
        ''' Disable again with GL.Disable(EnableCap.Texture2D).
        ''' Call this before drawing any texture, when you boot your
        ''' application, eg. in OnLoad() of GameWindow or Form_Load()
        ''' if you're building a WinForm app.
        ''' </summary>
        Public Shared Sub InitTexturing()

            '


            GL.Enable(EnableCap.CullFace)
            '  GL.Disable(EnableCap.CullFace)

            GL.CullFace(CullFaceMode.Back)




            GL.Enable(EnableCap.Texture2D)

            GL.Enable(EnableCap.Blend)
            GL.Enable(EnableCap.AlphaTest)
         


            GL.AlphaFunc(AlphaFunction.Greater, 0)
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha)
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1)
        End Sub

        ''' <summary>
        ''' Create an opaque OpenGL texture object from a given byte-array of r,g,b-triplets.
        ''' Make sure width and height is 1, 2, .., 32, 64, 128, 256 and so on in size since all
        ''' 3d graphics cards support those dimensions. Not necessarily square. Don't forget
        ''' to call GL.DeleteTexture(int) when you don't need the texture anymore (eg. when switching
        ''' levels in your game).
        ''' </summary>
        Public Shared Function CreateRGBTexture(ByVal width As Integer, ByVal height As Integer, ByVal rgb As Byte()) As Integer
            Return CreateTexture(width, height, False, rgb)
        End Function

        ''' <summary>
        ''' Create a translucent OpenGL texture object from given byte-array of r,g,b,a-triplets.
        ''' See CreateRGBTexture for more info.
        ''' </summary>
        Public Shared Function CreateRGBATexture(ByVal width As Integer, ByVal height As Integer, ByVal rgba As Byte()) As Integer
            Return CreateTexture(width, height, True, rgba)
        End Function
        Public Shared Function getColorAvg(ByVal color As Color) As Integer
            Return Math.Sqrt(color.R ^ 2 + color.G ^ 2 + color.B ^ 2)
        End Function
        ''' <summary>
        ''' Create an OpenGL texture (translucent or opaque) from a given Bitmap.
        ''' 24- and 32-bit bitmaps supported.
        ''' </summary>
        Public Shared Function CreateTextureFromBitmap(ByVal bitmap As Bitmap) As Integer
            bitmap.MakeTransparent(Color.Black)

            'bitmap.RotateFlip(RotateFlipType.Rotate180FlipXY)

            Dim data As Img.BitmapData = bitmap.LockBits(New Rectangle(0, 0, bitmap.Width, bitmap.Height), Img.ImageLockMode.[ReadOnly], Img.PixelFormat.Format32bppArgb)
            Dim tex = GiveMeATexture()
            GL.BindTexture(TextureTarget.Texture2D, tex)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, _
             PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0)

            bitmap.UnlockBits(data)
            SetParameters()

            Return tex
        End Function

        ''' <summary>
        ''' Create an OpenGL texture (translucent or opaque) by loading a bitmap
        ''' from file. 24- and 32-bit bitmaps supported.
        ''' </summary>
        Public Shared Function CreateTextureFromFile(ByVal path As String) As Integer
            If IO.File.Exists(path) = False Then Return CreateTexture(1, 1, False, Nothing)
            Return CreateTextureFromBitmap(New Bitmap(Bitmap.FromFile(path)))
        End Function

        Public Shared Sub RemoveTexture(ByVal TextureID%)
            GL.DeleteTexture(TextureID)

        End Sub

#End Region

        Private Shared Function CreateTexture(ByVal width As Integer, ByVal height As Integer, ByVal alpha As Boolean, ByVal bytes As Byte()) As Integer
            Dim expectedBytes As Integer = width * height * (If(alpha, 4, 3))
            'Debug.Assert(expectedBytes = bytes.Length)
            Dim tex As Integer = GiveMeATexture()
            Upload(width, height, alpha, bytes)
            SetParameters()
            Return tex
        End Function

        Private Shared Function GiveMeATexture() As Integer
            Dim tex As Integer = GL.GenTexture()
            GL.BindTexture(TextureTarget.Texture2D, tex)
            Return tex
        End Function

        Private Shared Sub SetParameters()
            GL.Enable(EnableCap.Texture2D)

            GL.TexParameter(TextureTarget.ProxyTexture2D, TextureParameterName.GenerateMipmap, 1)

            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, TextureEnvMode.Modulate)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureDepth, 1.0F)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, TextureMinFilter.Nearest)
            ' GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, TextureMinFilter.Nearest)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear)
            ' GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, TextureWrapMode.ClampToEdge)
            ' GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, TextureWrapMode.ClampToEdge)




        End Sub

        Private Shared Sub Upload(ByVal width As Integer, ByVal height As Integer, ByVal alpha As Boolean, ByVal bytes As Byte())
            Dim internalFormat = If(alpha, PixelInternalFormat.Rgba, PixelInternalFormat.Rgb)
            Dim format = If(alpha, PixelFormat.Rgba, PixelFormat.Rgb)
            GL.TexImage2D(Of Byte)(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, _
             format, PixelType.UnsignedByte, bytes)
            '  GL.GenerateMipmap(GenerateMipmapTarget.Texture2D)

        End Sub
    End Class
End Namespace
