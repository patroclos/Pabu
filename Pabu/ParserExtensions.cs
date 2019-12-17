using System;
using System.Collections.Generic;
using System.Linq;

namespace Pabu
{
    public static class ParserExtensions
    {
        public static IParserDesc<TR, TSeq, TU> Select<TO, TR, TSeq, TU>(
            this IParserDesc<TO, TSeq, TU> parserDesc,
            Func<TO, TR> map,
            string label = default
        ) => new ParserDesc<TR, TSeq, TU>((state) =>
        {
            var r = parserDesc.Run(state);
            return r.Join<IParseResult<TR, TSeq, TU>>(
                a => new ParseResult<TR, TSeq, TU>(new ParseSuccess<TR, TSeq, TU>(map(a.Result), a.Next)),
                a => new ParseResult<TR, TSeq, TU>(a)
            );
        }, label);

        public static IParserDesc<TR, TSeq, TU> WithLabel<TR, TSeq, TU>(
            this IParserDesc<TR,
                TSeq, TU> self,
            string label
        ) => self.Label == null
            ? self.Select(a => a, label)
            : new ParserDesc<TR, TSeq, TU>(
                (state) => self.Run(state).BiSelect(a => a, b => b.WithLabels(b.ParserLabels.SkipLast(1))), label);

        public static IParserDesc<TP, TSeq, TU> SelectMany<TO, TR, TP, TSeq, TU>(
            this IParserDesc<TO, TSeq, TU> parserDesc,
            Func<TO, IParserDesc<TR, TSeq, TU>> bind,
            Func<TO, TR, TP> project,
            string label = default
        ) =>
            new ParserDesc<TP, TSeq, TU>((state) =>
            {
                var r = parserDesc.Run(state);
                return r.Join<IParseResult<TP, TSeq, TU>>(
                    success =>
                        bind(success.Result).Select((r2) => project(success.Result, r2), null)
                            .Run(success.Next)
                            .BiSelect(
                                a => a,
                                b => label != null ? b.PushLabel(label) : b
                            ),
                    e => new ParseResult<TP, TSeq, TU>(label != null ? e.PushLabel(label) : e));
            });

        public static IParserDesc<List<T>, TSeq, TU> Take<T, TSeq, TU>(this IParserDesc<T, TSeq, TU> self, int count)
            => new ParserDesc<List<T>, TSeq, TU>((state) =>
            {
                var results = new List<T>();
                var remain = state;

                for (var i = 0; i < count; i++)
                {
                    var result = self.Run(remain);
                    if (result.HasFailure)
                        return new ParseResult<List<T>, TSeq, TU>(new ParserError<TSeq, TU>(remain,
                            remain.Cursor - state.Cursor, new[] {$"Tried taking {count} of {self.Label}"}));
                    remain = result.Result.Next;
                    results.Add(result.Result.Result);
                }

                return new ParseResult<List<T>, TSeq, TU>(new ParseSuccess<List<T>, TSeq, TU>(results, remain));
            });

        public static IParserDesc<List<T>, TSeq, TU> Many<T, TSeq, TU>(this IParserDesc<T, TSeq, TU> self)
            => new ParserDesc<List<T>, TSeq, TU>(
                state =>
                {
                    var results = new List<T>();
                    var remain = state;
                    IParseResult<T, TSeq, TU> current;
                    while ((current = self.Run(remain)).HasResult)
                    {
                        remain = current.Result.Next;
                        results.Add(current.Result.Result);
                    }

                    return new ParseResult<List<T>, TSeq, TU>(new ParseSuccess<List<T>, TSeq, TU>(results, remain));
                }
            );
    }
}