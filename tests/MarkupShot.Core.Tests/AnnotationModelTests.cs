using MarkupShot.Core;

namespace MarkupShot.Core.Tests;

public sealed class AnnotationModelTests
{
    [Fact]
    public void HitTesting_WorksForSupportedShapeTools()
    {
        var rectangle = new RectangleAnnotation(new AnnotationRect(10, 10, 60, 30));
        var ellipse = new EllipseAnnotation(new AnnotationRect(100, 20, 50, 40));
        var line = new LineAnnotation(new AnnotationPoint(200, 10), new AnnotationPoint(260, 40));
        var arrow = new ArrowAnnotation(new AnnotationPoint(20, 90), new AnnotationPoint(90, 90));
        var highlighter = new HighlighterAnnotation(new AnnotationRect(120, 90, 90, 24));

        Assert.True(rectangle.HitTest(new AnnotationPoint(20, 20)));
        Assert.False(rectangle.HitTest(new AnnotationPoint(1, 1), tolerance: 0));

        Assert.True(ellipse.HitTest(new AnnotationPoint(125, 40)));
        Assert.False(ellipse.HitTest(new AnnotationPoint(160, 80), tolerance: 0));

        Assert.True(line.HitTest(new AnnotationPoint(230, 25), tolerance: 3));
        Assert.False(line.HitTest(new AnnotationPoint(230, 60), tolerance: 3));

        Assert.True(arrow.HitTest(new AnnotationPoint(60, 91), tolerance: 3));
        Assert.False(arrow.HitTest(new AnnotationPoint(60, 130), tolerance: 3));

        Assert.True(highlighter.HitTest(new AnnotationPoint(140, 95)));
        Assert.False(highlighter.HitTest(new AnnotationPoint(90, 50), tolerance: 0));
    }

    [Fact]
    public void Document_SelectMoveResizeDelete_Works()
    {
        var document = new MarkupDocument();
        var rectangle = new RectangleAnnotation(new AnnotationRect(10, 10, 60, 30));
        document.AddAnnotation(rectangle);

        Assert.True(document.SelectByPoint(new AnnotationPoint(20, 20)));
        Assert.Equal(rectangle.Id, document.SelectedAnnotationId);

        Assert.True(document.MoveSelected(5, -5));
        Assert.Equal(new AnnotationRect(15, 5, 60, 30), rectangle.Bounds);

        var handle = rectangle.HitTestHandle(new AnnotationPoint(rectangle.Bounds.Right, rectangle.Bounds.Bottom));
        Assert.Equal(AnnotationHandle.BottomRight, handle);

        Assert.True(document.ResizeSelected(handle, 20, 10));
        Assert.Equal(new AnnotationRect(15, 5, 80, 40), rectangle.Bounds);

        Assert.True(document.RemoveAnnotation(rectangle.Id));
        Assert.Empty(document.Annotations);
        Assert.Null(document.SelectedAnnotationId);
    }

    [Fact]
    public void LineLikeTools_ResizeViaEndpointHandles()
    {
        var line = new LineAnnotation(new AnnotationPoint(20, 20), new AnnotationPoint(40, 40));
        var arrow = new ArrowAnnotation(new AnnotationPoint(60, 30), new AnnotationPoint(90, 45));

        var lineStartHandle = line.HitTestHandle(new AnnotationPoint(20, 20));
        var lineEndHandle = line.HitTestHandle(new AnnotationPoint(40, 40));
        var arrowStartHandle = arrow.HitTestHandle(new AnnotationPoint(60, 30));
        var arrowEndHandle = arrow.HitTestHandle(new AnnotationPoint(90, 45));

        Assert.Equal(AnnotationHandle.StartPoint, lineStartHandle);
        Assert.Equal(AnnotationHandle.EndPoint, lineEndHandle);
        Assert.Equal(AnnotationHandle.StartPoint, arrowStartHandle);
        Assert.Equal(AnnotationHandle.EndPoint, arrowEndHandle);

        line.Resize(lineStartHandle, -10, 5);
        line.Resize(lineEndHandle, 20, 0);

        arrow.Resize(arrowStartHandle, -5, 5);
        arrow.Resize(arrowEndHandle, 10, 10);

        Assert.Equal(new AnnotationPoint(10, 25), line.Start);
        Assert.Equal(new AnnotationPoint(60, 40), line.End);

        Assert.Equal(new AnnotationPoint(55, 35), arrow.Start);
        Assert.Equal(new AnnotationPoint(100, 55), arrow.End);
    }

    [Fact]
    public void TextAnnotation_SupportsMultilineTextAndFontSize()
    {
        var text = new TextAnnotation(new AnnotationRect(10, 10, 200, 80), "First line\nSecond line", fontSize: 18);

        Assert.True(text.HitTest(new AnnotationPoint(30, 30)));
        Assert.Equal(18d, text.FontSize);

        text.SetText("Updated\nMultiline\nText");
        text.SetFontSize(24d);

        Assert.Equal("Updated\nMultiline\nText", text.Text);
        Assert.Equal(24d, text.FontSize);
    }

    [Fact]
    public void InkAnnotation_SupportsHitMoveAndResize()
    {
        var ink = new InkAnnotation(
        [
            new AnnotationPoint(10, 10),
            new AnnotationPoint(20, 30),
            new AnnotationPoint(35, 15)
        ],
        strokeThickness: 3d);

        Assert.True(ink.HitTest(new AnnotationPoint(20, 22), tolerance: 5));
        Assert.False(ink.HitTest(new AnnotationPoint(90, 90), tolerance: 2));

        var originalBounds = ink.Bounds;
        ink.MoveBy(5, -5);

        Assert.Equal(originalBounds.MoveBy(5, -5), ink.Bounds);

        var handle = ink.HitTestHandle(new AnnotationPoint(ink.Bounds.Right, ink.Bounds.Bottom));
        Assert.NotEqual(AnnotationHandle.None, handle);

        ink.Resize(handle, 20, 10);
        Assert.True(ink.Bounds.Width >= originalBounds.Width);
        Assert.True(ink.Bounds.Height >= originalBounds.Height - 1);
    }

    [Fact]
    public void SerializeProject_RoundTripsAnnotationModel()
    {
        var document = new MarkupDocument();
        var rect = new RectangleAnnotation(Guid.NewGuid(), new AnnotationRect(5, 10, 20, 30), "#FFFF0000", 2d);
        var ellipse = new EllipseAnnotation(Guid.NewGuid(), new AnnotationRect(30, 40, 60, 20), "#FF00FF00", 3d);
        var line = new LineAnnotation(Guid.NewGuid(), new AnnotationPoint(1, 2), new AnnotationPoint(3, 4), "#FF0000FF", 4d);
        var arrow = new ArrowAnnotation(Guid.NewGuid(), new AnnotationPoint(8, 18), new AnnotationPoint(22, 35), "#FFAA00AA", 5d);
        var ink = new InkAnnotation(Guid.NewGuid(),
        [
            new AnnotationPoint(50, 52),
            new AnnotationPoint(60, 70),
            new AnnotationPoint(80, 68)
        ], "#FF00AAAA", 3d);
        var text = new TextAnnotation(Guid.NewGuid(), new AnnotationRect(120, 80, 160, 70), "Line 1\nLine 2", 20d, "#FFFFFFFF", 1d);
        var highlighter = new HighlighterAnnotation(Guid.NewGuid(), new AnnotationRect(20, 120, 200, 30), "#FFFFFF00", 1d, 0.4d);

        document.AddAnnotation(rect);
        document.AddAnnotation(ellipse);
        document.AddAnnotation(line);
        document.AddAnnotation(arrow);
        document.AddAnnotation(ink);
        document.AddAnnotation(text);
        document.AddAnnotation(highlighter);

        var json = document.SerializeProject();
        var restored = MarkupDocument.DeserializeProject(json);

        Assert.Equal(7, restored.Annotations.Count);
        Assert.Equal(AnnotationKind.Rectangle, restored.Annotations[0].Kind);
        Assert.Equal(AnnotationKind.Ellipse, restored.Annotations[1].Kind);
        Assert.Equal(AnnotationKind.Line, restored.Annotations[2].Kind);
        Assert.Equal(AnnotationKind.Arrow, restored.Annotations[3].Kind);
        Assert.Equal(AnnotationKind.Ink, restored.Annotations[4].Kind);
        Assert.Equal(AnnotationKind.Text, restored.Annotations[5].Kind);
        Assert.Equal(AnnotationKind.Highlighter, restored.Annotations[6].Kind);

        var restoredRect = Assert.IsType<RectangleAnnotation>(restored.Annotations[0]);
        var restoredEllipse = Assert.IsType<EllipseAnnotation>(restored.Annotations[1]);
        var restoredLine = Assert.IsType<LineAnnotation>(restored.Annotations[2]);
        var restoredArrow = Assert.IsType<ArrowAnnotation>(restored.Annotations[3]);
        var restoredInk = Assert.IsType<InkAnnotation>(restored.Annotations[4]);
        var restoredText = Assert.IsType<TextAnnotation>(restored.Annotations[5]);
        var restoredHighlighter = Assert.IsType<HighlighterAnnotation>(restored.Annotations[6]);

        Assert.Equal(rect.Bounds, restoredRect.Bounds);
        Assert.Equal(ellipse.Bounds, restoredEllipse.Bounds);
        Assert.Equal(line.Start, restoredLine.Start);
        Assert.Equal(line.End, restoredLine.End);
        Assert.Equal(arrow.Start, restoredArrow.Start);
        Assert.Equal(arrow.End, restoredArrow.End);
        Assert.Equal(ink.Points, restoredInk.Points);
        Assert.Equal(text.Text, restoredText.Text);
        Assert.Equal(text.FontSize, restoredText.FontSize);
        Assert.Equal(highlighter.FillOpacity, restoredHighlighter.FillOpacity, precision: 3);
    }
}
