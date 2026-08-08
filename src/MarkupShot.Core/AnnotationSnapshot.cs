namespace MarkupShot.Core;

public sealed record AnnotationSnapshot
{
    public required Guid Id { get; init; }

    public required AnnotationKind Kind { get; init; }

    public double X { get; init; }

    public double Y { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }

    public double X2 { get; init; }

    public double Y2 { get; init; }

    public string StrokeHex { get; init; } = "#FFFF4F4F";

    public double StrokeThickness { get; init; } = 2d;

    public string? Text { get; init; }

    public double FontSize { get; init; } = 18d;

    public double FillOpacity { get; init; } = 0.35d;

    public List<AnnotationPoint>? Points { get; init; }

    public RedactionMode RedactionMode { get; init; } = RedactionMode.Blur;

    public int StepNumber { get; init; } = 1;

    public string FillHex { get; init; } = "#FFE53935";
}
