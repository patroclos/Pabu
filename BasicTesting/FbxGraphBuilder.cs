using System;
using System.Collections.Generic;
using System.Linq;
using BasicTesting.Fbx;
using DotNetGraph;

namespace BasicTesting
{
    public class FbxGraphBuilder
    {
        private DotGraph _graph = new DotGraph("FBX", true);
        private Dictionary<Node, DotNode> _nodes = new Dictionary<Node, DotNode>();

        private readonly struct FbxConnection
        {
            public readonly string Mode;
            public readonly long From;
            public readonly long To;

            public FbxConnection(string mode, long @from, long to)
            {
                Mode = mode;
                From = @from;
                To = to;
            }
        }

        public void FromFbx(FbxData data)
        {
            T ReadPropAtIndex<T>(Node node, int index)
            {
                return ((IProperty<T>) node.Properties[index]).Value;
            }
            var objects = data.Nodes
                .Select(n => n as Node?)
                .FirstOrDefault(n => n.Value.Name == "Objects")
                ?.Children
                .ToDictionary(o => ReadPropAtIndex<long>(o, 0))
                ?? new Dictionary<long, Node>();
            
            objects.Add(0, new Node("RootNode", new IProperty[]{new Property<long>(0)}, new Node[0]));

            var connections = data.Nodes
                .Select(n => n as Node?)
                .FirstOrDefault(n => n.Value.Name == "Connections")
                ?.Children.Where(c => c.Name == "C")
                .Select(c =>
                {
                    var mode = ReadPropAtIndex<string>(c, 0);
                    var from = ReadPropAtIndex<long>(c, 1);
                    var to = ReadPropAtIndex<long>(c, 2);
                    return new FbxConnection(mode, from, to);
                }) ?? new FbxConnection[0];

            foreach (var c in connections)
            {
                var @from = objects[c.From];
                var to = objects[c.To];

                static DotNode MakeGraphNode(Node node, long id) => new DotNode($"\"{node.Name} - {id}\"")
                {
                    Shape = DotNodeShape.Box,
                    FillColor = DotColor.Yellow,
                    Style = DotNodeStyle.Filled,
                    Label = $"{@node.Name} - {id}\\l{string.Join("\\l", node.Properties.Select(p=>p.ToString()))}"
                };

                void AddChildrenRecursively(Node root, long? id)
                {
                    foreach (var child in root.Children)
                    {
                        AddEdge(child, null, root, id, "child of");
                        AddChildrenRecursively(child, null);
                    }
                }
                
                DotNode fromNode, toNode;
                if (!_nodes.ContainsKey(@from))
                {
                    _nodes.Add(@from, fromNode = MakeGraphNode(@from, c.From));
                    _graph.Add(fromNode);
                    
                    AddChildrenRecursively(@from, c.From);
                }
                else
                    fromNode = _nodes[@from];

                if (!_nodes.ContainsKey(to))
                {
                    _nodes.Add(to, toNode = MakeGraphNode(to, c.To));
                    _graph.Add(toNode);
                    
                    AddChildrenRecursively(to, c.To);
                }
                else
                    toNode = _nodes[to];
                
                _graph.Add(new DotArrow(fromNode, toNode){ArrowLabel = c.Mode, ArrowColor = DotColor.Darkorange});
            }
        }

        private void AddEdge(Node @from, long? fromId, Node to, long? toId, string arrowLabel)
        {
                static DotNode MakeGraphNode(Node node, long? id) => new DotNode(id.HasValue ? $"\"{node.Name} - {id}\"" : $"\"{Guid.NewGuid().ToString()}\"")
                {
                    Shape = DotNodeShape.Box,
                    Style = DotNodeStyle.Dashed,
                    Label = $"{@node.Name} - {id}\\l{string.Join("\\l", node.Properties.Select(p=>p.ToString()))}"
                };
                
                DotNode fromNode, toNode;
                if (!_nodes.ContainsKey(@from))
                {
                    _nodes.Add(@from, fromNode = MakeGraphNode(@from, null));
                    _graph.Add(fromNode);
                }
                else
                    fromNode = _nodes[@from];

                if (!_nodes.ContainsKey(to))
                {
                    _nodes.Add(to, toNode = MakeGraphNode(to, null));
                    _graph.Add(toNode);
                }
                else
                    toNode = _nodes[to];
                
                _graph.Add(new DotArrow(fromNode, toNode){ArrowLabel = arrowLabel});
        }

        public override string ToString() => _graph.Compile(false);
    }
}