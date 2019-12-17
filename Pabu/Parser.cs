using System.Buffers.Binary;
using System.Text;

namespace Pabu
{
    public delegate IParseResult<T, TSeq, TU> Parser<T, TSeq, TU>(ParserState<TSeq, TU> input);

#if old
    public readonly ref struct ParseResult<T, TSeq>
    {
        public readonly T Result;
        public readonly ParserState<TSeq> Remaining;
        private readonly bool _isInitialized;

        public bool IsValid => _isInitialized;


        public ParseResult(T result, ParserState<TSeq> remainder)
        {
            Result = result;
            Remaining = remainder;
            _isInitialized = true;
        }

        public override string ToString()
        {
            return !IsValid ? "ParseResult(Invalid)" : $"ParseResult({Result}, {Remaining.CompleteInput.Length - Remaining.Cursor} remaining)";
        }
    }

    public readonly struct ParserState<TSeq>
    {
        public readonly ReadOnlyMemory<TSeq> CompleteInput;
        public readonly int Cursor;
        public readonly bool IsValid;

        public ReadOnlyMemory<TSeq> View => CompleteInput.Slice(Cursor);
        public int Length => CompleteInput.Length - Cursor;

        public ParserState(ReadOnlyMemory<TSeq> buffer, int cursor = 0)
        {
            CompleteInput = buffer;
            Cursor = cursor;
            IsValid = true;
        }

        public static implicit operator ReadOnlyMemory<TSeq>(ParserState<TSeq> self) =>
            self.CompleteInput.Slice(self.Cursor);

        public ParserState<TSeq> Advance(int num) => new ParserState<TSeq>(CompleteInput, Cursor + num);
    parpartial}

    public delegate ParseResult<T, byte> BinParser<T>(ParserState<byte> buffer);


    public readonly struct Unit : IComparable<Unit>, IEquatable<Unit>
    {
        public int CompareTo(Unit other)
        {
            return 0;
        }

        public bool Equals(Unit other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is Unit other && Equals(other);
        }

        public override int GetHashCode()
        {
            return int.MaxValue;
        }

        public static bool operator ==(Unit left, Unit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Unit left, Unit right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct Codepoint
    {
        public readonly bool IsValid;
        public readonly int Value;

        public Codepoint(int value)
        {
            Value = value;
            IsValid = true;
        }

        public override string ToString() => char.ConvertFromUtf32(Value);
        public override bool Equals(object obj) => obj is Codepoint c && c.Value == Value && c.IsValid == IsValid;
        public override int GetHashCode() => Value.GetHashCode();
    }

    public static class BinParser
    {
        public static BinParser<T2> Select<T, T2>(this BinParser<T> self, Func<T, T2> map) =>
            (buf) =>
            {
                var r = self(buf);
                return r.IsValid
                    ? new ParseResult<T2, byte>(map(r.Result), r.Remaining)
                    : default;
            };

        public static BinParser<T3> SelectMany<T, T2, T3>(this BinParser<T> self, Func<T, BinParser<T2>> bind,
            Func<T, T2, T3> project) =>
            (buf) =>
            {
                var r = self(buf);
                var r2 = r.IsValid ? bind(r.Result)(r.Remaining) : default;
                return r2.IsValid
                    ? new ParseResult<T3, byte>(project(r.Result, r2.Result), r2.Remaining)
                    : default;
            };

        public static BinParser<T2> SelectMany<T, T2>(this BinParser<T> self, Func<T, BinParser<T2>> bind) =>
            (buf) =>
            {
                var r = self(buf);
                return r.IsValid ? bind(r.Result)(r.Remaining) : default;
            };

        public static BinParser<ReadOnlyMemory<byte>> ReadBuffer(int len) =>
            (state) =>
            {
                var buf = state.View;
                return buf.Length >= len
                    ? new ParseResult<ReadOnlyMemory<byte>, byte>(buf.Slice(0, len), state.Advance(len))
                    : default;
            };

        public static BinParser<byte> Head() => (state) => (state.CompleteInput.Length - state.Cursor) >= 1
            ? new ParseResult<byte, byte>(state.View.Span[0], new ParserState<byte>(state.CompleteInput, state.Cursor + 1))
            : default;

        public static BinParser<T> Fail<T>() => (buf) => default;
        public static BinParser<T> Return<T>(T value) => (buf) => new ParseResult<T, byte>(value, buf);

        public static BinParser<List<T>> Take<T>(this BinParser<T> self, int count) => (buf) =>
        {
            var results = new List<T>();
            var remainder = buf;

            for (var i = 0; i < count; i++)
            {
                var result = self(remainder);
                if (!result.IsValid)
                    return default;
                remainder = result.Remaining;
                results.Add(result.Result);
            }

            return new ParseResult<List<T>, byte>(results, remainder);
        };

        public static BinParser<List<T>> Many<T>(this BinParser<T> self) => (buf) =>
        {
            var results = new List<T>();
            var remainder = buf;
            ParseResult<T, byte> current;
            while ((current = self(remainder)).IsValid)
            {
                remainder = current.Remaining;
                results.Add(current.Result);
            }

            return new ParseResult<List<T>, byte>(results, remainder);
        };

        public static BinParser<T> LookAhead<T>(this BinParser<T> self) => (buf) =>
        {
            var result = self(buf);
            return result.IsValid ? new ParseResult<T, byte>(result.Result, buf) : default;
        };

        public static BinParser<T?> Option<T>(this BinParser<T> self) where T : struct => (buf) =>
        {
            var result = self(buf);
            return result.IsValid
                ? new ParseResult<T?, byte>(result.Result, result.Remaining)
                : new ParseResult<T?, byte>(null, buf);
        };
        
        public static BinParser<int> ParserPosition => (buf)=>new ParseResult<int, byte>(buf.Cursor, buf);

        public static BinParser<Unit> Eof() =>
            (buf) => buf.View.IsEmpty ? new ParseResult<Unit, byte>(default, buf) : default;

        public static BinParser<List<T>> ManyTill<T, TTerm>(this BinParser<T> self, BinParser<TTerm> terminator) =>
            (buf) =>
            {
                var results = new List<T>();
                var remainder = buf;
                ParseResult<T, byte> current;
                ParseResult<TTerm, byte> currentTerminator;

                bool TryTerminate(out ParseResult<TTerm, byte> currentTerminator)
                {
                    return (currentTerminator = terminator(remainder)).IsValid;
                }

                if (!TryTerminate(out currentTerminator))
                    while (true)
                    {
                        if (TryTerminate(out currentTerminator))
                        {
                            remainder = currentTerminator.Remaining;
                            break;
                        }

                        if (!(current = self(remainder)).IsValid)
                            return default;
                        remainder = current.Remaining;
                        results.Add(current.Result);
                    }
                else
                    remainder = currentTerminator.Remaining;

                return new ParseResult<List<T>, byte>(results, remainder);
            };

        public static BinParser<T> Where<T>(this BinParser<T> self, Predicate<T> predicate) =>
            (buf) =>
            {
                var result = self(buf);
                if (!result.IsValid || !predicate(result.Result))
                    return default;

                return result;
            };

        public static BinParser<T> Choice<T>(params BinParser<T>[] parsers) => (buf) =>
        {
            foreach (var parser in parsers)
            {
                var result = parser(buf);
                if (result.IsValid)
                    return result;
            }

            return default;
        };

        public static BinParser<ReadOnlyMemory<byte>> ConsumeExpectedBuffer(ReadOnlyMemory<byte> expectation) =>
            from m in ReadBuffer(expectation.Length)
            from x in expectation.Span.SequenceEqual(m.Span) ? Return(m) : Fail<ReadOnlyMemory<byte>>()
            select x;

        #region Numbers

        public static BinParser<short> I16Be =>
            from m in ReadBuffer(2)
            select BinaryPrimitives.ReadInt16BigEndian(m.Span);

        public static BinParser<short> I16Le =>
            from m in ReadBuffer(2)
            select BinaryPrimitives.ReadInt16LittleEndian(m.Span);

        public static BinParser<ushort> U16Be =>
            from m in ReadBuffer(2)
            select BinaryPrimitives.ReadUInt16BigEndian(m.Span);

        public static BinParser<ushort> U16Le =>
            from m in ReadBuffer(2)
            select BinaryPrimitives.ReadUInt16LittleEndian(m.Span);

        public static BinParser<int> I32Be =>
            from m in ReadBuffer(4)
            select BinaryPrimitives.ReadInt32BigEndian(m.Span);

        public static BinParser<int> I32Le =>
            from m in ReadBuffer(4)
            select BinaryPrimitives.ReadInt32LittleEndian(m.Span);

        public static BinParser<uint> U32Be =>
            from m in ReadBuffer(4)
            select BinaryPrimitives.ReadUInt32BigEndian(m.Span);

        public static BinParser<uint> U32Le =>
            from m in ReadBuffer(4)
            select BinaryPrimitives.ReadUInt32LittleEndian(m.Span);

        public static BinParser<long> I64Be =>
            from m in ReadBuffer(8)
            select BinaryPrimitives.ReadInt64BigEndian(m.Span);

        public static BinParser<long> I64Le =>
            from m in ReadBuffer(8)
            select BinaryPrimitives.ReadInt64LittleEndian(m.Span);

        public static BinParser<ulong> U64Be =>
            from m in ReadBuffer(8)
            select BinaryPrimitives.ReadUInt64BigEndian(m.Span);

        public static BinParser<ulong> U64Le =>
            from m in ReadBuffer(8)
            select BinaryPrimitives.ReadUInt64LittleEndian(m.Span);


        public static BinParser<char> Digit => ReadChar(Encoding.Default).Where(char.IsDigit);

        #endregion

        public static BinParser<Codepoint> Utf8Char() => ReadCodepoint(Encoding.UTF8);
        public static BinParser<List<Codepoint>> Utf8String => Utf8Char().Many();

        public static BinParser<Codepoint> ReadCodepoint(Encoding enc) =>
            (state) =>
            {
                var buf = state.View;
                if (buf.IsEmpty)
                    return default;
                var chars = new char[4];
                var tryW = Math.Min(buf.Length, 4);
                var numChars = enc.GetChars(buf.Slice(0, tryW).Span, chars);
                if (numChars == 0)
                    return default;
                if (char.IsHighSurrogate(chars[0]))
                {
                    if (numChars < 2 || !char.IsLowSurrogate(chars[1]))
                        return default;
                    var utf32 = char.ConvertToUtf32(chars[0], chars[1]);
                    var readBytes = enc.GetByteCount(chars, 0, 2);
                    return new ParseResult<Codepoint, byte>(new Codepoint(utf32), new ParserState<byte>(state.CompleteInput, state.Cursor + readBytes));
                }
                else
                {
                    var readBytes = enc.GetByteCount(chars, 0, 1);
                    return new ParseResult<Codepoint, byte>(new Codepoint(chars[0]), new ParserState<byte>(state.CompleteInput, state.Cursor + readBytes));
                }
            };

        public static BinParser<char> ReadChar(Encoding enc = null) =>
            (state) =>
            {
                var buf = state.View;
                enc ??= Encoding.Default;
                if (buf.IsEmpty)
                    return default;
                var chars = new char[4];
                var tryW = Math.Min(buf.Length, 4);
                var numChars = enc.GetChars(buf.Slice(0, tryW).Span, chars);
                return numChars == 0
                    ? default
                    : new ParseResult<char, byte>(chars[0], state.Advance(enc.GetByteCount(chars, 0, 1)));
            };
    }
#endif
}