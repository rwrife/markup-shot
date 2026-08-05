using System.Buffers.Binary;

namespace MarkupShot.Core;

internal static class ImageMetadataReader
{
    private static readonly byte[] PngSignature =
    [
        0x89, 0x50, 0x4E, 0x47,
        0x0D, 0x0A, 0x1A, 0x0A
    ];

    public static bool TryRead(ReadOnlySpan<byte> data, out ImageMetadata metadata)
    {
        if (TryReadPng(data, out metadata))
        {
            return true;
        }

        if (TryReadJpeg(data, out metadata))
        {
            return true;
        }

        metadata = default;
        return false;
    }

    private static bool TryReadPng(ReadOnlySpan<byte> data, out ImageMetadata metadata)
    {
        metadata = default;

        if (data.Length < 24 || !data[..8].SequenceEqual(PngSignature))
        {
            return false;
        }

        var chunkType = data.Slice(12, 4);
        if (!chunkType.SequenceEqual("IHDR"u8))
        {
            return false;
        }

        var width = BinaryPrimitives.ReadInt32BigEndian(data.Slice(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(20, 4));

        if (width <= 0 || height <= 0)
        {
            return false;
        }

        metadata = new ImageMetadata(width, height, ImageFileFormat.Png);
        return true;
    }

    private static bool TryReadJpeg(ReadOnlySpan<byte> data, out ImageMetadata metadata)
    {
        metadata = default;

        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
        {
            return false;
        }

        var index = 2;

        while (index + 1 < data.Length)
        {
            if (data[index] != 0xFF)
            {
                index++;
                continue;
            }

            while (index < data.Length && data[index] == 0xFF)
            {
                index++;
            }

            if (index >= data.Length)
            {
                break;
            }

            var marker = data[index++];

            if (marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7) || marker == 0x01)
            {
                continue;
            }

            if (index + 1 >= data.Length)
            {
                break;
            }

            var segmentLength = (data[index] << 8) | data[index + 1];
            index += 2;

            if (segmentLength < 2 || index + segmentLength - 2 > data.Length)
            {
                break;
            }

            if (IsStartOfFrame(marker) && segmentLength >= 7)
            {
                var height = (data[index + 1] << 8) | data[index + 2];
                var width = (data[index + 3] << 8) | data[index + 4];

                if (width > 0 && height > 0)
                {
                    metadata = new ImageMetadata(width, height, ImageFileFormat.Jpeg);
                    return true;
                }
            }

            index += segmentLength - 2;
        }

        return false;
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker is 0xC0 or 0xC1 or 0xC2 or 0xC3
        or 0xC5 or 0xC6 or 0xC7
        or 0xC9 or 0xCA or 0xCB
        or 0xCD or 0xCE or 0xCF;
}
