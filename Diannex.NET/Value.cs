using System;

namespace Diannex.NET
{
    /// <summary>
    /// A Diannex Value as seen by the code.
    /// </summary>
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

        public string StringValue
        {
            get
            {
                return Type switch
                {
                    ValueType.String => stringValue,
                    ValueType.Int32 => $"{intValue}",
                    ValueType.Double => $"{doubleValue}",
                    ValueType.Undefined => null,
                    _ => throw new NotImplementedException(),
                };
            }
        }
        public int IntValue
        {
            get
            {
                return Type switch
                {
                    ValueType.Int32 => intValue,
                    ValueType.Double => (int)doubleValue,
                    _ => throw new NotImplementedException(),
                };
            }
        }
        public double DoubleValue
        {
            get
            {
                return Type switch
                {
                    ValueType.Int32 => (double)intValue,
                    ValueType.Double => doubleValue,
                    _ => throw new NotImplementedException(),
                };
            }
        }
        public Value[] ArrayValue
        {
            get
            {
                return Type switch
                {
                    ValueType.Array => arrayValue,
                    _ => throw new NotImplementedException(),
                };
            }
        }

        private string stringValue = default;
        private int intValue = default;
        private double doubleValue = default;
        private Value[] arrayValue = default;

        public ValueType Type = ValueType.Undefined;

        public Value()
        { }

        public Value(string data, ValueType type = ValueType.String)
        {
            stringValue = data;
            Type = type;
        }

        public Value(int data, ValueType type = ValueType.Int32)
        {
            intValue = data;
            Type = type;
        }

        public Value(double data, ValueType type = ValueType.Double)
        {
            doubleValue = data;
            Type = type;
        }

        public Value(Value[] data, ValueType type = ValueType.Array)
        {
            arrayValue = data;
            Type = type;
        }

        public Value(Value that)
        {
            this.Type = that.Type;
            this.stringValue = that.stringValue;
            this.intValue = that.intValue;
            this.doubleValue = that.doubleValue;
            this.arrayValue = that.arrayValue;
        }

        public static Value operator +(Value a)
        {
            if (a.Type == ValueType.Int32)
            {
                return new Value(+a.intValue, a.Type);
            }

            if (a.Type == ValueType.Double)
            {
                return new Value(+a.doubleValue, a.Type);
            }

            throw new ValueConversionException("Value is not a numerical!");
        }

        public static Value operator -(Value a)
        {
            if (a.Type == ValueType.Int32)
            {
                return new Value(-a.intValue, a.Type);
            }

            if (a.Type == ValueType.Double)
            {
                return new Value(-a.doubleValue, a.Type);
            }

            throw new ValueConversionException("Value is not a numerical!");
        }

        public static Value operator +(Value a, Value b)
        {
            if (a.Type == ValueType.String && b.Type == ValueType.String)
            {
                return new Value(a.stringValue + b.stringValue, ValueType.String);
            }

            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                if (a.Type == ValueType.Double || b.Type == ValueType.Double)
                {
                    return new Value(a.doubleValue + b.doubleValue, ValueType.Double);
                }
                else
                {
                    return new Value(a.intValue + b.intValue, ValueType.Int32);
                }
            }
            
            throw new ValueConversionException("Value is not a numerical!");
        }

        public static Value operator -(Value a, Value b)
        {
            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                if (a.Type == ValueType.Double || b.Type == ValueType.Double)
                {
                    return new Value(a.doubleValue - b.doubleValue, ValueType.Double);
                }
                else
                {
                    return new Value(a.intValue - b.intValue, ValueType.Int32);
                }
            }

            throw new ValueConversionException("Value is not a numerical!");
        }

        public static Value operator *(Value a, Value b)
        {
            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                if (a.Type == ValueType.Double || b.Type == ValueType.Double)
                {
                    return new Value(a.doubleValue * b.doubleValue, ValueType.Double);
                }
                else
                {
                    return new Value(a.intValue * b.intValue, ValueType.Int32);
                }
            }

            throw new ValueConversionException("Value is not a numerical!");
        }

        public static Value operator /(Value a, Value b)
        {
            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                if (a.Type == ValueType.Double || b.Type == ValueType.Double)
                {
                    return new Value(a.doubleValue / b.doubleValue, ValueType.Double);
                }
                else
                {
                    return new Value(a.intValue / b.intValue, ValueType.Int32);
                }
            }

            throw new ValueConversionException("Value is not a numerical!");
        }

        public static Value operator !(Value a)
        {
            if (a.Type == ValueType.Double)
            {
                return new Value(a.doubleValue > 0 ? 0 : 1, ValueType.Double);
            }

            if (a.Type == ValueType.Int32)
            {
                return new Value(a.intValue > 0 ? 0 : 1, ValueType.Int32);
            }

            if (a.Type == ValueType.String)
            {
                return new Value(a.stringValue.Length > 0 ? 0 : 1, ValueType.Int32);
            }

            if (a.Type == ValueType.Array)
            {
                return new Value(a.arrayValue.Length > 0 ? 0 : 1, ValueType.Int32);
            }

            throw new ValueConversionException("Value is not invertable!");
        }

        public static Value operator %(Value a, Value b)
        {
            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                if (a.Type == ValueType.Double || b.Type == ValueType.Double)
                {
                    return new Value(a.doubleValue % b.doubleValue, ValueType.Double);
                }
                else
                {
                    return new Value(a.intValue % b.intValue, ValueType.Int32);
                }
            }

            throw new ValueConversionException("Value is not a numerical!");
        }

        public static Value operator &(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 || b.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not AND-able!");
            }

            return new Value(a.intValue & b.intValue, ValueType.Int32);
        }

        public static Value operator |(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 || b.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not OR-able!");
            }

            return new Value(a.intValue | b.intValue, ValueType.Int32);
        }

        public static Value operator ^(Value a, Value b)
        {
            if (a.Type != ValueType.Int32 || b.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not XOR-able!");
            }

            return new Value(a.intValue ^ b.intValue, ValueType.Int32);
        }

        public static Value operator ~(Value a)
        {
            if (a.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not Negate-able!");
            }

            return new Value(~a.intValue, ValueType.Int32);
        }

        public static Value operator <<(Value a, int b)
        {
            if (a.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not LSHIFTable!");
            }

            return new Value(a.intValue << b, ValueType.Int32);
        }

        public static Value operator >>(Value a, int b)
        {
            if (a.Type != ValueType.Int32)
            {
                throw new ValueConversionException("Value is not RSHIFTable!");
            }

            return new Value(a.intValue >> b, ValueType.Int32);
        }

        public static Value operator ==(Value a, Value b)
        {
            if (a.Type != b.Type)
            {
                return new Value(0, ValueType.Int32);
            }

            return a.Type switch
            {
                ValueType.String => new Value(a.stringValue == b.stringValue ? 1 : 0, ValueType.Int32),
                ValueType.Int32 => new Value(a.intValue == b.intValue ? 1 : 0, ValueType.Int32),
                ValueType.Double => new Value(a.doubleValue == b.doubleValue ? 1.0 : 0.0, ValueType.Double),
                ValueType.Array => new Value(a.arrayValue == b.arrayValue ? 1 : 0, ValueType.Int32),
                _ => new Value(0, ValueType.Int32),
            };
        }

        public static Value operator !=(Value a, Value b)
        {
            return new Value((!(a == b)).intValue, ValueType.Int32);
        }

        public static Value operator <(Value a, Value b)
        {
            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                return new Value(
                    (a.Type == ValueType.Int32 ? a.intValue : a.doubleValue) <
                    (b.Type == ValueType.Int32 ? b.intValue : b.doubleValue) ? 1 : 0,
                    ValueType.Int32);
            }

            throw new ValueConversionException("Value is not comparable!");
        }

        public static explicit operator bool(Value a)
        {
            return a.Type switch
            {
                ValueType.String => a.stringValue.Length > 0,
                ValueType.Int32 => a.intValue > 0,
                ValueType.Double => a.doubleValue > 0,
                ValueType.Array => a.arrayValue.Length > 0,
                _ => false,
            };
        }

        public static Value operator >(Value a, Value b)
        {
            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                return new Value(
                    (a.Type == ValueType.Int32 ? a.intValue : a.doubleValue) >
                    (b.Type == ValueType.Int32 ? b.intValue : b.doubleValue) ? 1 : 0,
                    ValueType.Int32);
            }

            throw new ValueConversionException("Value is not comparable!");
        }

        public static Value operator <=(Value a, Value b)
        {
            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                return new Value(
                    (a.Type == ValueType.Int32 ? a.intValue : a.doubleValue) <=
                    (b.Type == ValueType.Int32 ? b.intValue : b.doubleValue) ? 1 : 0,
                    ValueType.Int32);
            }

            throw new ValueConversionException("Value is not comparable!");
        }

        public static Value operator >=(Value a, Value b)
        {
            if ((a.Type == ValueType.Int32 || a.Type == ValueType.Double) && (b.Type == ValueType.Int32 || b.Type == ValueType.Double))
            {
                return new Value(
                    (a.Type == ValueType.Int32 ? a.intValue : a.doubleValue) >=
                    (b.Type == ValueType.Int32 ? b.intValue : b.doubleValue) ? 1 : 0,
                    ValueType.Int32);
            }

            throw new ValueConversionException("Value is not comparable!");
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
            return Type switch
            {
                ValueType.String => stringValue.GetHashCode(),
                ValueType.Int32 => intValue.GetHashCode(),
                ValueType.Double => doubleValue.GetHashCode(),
                ValueType.Array => arrayValue.GetHashCode(),
                _ => base.GetHashCode(),
            };
        }

        public class ValueConversionException : Exception
        {
            public ValueConversionException(string message) : base(message) { }
        }
    }
}
