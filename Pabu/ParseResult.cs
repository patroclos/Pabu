using System;
using System.Runtime.CompilerServices;

namespace Pabu
{
    public readonly struct ParseResult<TR, TSeq, TU> : IParseResult<TR, TSeq, TU>
    {
        private readonly bool _initialized;
        private readonly bool _isResult;
        private readonly ParseSuccess<TR, TSeq, TU> _result;
        private readonly ParserError<TSeq, TU> _failure;

        public bool HasResult => _isResult;
        public bool HasFailure => !_isResult;

        public ParseSuccess<TR, TSeq, TU> Result => _isResult ? _result : throw new InvalidOperationException();

        public ParserError<TSeq, TU> Failure => !_isResult ? _failure : throw new InvalidOperationException();

        public ParseResult(ParseSuccess<TR, TSeq, TU> success)
        {
            _result = success;
            _isResult = true;
            _initialized = true;
            _failure = default;
        }

        public ParseResult(ParserError<TSeq, TU> error)
        {
            _result = default;
            _failure = error;
            _initialized = true;
            _isResult = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertInitialized()
        {
            if (!_initialized)
                throw new Exception("Tried using uninitialized");
        }

        public void WithResult(Action<ParseSuccess<TR, TSeq, TU>> handler)
        {
            AssertInitialized();
            if (_isResult)
                handler(_result);
        }

        public void WithFailure(Action<ParserError<TSeq, TU>> handler)
        {
            AssertInitialized();
            if (!_isResult)
                handler(_failure);
        }

        public T Join<T>(Func<ParseSuccess<TR, TSeq, TU>, T> mapResult, Func<ParserError<TSeq, TU>, T> mapFailure)
        {
            AssertInitialized();
            return _isResult ? mapResult(_result) : mapFailure(_failure);
        }

        public IParseResult<TR, TSeq, TU> BiSelect(
            Func<ParseSuccess<TR, TSeq, TU>, ParseSuccess<TR, TSeq, TU>> mapResult,
            Func<ParserError<TSeq, TU>, ParserError<TSeq, TU>> mapError)
        {
            AssertInitialized();
            return _isResult
                ? new ParseResult<TR, TSeq, TU>(mapResult(_result))
                : new ParseResult<TR, TSeq, TU>(mapError(_failure));
        }
    }
}