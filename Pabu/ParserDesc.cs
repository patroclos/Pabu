namespace Pabu
{
    public readonly struct ParserDesc<TR, TSeq, TU> : IParserDesc<TR, TSeq, TU>
    {
        private readonly Parser<TR, TSeq, TU> _parser;
        public string Label { get; }

        public ParserDesc(Parser<TR, TSeq, TU> parser, string label = null)
        {
            _parser = parser;
            Label = label;
        }

        public override string ToString()
        {
            var labelInfo = string.IsNullOrEmpty(Label) ? $" <?> \"{Label}\"" : string.Empty;
            return $"Parser<{nameof(TR)}, {nameof(TSeq)}>{labelInfo}";
        }

        public static implicit operator ParserDesc<TR, TSeq, TU>(Parser<TR, TSeq, TU> parser) =>
            new ParserDesc<TR, TSeq, TU>(parser);

        public ParserDesc<TR, TSeq, TU> WithLabel(string label = null) => new ParserDesc<TR, TSeq, TU>(_parser, label);

        public IParseResult<TR, TSeq, TU> Run(ParserState<TSeq, TU> parserState)
        {
            var result = _parser(parserState);
            var label = Label;
            return result.BiSelect(a => a, a => label != null ? a.PushLabel(label) : a);
        }
    }
}