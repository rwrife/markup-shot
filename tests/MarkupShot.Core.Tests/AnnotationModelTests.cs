using MarkupShot.Core;

namespace MarkupShot.Core.Tests;

public sealed class AnnotationModelTests
{
    [Fact]
    public void HitTesting_WorksForBaseShapes()
    {
        var rectangle = new RectangleAnnotation(new AnnotationRect(10, 10, 60, 30));
        var ellipse = new EllipseAnnotation(new AnnotationRect(100, 20, 50, 40));
        var line = new LineAnnotation(new AnnotationPoint(200, 10), new AnnotationPoint(260, 40));

        Assert.True(rectangle.HitTest(new AnnotationPoint(20, 20)));
        Assert.False(rectangle.HitTest(new AnnotationPoint(1, 1), tolerance: 0));

        Assert.True(ellipse.HitTest(new AnnotationPoint(125, 40)));
        Assert.False(ellipse.HitTest(new AnnotationPoint(160, 80), tolerance: 0));

        Assert.True(line.HitTest(new AnnotationPoint(230, 25), tolerance: 3));
        Assert.False(line.HitTest(new AnnotationPoint(230, 60), tolerance: 3));
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
    public void LineAnnotation_ResizesViaEndpointHandles()
    {
        var line = new LineAnnotation(new AnnotationPoint(20, 20), new AnnotationPoint(40, 40));

        var startHandle = line.HitTestHandle(new AnnotationPoint(20, 20));
        var endHandle = line.HitTestHandle(new AnnotationPoint(40, 40));

        Assert.Equal(AnnotationHandle.StartPoint, startHandle);
        Assert.Equal(AnnotationHandle.EndPoint, endHandle);

        line.Resize(startHandle, -10, 5);
        line.Resize(endHandle, 20, 0);

        Assert.Equal(new AnnotationPoint(10, 25), line.Start);
        Assert.Equal(new AnnotationPoint(60, 40), line.End);
    }

    [Fact]
    public void SerializeProject_RoundTripsAnnotationModel()
    {
        var document = new MarkupDocument();
        var rect = new RectangleAnnotation(Guid.NewGuid(), new AnnotationRect(5, 10, 20, 30), "#FFFF0000", 2d);
        var ellipse = new EllipseAnnotation(Guid.NewGuid(), new AnnotationRect(30, 40, 60, 20), "#FF00FF00", 3d);
        var line = new LineAnnotation(Guid.NewGuid(), new AnnotationPoint(1, 2), new AnnotationPoint(3, 4), "#FF0000FF", 4d);

        document.AddAnnotation(rect);
        document.AddAnnotation(ellipse);
        document.AddAnnotation(line);

        var json = document.SerializeProject();
        var restored = MarkupDocument.DeserializeProject(json);

        Assert.Equal(3, restored.Annotations.Count);
        Assert.Equal(AnnotationKind.Rectangle, restored.Annotations[0].Kind);
        Assert.Equal(AnnotationKind.Ellipse, restored.Annotations[1].Kind);
        Assert.Equal(AnnotationKind.Line, restored.Annotations[2].Kind);

        var restoredRect = Assert.IsType<RectangleAnnotation>(restored.Annotations[0]);
        var restoredEllipse = Assert.IsType<EllipseAnnotation>(restored.Annotations[1]);
        var restoredLine = Assert.IsType<LineAnnotation>(restored.Annotations[2]);

        Assert.Equal(rect.Bounds, restoredRect.Bounds);
        Assert.Equal(ellipse.Bounds, restoredEllipse.Bounds);
        Assert.Equal(line.Start, restoredLine.Start);
        Assert.Equal(line.End, restoredLine.End);
    }
}
