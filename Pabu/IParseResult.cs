using System;

namespace Pabu
{
    public interface IParseResult<TR, TSeq, TU>
    {
        bool HasResult { get; }
        bool HasFailure { get; }
        
        ParseSuccess<TR, TSeq, TU> Result { get; }
        ParserError<TSeq, TU> Failure { get; }
        
        void WithResult(Action<ParseSuccess<TR, TSeq, TU>> handler);
        void WithFailure(Action<ParserError<TSeq, TU>> handler);
        T Join<T>(Func<ParseSuccess<TR, TSeq, TU>, T> mapResult, Func<ParserError<TSeq, TU>, T> mapFailure);

        IParseResult<TR, TSeq, TU> BiSelect(
            Func<ParseSuccess<TR, TSeq, TU>, ParseSuccess<TR, TSeq, TU>> mapResult,
            Func<ParserError<TSeq, TU>, ParserError<TSeq, TU>> mapError
        );
    }
}