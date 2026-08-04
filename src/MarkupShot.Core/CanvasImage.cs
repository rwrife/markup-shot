using System.IO;

namespace MarkupShot.Core;

public sealed class CanvasImage
{
    private readonly byte[] _bytes;

    private CanvasImage(byte[] bytes, ImageMetadata metadata)
    {
        _bytes = bytes;
        Metadata = metadata;
    }

    public ImageMetadata Metadata { get; }

    public int Width => Metadata.Width;

    public int Height => Metadata.Height;

    public ImageFileFormat Format => Metadata.Format;

    public ReadOnlyMemory<byte> Bytes => _bytes;

    public static CanvasImage Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        var bytes = File.ReadAllBytes(path);
        return FromBytes(bytes);
    }

    public static CanvasImage FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length == 0)
        {
            throw new InvalidDataException("Image payload is empty.");
        }

        if (!ImageMetadataReader.TryRead(bytes, out var metadata))
        {
            throw new InvalidDataException("Unsupported or invalid image payload. Only PNG and JPEG are currently supported.");
        }

        return new CanvasImage(bytes.ToArray(), metadata);
    }

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be empty.", nameof(path));
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, _bytes);
    }
}
