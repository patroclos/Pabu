using System;
using System.IO;
using System.Linq;
using System.Text;
using Pabu;
using BinParsing = Pabu.Parsing<byte, int>;

namespace BasicTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var p =
                (from x in BinParsing.Head().WithLabel("get x")
                    from _ in BinParsing.SetState(200)
                    from y in BinParsing.Head().WithLabel("get y")
                    from __ in BinParsing.SetState(300)
                    from z in BinParsing.Head().WithLabel("some stuff").Take(3)
                    from u in TextParsing<int>.ReadCodepoint(Encoding.UTF8)
                    select (x,u.Value)).WithLabel("get tuple");
            //var parser = Parsing.TakeByte<int>().SelectMany(x=>Parsing.TakeByte<int>(), a=>a, "another one");
            var buf = File.ReadAllBytes(@"C:\Users\Joshua\Documents\workspace\Pabu\BasicTesting\test.txt");
            Console.WriteLine(BitConverter.ToString(buf));
            //var buf = Encoding.UTF8.GetBytes("ABCDE😋");
            var result = p.Run(new ParserState<byte, int>(buf));
            result.WithResult(r => Console.WriteLine(r.ToString()));
            result.WithFailure(r => Console.WriteLine(r.ToString()));
        }
    }
}