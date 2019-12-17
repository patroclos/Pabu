using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;

namespace BasicTesting.Fbx
{
    public readonly struct Node
    {
        public readonly string Name;
        public readonly IReadOnlyList<IProperty> Properties;
        public readonly IReadOnlyList<Node> Children;

        public Node(string name, IReadOnlyList<IProperty> properties, IReadOnlyList<Node> children)
        {
            Name = name;
            Properties = properties;
            Children = children;
        }

        public void WriteTo(IndentedTextWriter writer)
        {
            writer.WriteLine($"[{Name}]:");
            writer.Indent++;
            if (Properties.Any())
            {
                writer.WriteLine("Properties");
                writer.Indent++;
                foreach (var property in Properties)
                    property.WriteTo(writer);
                writer.Indent--;
            }

            if (Children.Any())
            {
                writer.WriteLine("Children");
                writer.Indent++;
                foreach (var child in Children)
                    child.WriteTo(writer);
                writer.Indent--;
            }

            writer.Indent--;
        }
    }
}