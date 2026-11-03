Imports System.IO
Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL
Imports System.Math
Imports CarStudio.POLY


' ///////////////////////////////////
' //   PRM (Polygon Rendering Model) Class   //
' ///////////////////////////////////
' // First modification August'8th 2012
' // Latest              Dec'5th 2012
' // added TEXTURED     Dec'20th 2013
' // Modernized          2025
' // By theKDL
'
' This class handles loading, rendering, and manipulating Re-Volt PRM model files.
' PRM files contain polygon meshes with vertex positions, normals, colors, and texture coordinates.

'VOLTGL, modified engine  starts -->
Public Class PRM
    Public Directory As String
    Public FileName As String
    Public DirectoryName As String
    Public Shared TEXTURED = True 'shared now

    Public GlobalAlpha! = 1

    Public isVisible As Boolean = True  'Render or not to render
    Public Persistent As Boolean = False 'do never remove

    Public MyModel As New MODEL
    Public OtherLODs As Byte() = Nothing 'the rest of the file, after the first model
    Public TextureI As Integer = 0 'Texture Index

    Public RenderType As OpenGL.BeginMode = BeginMode.Triangles


    Private M As Matrix4 = Matrix4.Identity 'Matrix
    Private P As New Vector3
    Public BoundingBox As New BBOX

    Public RenderBBOX = False


    Dim scalefX! = 1
    Dim scalefY! = 1
    Dim scalefZ! = 1

    Dim Scalef!

    Public Shared Theta As Single = 0
    Public Shared Phi As Single = 20
    Private ownTheta As Single = 0
    Private ownPhi As Single = 0
    Private ownGamma = 0.0F
    Function Clone() As PRM
        Clone = New PRM("", True)
        Clone.Directory = Me.Directory
        Clone.FileName = FileName
        Clone.DirectoryName = DirectoryName
        Clone.isVisible = isVisible
        Clone.Persistent = Persistent
        Clone.MyModel.polynum = MyModel.polynum
        Clone.MyModel.vertnum = MyModel.vertnum
        Clone.OtherLODs = OtherLODs

        ReDim Clone.MyModel.polyl(MyModel.polynum)
        ReDim Clone.MyModel.vexl(MyModel.vertnum)

        For i = 0 To MyModel.polynum - 1
            Clone.MyModel.polyl(i) = New POLY
            ReDim Clone.MyModel.polyl(i).c(3)
            Clone.MyModel.polyl(i).c(0) = Me.MyModel.polyl(i).c(0).Clone
            Clone.MyModel.polyl(i).c(1) = Me.MyModel.polyl(i).c(1).Clone
            Clone.MyModel.polyl(i).c(2) = Me.MyModel.polyl(i).c(2).Clone
            Clone.MyModel.polyl(i).c(3) = Me.MyModel.polyl(i).c(3).Clone
            Clone.MyModel.polyl(i).Tpage = Me.MyModel.polyl(i).Tpage
            Clone.MyModel.polyl(i).type = Me.MyModel.polyl(i).type
            Clone.MyModel.polyl(i).u0 = Me.MyModel.polyl(i).u0
            Clone.MyModel.polyl(i).u1 = Me.MyModel.polyl(i).u1
            Clone.MyModel.polyl(i).u2 = Me.MyModel.polyl(i).u2
            Clone.MyModel.polyl(i).u3 = Me.MyModel.polyl(i).u3
            Clone.MyModel.polyl(i).v0 = Me.MyModel.polyl(i).v0
            Clone.MyModel.polyl(i).v1 = Me.MyModel.polyl(i).v1
            Clone.MyModel.polyl(i).v2 = Me.MyModel.polyl(i).v2
            Clone.MyModel.polyl(i).v3 = Me.MyModel.polyl(i).v3
            Clone.MyModel.polyl(i).vi0 = Me.MyModel.polyl(i).vi0
            Clone.MyModel.polyl(i).vi1 = Me.MyModel.polyl(i).vi1
            Clone.MyModel.polyl(i).vi2 = Me.MyModel.polyl(i).vi2
            Clone.MyModel.polyl(i).vi3 = Me.MyModel.polyl(i).vi3
        Next

        For i = 0 To MyModel.vertnum - 1
            Clone.MyModel.vexl(i) = New VERTEX
            Clone.MyModel.vexl(i).Position = New Vector3(MyModel.vexl(i).Position.X, MyModel.vexl(i).Position.Y, MyModel.vexl(i).Position.Z)
            Clone.MyModel.vexl(i).normal = New Vector3(MyModel.vexl(i).normal.X, MyModel.vexl(i).normal.Y, MyModel.vexl(i).normal.Z)

        Next

        Clone.TextureI = TextureI
        Clone.RenderType = BeginMode.Triangles
        Clone.M = M
        Clone.P = P
        ' Clone.BoundingBox = BBOX
        Clone.RenderBBOX = False
        Clone.scalefX = scalefX
        Clone.scalefY = scalefY
        Clone.scalefZ = scalefZ
        Clone.ownPhi = ownPhi
        Clone.ownTheta = ownTheta



    End Function

    Public Property Rotation() As Vector3d
        Get
            Return New Vector3(ownTheta, ownPhi, ownGamma)
        End Get
        Set(ByVal value As Vector3d)
            ownTheta = value.X
            ownPhi = value.Y
            ownGamma = value.Z
            M *= Matrix4.CreateRotationY(ownPhi)
            M *= Matrix4.CreateRotationZ(ownTheta)
            M *= Matrix4.CreateRotationX(ownGamma)


        End Set
    End Property

    Public Property Scale() As Vector3
        Get
            Return New Vector3(scalefX, scalefY, scalefZ)
        End Get
        Set(ByVal v As Vector3)
            M *= Matrix4.Scale(v.X / scalefX, v.Y / scalefY, v.Z / scalefZ)
            scalefX = v.X
            scalefY = v.Y
            scalefZ = v.Z
        End Set
    End Property
    Public WriteOnly Property ScaleFloat() As Single

        Set(ByVal s As Single)
            M *= Matrix4.Scale(s ^ 3 / scalefX / scalefY / scalefZ)
            scalefX = s
            scalefY = s
            scalefZ = s
        End Set
    End Property



    Public Property MATRIX() As Matrix4
        Get
            Return M
        End Get
        Set(ByVal value As Matrix4)
            M = value
        End Set
    End Property
    Public Property Position() As Vector3
        Get
            Return P
        End Get
        Set(ByVal value As Vector3)
            M *= Matrix4.CreateTranslation(value - P)
            P = value

        End Set

    End Property
    Public ReadOnly Property getCoM() As Vector3
        Get
            Dim p As New Vector3
            For i = 0 To Me.MyModel.vertnum - 1
                p += Me.MyModel.vexl(i).Position
            Next
            Return p / Me.MyModel.vertnum
        End Get
    End Property
    Public ReadOnly Property getQuadCoM() As Vector3
        Get
            Dim p As New Vector3
            For i = 0 To Me.MyModel.vertnum - 1
                p = New Vector3(Sqrt(p.X ^ 2 + Me.MyModel.vexl(i).Position.X ^ 2), _
                Sqrt(p.Y ^ 2 + Me.MyModel.vexl(i).Position.Y ^ 2), _
             0)
            Next
            Return (p / Me.MyModel.vertnum + getCoM) / 2 - New Vector3(0, 0, BoundingBox.maxZ * 2 + BoundingBox.MinZ) / 3
        End Get
    End Property


    Public Class BBOX
        Public MaxEdge, MinEdge As New Vector3

        Sub New()
            MaxEdge = New Vector3
            MinEdge = New Vector3
        End Sub
        Public ReadOnly Property Radius()
            Get
                Return (MaxEdge - MinEdge).Length
            End Get
        End Property
        Public ReadOnly Property MinX() As Single
            Get
                Return MinEdge.X
            End Get
        End Property
        Public ReadOnly Property MinY() As Single
            Get
                Return MinEdge.Y
            End Get
        End Property
        Public ReadOnly Property MinZ() As Single
            Get
                Return MinEdge.Z
            End Get
        End Property

        Public ReadOnly Property maxX() As Single
            Get
                Return MaxEdge.X
            End Get
        End Property
        Public ReadOnly Property maxY() As Single
            Get
                Return MaxEdge.Y
            End Get
        End Property
        Public ReadOnly Property maxZ() As Single
            Get
                Return MaxEdge.Z
            End Get
        End Property
        Public ReadOnly Property getCenter() As Vector3
            Get
                Return New Vector3((MinX + maxX) / 2, (MinY + maxY) / 2, (MinZ + maxZ) / 2)
            End Get
        End Property
        Public Sub FromMODEL_VERTEX_MORPH(ByVal MVM() As VERTEX)

            'init
            MaxEdge.X = MVM(0).Position.X
            MinEdge.X = MVM(0).Position.X
            MaxEdge.Y = MVM(0).Position.Y
            MinEdge.Y = MVM(0).Position.Y
            MaxEdge.Z = MVM(0).Position.Z
            MinEdge.Z = MVM(0).Position.Z

            For i = 1 To MVM.Count - 1
                MaxEdge.X = Max(MaxEdge.X, MVM(i).Position.X)
                MaxEdge.Y = Max(MaxEdge.Y, MVM(i).Position.Y)
                MaxEdge.Z = Max(MaxEdge.Z, MVM(i).Position.Z)

                MinEdge.X = Min(MinEdge.X, MVM(i).Position.X)
                MinEdge.Y = Min(MinEdge.Y, MVM(i).Position.Y)
                MinEdge.Z = Min(MinEdge.Z, MVM(i).Position.Z)
            Next

        End Sub
        Dim Clr = New OpenTK.Graphics.Color4(0.5!, 0.5!, 0.5!, 1) 'BBOX line color
        Dim ClrT = 0, CC
        Sub Render(ByVal Matrix As Matrix4, ByVal zoom!) 'render BBOX
            ' BBOX rendering disabled - can cause rendering artifacts with current GL state
            ' TODO: Re-enable when proper GL state isolation is implemented
            Exit Sub

            GL.PushMatrix()
            GL.MultMatrix(Matrix)
            GL.Scale(zoom, zoom, zoom)



            GL.LineWidth(2)

            GL.BindTexture(TextureTarget.Texture2D, -1)
            GL.Begin(BeginMode.LineStrip)


            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MinEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MaxEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MaxEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MinEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MinEdge.Y, MinEdge.Z)

            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MinEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MaxEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MaxEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MinEdge.Y, MinEdge.Z)



            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MinEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)

            GL.Vertex3(MaxEdge.X, -MinEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MinEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MinEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MinEdge.Y, MinEdge.Z)
            GL.End()
            GL.Begin(BeginMode.LineStrip)



            GL.Vertex3(MaxEdge.X, -MaxEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MinEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MinEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MaxEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MaxEdge.Y, MaxEdge.Z)

            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MaxEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MinEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MinEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MaxEdge.Y, MaxEdge.Z)



            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MaxEdge.Y, MinEdge.Z)
            GL.Color3(Clr)

            GL.Vertex3(MinEdge.X, -MaxEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MaxEdge.Y, MinEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MinEdge.X, -MaxEdge.Y, MaxEdge.Z)
            GL.Color3(Clr)
            GL.Vertex3(MaxEdge.X, -MaxEdge.Y, MaxEdge.Z)






            GL.End()

            GL.PopMatrix()


        End Sub
    End Class


    ''' <summary>
    ''' Creates a new PRM model by loading from a file
    ''' </summary>
    ''' <param name="filepath">Full path to the .prm file</param>
    ''' <param name="DoNotAdd">If true, don't add to global Models list</param>
    ''' <param name="DoNeverRemove">If true, mark as persistent (won't be removed from memory)</param>
    Sub New(ByVal filepath As String, Optional ByVal DoNotAdd As Boolean = False, Optional ByVal DoNeverRemove As Boolean = False)
        If String.IsNullOrEmpty(filepath) Then Exit Sub
        If Not IO.File.Exists(filepath) Then
            Console_.W("File doesn't exist (" & filepath & ")")
            Exit Sub
        End If

        Persistent = DoNeverRemove

        Try
            ' Open PRM file for reading
            Using J As New BinaryReader(New FileStream(Replace(filepath, Chr(34), ""), FileMode.Open, FileAccess.Read, FileShare.Read))

                'Vert/Poly count
                MyModel.polynum = J.ReadInt16()
                CarBrowser.PolyCount += MyModel.polynum
                MyModel.vertnum = J.ReadInt16()

                ReDim MyModel.polyl(MyModel.polynum)
                For i = 0 To MyModel.polynum - 1
                    MyModel.polyl(i).type = J.ReadInt16
                    MyModel.polyl(i).Tpage = J.ReadInt16

                    MyModel.polyl(i).vi0 = J.ReadInt16
                    MyModel.polyl(i).vi1 = J.ReadInt16
                    MyModel.polyl(i).vi2 = J.ReadInt16
                    MyModel.polyl(i).vi3 = J.ReadInt16

                    ReDim MyModel.polyl(i).c(3)
                    For k = 0 To 3
                        MyModel.polyl(i).c(k) = New BakedColor(J.ReadInt32, MyModel.polyl(i).type And PolyType.SEMI_TRANS)
                    Next

                    MyModel.polyl(i).u0 = J.ReadSingle
                    MyModel.polyl(i).v0 = J.ReadSingle
                    MyModel.polyl(i).u1 = J.ReadSingle
                    MyModel.polyl(i).v1 = J.ReadSingle
                    MyModel.polyl(i).u2 = J.ReadSingle
                    MyModel.polyl(i).v2 = J.ReadSingle
                    MyModel.polyl(i).u3 = J.ReadSingle
                    MyModel.polyl(i).v3 = J.ReadSingle
                Next

                ReDim MyModel.vexl(MyModel.vertnum)
                For a = 0 To MyModel.vertnum - 1
                    Dim x, y, z As Single
                    x = J.ReadSingle
                    y = J.ReadSingle
                    z = J.ReadSingle
                    MyModel.vexl(a).Position = New Vector3(x, y, z)

                    x = J.ReadSingle
                    y = J.ReadSingle
                    z = J.ReadSingle
                    MyModel.vexl(a).normal = New Vector3(x, y, z)
                Next
                If MyModel.vertnum > 0 Then BoundingBox.FromMODEL_VERTEX_MORPH(MyModel.vexl)

                'Files can hold more LODs after the first one. We only show the first, but must not lose the others
                Dim restLen = J.BaseStream.Length - J.BaseStream.Position
                If restLen > 0 Then OtherLODs = J.ReadBytes(CInt(restLen))
            End Using

            'let's set Directory and also Filename
            Me.FileName = filepath.Split("\")(UBound(filepath.Split("\")))
            Me.Directory = filepath.Substring(0, filepath.Length - Me.FileName.Length)
            Me.DirectoryName = filepath.Split("\")(UBound(filepath.Split("\")) - 1)
            If Persistent Then
                PermaModels.Add(Me)
            Else
                If Not DoNotAdd Then Models.Add(Me)
            End If
        Catch ex As Exception
            Console_.W("Error loading PRM file (" & filepath & "): " & ex.Message)
            Throw
        End Try
    End Sub
    'export (save prm)
    Sub Export()

        Export(Directory & FileName)
    End Sub

    'export(save prm)
    Sub Export(ByVal filepath As String)

        Dim J As New BinaryWriter(New FileStream(filepath, FileMode.Create))


        'Vert/Poly count
        J.Write(Convert.ToInt16(MyModel.polynum))
        J.Write(Convert.ToInt16(MyModel.vertnum))



        For i = 0 To MyModel.polynum - 1




            '
            J.Write(Convert.ToInt16(MyModel.polyl(i).type))
            J.Write(Convert.ToInt16(MyModel.polyl(i).Tpage))

            J.Write(Convert.ToInt16(MyModel.polyl(i).vi0))
            J.Write(Convert.ToInt16(MyModel.polyl(i).vi1))
            J.Write(Convert.ToInt16(MyModel.polyl(i).vi2))
            J.Write(Convert.ToInt16(MyModel.polyl(i).vi3))

            J.Write(Convert.ToInt32(MyModel.polyl(i).c(0).returnLong))
            J.Write(Convert.ToInt32(MyModel.polyl(i).c(1).returnLong))
            J.Write(Convert.ToInt32(MyModel.polyl(i).c(2).returnLong))
            J.Write(Convert.ToInt32(MyModel.polyl(i).c(3).returnLong))

            J.Write(CSng(MyModel.polyl(i).u0))
            J.Write(CSng(MyModel.polyl(i).v0))
            J.Write(CSng(MyModel.polyl(i).u1))
            J.Write(CSng(MyModel.polyl(i).v1))
            J.Write(CSng(MyModel.polyl(i).u2))
            J.Write(CSng(MyModel.polyl(i).v2))
            J.Write(CSng(MyModel.polyl(i).u3))
            J.Write(CSng(MyModel.polyl(i).v3))


        Next



        For a = 0 To MyModel.vertnum - 1


            J.Write(CSng(MyModel.vexl(a).Position.X))
            J.Write(CSng(MyModel.vexl(a).Position.Y))
            J.Write(CSng(MyModel.vexl(a).Position.Z))

            J.Write(CSng(MyModel.vexl(a).normal.X))
            J.Write(CSng(MyModel.vexl(a).normal.Y))
            J.Write(CSng(MyModel.vexl(a).normal.Z))



        Next

        If OtherLODs IsNot Nothing Then J.Write(OtherLODs)
        J.Close()

    End Sub

    ''' <summary>Draw everything at once. For a scene with several models use Render.DrawModels</summary>
    Sub Render()
        For pass = 0 To 2
            Render(pass)
        Next
    End Sub

    'Which pass a polygon belongs to: 0 opaque, 1 alpha blended, 2 additive
    Private Function PassOf(ByVal type As Short) As Integer
        If type And PolyType.ADDITIVE_TRANS Then Return 2
        If (type And PolyType.SEMI_TRANS) OrElse GlobalAlpha < 1.0F Then Return 1
        Return 0
    End Function

    ''' <summary>Draw this model's polygons of one pass. The pass' blending is set up by Render.DrawModels</summary>
    Sub Render(ByVal pass As Integer)
        If Not isVisible OrElse MyModel.polyl Is Nothing OrElse MyModel.vexl Is Nothing Then Exit Sub

        GL.PushMatrix()
        GL.Scale(-1, 1, 1)
        GL.MultMatrix(M)
        GL.Scale(Zoom, Zoom, Zoom)

        Dim mode = PolygonMode.Fill
        If RenderType = BeginMode.LineStrip OrElse RenderType = BeginMode.Lines Then mode = PolygonMode.Line
        If RenderType = BeginMode.Points Then mode = PolygonMode.Point
        GL.PolygonMode(MaterialFace.FrontAndBack, mode)

        Dim bound = -1
        Dim drawing = False
        For i = 0 To MyModel.polynum - 1
            Dim poly = MyModel.polyl(i)
            If PassOf(poly.type) <> pass Then Continue For

            Dim tex = If(TEXTURED AndAlso poly.Tpage <> -1, Textures(TextureI), 0)
            If tex <> bound Then
                If drawing Then GL.End() : drawing = False
                GL.BindTexture(TextureTarget.Texture2D, tex)
                bound = tex
            End If
            If Not drawing Then GL.Begin(BeginMode.Triangles) : drawing = True

            'Re-Volt polygons are clockwise seen from the front
            Dim quad = (poly.type And PolyType.QUAD) <> 0
            Vert(poly, 0) : Vert(poly, 2) : Vert(poly, 1)
            If quad Then Vert(poly, 0) : Vert(poly, 3) : Vert(poly, 2)
            If poly.type And PolyType.DOUBLE_SIDED Then
                Vert(poly, 0) : Vert(poly, 1) : Vert(poly, 2)
                If quad Then Vert(poly, 0) : Vert(poly, 2) : Vert(poly, 3)
            End If
        Next
        If drawing Then GL.End()

        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill)
        GL.PopMatrix()
    End Sub

    'One corner of a polygon
    Private Sub Vert(ByRef p As POLY, ByVal corner As Integer)
        Dim vi As Integer, u, v As Single
        Select Case corner
            Case 0 : vi = p.vi0 : u = p.u0 : v = p.v0
            Case 1 : vi = p.vi1 : u = p.u1 : v = p.v1
            Case 2 : vi = p.vi2 : u = p.u2 : v = p.v2
            Case Else : vi = p.vi3 : u = p.u3 : v = p.v3
        End Select
        If vi < 0 OrElse vi >= MyModel.vertnum Then Exit Sub 'broken model

        If RENDER_SHADE Then
            GL.Color4(p.c(corner).ReturnBaked(GlobalAlpha))
        Else
            GL.Color4(1.0F, 1.0F, 1.0F, GlobalAlpha)
        End If
        GL.TexCoord2(u, v)
        'file: Y down. The whole model is turned around X in Render, which keeps it from being mirrored
        Dim n = MyModel.vexl(vi).normal
        GL.Normal3(n.X, -n.Y, n.Z)
        Dim pos = MyModel.vexl(vi).Position
        GL.Vertex3(pos.X, -pos.Y, pos.Z)
    End Sub


End Class


'Structure

Public Structure POLY
    ' a polygon
    Dim type, Tpage As Int16
    Dim vi0, vi1, vi2, vi3 As Int16


    Public Class BakedColor
        Public Shared BaseColor As Color4 = Color4.White
        Private LongColor As Int32
        Private BakedColor As Color4
        Private AlphaEnabled = True
        Sub New(ByVal lColor As Int32, ByVal useAlpha As Boolean)
            AlphaEnabled = useAlpha
            LongColor = lColor
            BakedColor = UintToColor(lColor, useAlpha)
        End Sub
        Function ReturnBaked(ByVal GlobalAlpha As Single) As Color4
            ' GlobalAlpha should be in range 0.0 to 1.0
            ' Clamp alpha to prevent overflow
            If GlobalAlpha >= 1.0F Then Return BakedColor
            If GlobalAlpha <= 0.0F Then Return New Color4(BakedColor.R, BakedColor.G, BakedColor.B, 0.0F)

            ' Apply global alpha multiplier safely
            Dim finalAlpha As Single = Math.Max(0.0F, Math.Min(1.0F, BakedColor.A * GlobalAlpha))
            Dim bck As Color4 = New Color4(BakedColor.R, BakedColor.G, BakedColor.B, finalAlpha)

            Return bck
        End Function

        Function ReturnLong() As Int32
            Return LongColor
        End Function

        'Put a colour from the file back, as if it was never touched
        Sub SetLong(ByVal lColor As Int32)
            LongColor = lColor
            BakedColor = UintToColor(lColor, AlphaEnabled)
        End Sub

        Function Clone() As BakedColor
            Clone = New BakedColor(LongColor, AlphaEnabled)
        End Function

        '----------------- RIKO IMPORTED----------------
        Property Gray()
            Set(ByVal value)
                Dim g = Math.Max(0.0F, Math.Min(1.0F, CSng(value)))
                BakedColor = New Color4(g * BaseColor.R, g * BaseColor.G, g * BaseColor.B, BakedColor.A)
                LongColor = (CInt(BakedColor.A * 255) << 24) Or (CInt(BakedColor.R * 255) << 16) Or (CInt(BakedColor.G * 255) << 8) Or CInt(BakedColor.B * 255)
            End Set
            Get
                Return Nothing

            End Get
        End Property
    End Class
    Public c() As BakedColor

    Dim u0, v0, u1, v1, u2, v2, u3, v3 As Single
End Structure

Public Structure VERTEX
    Public Position As Vector3
    Public normal As Vector3
End Structure
Public Class Sphere
    Public Radius As Single
    Public center As New Vector3d
    Sub New(ByVal x As Single, ByVal y As Single, ByVal z As Single, ByVal radius As Single)
        center.X = x
        center.Y = y
        center.Z = z
        Me.Radius = radius
    End Sub
    Sub New(ByVal Center As Vector3d, ByVal radius As Single)
        Me.center = Center
        Me.Radius = radius
    End Sub
End Class



Public Class MODEL
    Public polynum, vertnum As Short
    Public polyl() As POLY
    Public vexl() As VERTEX


End Class


