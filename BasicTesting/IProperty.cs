using System.CodeDom.Compiler;

namespace BasicTesting.Fbx
{
    public interface IProperty
    {
        PropertyTypeCode TypeCode { get; }
        void WriteTo(IndentedTextWriter writer);
    }

    public interface IProperty<T> : IProperty
    {
        T Value { get; }
    }
}