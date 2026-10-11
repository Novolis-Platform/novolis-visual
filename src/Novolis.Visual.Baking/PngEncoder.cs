using System.Buffers.Binary;
using System.IO.Compression;

namespace Novolis.Visual.Baking;

internal static class PngEncoder
{
    private static readonly byte[] Signature =
        [137, 80, 78, 71, 13, 10, 26, 10];

    public static byte[] Encode(VisualBakeImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Pixels.Length != image.Width * image.Height)
        {
            throw new ArgumentException(
                "Pixel count must match image dimensions.",
                nameof(image));
        }

        using var output = new MemoryStream();
        output.Write(Signature);
        WriteChunk(output, "IHDR", CreateHeader(image.Width, image.Height));

        using var raw = new MemoryStream();
        for (var y = 0; y < image.Height; y++)
        {
            raw.WriteByte(0);
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                raw.WriteByte(pixel.R);
                raw.WriteByte(pixel.G);
                raw.WriteByte(pixel.B);
                raw.WriteByte(pixel.A);
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(
                   compressed,
                   CompressionLevel.SmallestSize,
                   leaveOpen: true))
        {
            raw.Position = 0;
            raw.CopyTo(zlib);
        }

        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static byte[] CreateHeader(int width, int height)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8;
        header[9] = 6;
        return header;
    }

    private static void WriteChunk(
        Stream destination,
        string type,
        byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        destination.Write(length);
        destination.Write(typeBytes);
        destination.Write(data);

        var crc = Crc32(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        destination.Write(crcBytes);
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        var crc = 0xffffffffu;
        foreach (var value in type.Concat(data))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xedb88320u
                    : crc >> 1;
            }
        }

        return ~crc;
    }
}
