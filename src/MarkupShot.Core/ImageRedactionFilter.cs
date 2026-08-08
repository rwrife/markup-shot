namespace MarkupShot.Core;

public static class ImageRedactionFilter
{
    public static void ApplyInPlace(
        byte[] pixels,
        int width,
        int height,
        int stride,
        AnnotationRect region,
        RedactionMode mode)
    {
        ArgumentNullException.ThrowIfNull(pixels);

        if (width <= 0 || height <= 0 || stride <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions and stride must be positive.");
        }

        if (pixels.Length < stride * height)
        {
            throw new ArgumentException("Pixel buffer is smaller than width/height/stride imply.", nameof(pixels));
        }

        var clipped = ClipRegion(region, width, height);
        if (clipped is null)
        {
            return;
        }

        switch (mode)
        {
            case RedactionMode.Blur:
                ApplyBoxBlurInPlace(pixels, width, height, stride, clipped.Value, radius: 4);
                break;
            case RedactionMode.Pixelate:
                ApplyPixelateInPlace(pixels, width, height, stride, clipped.Value, blockSize: 10);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported redaction mode.");
        }
    }

    private static void ApplyPixelateInPlace(byte[] pixels, int width, int height, int stride, PixelRegion region, int blockSize)
    {
        for (var blockY = region.Top; blockY < region.BottomExclusive; blockY += blockSize)
        {
            for (var blockX = region.Left; blockX < region.RightExclusive; blockX += blockSize)
            {
                var endY = Math.Min(region.BottomExclusive, blockY + blockSize);
                var endX = Math.Min(region.RightExclusive, blockX + blockSize);

                long sumB = 0;
                long sumG = 0;
                long sumR = 0;
                long sumA = 0;
                var count = 0;

                for (var y = blockY; y < endY; y++)
                {
                    var rowOffset = y * stride;
                    for (var x = blockX; x < endX; x++)
                    {
                        var index = rowOffset + x * 4;
                        sumB += pixels[index];
                        sumG += pixels[index + 1];
                        sumR += pixels[index + 2];
                        sumA += pixels[index + 3];
                        count++;
                    }
                }

                if (count == 0)
                {
                    continue;
                }

                var avgB = (byte)(sumB / count);
                var avgG = (byte)(sumG / count);
                var avgR = (byte)(sumR / count);
                var avgA = (byte)(sumA / count);

                for (var y = blockY; y < endY; y++)
                {
                    var rowOffset = y * stride;
                    for (var x = blockX; x < endX; x++)
                    {
                        var index = rowOffset + x * 4;
                        pixels[index] = avgB;
                        pixels[index + 1] = avgG;
                        pixels[index + 2] = avgR;
                        pixels[index + 3] = avgA;
                    }
                }
            }
        }
    }

    private static void ApplyBoxBlurInPlace(byte[] pixels, int width, int height, int stride, PixelRegion region, int radius)
    {
        var source = pixels.ToArray();

        for (var y = region.Top; y < region.BottomExclusive; y++)
        {
            for (var x = region.Left; x < region.RightExclusive; x++)
            {
                long sumB = 0;
                long sumG = 0;
                long sumR = 0;
                long sumA = 0;
                var count = 0;

                var minY = Math.Max(0, y - radius);
                var maxY = Math.Min(height - 1, y + radius);
                var minX = Math.Max(0, x - radius);
                var maxX = Math.Min(width - 1, x + radius);

                for (var ny = minY; ny <= maxY; ny++)
                {
                    var rowOffset = ny * stride;
                    for (var nx = minX; nx <= maxX; nx++)
                    {
                        var index = rowOffset + nx * 4;
                        sumB += source[index];
                        sumG += source[index + 1];
                        sumR += source[index + 2];
                        sumA += source[index + 3];
                        count++;
                    }
                }

                if (count == 0)
                {
                    continue;
                }

                var destIndex = y * stride + x * 4;
                pixels[destIndex] = (byte)(sumB / count);
                pixels[destIndex + 1] = (byte)(sumG / count);
                pixels[destIndex + 2] = (byte)(sumR / count);
                pixels[destIndex + 3] = (byte)(sumA / count);
            }
        }
    }

    private static PixelRegion? ClipRegion(AnnotationRect region, int width, int height)
    {
        var normalized = region.Normalize();
        var left = Math.Max(0, (int)Math.Floor(normalized.Left));
        var top = Math.Max(0, (int)Math.Floor(normalized.Top));
        var right = Math.Min(width, (int)Math.Ceiling(normalized.Right));
        var bottom = Math.Min(height, (int)Math.Ceiling(normalized.Bottom));

        if (right <= left || bottom <= top)
        {
            return null;
        }

        return new PixelRegion(left, top, right, bottom);
    }

    private readonly record struct PixelRegion(int Left, int Top, int RightExclusive, int BottomExclusive);
}
