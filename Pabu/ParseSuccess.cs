namespace Pabu
{
    public readonly struct ParseSuccess<TR, TSeq, TU>
    {
        public readonly TR Result;
        public readonly ParserState<TSeq, TU> Next;

        public ParseSuccess(TR result, ParserState<TSeq, TU> next)
        {
            Result = result;
            Next = next;
        }

        public override string ToString()
        {
            return $"{nameof(Result)}: {Result}, {nameof(Next)}: {Next}";
        }
    }
}