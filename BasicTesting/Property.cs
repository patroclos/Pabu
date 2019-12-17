using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace BasicTesting.Fbx
{
    public readonly struct Property<T> : IProperty<T>
    {
        private static readonly Dictionary<Type, PropertyTypeCode> _TypeCodeMap = new Dictionary<Type, PropertyTypeCode>
        {
            {typeof(short), PropertyTypeCode.SShort},
            {typeof(bool), PropertyTypeCode.Bit},
            {typeof(int), PropertyTypeCode.Int},
            {typeof(float), PropertyTypeCode.Float},
            {typeof(double), PropertyTypeCode.Double},
            {typeof(long), PropertyTypeCode.Long},
            {typeof(List<float>), PropertyTypeCode.FloatArr},
            {typeof(List<double>), PropertyTypeCode.DoubleArr},
            {typeof(List<long>), PropertyTypeCode.LongArr},
            {typeof(List<int>), PropertyTypeCode.IntArr},
            {typeof(List<bool>), PropertyTypeCode.BoolArr},
            {typeof(string), PropertyTypeCode.String},
            {typeof(ReadOnlyMemory<byte>), PropertyTypeCode.Raw},
        };

        private readonly T _value;
        private readonly PropertyTypeCode _typeCode;
        public T Value => _value;
        public PropertyTypeCode TypeCode => _typeCode;

        public Property(T value)
        {
            if (_TypeCodeMap.ContainsKey(value?.GetType() ?? throw new NoNullAllowedException()))
                _typeCode = _TypeCodeMap[(_value = value).GetType()];
            else
            {
                _value = value;
                _typeCode = default;
            }
        }

        public void WriteTo(IndentedTextWriter writer)
        {
            writer.Write($"Property<{_typeCode}>:");
            writer.Indent++;
            if (new[]
            {
                PropertyTypeCode.BoolArr,
                PropertyTypeCode.IntArr,
                PropertyTypeCode.LongArr,
                PropertyTypeCode.FloatArr,
                PropertyTypeCode.DoubleArr,
            }.Contains(_typeCode))
                writer.WriteLine(string.Join(", ", (Value as IEnumerable).Cast<object>().Select(c=>c.ToString())));
            else if (_typeCode == PropertyTypeCode.Raw)
                writer.WriteLine(BitConverter.ToString((byte[]) (Value.GetType().GetMethod("ToArray").Invoke(Value, null))));
            else
                writer.WriteLine(Value.ToString());
            writer.Indent--;
        }
    }
}