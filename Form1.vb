Public Class Form1
    ' List to store all click locations and lines
    Private pointList As New List(Of Point)
    Private vertexList As New List(Of Line2D)
    Private clickRadius As Integer = 6
    Private selectedPoint As Point? ' Add this to keep track of the current right-clicked point

    ' Context menu for the form
    Private WithEvents myContextMenu As ContextMenuStrip

    Private Function CanAddNewPoint(location As Point) As Boolean
        For Each existingPoint In pointList
            Dim distance As Double = Math.Sqrt((existingPoint.X - location.X) ^ 2 + (existingPoint.Y - location.Y) ^ 2)
            If distance <= clickRadius Then
                ' A point is too close, so we cannot add the new point
                Return False
            End If
        Next
        ' No points are too close, so we can add the new point
        Return True
    End Function

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InitializeContextMenu()
        InitializeDataBindings()
        AddHandler ListBox1.KeyDown, AddressOf ListBox_KeyDown
        AddHandler ListBox2.KeyDown, AddressOf ListBox_KeyDown

    End Sub

    Private Sub InitializeContextMenu()
        myContextMenu = New ContextMenuStrip
        myContextMenu.Items.Add("Link Point") ' Assuming this is an existing functionality
        myContextMenu.Items.Add("Remove Point")
        ' Add more menu items as needed
    End Sub

    Private Sub InitializeDataBindings()
        ListBox1.DataSource = New BindingSource(pointList, Nothing)
        ListBox2.DataSource = New BindingSource(vertexList, Nothing)

        ListBox1.DisplayMember = "Display" ' Assuming Point has a 'Display' property or you need to format it
        ListBox2.DisplayMember = "Display" ' Assuming Line2D has a 'Display' property or you need to format it
    End Sub

    Private Sub UpdateListBoxDataSource(listBox As ListBox, dataSource As Object)
        listBox.DataSource = Nothing
        listBox.DataSource = New BindingSource(dataSource, Nothing)
        ' Optional: Update DisplayMember if your objects have a specific property to display
        listBox.DisplayMember = "Display"
    End Sub

    Private Sub ListBox_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Delete Then
            Dim listBox As ListBox = DirectCast(sender, ListBox)
            If listBox.SelectedItem IsNot Nothing Then
                If listBox.Equals(ListBox1) Then
                    ' Remove point
                    Dim pointToRemove As Point = DirectCast(listBox.SelectedItem, Point)
                    pointList.Remove(pointToRemove)
                    vertexList.RemoveAll(Function(line) line.StartPoint.Equals(pointToRemove) OrElse line.EndPoint.Equals(pointToRemove))
                    UpdateListBoxDataSource(ListBox1, pointList)
                ElseIf listBox.Equals(ListBox2) Then
                    ' Remove vertex
                    Dim vertexToRemove As Line2D = DirectCast(listBox.SelectedItem, Line2D)
                    vertexList.Remove(vertexToRemove)
                    UpdateListBoxDataSource(ListBox2, vertexList)
                End If
                Panel1.Invalidate() ' Refresh the drawing panel
            End If
        End If
    End Sub

    Private Function FindNearestPoint(location As Point) As Point?
        Dim nearestPoint As Point? = Nothing
        Dim minDistance As Double = Double.MaxValue

        For Each point In pointList
            Dim distance As Double = Math.Sqrt((point.X - location.X) ^ 2 + (point.Y - location.Y) ^ 2)
            If distance < minDistance And distance <= clickRadius Then
                minDistance = distance
                nearestPoint = point
            End If
        Next

        Return nearestPoint
    End Function

    Private Sub UpdateContextMenu(location As Point)
        myContextMenu.Items.Clear()

        Dim nearestPoint = FindNearestPoint(location)
        If nearestPoint.HasValue Then
            myContextMenu.Items.Add("Link Point")
            myContextMenu.Items.Add("Remove Point")
        Else
            myContextMenu.Items.Add($"Create point at {location.X}, {location.Y}")
        End If
    End Sub

    Private Sub AddPoint(location As Point)
        pointList.Add(location)

        UpdateListBoxDataSource(ListBox1, pointList)
        Panel1.Invalidate()
    End Sub

    Private Sub RemovePoint()
        If selectedPoint.HasValue Then
            pointList.Remove(selectedPoint.Value)
            vertexList.RemoveAll(Function(line) line.StartPoint.Equals(selectedPoint.Value) OrElse line.EndPoint.Equals(selectedPoint.Value))
            selectedPoint = Nothing

            UpdateListBoxDataSource(ListBox1, pointList)
            UpdateListBoxDataSource(ListBox2, vertexList)
            Panel1.Invalidate()
        End If
    End Sub

    Private Sub LinkPoint()
        If selectedPoint.HasValue Then
            Panel1.Tag = "Linking"
        End If
    End Sub

    Private Sub AddLine(firstPoint As Point, secondPoint As Point)
        vertexList.Add(New Line2D(firstPoint, secondPoint))

        UpdateListBoxDataSource(ListBox2, vertexList)
        Panel1.Invalidate()
    End Sub

    Private Sub Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Panel1.Paint
        Using pen As New Pen(Color.White)
            For Each point In pointList
                e.Graphics.DrawEllipse(pen, point.X - 3, point.Y - 3, 6, 6)
            Next
        End Using

        Using pen As New Pen(Color.Red)
            For Each line In vertexList
                e.Graphics.DrawLine(pen, line.StartPoint, line.EndPoint)
            Next
        End Using
    End Sub

    Private Sub Panel1_MouseClick(sender As Object, e As MouseEventArgs) Handles Panel1.MouseClick
        If e.Button = MouseButtons.Left Then
            HandleLeftClick(e.Location)
        ElseIf e.Button = MouseButtons.Right Then
            HandleRightClick(e.Location)
        End If
    End Sub

    Private Sub HandleLeftClick(location As Point)
        If Panel1.Tag = "Linking" And selectedPoint.HasValue Then
            ' Existing linking logic...
            LinkNewPoint(location)
        Else
            ' Use the CanAddNewPoint method to determine if a new point can be added
            If CanAddNewPoint(location) Then
                AddPoint(location)
            End If
        End If
    End Sub

    Private Sub LinkNewPoint(location As Point)
        Dim secondPoint As Point? = FindNearestPoint(location)
        If secondPoint.HasValue And Not secondPoint.Equals(selectedPoint) Then
            AddLine(selectedPoint.Value, secondPoint.Value)
        End If
        ResetLinkingMode()
    End Sub

    Private Sub ResetLinkingMode()
        Panel1.Tag = Nothing
        selectedPoint = Nothing
    End Sub

    Private Sub HandleRightClick(location As Point)
        Dim nearestPoint = FindNearestPoint(location)
        If nearestPoint.HasValue Then
            selectedPoint = nearestPoint
        End If
        UpdateContextMenu(location)
        myContextMenu.Show(Panel1, location)
    End Sub

    Private Sub ContextMenuItem_Click(sender As Object, e As ToolStripItemClickedEventArgs) Handles myContextMenu.ItemClicked
        Select Case e.ClickedItem.Text
            Case "Remove Point"
                RemovePoint()
            Case "Link Point"
                LinkPoint()
            Case Else
                If e.ClickedItem.Text.StartsWith("Create point at") Then
                    Dim coords As String() = e.ClickedItem.Text.Substring(16).Split(",")
                    Dim x As Integer = Int32.Parse(coords(0))
                    Dim y As Integer = Int32.Parse(coords(1).Trim())
                    Dim newPoint As New Point(x, y)
                    AddPoint(newPoint)
                End If
        End Select
    End Sub

End Class
