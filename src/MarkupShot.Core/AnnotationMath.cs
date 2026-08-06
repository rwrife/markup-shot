namespace MarkupShot.Core;

internal static class AnnotationMath
{
    public static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    public static bool IsPointNear(AnnotationPoint point, AnnotationPoint target, double radius)
    {
        var dx = point.X - target.X;
        var dy = point.Y - target.Y;
        return dx * dx + dy * dy <= radius * radius;
    }

    public static double DistanceToSegment(AnnotationPoint point, AnnotationPoint start, AnnotationPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;

        if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
        {
            var px = point.X - start.X;
            var py = point.Y - start.Y;
            return Math.Sqrt(px * px + py * py);
        }

        var t = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / (dx * dx + dy * dy);
        t = Clamp(t, 0d, 1d);

        var projectedX = start.X + t * dx;
        var projectedY = start.Y + t * dy;

        var diffX = point.X - projectedX;
        var diffY = point.Y - projectedY;
        return Math.Sqrt(diffX * diffX + diffY * diffY);
    }

    public static AnnotationRect ResizeRect(AnnotationRect source, AnnotationHandle handle, double dx, double dy, double minimumSize)
    {
        var left = source.Left;
        var right = source.Right;
        var top = source.Top;
        var bottom = source.Bottom;

        switch (handle)
        {
            case AnnotationHandle.TopLeft:
                left += dx;
                top += dy;
                break;
            case AnnotationHandle.Top:
                top += dy;
                break;
            case AnnotationHandle.TopRight:
                right += dx;
                top += dy;
                break;
            case AnnotationHandle.Right:
                right += dx;
                break;
            case AnnotationHandle.BottomRight:
                right += dx;
                bottom += dy;
                break;
            case AnnotationHandle.Bottom:
                bottom += dy;
                break;
            case AnnotationHandle.BottomLeft:
                left += dx;
                bottom += dy;
                break;
            case AnnotationHandle.Left:
                left += dx;
                break;
            default:
                return source;
        }

        if (right < left)
        {
            (left, right) = (right, left);
        }

        if (bottom < top)
        {
            (top, bottom) = (bottom, top);
        }

        if (right - left < minimumSize)
        {
            var midpoint = (left + right) / 2d;
            left = midpoint - minimumSize / 2d;
            right = midpoint + minimumSize / 2d;
        }

        if (bottom - top < minimumSize)
        {
            var midpoint = (top + bottom) / 2d;
            top = midpoint - minimumSize / 2d;
            bottom = midpoint + minimumSize / 2d;
        }

        return new AnnotationRect(left, top, right - left, bottom - top);
    }
}
