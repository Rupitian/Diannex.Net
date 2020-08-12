using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diannex.NET
{
    /// <summary>
    /// Holds external Diannex functions to be invoked by the Interpreter.
    /// </summary>
    public class FunctionHandler
    {
        private Dictionary<string, Func<Value[], Value>> funcs = new Dictionary<string, Func<Value[], Value>>();

        /// <param name="withAttributes">Specifies whether to automatically register methods with <seealso cref="DiannexFunctionAttribute"/></param>
        public FunctionHandler(bool withAttributes = true)
        {
            if (!withAttributes) return;

            var functions =
                from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                from t in a.GetTypes()
                from f in t.GetMethods()
                where f.IsDefined(typeof(DiannexFunctionAttribute), false)
                select new { Function = f, Attribute = (DiannexFunctionAttribute)f.GetCustomAttributes(typeof(DiannexFunctionAttribute), false)[0] };

            foreach (var func in functions)
            {
                string name = func.Attribute.DiannexName;
                var parameters = func.Function.GetParameters();
                var returnType = func.Function.ReturnType;
                funcs.Add(name, (args) =>
                {
                    List<object> arguments = new List<object>();
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        var param = parameters[i];
                        if (i >= args.Length)
                        {
                            if (param.HasDefaultValue)
                                arguments.Add(param.DefaultValue);
                            else
                                throw new ArgumentException($"Arguments to unamanged '{name}' do not match managed method '{func.Function.Name}'", param.Name);

                        }
                        var arg = args[i];
                        if (param.ParameterType == typeof(double))
                        {
                            if (arg.Type == Value.ValueType.Int32 || arg.Type == Value.ValueType.Double)
                            {
                                arguments.Add((double)arg.Data);
                            }
                            throw new ArgumentException($"Arguments to unamanged '{name}' do not match managed method '{func.Function.Name}'", param.Name);
                        }
                        else if (param.ParameterType == typeof(int))
                        {
                            if (arg.Type == Value.ValueType.Int32 || arg.Type == Value.ValueType.Double)
                            {
                                arguments.Add((int)arg.Data);
                            }
                            throw new ArgumentException($"Arguments to unamanged '{name}' do not match managed method '{func.Function.Name}'", param.Name);
                        }
                        else if (param.ParameterType == typeof(string))
                        {
                            if (arg.Type == Value.ValueType.Int32 || arg.Type == Value.ValueType.Double)
                            {
                                arguments.Add($"{arg.Data}");
                            }
                            else if (arg.Type == Value.ValueType.String)
                            {
                                arguments.Add(arg.Data);
                            }
                            throw new ArgumentException($"Arguments to unamanged '{name}' do not match managed method '{func.Function.Name}'", param.Name);
                        }
                        else
                        {
                            throw new ArgumentException($"Arguments in managed method '{func.Function.Name}' aren't castable from unmanaged method '{name}'", param.Name);
                        }
                    }

                    var result = func.Function.Invoke(null, arguments.ToArray());
                    if (result == null)
                        return new Value();
                    if (returnType == typeof(string))
                        return new Value((string)result);
                    else if (returnType == typeof(double))
                        return new Value((double)result);
                    else if (returnType == typeof(int))
                        return new Value((int)result);
                    throw new InvalidCastException($"Return type of managed method '{func.Function.Name}' aren't castable to an unmanaged Value");
                });
            }
        }

        public Value Invoke(string name, Value[] args)
        {
            if (funcs.ContainsKey(name))
                return funcs[name](args);
            throw new Exception("Invalid function.");
        }

        public void RegisterFunction(string name, Func<Value[], Value> func)
        {
            funcs[name] = func;
        }

        public void UnregisterFunction(string name)
        {
            if (funcs.ContainsKey(name))
                funcs.Remove(name);
        }

        public void Clear()
        {
            funcs.Clear();
        }
    }

    /// <summary>
    /// Specifies that the following method should be registered as a Diannex external function.<br/><br/>
    /// Restraints:<br/>
    /// * Method must be static.<br/>
    /// * Method may have optional parameters.<br/>
    /// * Method must return any of the following: <b>void</b>, <b>double</b>, <b>int</b>, <b>string</b>.<br/>
    /// * Method's parameters must be one of the following: <b>double</b>, <b>int</b>, <b>string</b>.<br/>
    /// <br/>
    /// Note, These specific parameter combinations can be casted:<br/>
    /// * DiannexInt32 can be casted to <b>double</b>.<br/>
    /// * DiannexDouble can be casted to <b>int</b>.<br/>
    /// * DiannexInt32 and DiannexDouble can be casted to <b>string</b>.<br/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class DiannexFunctionAttribute : Attribute
    {
        /// <summary>
        /// The name exposed to a Diannex script.
        /// </summary>
        public string DiannexName;

        /// <param name="diannexName">Exposed function name</param>
        public DiannexFunctionAttribute(string diannexName)
        {
            DiannexName = diannexName;
        }
    }
}
