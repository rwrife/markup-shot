namespace MarkupShot.Core;

public readonly record struct AnnotationRect(double X, double Y, double Width, double Height)
{
    public double Left => X;

    public double Top => Y;

    public double Right => X + Width;

    public double Bottom => Y + Height;

    public AnnotationPoint Center => new(X + Width / 2d, Y + Height / 2d);

    public bool Contains(AnnotationPoint point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    public AnnotationRect Inflate(double amount) =>
        new(X - amount, Y - amount, Width + amount * 2d, Height + amount * 2d);

    public AnnotationRect Normalize()
    {
        var left = Math.Min(Left, Right);
        var top = Math.Min(Top, Bottom);
        var right = Math.Max(Left, Right);
        var bottom = Math.Max(Top, Bottom);

        return new AnnotationRect(left, top, right - left, bottom - top);
    }

    public AnnotationRect MoveBy(double dx, double dy) =>
        new(X + dx, Y + dy, Width, Height);
}
