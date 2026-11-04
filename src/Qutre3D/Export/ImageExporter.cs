using System.IO.Compression;
using System.Text;

namespace Qutre3D.Export;

/// <summary>
/// Запись PNG на чистом C# (zlib + CRC32). Работает одинаково на Windows и Linux,
/// без System.Drawing.Common и нативных библиотек.
/// </summary>
public static class ImageExporter
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    /// <summary>Сохранить кадр RGBA (строки сверху вниз) в PNG без альфа-канала.</summary>
    public static void SavePng(byte[] rgba, int width, int height, string path)
    {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (rgba.Length < width * height * 4) throw new ArgumentException("Not enough pixel data.", nameof(rgba));

        string directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        using var file = File.Create(path);
        file.Write(Signature);

        var header = new byte[13];
        WriteUInt32(header, 0, (uint)width);
        WriteUInt32(header, 4, (uint)height);
        header[8] = 8;  // bit depth
        header[9] = 2;  // colour type: truecolour (RGB)
        header[10] = 0; // deflate
        header[11] = 0; // adaptive filtering
        header[12] = 0; // no interlace
        WriteChunk(file, "IHDR", header);

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            var row = new byte[width * 3 + 1];
            row[0] = 0; // filter: none

            for (int y = 0; y < height; y++)
            {
                int src = y * width * 4;
                int dst = 1;
                for (int x = 0; x < width; x++, src += 4, dst += 3)
                {
                    row[dst + 0] = rgba[src + 0];
                    row[dst + 1] = rgba[src + 1];
                    row[dst + 2] = rgba[src + 2];
                }

                zlib.Write(row, 0, row.Length);
            }
        }

        WriteChunk(file, "IDAT", compressed.ToArray());
        WriteChunk(file, "IEND", Array.Empty<byte>());
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        WriteUInt32(length, 0, (uint)data.Length);
        stream.Write(length);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        uint crc = Crc32(typeBytes, 0xFFFFFFFFu);
        crc = Crc32(data, crc) ^ 0xFFFFFFFFu;

        var crcBytes = new byte[4];
        WriteUInt32(crcBytes, 0, crc);
        stream.Write(crcBytes);
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset + 0] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static uint Crc32(byte[] data, uint crc)
    {
        foreach (byte b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
