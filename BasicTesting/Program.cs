﻿using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text;
using BasicTesting.Fbx;
using Pabu;
using BinParsing = Pabu.Parsing<byte, int>;

namespace BasicTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
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
            var buf = File.ReadAllBytes(@"C:\Users\j.jensch\Documents\workspace\Pabu\BasicTesting\test.txt");
            Console.WriteLine(BitConverter.ToString(buf));
            //var buf = Encoding.UTF8.GetBytes("ABCDE😋");
            var result = p.Run(new ParserState<byte, int>(buf));
            result.WithResult(r => Console.WriteLine(r.ToString()));
            result.WithFailure(r => Console.WriteLine(r.ToString()));
            */

            var buf = File.ReadAllBytes(@"C:\Users\j.jensch\Desktop\untitled.fbx");
            var p = FbxParser.ReadFbx.Run(new ParserState<byte, FbxParserState>(buf));
            p.WithFailure(f=>Console.WriteLine(f));
            p.WithResult(nodes =>
            {
                var btw = new StringWriter();
                var tw = new IndentedTextWriter(btw);
                foreach(var n in nodes.Result.Nodes)
                    n.WriteTo(tw);
                Console.WriteLine(btw.ToString());
                File.WriteAllText(@"C:\Users\j.jensch\Desktop\out.txt", btw.ToString());

                var graphBuilder = new FbxGraphBuilder();
                graphBuilder.FromFbx(nodes.Result);
                File.WriteAllText(@"C:\Users\j.jensch\Desktop\out.dot", graphBuilder.ToString());
            });
        }
    }
}