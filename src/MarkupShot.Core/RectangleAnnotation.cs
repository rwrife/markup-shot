namespace MarkupShot.Core;

public sealed class RectangleAnnotation : RectangularAnnotationBase
{
    public RectangleAnnotation(AnnotationRect bounds, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
        : this(Guid.NewGuid(), bounds, strokeHex, strokeThickness)
    {
    }

    public RectangleAnnotation(Guid id, AnnotationRect bounds, string strokeHex = "#FFFF4F4F", double strokeThickness = 2d)
        : base(id, AnnotationKind.Rectangle, bounds, strokeHex, strokeThickness)
    {
    }

    public override bool HitTest(AnnotationPoint point, double tolerance = 6d) =>
        Bounds.Inflate(tolerance).Contains(point);
}
