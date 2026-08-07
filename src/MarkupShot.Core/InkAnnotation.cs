namespace MarkupShot.Core;

public sealed class InkAnnotation : IAnnotation
{
    private readonly List<AnnotationPoint> _points;

    public InkAnnotation(IEnumerable<AnnotationPoint> points, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
        : this(Guid.NewGuid(), points, strokeHex, strokeThickness)
    {
    }

    public InkAnnotation(Guid id, IEnumerable<AnnotationPoint> points, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
    {
        ArgumentNullException.ThrowIfNull(points);

        _points = points.ToList();
        if (_points.Count == 0)
        {
            throw new ArgumentException("Ink annotation must contain at least one point.", nameof(points));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StrokeHex = string.IsNullOrWhiteSpace(strokeHex) ? "#FFFF4F4F" : strokeHex;
        StrokeThickness = strokeThickness <= 0 ? 1d : strokeThickness;
    }

    public Guid Id { get; }

    public AnnotationKind Kind => AnnotationKind.Ink;

    public IReadOnlyList<AnnotationPoint> Points => _points;

    public string StrokeHex { get; private set; }

    public double StrokeThickness { get; private set; }

    public AnnotationRect Bounds
    {
        get
        {
            var minX = _points.Min(point => point.X);
            var minY = _points.Min(point => point.Y);
            var maxX = _points.Max(point => point.X);
            var maxY = _points.Max(point => point.Y);
            return new AnnotationRect(minX, minY, maxX - minX, maxY - minY);
        }
    }

    public bool HitTest(AnnotationPoint point, double tolerance = 6d)
    {
        if (_points.Count == 1)
        {
            return AnnotationMath.IsPointNear(point, _points[0], tolerance);
        }

        var effectiveTolerance = tolerance + (StrokeThickness / 2d);

        for (var index = 0; index < _points.Count - 1; index++)
        {
            if (AnnotationMath.DistanceToSegment(point, _points[index], _points[index + 1]) <= effectiveTolerance)
            {
                return true;
            }
        }

        return false;
    }

    public AnnotationHandle HitTestHandle(AnnotationPoint point, double handleRadius = 6d)
    {
        var bounds = Bounds;
        var center = bounds.Center;

        var handles = new (AnnotationHandle Handle, AnnotationPoint Point)[]
        {
            (AnnotationHandle.TopLeft, new AnnotationPoint(bounds.Left, bounds.Top)),
            (AnnotationHandle.Top, new AnnotationPoint(center.X, bounds.Top)),
            (AnnotationHandle.TopRight, new AnnotationPoint(bounds.Right, bounds.Top)),
            (AnnotationHandle.Right, new AnnotationPoint(bounds.Right, center.Y)),
            (AnnotationHandle.BottomRight, new AnnotationPoint(bounds.Right, bounds.Bottom)),
            (AnnotationHandle.Bottom, new AnnotationPoint(center.X, bounds.Bottom)),
            (AnnotationHandle.BottomLeft, new AnnotationPoint(bounds.Left, bounds.Bottom)),
            (AnnotationHandle.Left, new AnnotationPoint(bounds.Left, center.Y))
        };

        foreach (var (handle, handlePoint) in handles)
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
        for (var index = 0; index < _points.Count; index++)
        {
            var point = _points[index];
            _points[index] = new AnnotationPoint(point.X + dx, point.Y + dy);
        }
    }

    public void Resize(AnnotationHandle handle, double dx, double dy, double minimumSize = 4d)
    {
        var sourceBounds = Bounds;
        var resizedBounds = AnnotationMath.ResizeRect(sourceBounds, handle, dx, dy, minimumSize);

        for (var index = 0; index < _points.Count; index++)
        {
            var point = _points[index];
            var normalizedX = sourceBounds.Width <= double.Epsilon
                ? 0.5d
                : (point.X - sourceBounds.Left) / sourceBounds.Width;
            var normalizedY = sourceBounds.Height <= double.Epsilon
                ? 0.5d
                : (point.Y - sourceBounds.Top) / sourceBounds.Height;

            var resizedX = resizedBounds.Left + normalizedX * resizedBounds.Width;
            var resizedY = resizedBounds.Top + normalizedY * resizedBounds.Height;

            if (sourceBounds.Width <= double.Epsilon)
            {
                resizedX = resizedBounds.Left + resizedBounds.Width / 2d;
            }

            if (sourceBounds.Height <= double.Epsilon)
            {
                resizedY = resizedBounds.Top + resizedBounds.Height / 2d;
            }

            _points[index] = new AnnotationPoint(resizedX, resizedY);
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
            StrokeHex = StrokeHex,
            StrokeThickness = StrokeThickness,
            Points = _points.ToList()
        };
}
