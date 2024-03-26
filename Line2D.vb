Public Class Line2D
    Public Property StartPoint As Point
    Public Property EndPoint As Point

    Public Sub New()
        ' Default constructor
    End Sub

    Public Sub New(ByVal start As Point, ByVal [end] As Point)
        Me.StartPoint = start
        Me.EndPoint = [end]
    End Sub

    Public Overrides Function ToString() As String
        Return $"({StartPoint.X}, {StartPoint.Y}) to ({EndPoint.X}, {EndPoint.Y})"
    End Function
End Class
