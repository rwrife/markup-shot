using MarkupShot.Core;

namespace MarkupShot.Core.Tests;

public sealed class ImageRedactionFilterTests
{
    [Fact]
    public void ApplyInPlace_Pixelate_ChangesOnlyTargetRegion()
    {
        const int width = 4;
        const int height = 4;
        const int stride = width * 4;

        var pixels = BuildGradientPixels(width, height);
        var original = pixels.ToArray();

        ImageRedactionFilter.ApplyInPlace(
            pixels,
            width,
            height,
            stride,
            new AnnotationRect(1, 1, 2, 2),
            RedactionMode.Pixelate);

        AssertPixel(pixels, width, 0, 0, expectedB: 0, expectedG: 0, expectedR: 0);
        AssertPixel(pixels, width, 3, 3, expectedB: 30, expectedG: 30, expectedR: 60);

        AssertPixel(pixels, width, 1, 1, expectedB: 15, expectedG: 15, expectedR: 30);
        AssertPixel(pixels, width, 2, 1, expectedB: 15, expectedG: 15, expectedR: 30);
        AssertPixel(pixels, width, 1, 2, expectedB: 15, expectedG: 15, expectedR: 30);
        AssertPixel(pixels, width, 2, 2, expectedB: 15, expectedG: 15, expectedR: 30);

        Assert.Equal(original.Length, pixels.Length);
    }

    [Fact]
    public void ApplyInPlace_Blur_IsDeterministic()
    {
        const int width = 4;
        const int height = 4;
        const int stride = width * 4;

        var first = BuildGradientPixels(width, height);
        var second = BuildGradientPixels(width, height);

        var region = new AnnotationRect(0, 0, width, height);
        ImageRedactionFilter.ApplyInPlace(first, width, height, stride, region, RedactionMode.Blur);
        ImageRedactionFilter.ApplyInPlace(second, width, height, stride, region, RedactionMode.Blur);

        Assert.Equal(first, second);
        AssertPixel(first, width, 0, 0, expectedB: 15, expectedG: 15, expectedR: 30);
        AssertPixel(first, width, 2, 3, expectedB: 15, expectedG: 15, expectedR: 30);
    }

    private static byte[] BuildGradientPixels(int width, int height)
    {
        var pixels = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width + x) * 4;
                pixels[index] = (byte)(x * 10); // B
                pixels[index + 1] = (byte)(y * 10); // G
                pixels[index + 2] = (byte)((x + y) * 10); // R
                pixels[index + 3] = 255; // A
            }
        }

        return pixels;
    }

    private static void AssertPixel(byte[] pixels, int width, int x, int y, byte expectedB, byte expectedG, byte expectedR)
    {
        var index = (y * width + x) * 4;
        Assert.Equal(expectedB, pixels[index]);
        Assert.Equal(expectedG, pixels[index + 1]);
        Assert.Equal(expectedR, pixels[index + 2]);
        Assert.Equal((byte)255, pixels[index + 3]);
    }
}
