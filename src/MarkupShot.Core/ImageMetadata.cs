namespace MarkupShot.Core;

public readonly record struct ImageMetadata(
    int Width,
    int Height,
    ImageFileFormat Format);
