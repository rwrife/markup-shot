namespace MarkupShot.Core;

public sealed class MarkupDocument
{
    public CanvasImage? BaseImage { get; private set; }

    public string? SourcePath { get; private set; }

    public bool HasImage => BaseImage is not null;

    public void SetImage(CanvasImage image, string? sourcePath = null)
    {
        BaseImage = image ?? throw new ArgumentNullException(nameof(image));
        SourcePath = sourcePath;
    }

    public void LoadFromFile(string path)
    {
        SetImage(CanvasImage.Load(path), path);
    }

    public void SaveCurrentImage(string path)
    {
        if (BaseImage is null)
        {
            throw new InvalidOperationException("No image loaded.");
        }

        BaseImage.Save(path);
    }
}
