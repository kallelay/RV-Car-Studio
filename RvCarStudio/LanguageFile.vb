Module LanguageFile

    'Make different languages?

    Public Sub FindAllLanguages()
        'cleanup
        Languages.Clear()
        Config.ComboBox1.Items.Clear()

        'Prioritize English....
        'Dim AllLangs = IO.Directory.GetFiles(Application.StartupPath & "\data\lang\", "english.txt").Union(IO.Directory.GetFiles("data\lang\", "*.txt"))
        Dim AllLangs = (IO.Directory.GetFiles(Application.StartupPath & "\data\lang\", "*.txt"))

        For i = 0 To AllLangs.Count - 1
            AddLanguage(AllLangs(i)) 'add a new language

        Next

    End Sub
    ''' <summary>
    ''' Language Class
    ''' </summary>
    ''' <remarks></remarks>
    Public Class LANGUAGE
        Public Name As String = "Eigo"
        Public LPath As String
        Public FileString$

        Public FileSt As IO.FileStream
        Public StrReader As IO.StreamReader
        ''' <summary>
        ''' Requires Path of language file
        ''' </summary>
        ''' <param name="path">Path of Language file</param>
        ''' <remarks></remarks>
        Sub New(ByVal path$)
            'set path
            LPath = path
            FileSt = New IO.FileStream(path, IO.FileMode.Open)
            StrReader = New IO.StreamReader(FileSt)

            'set name
            Name = Replace(Split(path, "\").Last, ".txt", "")

            'add to languages
            Languages.Add(Me)
            Config.ComboBox1.Items.Add(Name)

            'start loading
            Me.LoadAll()
            'FileSt.F()


        End Sub
        ''' Load main section
        Sub LoadMain()
            FileSt.Position = 0
            Do Until InStr(StrReader.ReadLine(), "[MAIN]") > 0
                Application.DoEvents()
            Loop
            Dim Buffer$ = StrReader.ReadLine
            Do Until InStr(Buffer, "[") < 5 And Buffer.Length < 3

                Dim arg = Mid(Buffer, Len(Split(Buffer, "=")(0)) + 2)

                Select Case Split(Buffer, "=")(0)
                    Case "TITLE"
                        Config.Title.Text = arg
                    Case "LOADING"
                        SplashScreen.loading.Text = arg
                End Select
                Buffer$ = StrReader.ReadLine
                If Buffer Is Nothing Then Exit Do
            Loop
        End Sub
        ''' Load main section
        Sub LoadYESNO()
            FileSt.Position = 0
            Do Until InStr(StrReader.ReadLine(), "[YESNO]") > 0
                Application.DoEvents()
            Loop
            Dim Buffer$ = StrReader.ReadLine
            Do Until InStr(Buffer, "[") < 5 And Buffer.Length < 3

                Dim arg = Mid(Buffer, Len(Split(Buffer, "=")(0)) + 2)

                Select Case Split(Buffer, "=")(0)
                    Case "YES"
                        YESNO.Yes.Text = arg
                    Case "NO"
                        YESNO.No.Text = arg

                End Select
                Buffer$ = StrReader.ReadLine
                If Buffer Is Nothing Then Exit Do
            Loop
        End Sub
        '' Load Config section
        Sub LoadConfig()
            FileSt.Position = 0
            Do Until InStr(StrReader.ReadLine(), "[CONFIGURATION]") > 0
                Application.DoEvents()
            Loop

            Dim Buffer$ = StrReader.ReadLine
            Do Until InStr(Buffer, "[") < 5 And Buffer.Length < 3

                Dim arg = Mid(Buffer, Len(Split(Buffer, "=")(0)) + 2)

                Select Case UCase(Split(Buffer, "=")(0))
                    Case "MESSAGE", "Message"
                        Config.Label1.Text = arg
                    Case "NEXT"
                        Config.Button1.Text = arg
                    Case "PREVIOUS"
                        Config.Button2.Text = arg
                    Case "TRANSLATOR"
                        Config.trans.Text = arg
                End Select
                Buffer$ = StrReader.ReadLine
                If Buffer Is Nothing Then Exit Do

            Loop
        End Sub
        'Select folder
        Sub LoadSelectFolder()
            FileSt.Close()
            FileSt = New IO.FileStream(LPath, IO.FileMode.Open)
            StrReader = New IO.StreamReader(FileSt)

            Do Until InStr(StrReader.ReadLine(), "[SELECTFOLDER]") > 0
                Application.DoEvents()
            Loop

            Dim Buffer$ = StrReader.ReadLine
            Do Until InStr(Buffer, "[") < 5 And Buffer.Length < 3

                Dim arg = Mid(Buffer, Len(Split(Buffer, "=")(0)) + 2)

                Select Case UCase(Split(Buffer, "=")(0))
                    Case "MESSAGE", "Message"
                        Config.Label1.Text = arg
                    Case "SPECIAL FOLDERS"
                        Config.Label3.Text = arg
                    Case "DRIVE"
                        Config.Label2.Text = arg
                    Case "NEXT"
                        Config.Button1.Text = arg
                    Case "PREVIOUS"
                        Config.Button2.Text = arg
                End Select
                Buffer$ = StrReader.ReadLine
                If Buffer Is Nothing Then Exit Do

            Loop
        End Sub
        Sub LoadFinishConfig()
            FileSt.Close()
            FileSt = New IO.FileStream(LPath, IO.FileMode.Open)
            StrReader = New IO.StreamReader(FileSt)

            Do Until InStr(StrReader.ReadLine(), "[FINISHCONFIG]") > 0
                Application.DoEvents()
            Loop

            Dim Buffer$ = StrReader.ReadLine
            Do Until InStr(Buffer, "[") < 5 And Buffer.Length < 3

                Dim arg = Mid(Buffer, Len(Split(Buffer, "=")(0)) + 2)

                Select Case UCase(Split(Buffer, "=")(0))
                    Case "MESSAGE", "Message"
                        Config.Label1.Text = arg
                    Case "NEXT"
                        Config.Button1.Text = arg
                    Case "PREVIOUS"
                        Config.Button2.Text = arg
                End Select
                Buffer$ = StrReader.ReadLine
                If Buffer Is Nothing Then Exit Do

            Loop
        End Sub
        Sub LoadCarSelection()
            FileSt.Close()
            FileSt = New IO.FileStream(LPath, IO.FileMode.Open)
            StrReader = New IO.StreamReader(FileSt)

            Do Until InStr(StrReader.ReadLine(), "[CARSELECTION]") > 0
                Application.DoEvents()
            Loop

            Dim Buffer$ = StrReader.ReadLine
            Do Until InStr(Buffer, "[") < 5 And Buffer.Length < 3

                Dim arg = Mid(Buffer, Len(Split(Buffer, "=")(0)) + 2)


                For i = 0 To CarBrowser.PANEL1.Controls.Count - 1
                    If UCase(Split(Buffer, "=")(0)) = UCase(CarBrowser.PANEL1.Controls(i).Name) Then
                        CarBrowser.PANEL1.Controls(i).Text = arg
                        Exit For
                    End If
                Next

                For i = 0 To CarBrowser.Panel3.Controls.Count - 1
                    If UCase(Split(Buffer, "=")(0)) = UCase(CarBrowser.Panel3.Controls(i).Name) Then
                        CarBrowser.Panel3.Controls(i).Text = arg
                        Exit For
                    End If
                Next

                For i = 0 To CarBrowser.Panel6.Controls.Count - 1
                    If UCase(Split(Buffer, "=")(0)) = UCase(CarBrowser.Panel6.Controls(i).Name) Then
                        CarBrowser.Panel6.Controls(i).Text = arg
                        Exit For
                    End If
                Next

                For i = 0 To CarBrowser.Panel5.Controls.Count - 1
                    If UCase(Split(Buffer, "=")(0)) = UCase(CarBrowser.Panel5.Controls(i).Name) Then
                        CarBrowser.Panel5.Controls(i).Text = arg
                        Exit For
                    End If
                Next

                For i = 0 To CarBrowser.Controls.Count - 1
                    If UCase(Split(Buffer, "=")(0)) = UCase(CarBrowser.Controls(i).Name) Then
                        CarBrowser.Controls(i).Text = arg
                        Exit For
                    End If
                Next
                Buffer$ = StrReader.ReadLine
                If Buffer Is Nothing Then Exit Do

            Loop
        End Sub
        Sub LoadAll()
            LoadConfig()
            LoadMain()
            LoadYESNO()
            LoadCarSelection()
        End Sub

    End Class



    ''' <summary>
    ''' The languages
    ''' </summary>
    ''' <remarks></remarks>
    Public Languages As New List(Of LANGUAGE)


    ''' <summary>
    ''' procedure to add a new language
    ''' </summary>
    ''' <param name="path">Path of language file</param>
    ''' <remarks></remarks>
    Public Sub AddLanguage(ByVal path$)
        Dim X As New LANGUAGE(path)
    End Sub
End Module
