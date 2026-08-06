namespace MarkupShot.Core;

public sealed class TextAnnotation : RectangularAnnotationBase
{
    public TextAnnotation(
        AnnotationRect bounds,
        string text,
        double fontSize = 18d,
        string strokeHex = "#FFFFFFFF",
        double strokeThickness = 1d)
        : this(Guid.NewGuid(), bounds, text, fontSize, strokeHex, strokeThickness)
    {
    }

    public TextAnnotation(
        Guid id,
        AnnotationRect bounds,
        string text,
        double fontSize = 18d,
        string strokeHex = "#FFFFFFFF",
        double strokeThickness = 1d)
        : base(id, AnnotationKind.Text, bounds, strokeHex, strokeThickness)
    {
        Text = string.IsNullOrWhiteSpace(text) ? "Text" : text;
        FontSize = fontSize <= 0 ? 18d : fontSize;
    }

    public string Text { get; private set; }

    public double FontSize { get; private set; }

    public override bool HitTest(AnnotationPoint point, double tolerance = 6d) =>
        Bounds.Inflate(tolerance).Contains(point);

    public void SetText(string text)
    {
        Text = string.IsNullOrWhiteSpace(text) ? "Text" : text;
    }

    public void SetFontSize(double fontSize)
    {
        FontSize = fontSize <= 0 ? 18d : fontSize;
    }

    public override AnnotationSnapshot ToSnapshot()
    {
        var snapshot = base.ToSnapshot();
        return snapshot with
        {
            Text = Text,
            FontSize = FontSize
        };
    }
}
