using System;
using System.Collections.Generic;
using System.Linq;

namespace Pabu
{
    public readonly struct ParserError<TSeq, TU>
    {
        public readonly ParserState<TSeq, TU> State;
        public readonly int Consumed;
        public readonly IEnumerable<string> ParserLabels;

        public bool IsValid => _initialized;
        private readonly bool _initialized;

        public ParserError(ParserState<TSeq, TU> state, int consumed, IEnumerable<string> parserLabels = null)
        {
            State = state;
            Consumed = consumed;
            ParserLabels = parserLabels ?? Array.Empty<string>();
            _initialized = true;
        }

        public ParserError<TSeq, TU> WithLabels(IEnumerable<string> labels) =>
            new ParserError<TSeq, TU>(State, Consumed, labels);

        public ParserError<TSeq, TU> PushLabel(string label) =>
            new ParserError<TSeq, TU>(State, Consumed, ParserLabels.Append(label));

        public override string ToString()
        {
            return $"Error parsing [{string.Join(" > ", ParserLabels.Reverse())}] (state: {State})";
        }
    }
}