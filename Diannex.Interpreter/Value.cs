using System;
using System.Collections.Generic;
using System.Text;

namespace Diannex.Interpreter
{
    public class Value
    {
        public enum ValueType
        {
            Undefined,
            String,
            Int32,
            Double,
            Array
        }

        public dynamic Data;
        public ValueType Type;

        public Value(dynamic data, ValueType type)
        {
            Data = data;
            Type = type;
        }

        public static Value operator +(Value a)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            return a;
        }

        public static Value operator-(Value a)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            return new Value(-a.Data, a.Type);
        }

        public static Value operator+(Value a, Value b)
        {
            if (a.Type == ValueType.String && b.Type == ValueType.String)
            {
                return new Value(a.Data + b.Data, ValueType.String);
            }

            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            // Take the bigger type, or default to a's type if both are the same
            return new Value(a.Data + b.Data, a.Type >= b.Type ? a.Type : b.Type);
        }

        public static Value operator -(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            // Take the bigger type, or default to a's type if both are the same
            return new Value(a.Data - b.Data, a.Type >= b.Type ? a.Type : b.Type);
        }

        public static Value operator *(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            // Take the bigger type, or default to a's type if both are the same
            return new Value(a.Data * b.Data, a.Type >= b.Type ? a.Type : b.Type);
        }

        public static Value operator /(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            // Take the bigger type, or default to a's type if both are the same
            return new Value(a.Data / b.Data, a.Type >= b.Type ? a.Type : b.Type);
        }

        public static Value operator!(Value a)
        {
            if (a.Type == ValueType.Double)
            {
                double result = a.Data > 0 ? 0 : 1;
                return new Value(result, ValueType.Double);
            }

            if (a.Type == ValueType.Int32)
            {
                int result = a.Data > 0 ? 0 : 1;
                return new Value(result, ValueType.Int32);
            }

            if (a.Type == ValueType.String)
            {
                int result = ((string)a.Data).Length > 0 ? 0 : 1;
                return new Value(result, ValueType.Int32);
            }

            if (a.Type == ValueType.Array)
            {
                int result = ((Value[])a.Data).Length > 0 ? 0 : 1;
            }

            throw new ValueConversionException("Value is not invertable!");
        }

        public static Value operator%(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not a numerical!");
            }

            return new Value(a.Data % b.Data, a.Type >= b.Type ? a.Type : b.Type);
        }

        public static Value operator&(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 || b.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not AND-able!");
            }

            return new Value(a.Data % b.Data, ValueType.Int32);
        }

        public static Value operator |(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 || b.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not OR-able!");
            }

            return new Value(a.Data | b.Data, ValueType.Int32);
        }

        public static Value operator ^(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 || b.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not XOR-able!");
            }

            return new Value(a.Data ^ b.Data, ValueType.Int32);
        }

        public static Value operator ~(Value a)
        {
            if (a.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not Negate-able!");
            }

            return new Value(~a.Data, ValueType.Int32);
        }

        public static Value operator <<(Value a, int b)
        {
            if (a.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not LSHIFTable!");
            }

            return new Value(a.Data << b, ValueType.Int32);
        }

        public static Value operator >>(Value a, int b)
        {
            if (a.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not RSHIFTable!");
            }

            return new Value(a.Data >> b, ValueType.Int32);
        }

        public static Value operator ==(Value a, Value b)
        {
            if (a.Type != b.Type)
            {
                return new Value(0, ValueType.Int32);
            }

            return new Value(a.Data == b.Data ? 1 : 0, ValueType.Int32);
        }

        public static Value operator !=(Value a, Value b)
        {
            return new Value(!(a == b), ValueType.Int32);
        }

        public static Value operator <(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not comparable!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not comparable!");
            }

            return new Value(a.Data < b.Data ? 1 : 0, ValueType.Int32);
        }

        public static explicit operator bool(Value a)
        {
            if (a.Type == ValueType.Double || a.Type == ValueType.Int32)
                return a.Data > 0;
            if (a.Type == ValueType.String || a.Type == ValueType.Array)
                return a.Data.Length > 0;
            return false;
        }

        public static Value operator >(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not comparable!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not comparable!");
            }

            return new Value(a.Data > b.Data ? 1 : 0, ValueType.Int32);
        }

        public static Value operator <=(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not comparable!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not comparable!");
            }

            return new Value(a.Data <= b.Data ? 1 : 0, ValueType.Int32);
        }

        public static Value operator >=(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 && a.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not comparable!");
            }

            if (b.Type != ValueType.Int32 && b.Type != ValueType.Double)
            {
                throw new ValueConversionException("Value is not comparable!");
            }

            return new Value(a.Data >= b.Data ? 1 : 0, ValueType.Int32);
        }

        public override bool Equals(object obj)
        {
            if (obj is Value v)
            {
                return (bool)(this == v);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        public class ValueConversionException : Exception
        {
            public ValueConversionException(string message) : base(message)
            {

            }
        }
    }
}
