using System.IO;
using MarkupShot.Core;

namespace MarkupShot.Core.Tests;

public sealed class CanvasImageTests
{
    private static readonly byte[] MinimalPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D,
        0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x03,
        0x00, 0x00, 0x00, 0x02,
        0x08, 0x02, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
        0x49, 0x45, 0x4E, 0x44,
        0xAE, 0x42, 0x60, 0x82
    ];

    private static readonly byte[] MinimalJpeg =
    [
        0xFF, 0xD8,
        0xFF, 0xC0, 0x00, 0x11,
        0x08,
        0x00, 0x02,
        0x00, 0x03,
        0x03,
        0x01, 0x11, 0x00,
        0x02, 0x11, 0x00,
        0x03, 0x11, 0x00,
        0xFF, 0xD9
    ];

    [Fact]
    public void FromBytes_DetectsPngMetadata()
    {
        var image = CanvasImage.FromBytes(MinimalPng);

        Assert.Equal(ImageFileFormat.Png, image.Format);
        Assert.Equal(3, image.Width);
        Assert.Equal(2, image.Height);
    }

    [Fact]
    public void FromBytes_DetectsJpegMetadata()
    {
        var image = CanvasImage.FromBytes(MinimalJpeg);

        Assert.Equal(ImageFileFormat.Jpeg, image.Format);
        Assert.Equal(3, image.Width);
        Assert.Equal(2, image.Height);
    }

    [Fact]
    public void SaveCurrentImage_WritesImageBytes()
    {
        var document = new MarkupDocument();
        document.SetImage(CanvasImage.FromBytes(MinimalPng));

        var outputPath = Path.Combine(Path.GetTempPath(), $"markup-shot-{Guid.NewGuid():N}.png");

        try
        {
            document.SaveCurrentImage(outputPath);

            Assert.True(File.Exists(outputPath));
            Assert.Equal(MinimalPng, File.ReadAllBytes(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void FromBytes_ThrowsForUnsupportedPayload()
    {
        Assert.Throws<InvalidDataException>(() => CanvasImage.FromBytes([0x01, 0x02, 0x03]));
    }
}
