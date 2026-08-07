namespace MarkupShot.Core;

public sealed class HighlighterAnnotation : RectangularAnnotationBase
{
    public HighlighterAnnotation(
        AnnotationRect bounds,
        string strokeHex = "#FFFFFF00",
        double strokeThickness = 1d,
        double fillOpacity = 0.35d)
        : this(Guid.NewGuid(), bounds, strokeHex, strokeThickness, fillOpacity)
    {
    }

    public HighlighterAnnotation(
        Guid id,
        AnnotationRect bounds,
        string strokeHex = "#FFFFFF00",
        double strokeThickness = 1d,
        double fillOpacity = 0.35d)
        : base(id, AnnotationKind.Highlighter, bounds, strokeHex, strokeThickness)
    {
        FillOpacity = Math.Clamp(fillOpacity, 0.05d, 1d);
    }

    public double FillOpacity { get; private set; }

    public override bool HitTest(AnnotationPoint point, double tolerance = 6d) =>
        Bounds.Inflate(tolerance).Contains(point);

    public void SetFillOpacity(double fillOpacity)
    {
        FillOpacity = Math.Clamp(fillOpacity, 0.05d, 1d);
    }

    public override AnnotationSnapshot ToSnapshot()
    {
        var snapshot = base.ToSnapshot();
        return snapshot with
        {
            FillOpacity = FillOpacity
        };
    }
}
