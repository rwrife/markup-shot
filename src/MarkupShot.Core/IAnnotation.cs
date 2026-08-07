namespace MarkupShot.Core;

public interface IAnnotation
{
    Guid Id { get; }

    AnnotationKind Kind { get; }

    AnnotationRect Bounds { get; }

    bool HitTest(AnnotationPoint point, double tolerance = 6d);

    AnnotationHandle HitTestHandle(AnnotationPoint point, double handleRadius = 6d);

    void MoveBy(double dx, double dy);

    void Resize(AnnotationHandle handle, double dx, double dy, double minimumSize = 4d);

    void SetStroke(string strokeHex, double strokeThickness);

    AnnotationSnapshot ToSnapshot();
}
