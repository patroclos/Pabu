using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Pabu;

// ReSharper disable HeapView.BoxingAllocation

namespace BasicTesting.Fbx
{
    using FbxParsing = Pabu.Parsing<byte, FbxParserState>;
    using BP = BinaryParsing<FbxParserState>;

    public readonly struct FbxParserState
    {
        public readonly uint FormatVersion;

        public FbxParserState(uint version)
        {
            FormatVersion = version;
        }
    }

    public static class FbxParser
    {
        private const string HeaderSignature = "Kaydara FBX Binary  \x00";

        public static IParserDesc<List<Node>, byte, FbxParserState> ReadFbx =>
            from header in ReadHeader
            from _ in FbxParsing.SetState(header)
            from nodes in ReadNode.Many()
            select nodes;

        private static IParserDesc<FbxParserState, byte, FbxParserState> ReadHeader =>
            (from signature in FbxParsing.ExpectSequence(Encoding.ASCII.GetBytes(HeaderSignature))
                 .WithLabel("Signature")
             from reserved in FbxParsing.ExpectSequence(new byte[] {0x1a, 0}).WithLabel("Reserved Bytes")
             from version in BinaryParsing<FbxParserState>.U32Le.WithLabel("Version")
             select new FbxParserState(version)).WithLabel("File Header");

        private static IParserDesc<Node, byte, FbxParserState> ReadNode =>
            (from version in FbxParsing.CurrentParserState.Select(state => state.UserState.FormatVersion)
             let intParser = version > 7400
                 ? BinaryParsing<FbxParserState>.I64Le
                 : BinaryParsing<FbxParserState>.I32Le.Select(x => (long) x)
             from eo in intParser
             from np in intParser
             from pll in intParser
             from nl in FbxParsing.Head()
             from name in TextParsing<FbxParserState>.ReadCodepoint(Encoding.UTF8)
                 .Take(nl)
                 .Select(chars => string.Join(null, chars.Select(cp => cp.ToString())))
             from props in ReadProperty.Take((int) np).WithLabel($"{np} properties of {name}")
             from children in ReadNodeChildren(eo).WithLabel($"children of {name}")
             select new Node(name, props, children)).WithLabel("Node");

        private static IParserDesc<Unit, byte, FbxParserState> Log(string msg) =>
            new ParserDesc<Unit, byte, FbxParserState>(
                state =>
                {
                    Console.WriteLine($"LOG: {msg} (at {state.Cursor:X})");
                    return new ParseResult<Unit, byte, FbxParserState>(
                        new ParseSuccess<Unit, byte, FbxParserState>(default, state));
                });

        private static IParserDesc<IProperty, byte, FbxParserState> ReadProperty =>
            (from typecode in FbxParsing.Head().Select(c => (PropertyTypeCode) c)
             from __ in PropertyParsers.ContainsKey(typecode)
                 ? FbxParsing.Return<Unit>(default)
                 : FbxParsing.Fail<Unit>()
             from prop in PropertyParsers[typecode].WithLabel($"{typecode} data")
             select prop).WithLabel("Property");

        private static IParserDesc<List<Node>, byte, FbxParserState> ReadNodeChildren(long endOffset) =>
            new ParserDesc<List<Node>, byte, FbxParserState>(
                state =>
                {
                    var list = new List<Node>();
                    var current = state;
                    for (var remainder = endOffset - current.Cursor;
                        remainder > 0;
                        remainder = endOffset - current.Cursor)
                    {
                        if (remainder == 13)
                        {
                            current = current.WithAdvancedCursor(13);
                            break;
                        }

                        var result = ReadNode.Run(current);
                        if (result.HasFailure)
                            return new ParseResult<List<Node>, byte, FbxParserState>(
                                result.Failure
                            );
                        current = result.Result.Next;
                        list.Add(result.Result.Result);
                    }

                    return new ParseResult<List<Node>, byte, FbxParserState>(
                        new ParseSuccess<List<Node>, byte, FbxParserState>(list, current));
                },
                label: "Node Children"
            );


        private static T ApplyParserUnsafe<T>(IParserDesc<T, byte, FbxParserState> parser, ReadOnlyMemory<byte> input)
            => parser.Run(new ParserState<byte, FbxParserState>(input)).Result.Result;
        
        private static readonly Dictionary<PropertyTypeCode, IParserDesc<IProperty, byte, FbxParserState>>
            PropertyParsers =
                new Dictionary<PropertyTypeCode, IParserDesc<IProperty, byte, FbxParserState>>
                {
                    {
                        PropertyTypeCode.Bit,
                        from v in FbxParsing.Head() select new Property<bool>(v == 1) as IProperty
                    },
                    {
                        PropertyTypeCode.Double,
                        from v in BinaryParsing<FbxParserState>.Double64Le select new Property<double>(v) as IProperty
                    },
                    {
                        PropertyTypeCode.Float,
                        from v in BinaryParsing<FbxParserState>.Float32Le select new Property<float>(v) as IProperty
                    },
                    {
                        PropertyTypeCode.Int,
                        from v in BinaryParsing<FbxParserState>.I32Le select new Property<int>(v) as IProperty
                    },
                    {
                        PropertyTypeCode.Long,
                        from v in BinaryParsing<FbxParserState>.I64Le select new Property<long>(v) as IProperty
                    },
                    {
                        PropertyTypeCode.SShort,
                        from v in BinaryParsing<FbxParserState>.I16Le select new Property<short>(v) as IProperty
                    },
                    {
                        PropertyTypeCode.String,
                        from len in BinaryParsing<FbxParserState>.U32Le
                        from content in FbxParsing.ReadBuffer((int) len)
                            .Select(buf => Encoding.UTF8.GetString(buf.ToArray()))
                        select new Property<string>(content) as IProperty
                    },
                    {
                        PropertyTypeCode.Raw,
                        from len in BinaryParsing<FbxParserState>.U32Le
                        from content in FbxParsing.ReadBuffer((int) len)
                        select new Property<ReadOnlyMemory<byte>>(content) as IProperty
                    },
                    {
                        PropertyTypeCode.BoolArr,
                        ReadArrayData(PropertyTypeCode.BoolArr)
                            .Select(data => ApplyParserUnsafe(FbxParsing.Head().Select(b=>b==1).Many(), data.Buffer))
                            .Select(data => new Property<List<bool>>(data) as IProperty)
                    },
                    {
                        PropertyTypeCode.FloatArr,
                        ReadArrayData(PropertyTypeCode.FloatArr)
                            .Select(data => ApplyParserUnsafe(BP.Float32Le.Many(), data.Buffer))
                            .Select(data => new Property<List<float>>(data) as IProperty)
                    },
                    {
                        PropertyTypeCode.DoubleArr,
                        ReadArrayData(PropertyTypeCode.DoubleArr)
                            .Select(data => ApplyParserUnsafe(BP.Double64Le.Many(), data.Buffer))
                            .Select(data => new Property<List<double>>(data) as IProperty)
                    },
                    {
                        PropertyTypeCode.IntArr,
                        ReadArrayData(PropertyTypeCode.IntArr)
                            .Select(data => ApplyParserUnsafe(BP.I32Le.Many(), data.Buffer))
                            .Select(data => new Property<List<int>>(data) as IProperty)
                    },
                    {
                        PropertyTypeCode.LongArr,
                        ReadArrayData(PropertyTypeCode.LongArr)
                            .Select(data => ApplyParserUnsafe(BP.I64Le.Many(), data.Buffer))
                            .Select(data => new Property<List<long>>(data) as IProperty)
                    },
                };

        private static readonly Dictionary<PropertyTypeCode, int> PropertyArrayElementSizes =
            new Dictionary<PropertyTypeCode, int>
            {
                {PropertyTypeCode.BoolArr, 1},
                {PropertyTypeCode.FloatArr, 4},
                {PropertyTypeCode.DoubleArr, 8},
                {PropertyTypeCode.IntArr, 4},
                {PropertyTypeCode.LongArr, 8},
            };

        public enum ArrayEncoding
        {
            Unencoded = 0,
            Encoded = 1,
        }

        public readonly struct ArrayPropertyData
        {
            public readonly ReadOnlyMemory<byte> Buffer;
            public readonly int Length;
            public readonly ArrayEncoding Encoding;
            public readonly int EncodedLength;

            public ArrayPropertyData(int length, ArrayEncoding encoding, int encodedLength, ReadOnlyMemory<byte> buffer)
            {
                Buffer = buffer;
                Length = length;
                Encoding = encoding;
                EncodedLength = encodedLength;
            }

            public override string ToString()
                => $"";
        }

        private static IParserDesc<ArrayPropertyData, byte, FbxParserState> ReadArrayData(PropertyTypeCode typeCode) =>
            from length in BinaryParsing<FbxParserState>.I32Le
            from encoding in BinaryParsing<FbxParserState>.I32Le.Select(i => (ArrayEncoding) i)
            from encodedLength in BinaryParsing<FbxParserState>.I32Le
            let finalLen = PropertyArrayElementSizes[typeCode] * length
            let bufferLen = encoding == ArrayEncoding.Encoded
                ? encodedLength
                : finalLen
            from buffer in FbxParsing.ReadBuffer(bufferLen)
            let data = encoding == ArrayEncoding.Unencoded ? buffer : DeflateWithChecksum(buffer)
            //from buffer in FbxParsing.ReadBuffer(bufferLen)
            select new ArrayPropertyData(length, encoding, encodedLength, data);

        private static ReadOnlyMemory<byte> DeflateWithChecksum(in ReadOnlyMemory<byte> buffer)
        {
            using var inStr = new MemoryStream(buffer.Span.ToArray(), 2, buffer.Length - 2);
            using var outStr = new MemoryStream();
            using var def = new DeflateStream(inStr, CompressionMode.Decompress);
            def.CopyTo(outStr);
            return outStr.ToArray();
        }
    }
}