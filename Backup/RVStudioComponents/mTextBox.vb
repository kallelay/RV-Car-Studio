Public Class mTextBox
    Public Overrides Property text() As String
        Get
            Return TextBox1.Text
        End Get
        Set(ByVal value$)
            TextBox1.Text = value
        End Set
    End Property
    Public Property Caption() As String
        Get
            Return TextBox1.Text
        End Get
        Set(ByVal value$)
            TextBox1.Text = value
        End Set
    End Property
    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox1.TextChanged

    End Sub
End Class
