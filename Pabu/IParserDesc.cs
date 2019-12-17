namespace Pabu
{
    public interface IParserDesc<TR, TSeq, TU>
    {
        string Label { get; }
        IParseResult<TR, TSeq, TU> Run(ParserState<TSeq, TU> parserState);
    }
}