Public Class Form1
    Private pointList As New List(Of Point)
    Private vertexList As New List(Of Line2D)
    Private clickRadius As Integer = 6
    Private selectedPoint As Point?
    Private isDragging As Boolean = False


    Private gridBitmap As Bitmap
    Private fullRepaintNeeded As Boolean = True

    Private selectionStart As Point? = Nothing
    Private selectionEnd As Point? = Nothing
    Private lastSelectionRect As Rectangle = Rectangle.Empty
    Private WithEvents selectionTimer As New Timer With {.Interval = 100} ' 100 ms delay
    Private WithEvents dragDelayTimer As New Timer With {.Interval = 150} ' 150 ms delay

    Private dashOffset As Single = 0.0F

    Private WithEvents myContextMenu As ContextMenuStrip

    Private Function CanAddNewPoint(location As Point) As Boolean
        If isDragging = True Then
            Return False
        End If
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
        SetStyle(ControlStyles.DoubleBuffer, True)
        SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
        SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        SetStyle(ControlStyles.UserPaint, True)
        UpdateStyles()

        DrawGrid()

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

    Private Sub selectionTimer_Tick(sender As Object, e As EventArgs) Handles selectionTimer.Tick
        dashOffset = (dashOffset + 1) Mod 100 ' Adjust for desired speed and pattern
        Dim updateRect = lastSelectionRect
        updateRect.Inflate(1, 1) ' Slightly inflate to cover antialiasing artifacts
        Panel1.Invalidate(updateRect)
    End Sub

    Private Sub dragDelayTimer_Tick(sender As Object, e As EventArgs) Handles dragDelayTimer.Tick
        isDragging = True
        selectionTimer.Start()
        dragDelayTimer.Stop() ' Stop the timer to prevent it from firing again
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
                    UpdateListBoxDataSource(ListBox2, vertexList)
                    ListBox1.SelectedIndex = -1
                ElseIf listBox.Equals(ListBox2) Then
                    ' Remove vertex
                    Dim vertexToRemove As Line2D = DirectCast(listBox.SelectedItem, Line2D)
                    vertexList.Remove(vertexToRemove)
                    UpdateListBoxDataSource(ListBox2, vertexList)
                    ListBox2.SelectedIndex = -1
                End If
                Panel1.Invalidate() ' Refresh the drawing panel
            End If
        End If
    End Sub

    Private Function FindNearestPoint(location As Point) As Point?
        Dim nearestPoint As Point? = Nothing

        For Each point In pointList
            Dim distance As Double = Math.Sqrt((point.X - location.X) ^ 2 + (point.Y - location.Y) ^ 2)
            If distance <= clickRadius Then
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
        If CanAddNewPoint(location) Then
            pointList.Add(location)
            UpdateListBoxDataSource(ListBox1, pointList)

            ' Invalidate only the area around the new point
            Panel1.Invalidate(New Rectangle(location.X - clickRadius - 3, location.Y - clickRadius - 3, 2 * (clickRadius + 3), 2 * (clickRadius + 3)))
        End If
    End Sub

    Private Sub RemovePoint()
        If selectedPoint.HasValue Then
            pointList.Remove(selectedPoint.Value)
            vertexList.RemoveAll(Function(line) line.StartPoint.Equals(selectedPoint.Value) OrElse line.EndPoint.Equals(selectedPoint.Value))
            selectedPoint = Nothing
            fullRepaintNeeded = True ' Set the flag to indicate a full repaint is needed

            UpdateListBoxDataSource(ListBox1, pointList)
            UpdateListBoxDataSource(ListBox2, vertexList)
            Panel1.Invalidate() ' Invalidate the whole panel
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

        ' Invalidate only the area around the new line
        Dim minX As Integer = Math.Min(firstPoint.X, secondPoint.X) - 1
        Dim minY As Integer = Math.Min(firstPoint.Y, secondPoint.Y) - 1
        Dim maxX As Integer = Math.Max(firstPoint.X, secondPoint.X) + 1
        Dim maxY As Integer = Math.Max(firstPoint.Y, secondPoint.Y) + 1
        Panel1.Invalidate(New Rectangle(minX, minY, maxX - minX, maxY - minY))
    End Sub

    Private Sub DrawGrid()
        If gridBitmap IsNot Nothing Then gridBitmap.Dispose()
        gridBitmap = New Bitmap(Panel1.Width, Panel1.Height)
        Using g As Graphics = Graphics.FromImage(gridBitmap), pen As New Pen(ColorTranslator.FromHtml("#3A3A3A"))
            ' Set the distance between grid lines
            Dim gridSize As Integer = 20
            ' Draw vertical lines
            For x As Integer = 0 To gridBitmap.Width Step gridSize
                g.DrawLine(pen, x, 0, x, gridBitmap.Height)
            Next
            ' Draw horizontal lines
            For y As Integer = 0 To gridBitmap.Height Step gridSize
                g.DrawLine(pen, 0, y, gridBitmap.Width, y)
            Next
        End Using
    End Sub

    Private Sub Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Panel1.Paint
        Panel1.Focus()
        ' Draw the corresponding part of the pre-rendered grid
        If gridBitmap IsNot Nothing Then
            ' This always draws the grid, but only in the invalidated region to improve performance.
            e.Graphics.DrawImage(gridBitmap, New Rectangle(e.ClipRectangle.X, e.ClipRectangle.Y, e.ClipRectangle.Width, e.ClipRectangle.Height), e.ClipRectangle, GraphicsUnit.Pixel)
        End If

        If selectionStart.HasValue AndAlso selectionEnd.HasValue Then
            Using p As New Pen(Color.White, 1) With {.DashPattern = New Single() {4, 4}, .DashOffset = dashOffset}
                e.Graphics.DrawRectangle(p, New Rectangle(selectionStart.Value, New Size(selectionEnd.Value.X - selectionStart.Value.X, selectionEnd.Value.Y - selectionStart.Value.Y)))
            End Using
        End If

        ' Draw points
        For Each point In pointList
            ' Check if the current point is the selected point
            If selectedPoint.HasValue AndAlso point.Equals(selectedPoint.Value) Then
                ' Draw the selected point in blue
                Using pen As New Pen(Color.Blue)
                    e.Graphics.DrawEllipse(pen, point.X - 3, point.Y - 3, 6, 6)
                End Using
            Else
                ' Draw non-selected points in white
                Using pen As New Pen(Color.White)
                    e.Graphics.DrawEllipse(pen, point.X - 3, point.Y - 3, 6, 6)
                End Using
            End If
        Next

        'Draw lines
        Using pen As New Pen(Color.Red)
            For Each line In vertexList
                ' This is a simplistic check; you might need a more accurate one based on line geometry
                Dim lineRect As New Rectangle(Math.Min(line.StartPoint.X, line.EndPoint.X) - 1, Math.Min(line.StartPoint.Y, line.EndPoint.Y) - 1, Math.Abs(line.StartPoint.X - line.EndPoint.X) + 2, Math.Abs(line.StartPoint.Y - line.EndPoint.Y) + 2)
                If e.ClipRectangle.IntersectsWith(lineRect) Then
                    e.Graphics.DrawLine(pen, line.StartPoint, line.EndPoint)
                End If
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

    Private Sub Panel1_MouseDown(sender As Object, e As MouseEventArgs) Handles Panel1.MouseDown
        If e.Button = MouseButtons.Left Then
            selectionStart = e.Location
            selectionEnd = Nothing ' Reset end point

            dragDelayTimer.Start()
        End If
    End Sub

    Private Sub Panel1_MouseMove(sender As Object, e As MouseEventArgs) Handles Panel1.MouseMove
        If e.Button = MouseButtons.Left AndAlso isDragging Then
            Dim newSelectionEnd = e.Location
            ' Calculate the new selection rectangle
            Dim newSelectionRect = GetSelectionRectangle(selectionStart.Value, newSelectionEnd)
            ' Calculate the area that needs to be redrawn
            Dim updateRect = Rectangle.Union(lastSelectionRect, newSelectionRect)
            ' Expand the update rectangle slightly to cover antialiasing artifacts
            updateRect.Inflate(1, 1)
            Panel1.Invalidate(updateRect)
            selectionEnd = newSelectionEnd
            lastSelectionRect = newSelectionRect
        End If
    End Sub

    Private Function GetSelectionRectangle(startPoint As Point, endPoint As Point) As Rectangle
        Debug.WriteLine(Math.Min(startPoint.X, endPoint.X))
        Return New Rectangle(
        Math.Min(startPoint.X, endPoint.X),
        Math.Min(startPoint.Y, endPoint.Y),
        Math.Abs(startPoint.X - endPoint.X),
        Math.Abs(startPoint.Y - endPoint.Y))
    End Function

    Private Sub Panel1_MouseUp(sender As Object, e As MouseEventArgs) Handles Panel1.MouseUp
        If e.Button = MouseButtons.Left Then
            dragDelayTimer.Stop()
            selectionTimer.Stop()
            ' Finalize dragging operation here
            isDragging = False
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

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click

    End Sub

    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click

    End Sub

    Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
        ResetLinkingMode()
        pointList.Clear()
        vertexList.Clear()
        selectionStart = Nothing
        selectionEnd = Nothing
        lastSelectionRect = Nothing
        UpdateListBoxDataSource(ListBox1, pointList)
        UpdateListBoxDataSource(ListBox2, vertexList)
        Panel1.Invalidate()
    End Sub
End Class
