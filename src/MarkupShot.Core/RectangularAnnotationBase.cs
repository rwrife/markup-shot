namespace MarkupShot.Core;

public abstract class RectangularAnnotationBase : IAnnotation
{
    private AnnotationRect _bounds;

    protected RectangularAnnotationBase(Guid id, AnnotationKind kind, AnnotationRect bounds, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Kind = kind;
        _bounds = bounds.Normalize();
        StrokeHex = string.IsNullOrWhiteSpace(strokeHex) ? "#FFFF4F4F" : strokeHex;
        StrokeThickness = strokeThickness <= 0 ? 1d : strokeThickness;
    }

    public Guid Id { get; }

    public AnnotationKind Kind { get; }

    public string StrokeHex { get; private set; }

    public double StrokeThickness { get; private set; }

    public AnnotationRect Bounds => _bounds;

    public abstract bool HitTest(AnnotationPoint point, double tolerance = 6d);

    public virtual AnnotationHandle HitTestHandle(AnnotationPoint point, double handleRadius = 6d)
    {
        foreach (var (handle, handlePoint) in GetHandlePoints())
        {
            if (AnnotationMath.IsPointNear(point, handlePoint, handleRadius))
            {
                return handle;
            }
        }

        return AnnotationHandle.None;
    }

    public void MoveBy(double dx, double dy)
    {
        _bounds = _bounds.MoveBy(dx, dy);
    }

    public void Resize(AnnotationHandle handle, double dx, double dy, double minimumSize = 4d)
    {
        _bounds = AnnotationMath.ResizeRect(_bounds, handle, dx, dy, minimumSize);
    }

    public virtual void SetStroke(string strokeHex, double strokeThickness)
    {
        StrokeHex = string.IsNullOrWhiteSpace(strokeHex) ? "#FFFF4F4F" : strokeHex;
        StrokeThickness = strokeThickness <= 0 ? 1d : strokeThickness;
    }

    protected IEnumerable<(AnnotationHandle Handle, AnnotationPoint Point)> GetHandlePoints()
    {
        var rect = _bounds;
        var center = rect.Center;

        yield return (AnnotationHandle.TopLeft, new AnnotationPoint(rect.Left, rect.Top));
        yield return (AnnotationHandle.Top, new AnnotationPoint(center.X, rect.Top));
        yield return (AnnotationHandle.TopRight, new AnnotationPoint(rect.Right, rect.Top));
        yield return (AnnotationHandle.Right, new AnnotationPoint(rect.Right, center.Y));
        yield return (AnnotationHandle.BottomRight, new AnnotationPoint(rect.Right, rect.Bottom));
        yield return (AnnotationHandle.Bottom, new AnnotationPoint(center.X, rect.Bottom));
        yield return (AnnotationHandle.BottomLeft, new AnnotationPoint(rect.Left, rect.Bottom));
        yield return (AnnotationHandle.Left, new AnnotationPoint(rect.Left, center.Y));
    }

    public virtual AnnotationSnapshot ToSnapshot() =>
        new()
        {
            Id = Id,
            Kind = Kind,
            X = _bounds.X,
            Y = _bounds.Y,
            Width = _bounds.Width,
            Height = _bounds.Height,
            StrokeHex = StrokeHex,
            StrokeThickness = StrokeThickness
        };
}
