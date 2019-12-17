using System;
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