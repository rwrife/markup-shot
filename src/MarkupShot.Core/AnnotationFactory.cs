namespace MarkupShot.Core;

public static class AnnotationFactory
{
    public static IAnnotation FromSnapshot(AnnotationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return snapshot.Kind switch
        {
            AnnotationKind.Rectangle => new RectangleAnnotation(snapshot.Id, new AnnotationRect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height), snapshot.StrokeHex, snapshot.StrokeThickness),
            AnnotationKind.Ellipse => new EllipseAnnotation(snapshot.Id, new AnnotationRect(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height), snapshot.StrokeHex, snapshot.StrokeThickness),
            AnnotationKind.Line => new LineAnnotation(snapshot.Id, new AnnotationPoint(snapshot.X, snapshot.Y), new AnnotationPoint(snapshot.X2, snapshot.Y2), snapshot.StrokeHex, snapshot.StrokeThickness),
            _ => throw new InvalidOperationException($"Unsupported annotation kind: {snapshot.Kind}")
        };
    }
}
