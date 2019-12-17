using System;
using System.Buffers.Binary;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Pabu
{
    public readonly struct Codepoint
    {
        public readonly bool IsInitialized;
        public readonly int Value;

        public Codepoint(int value)
        {
            Value = value;
            IsInitialized = true;
        }

        public override string ToString() => char.ConvertFromUtf32(Value);

        public override bool Equals(object obj) =>
            obj is Codepoint c && c.IsInitialized && IsInitialized && c.Value == Value;

        public override int GetHashCode() => Value.GetHashCode();
    }

    public static partial class BinaryParsing<TU>
    {
        #region Numbers

        public static IParserDesc<short, byte, TU> I16Be =>
            from m in Parsing<byte, TU>.ReadBuffer(2)
            select BinaryPrimitives.ReadInt16BigEndian(m.Span);

        public static IParserDesc<short, byte, TU> I16Le =>
            from m in Parsing<byte, TU>.ReadBuffer(2)
            select BinaryPrimitives.ReadInt16LittleEndian(m.Span);

        public static IParserDesc<ushort, byte, TU> U16Be =>
            from m in Parsing<byte, TU>.ReadBuffer(2)
            select BinaryPrimitives.ReadUInt16BigEndian(m.Span);

        public static IParserDesc<ushort, byte, TU> U16Le =>
            from m in Parsing<byte, TU>.ReadBuffer(2)
            select BinaryPrimitives.ReadUInt16LittleEndian(m.Span);

        public static IParserDesc<int, byte, TU> I32Be =>
            from m in Parsing<byte, TU>.ReadBuffer(4)
            select BinaryPrimitives.ReadInt32BigEndian(m.Span);

        public static IParserDesc<int, byte, TU> I32Le =>
            from m in Parsing<byte, TU>.ReadBuffer(4)
            select BinaryPrimitives.ReadInt32LittleEndian(m.Span);

        public static IParserDesc<uint, byte, TU> U32Be =>
            from m in Parsing<byte, TU>.ReadBuffer(4)
            select BinaryPrimitives.ReadUInt32BigEndian(m.Span);

        public static IParserDesc<uint, byte, TU> U32Le =>
            from m in Parsing<byte, TU>.ReadBuffer(4)
            select BinaryPrimitives.ReadUInt32LittleEndian(m.Span);

        public static IParserDesc<long, byte, TU> I64Be =>
            from m in Parsing<byte, TU>.ReadBuffer(8)
            select BinaryPrimitives.ReadInt64BigEndian(m.Span);

        public static IParserDesc<long, byte, TU> I64Le =>
            from m in Parsing<byte, TU>.ReadBuffer(8)
            select BinaryPrimitives.ReadInt64LittleEndian(m.Span);

        public static IParserDesc<ulong, byte, TU> U64Be =>
            from m in Parsing<byte, TU>.ReadBuffer(8)
            select BinaryPrimitives.ReadUInt64BigEndian(m.Span);

        public static IParserDesc<ulong, byte, TU> U64Le =>
            from m in Parsing<byte, TU>.ReadBuffer(8)
            select BinaryPrimitives.ReadUInt64LittleEndian(m.Span);

        public static IParserDesc<float, byte, TU> Float32Be =>
            from i in I32Be
            select BitConverter.Int32BitsToSingle(i);
        
        public static IParserDesc<float, byte, TU> Float32Le =>
            from i in I32Le
            select BitConverter.Int32BitsToSingle(i);
        
        public static IParserDesc<double, byte, TU> Double64Be =>
            from i in I64Be
            select BitConverter.Int64BitsToDouble(i);
        
        public static IParserDesc<double, byte, TU> Double64Le =>
            from i in I64Le
            select BitConverter.Int64BitsToDouble(i);


        #endregion
        
        
        #region Compression

        public static IParserDesc<ReadOnlyMemory<byte>, byte, TU> ReadDeflateCompressedBuffer(int len, int decompressedSize) =>
            from buffer in Parsing<byte, TU>.ReadBuffer(len)
            let decompressed = DecompressDeflateBuffer(buffer, decompressedSize)
            select new ReadOnlyMemory<byte>(decompressed);

        private static byte[] DecompressDeflateBuffer(ReadOnlyMemory<byte> buffer, int decompressedSize)
        {
            Console.WriteLine($"Decompressing {BitConverter.ToString(buffer.ToArray())}");
            using var mstr = new MemoryStream();
            using var inStream = new MemoryStream(buffer.ToArray());
            using var deflate = new DeflateStream(inStream, CompressionMode.Decompress);
            deflate.CopyTo(mstr);
            return mstr.ToArray();
        }
        #endregion
    }

    public static partial class TextParsing<TU>
    {
        public static IParserDesc<Codepoint, byte, TU> ReadCodepoint(Encoding enc)
            => new ParserDesc<Codepoint, byte, TU>(
                state =>
                {
                    var buf = state.Slice;
                    if (buf.IsEmpty)
                        return new ParseResult<Codepoint, byte, TU>(new ParserError<byte, TU>(state, 0));
                    var chars = new char[4];
                    var tryW = Math.Min(buf.Length, 4);
                    var numChars = enc.GetChars(buf.Slice(0, tryW).Span, chars);
                    if (numChars == 0)
                        return new ParseResult<Codepoint, byte, TU>(new ParserError<byte, TU>(state, 0));
                    if (char.IsHighSurrogate(chars[0]))
                    {
                        if (numChars < 2 || !char.IsLowSurrogate(chars[1]))
                            return new ParseResult<Codepoint, byte, TU>(new ParserError<byte, TU>(state, 0));
                        var utf32 = char.ConvertToUtf32(chars[0], chars[1]);
                        var readBytes = enc.GetByteCount(chars, 0, 2);
                        var newState = state.WithAdvancedCursor(readBytes);
                        return new ParseResult<Codepoint, byte, TU>(
                            new ParseSuccess<Codepoint, byte, TU>(new Codepoint(utf32), newState)
                        );
                    }
                    else
                    {
                        var readBytes = enc.GetByteCount(chars, 0, 1);
                        var newState = state.WithAdvancedCursor(readBytes);
                        return new ParseResult<Codepoint, byte, TU>(new ParseSuccess<Codepoint, byte, TU>(new Codepoint(chars[0]), newState));
                    }
                }
            );
    }
}