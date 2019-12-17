using System;

namespace Pabu
{
    public readonly struct ParserState<TSeq, TU>
    {
        public readonly ReadOnlyMemory<TSeq> CompleteInput;
        public readonly int Cursor;
        public readonly TU UserState;

        public bool IsValid => _initialized;
        public bool Eof => CompleteInput.Length <= Cursor;
        public int Length => CompleteInput.Length - Cursor;
        private readonly bool _initialized;

        public ReadOnlyMemory<TSeq> Slice => CompleteInput.Slice(Cursor);

        public ParserState(ReadOnlyMemory<TSeq> input, int cursor = 0, TU userState = default)
        {
            CompleteInput = input;
            Cursor = cursor;
            UserState = userState;
            _initialized = true;
        }

        public ParserState<TSeq, TU> WithAdvancedCursor(int delta) =>
            new ParserState<TSeq, TU>(CompleteInput, Cursor + delta, UserState);

        public ParserState<TSeq, TU> WithUserState(TU newState) =>
            new ParserState<TSeq, TU>(CompleteInput, Cursor, newState);

        public override string ToString()
        {
            return $"{GetType().Name}(Cursor = {Cursor}, UserState = {UserState})";
        }
    }
}