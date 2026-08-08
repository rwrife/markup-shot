namespace MarkupShot.Core;

public sealed class StepBadgeAnnotation : RectangularAnnotationBase
{
    public StepBadgeAnnotation(
        AnnotationRect bounds,
        int stepNumber,
        string strokeHex = "#FFFFFFFF",
        string fillHex = "#FFE53935",
        double strokeThickness = 2d)
        : this(Guid.NewGuid(), bounds, stepNumber, strokeHex, fillHex, strokeThickness)
    {
    }

    public StepBadgeAnnotation(
        Guid id,
        AnnotationRect bounds,
        int stepNumber,
        string strokeHex = "#FFFFFFFF",
        string fillHex = "#FFE53935",
        double strokeThickness = 2d)
        : base(id, AnnotationKind.StepBadge, NormalizeToSquare(bounds), strokeHex, strokeThickness)
    {
        StepNumber = Math.Max(1, stepNumber);
        FillHex = string.IsNullOrWhiteSpace(fillHex) ? "#FFE53935" : fillHex;
    }

    public int StepNumber { get; private set; }

    public string FillHex { get; private set; }

    public double Diameter => Math.Min(Bounds.Width, Bounds.Height);

    public override bool HitTest(AnnotationPoint point, double tolerance = 6d)
    {
        var center = Bounds.Center;
        var radius = Diameter / 2d + tolerance;
        return AnnotationMath.IsPointNear(point, center, radius);
    }

    public void SetStepNumber(int stepNumber)
    {
        StepNumber = Math.Max(1, stepNumber);
    }

    public void SetFill(string fillHex)
    {
        FillHex = string.IsNullOrWhiteSpace(fillHex) ? "#FFE53935" : fillHex;
    }

    public void SetDiameter(double diameter)
    {
        var size = Math.Max(18d, diameter);
        var center = Bounds.Center;
        SetBounds(new AnnotationRect(center.X - size / 2d, center.Y - size / 2d, size, size));
    }

    public override AnnotationSnapshot ToSnapshot()
    {
        var snapshot = base.ToSnapshot();
        return snapshot with
        {
            StepNumber = StepNumber,
            FillHex = FillHex
        };
    }

    private static AnnotationRect NormalizeToSquare(AnnotationRect bounds)
    {
        var normalized = bounds.Normalize();
        var size = Math.Max(18d, Math.Max(normalized.Width, normalized.Height));
        var center = normalized.Center;
        return new AnnotationRect(center.X - size / 2d, center.Y - size / 2d, size, size);
    }
}
