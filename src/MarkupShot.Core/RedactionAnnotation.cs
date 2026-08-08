namespace MarkupShot.Core;

public sealed class RedactionAnnotation : RectangularAnnotationBase
{
    public RedactionAnnotation(
        AnnotationRect bounds,
        RedactionMode mode = RedactionMode.Blur,
        string strokeHex = "#FFE53935",
        double strokeThickness = 1d)
        : this(Guid.NewGuid(), bounds, mode, strokeHex, strokeThickness)
    {
    }

    public RedactionAnnotation(
        Guid id,
        AnnotationRect bounds,
        RedactionMode mode = RedactionMode.Blur,
        string strokeHex = "#FFE53935",
        double strokeThickness = 1d)
        : base(id, AnnotationKind.Redaction, bounds, strokeHex, strokeThickness)
    {
        Mode = mode;
    }

    public RedactionMode Mode { get; private set; }

    public override bool HitTest(AnnotationPoint point, double tolerance = 6d) =>
        Bounds.Inflate(tolerance).Contains(point);

    public void SetMode(RedactionMode mode)
    {
        Mode = mode;
    }

    public override AnnotationSnapshot ToSnapshot()
    {
        var snapshot = base.ToSnapshot();
        return snapshot with
        {
            RedactionMode = Mode
        };
    }
}
