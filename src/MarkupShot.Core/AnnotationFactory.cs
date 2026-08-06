namespace MarkupShot.Core;

public static class AnnotationFactory
{
    public static IAnnotation FromSnapshot(AnnotationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.Kind switch
        {
            AnnotationKind.Rectangle =>
                new RectangleAnnotation(
                    snapshot.Id,
                    new AnnotationRect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height),
                    snapshot.StrokeHex,
                    snapshot.StrokeThickness),

            AnnotationKind.Ellipse =>
                new EllipseAnnotation(
                    snapshot.Id,
                    new AnnotationRect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height),
                    snapshot.StrokeHex,
                    snapshot.StrokeThickness),

            AnnotationKind.Line =>
                new LineAnnotation(
                    snapshot.Id,
                    new AnnotationPoint(snapshot.X, snapshot.Y),
                    new AnnotationPoint(snapshot.X2, snapshot.Y2),
                    snapshot.StrokeHex,
                    snapshot.StrokeThickness),

            AnnotationKind.Arrow =>
                new ArrowAnnotation(
                    snapshot.Id,
                    new AnnotationPoint(snapshot.X, snapshot.Y),
                    new AnnotationPoint(snapshot.X2, snapshot.Y2),
                    snapshot.StrokeHex,
                    snapshot.StrokeThickness),

            AnnotationKind.Ink =>
                new InkAnnotation(
                    snapshot.Id,
                    snapshot.Points is { Count: > 0 }
                        ? snapshot.Points
                        : BuildFallbackInkPoints(snapshot),
                    snapshot.StrokeHex,
                    snapshot.StrokeThickness),

            AnnotationKind.Text =>
                new TextAnnotation(
                    snapshot.Id,
                    new AnnotationRect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height),
                    snapshot.Text ?? "Text",
                    snapshot.FontSize,
                    snapshot.StrokeHex,
                    snapshot.StrokeThickness),

            AnnotationKind.Highlighter =>
                new HighlighterAnnotation(
                    snapshot.Id,
                    new AnnotationRect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height),
                    snapshot.StrokeHex,
                    snapshot.StrokeThickness,
                    snapshot.FillOpacity),

            _ => throw new InvalidOperationException($"Unsupported annotation kind: {snapshot.Kind}")
        };
    }

    private static IReadOnlyList<AnnotationPoint> BuildFallbackInkPoints(AnnotationSnapshot snapshot)
    {
        var left = snapshot.X;
        var top = snapshot.Y;
        var right = snapshot.Width <= 0d ? snapshot.X2 : snapshot.X + snapshot.Width;
        var bottom = snapshot.Height <= 0d ? snapshot.Y2 : snapshot.Y + snapshot.Height;

        return
        [
            new AnnotationPoint(left, top),
            new AnnotationPoint((left + right) / 2d, (top + bottom) / 2d),
            new AnnotationPoint(right, bottom)
        ];
    }
}
