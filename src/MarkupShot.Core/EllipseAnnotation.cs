namespace MarkupShot.Core;

public sealed class EllipseAnnotation : RectangularAnnotationBase
{
    public EllipseAnnotation(AnnotationRect bounds, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
        : this(Guid.NewGuid(), bounds, strokeHex, strokeThickness)
    {
    }

    public EllipseAnnotation(Guid id, AnnotationRect bounds, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
        : base(id, AnnotationKind.Ellipse, bounds, strokeHex, strokeThickness)
    {
    }

    public override bool HitTest(AnnotationPoint point, double tolerance = 6d)
    {
        var inflated = Bounds.Inflate(tolerance);

        if (inflated.Width <= 0 || inflated.Height <= 0)
        {
            return false;
        }

        var radiusX = inflated.Width / 2d;
        var radiusY = inflated.Height / 2d;
        var center = inflated.Center;

        var normX = (point.X - center.X) / radiusX;
        var normY = (point.Y - center.Y) / radiusY;

        return normX * normX + normY * normY <= 1d;
    }
}
