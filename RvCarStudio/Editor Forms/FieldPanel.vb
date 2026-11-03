Imports OpenTK

''' <summary>
''' An editing page made in code: rows of label + value, connected to a place in the car.
''' Used by the modes that are plain lists of values (Aerial, AI, Camera...).
''' </summary>
Public Class FieldPanel
    Inherits Panel

    Private Loaders As New List(Of Action)
    Private RowY As Integer = 4
    Private Loading As Boolean = False
    Private WithEvents ReloadTimer As New Timer With {.Interval = 400}

    Const LabelW = 92
    Const RowH = 24
    Const FullW = 268

    Sub New()
        AutoScroll = True
        BackColor = Color.FromArgb(96, 96, 96)
        ForeColor = Color.White
        'under the mode list of the editor
        Size = New Size(292, 268)
        Location = New Point(6, 54)
        Font = New Font("Microsoft Sans Serif", 8.25F)
    End Sub

    ''' <summary>Fill all rows from the current car</summary>
    Public Sub LoadValues()
        Loading = True
        For Each l In Loaders
            l()
        Next
        Loading = False
    End Sub

    Private Sub Changed(ByVal reloadView As Boolean)
        If Loading Then Exit Sub
        If reloadView Then
            ReloadTimer.Stop()
            ReloadTimer.Start()
        End If
    End Sub
    Private Sub ReloadTimer_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles ReloadTimer.Tick
        ReloadTimer.Stop()
        If cars.Count > 0 Then CarBrowser.ReLoadOneCar()
    End Sub

    Private Function AddLabel(ByVal text As String, Optional ByVal w As Integer = LabelW) As Label
        Dim l As New Label With {.Text = text, .AutoSize = False, .Location = New Point(4, RowY + 3), .Size = New Size(w, RowH - 4), .BackColor = Color.Transparent}
        Controls.Add(l)
        Return l
    End Function

    Public Sub AddHeader(ByVal text As String)
        RowY += 4
        Dim l As New Label With {.Text = text, .AutoSize = False, .Location = New Point(2, RowY), .Size = New Size(FullW, RowH - 4), .BackColor = Color.Transparent, .Font = New Font(Font, FontStyle.Bold)}
        Controls.Add(l)
        RowY += RowH - 4
    End Sub

    Private Shared Function Clamp(ByVal v As Double, ByVal n As NumericUpDown) As Decimal
        If Double.IsNaN(v) OrElse Double.IsInfinity(v) Then Return n.Minimum
        Return Math.Max(n.Minimum, Math.Min(n.Maximum, CDec(Math.Max(-1.0E+20, Math.Min(1.0E+20, v)))))
    End Function

    Private Function NewNumber(ByVal x As Integer, ByVal w As Integer, ByVal min As Double, ByVal max As Double, ByVal decimals As Integer) As NumericUpDown
        Dim n As New NumericUpDown With {.Location = New Point(x, RowY), .Size = New Size(w, 20), .Minimum = CDec(min), .Maximum = CDec(max), .DecimalPlaces = decimals, .Increment = If(decimals = 0, 1D, CDec(Math.Pow(10, 1 - decimals)))}
        Controls.Add(n)
        Return n
    End Function

    Public Sub AddNumber(ByVal text As String, ByVal getter As Func(Of Double), ByVal setter As Action(Of Double), Optional ByVal min As Double = -1000000, Optional ByVal max As Double = 1000000, Optional ByVal decimals As Integer = 3, Optional ByVal reloadView As Boolean = False)
        AddLabel(text)
        Dim n = NewNumber(LabelW + 6, 100, min, max, decimals)
        Loaders.Add(Sub() n.Value = Clamp(getter(), n))
        AddHandler n.ValueChanged, Sub()
                                       If Loading Then Exit Sub
                                       setter(CDbl(n.Value))
                                       Changed(reloadView)
                                   End Sub
        RowY += RowH
    End Sub

    Public Sub AddVector(ByVal text As String, ByVal getter As Func(Of Vector3d), ByVal setter As Action(Of Vector3d), Optional ByVal reloadView As Boolean = False)
        AddLabel(text, 60)
        Dim x = NewNumber(LabelW - 28, 64, -1000000, 1000000, 2)
        Dim y = NewNumber(LabelW + 38, 64, -1000000, 1000000, 2)
        Dim z = NewNumber(LabelW + 104, 64, -1000000, 1000000, 2)
        Loaders.Add(Sub()
                        Dim v = getter()
                        x.Value = Clamp(v.X, x) : y.Value = Clamp(v.Y, y) : z.Value = Clamp(v.Z, z)
                    End Sub)
        Dim onChange = Sub()
                           If Loading Then Exit Sub
                           setter(New Vector3d(CDbl(x.Value), CDbl(y.Value), CDbl(z.Value)))
                           Changed(reloadView)
                       End Sub
        AddHandler x.ValueChanged, onChange
        AddHandler y.ValueChanged, onChange
        AddHandler z.ValueChanged, onChange
        RowY += RowH
    End Sub

    Public Sub AddCheck(ByVal text As String, ByVal getter As Func(Of Boolean), ByVal setter As Action(Of Boolean), Optional ByVal reloadView As Boolean = False)
        Dim c As New CheckBox With {.Text = text, .Location = New Point(6, RowY), .Size = New Size(FullW - 8, RowH - 2), .BackColor = Color.Transparent}
        Controls.Add(c)
        Loaders.Add(Sub() c.Checked = getter())
        AddHandler c.CheckedChanged, Sub()
                                         If Loading Then Exit Sub
                                         setter(c.Checked)
                                         Changed(reloadView)
                                     End Sub
        RowY += RowH
    End Sub

    ''' <summary>A file of the game, as a path starting from the Re-Volt folder</summary>
    Public Sub AddPath(ByVal text As String, ByVal getter As Func(Of String), ByVal setter As Action(Of String), Optional ByVal filter As String = "All files|*.*", Optional ByVal reloadView As Boolean = False)
        AddLabel(text, 62)
        Dim t As New TextBox With {.Location = New Point(LabelW + 6 - 28, RowY), .Size = New Size(FullW - LabelW - 6, 20)}
        Dim b As New Button With {.Text = "...", .Location = New Point(FullW - 26, RowY - 1), .Size = New Size(26, 22), .BackColor = SystemColors.Control, .ForeColor = Color.Black}
        Controls.Add(t)
        Controls.Add(b)
        Loaders.Add(Sub() t.Text = getter())
        AddHandler t.TextChanged, Sub()
                                      If Loading Then Exit Sub
                                      setter(t.Text)
                                      Changed(reloadView)
                                  End Sub
        AddHandler b.Click, Sub()
                                Using d As New OpenFileDialog With {.Filter = filter, .InitialDirectory = cars(Active_Car).Path}
                                    If d.ShowDialog() <> DialogResult.OK Then Exit Sub
                                    If Not d.FileName.StartsWith(RVPATH & "\", StringComparison.OrdinalIgnoreCase) Then
                                        MsgBox("The file has to be inside the Re-Volt folder:" & vbNewLine & RVPATH, MsgBoxStyle.Exclamation)
                                        Exit Sub
                                    End If
                                    t.Text = d.FileName.Substring(RVPATH.Length + 1)
                                End Using
                            End Sub
        RowY += RowH
    End Sub

    Public Function AddTextBox(ByVal text As String, ByVal start As String, Optional ByVal lines As Integer = 1) As TextBox
        AddLabel(text, 62)
        Dim t As New TextBox With {.Location = New Point(LabelW + 6 - 28, RowY), .Size = New Size(FullW - LabelW + 22, 20 * lines), .Multiline = lines > 1, .ScrollBars = If(lines > 1, ScrollBars.Vertical, ScrollBars.None), .Text = start}
        Controls.Add(t)
        RowY += RowH + 18 * (lines - 1)
        Return t
    End Function

    Public Function AddChoice(ByVal text As String, ByVal items() As String) As ComboBox
        AddLabel(text, 62)
        Dim c As New ComboBox With {.Location = New Point(LabelW + 6 - 28, RowY), .Size = New Size(FullW - LabelW + 22, 20), .DropDownStyle = ComboBoxStyle.DropDownList}
        c.Items.AddRange(items)
        If items.Length > 0 Then c.SelectedIndex = 0
        Controls.Add(c)
        RowY += RowH
        Return c
    End Function

    Public Function AddButton(ByVal text As String, ByVal click As Action) As Button
        Dim b As New Button With {.Text = text, .Location = New Point(6, RowY), .Size = New Size(FullW - 8, 22), .BackColor = SystemColors.Control, .ForeColor = Color.Black}
        Controls.Add(b)
        AddHandler b.Click, Sub() click()
        RowY += RowH
        Return b
    End Function

    ''' <summary>Text worked out from the car each time the page opens</summary>
    Public Function AddInfo(ByVal getter As Func(Of String), Optional ByVal lines As Integer = 1) As Label
        Dim l As New Label With {.AutoSize = False, .Location = New Point(4, RowY), .Size = New Size(FullW, 15 * lines + 4), .BackColor = Color.Transparent}
        Controls.Add(l)
        Loaders.Add(Sub() l.Text = getter())
        RowY += 15 * lines + 6
        Return l
    End Function
End Class
