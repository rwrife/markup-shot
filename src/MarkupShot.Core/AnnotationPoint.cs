namespace MarkupShot.Core;

public readonly record struct AnnotationPoint(double X, double Y)
{
    public static AnnotationPoint operator +(AnnotationPoint point, AnnotationPoint offset) =>
        new(point.X + offset.X, point.Y + offset.Y);

    public static AnnotationPoint operator -(AnnotationPoint point, AnnotationPoint offset) =>
        new(point.X - offset.X, point.Y - offset.Y);
}
