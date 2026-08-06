namespace MarkupShot.Core;

public sealed class ArrowAnnotation : IAnnotation
{
    public ArrowAnnotation(AnnotationPoint start, AnnotationPoint end, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
        : this(Guid.NewGuid(), start, end, strokeHex, strokeThickness)
    {
    }

    public ArrowAnnotation(Guid id, AnnotationPoint start, AnnotationPoint end, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Start = start;
        End = end;
        StrokeHex = string.IsNullOrWhiteSpace(strokeHex) ? "#FFFF4F4F" : strokeHex;
        StrokeThickness = strokeThickness <= 0 ? 1d : strokeThickness;
    }

    public Guid Id { get; }

    public AnnotationKind Kind => AnnotationKind.Arrow;

    public AnnotationPoint Start { get; private set; }

    public AnnotationPoint End { get; private set; }

    public string StrokeHex { get; private set; }

    public double StrokeThickness { get; private set; }

    public AnnotationRect Bounds
    {
        get
        {
            var left = Math.Min(Start.X, End.X);
            var top = Math.Min(Start.Y, End.Y);
            var right = Math.Max(Start.X, End.X);
            var bottom = Math.Max(Start.Y, End.Y);
            return new AnnotationRect(left, top, right - left, bottom - top);
        }
    }

    public bool HitTest(AnnotationPoint point, double tolerance = 6d) =>
        AnnotationMath.DistanceToSegment(point, Start, End) <= tolerance;

    public AnnotationHandle HitTestHandle(AnnotationPoint point, double handleRadius = 6d)
    {
        if (AnnotationMath.IsPointNear(point, Start, handleRadius))
        {
            return AnnotationHandle.StartPoint;
        }

        if (AnnotationMath.IsPointNear(point, End, handleRadius))
        {
            return AnnotationHandle.EndPoint;
        }

        return AnnotationHandle.None;
    }

    public void MoveBy(double dx, double dy)
    {
        Start = new AnnotationPoint(Start.X + dx, Start.Y + dy);
        End = new AnnotationPoint(End.X + dx, End.Y + dy);
    }

    public void Resize(AnnotationHandle handle, double dx, double dy, double minimumSize = 4d)
    {
        switch (handle)
        {
            case AnnotationHandle.StartPoint:
                Start = new AnnotationPoint(Start.X + dx, Start.Y + dy);
                break;
            case AnnotationHandle.EndPoint:
                End = new AnnotationPoint(End.X + dx, End.Y + dy);
                break;
        }
    }

    public void SetStroke(string strokeHex, double strokeThickness)
    {
        StrokeHex = string.IsNullOrWhiteSpace(strokeHex) ? "#FFFF4F4F" : strokeHex;
        StrokeThickness = strokeThickness <= 0 ? 1d : strokeThickness;
    }

    public AnnotationSnapshot ToSnapshot() =>
        new()
        {
            Id = Id,
            Kind = Kind,
            X = Start.X,
            Y = Start.Y,
            X2 = End.X,
            Y2 = End.Y,
            StrokeHex = StrokeHex,
            StrokeThickness = StrokeThickness
        };
}
