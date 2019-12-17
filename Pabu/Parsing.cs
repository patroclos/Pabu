using System;
using System.Collections.Generic;

namespace Pabu
{
    public static partial class Parsing<TSeq, TU>
    {
        public static IParserDesc<TSeq, TSeq, TU> Head(string label = null) =>
            new ParserDesc<TSeq, TSeq, TU>(state =>
                state.Eof
                    ? new ParseResult<TSeq, TSeq, TU>(new ParserError<TSeq, TU>(state, 0))
                    : new ParseResult<TSeq, TSeq, TU>(
                        new ParseSuccess<TSeq, TSeq, TU>(state.Slice.Span[0], state.WithAdvancedCursor(1))), label);

        public static IParserDesc<Unit, TSeq, TU> SetState(TU newUserState)
            => new ParserDesc<Unit, TSeq, TU>(state =>
                new ParseResult<Unit, TSeq, TU>(
                    new ParseSuccess<Unit, TSeq, TU>(default, state.WithUserState(newUserState))));

        public static IParserDesc<TR, TSeq, TU> Fail<TR>(string message = null) =>
            new ParserDesc<TR, TSeq, TU>(
                (state) => new ParseResult<TR, TSeq, TU>(new ParserError<TSeq, TU>(state, 0)), message);

        public static IParserDesc<ReadOnlyMemory<TSeq>, TSeq, TU> ReadBuffer(int len)
            => new ParserDesc<ReadOnlyMemory<TSeq>, TSeq, TU>((state) =>
                state.Length < len
                    ? new ParseResult<ReadOnlyMemory<TSeq>, TSeq, TU>(new ParserError<TSeq, TU>(state, 0))
                    : new ParseResult<ReadOnlyMemory<TSeq>, TSeq, TU>(
                        new ParseSuccess<ReadOnlyMemory<TSeq>, TSeq, TU>(state.Slice.Slice(0, len),
                            state.WithAdvancedCursor(len)
                        )
                    )
            );

        public static IParserDesc<ParserState<TSeq, TU>, TSeq, TU> CurrentParserState
            => new ParserDesc<ParserState<TSeq, TU>, TSeq, TU>((state) =>
                new ParseResult<ParserState<TSeq, TU>, TSeq, TU>(
                    new ParseSuccess<ParserState<TSeq, TU>, TSeq, TU>(state, state)
                )
            );
        
        public static IParserDesc<T, TSeq, TU> Choice<T>(params IParserDesc<T, TSeq, TU>[] parsers)
        => new ParserDesc<T, TSeq, TU>(
            state =>
            {
                IParseResult<T, TSeq, TU> last = null;
                foreach (var p in parsers)
                {
                    last = p.Run(state);
                    if (last.HasResult)
                        return last;
                }

                return last ?? new ParseResult<T, TSeq, TU>(new ParserError<TSeq, TU>(state, 0));
            }
            );

    }
}