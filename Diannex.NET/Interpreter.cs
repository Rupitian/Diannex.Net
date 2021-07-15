using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Diannex.NET
{
    /// <summary>
    /// The Interpreter for the Diannex bytecode.
    /// </summary>
    public class Interpreter
    {
        /// <summary>
        /// A method used to randomly decide if a Choice/Choose option will be displayed.
        /// </summary>
        /// <param name="chance">The normalized chance of being displayed.<br/>By default it's 1 if no chances were declared.</param>
        /// <returns>Whether or not the Choice/Choose option should be displayed.</returns>
        public delegate bool ChanceHandler(double chance);
        /// <summary>
        /// A method used to randomly pick a Choose option.
        /// </summary>
        /// <param name="weights">A list of normalized weights for each Choose option.<br/>By default a weight is 1 if no weight was specified for that specific Choose option.</param>
        /// <returns>Which Choose option to select.</returns>
        public delegate int WeightedChanceHandler(double[] weights);

        /// <summary>
        /// <inheritdoc cref="Diannex.NET.Binary"/>
        /// </summary>
        public Binary Binary;
        /// <summary>
        /// A map of variables that persists between scopes.
        /// </summary>
        public Dictionary<string, Value> GlobalVariableStore;
        /// <summary>
        /// A map of global flags that persists between execution of the same scope.
        /// </summary>
        public Dictionary<string, Value> Flags;
        /// <summary>
        /// See <see cref="Diannex.NET.FunctionHandler"/>.
        /// </summary>
        public FunctionHandler FunctionHandler;

        /// <summary>
        /// See <see cref="ChanceHandler"/>.
        /// </summary>
        public ChanceHandler ChanceCallback;
        /// <summary>
        /// See <see cref="WeightedChanceHandler"/>.
        /// </summary>
        public WeightedChanceHandler WeightedChanceCallback;
        /// <summary>
        /// True whenever the Interpreter is processing Choices.
        /// </summary>
        public bool InChoice { get; private set; }
        /// <summary>
        /// True whenever the Interpreter is waiting for the User to select a Choice.
        /// </summary>
        /// <remarks>
        /// See <seealso cref="ChooseChoice(int)"/>.
        /// </remarks>
        public bool SelectChoice { get; private set; }
        /// <summary>
        /// True whenever the Interpreter is waiting for the User to finish displaying <seealso cref="CurrentText"/>.
        /// </summary>
        public bool RunningText { get; private set; }
        /// <summary>
        /// True whenever the Interpreter is waiting for any reason, or if <seealso cref="SceneCompleted"/> is true.
        /// </summary>
        public bool Paused { get; private set; }
        /// <summary>
        /// True whenever the Interpreter is done executing the <seealso cref="CurrentScene"/>.
        /// </summary>
        public bool SceneCompleted { get; private set; }
        /// <summary>
        /// The name of the currently running scene as set by <seealso cref="RunScene(string)"/>.
        /// </summary>
        public string CurrentScene { get; private set; }
        /// <summary>
        /// The line of dialogue to be displayed.
        /// </summary>
        public string CurrentText { get; private set; }
        /// <summary>
        /// A list of choices to display to the user to pick from.
        /// </summary>
        public List<string> Choices => choices.Select(c => c.Item2).ToList();

        private List<(int /* Relative Address to jump to */, string /* Text to display to the user */)> choices;
        private int instructionPointer;
        private Stack<Value> stack;
        private Value saveRegister;
        private LocalVariableStore localVarStore;
        private Stack<(int, Stack<Value>, LocalVariableStore)> callStack;
        private List<(double, int)> chooseOptions;
        private Dictionary<string, string> definitions;

        /// <summary>
        /// The Interpreter for the Diannex bytecode.
        /// </summary>
        /// <param name="binary">The specific Diannex <seealso cref="Diannex.NET.Binary"/> to interpret.</param>
        /// <param name="functionHandler">Used for executing external methods in Diannex code.</param>
        /// <param name="chanceCallback">See <see cref="ChanceHandler"/>.</param>
        /// <param name="weightedChanceCallback">See <see cref="WeightedChanceHandler"/>.</param>
        public Interpreter(Binary binary, FunctionHandler functionHandler, ChanceHandler chanceCallback = null, WeightedChanceHandler weightedChanceCallback = null)
        {
            Binary = binary;
            GlobalVariableStore = new Dictionary<string, Value>();
            Flags = new Dictionary<string, Value>();
            InChoice = false;
            SelectChoice = false;
            RunningText = false;
            
            if (chanceCallback == null)
                ChanceCallback = (d) => d == 1 || new Random().NextDouble() < d;
            else
                ChanceCallback = chanceCallback;

            if (weightedChanceCallback == null)
            {
                WeightedChanceCallback = (weights) =>
                {
                    double sum = 0;
                    double[] fixedWeights = new double[weights.Length];
                    for (int i = 0; i < weights.Length; i++)
                    {
                        fixedWeights[i] = sum;
                        sum += weights[i];
                    }

                    var random = new Random().NextDouble() * (sum - 1);
                    int selection = -1;
                    double previous = -1;

                    for (int i = 0; i < fixedWeights.Length; i++)
                    {
                        var current = fixedWeights[i];
                        if (Math.Round(random) >= current && current > previous)
                        {
                            selection = i;
                            previous = current;
                        }
                    }
                    return selection;
                };
            }
            else
            {
                WeightedChanceCallback = weightedChanceCallback;
            }

            CurrentScene = null;
            SceneCompleted = false;
            CurrentText = null;

            instructionPointer = 0;
            Paused = true;
            stack = new Stack<Value>();
            saveRegister = null;
            localVarStore = new LocalVariableStore(this);
            callStack = new Stack<(int, Stack<Value>, LocalVariableStore)>();
            choices = new List<(int, string)>();
            FunctionHandler = functionHandler;
            chooseOptions = new List<(double, int)>();
            definitions = new Dictionary<string, string>();

            if (Binary.TranslationLoaded)
            {
                foreach (var def in Binary.Definitions)
                {
                    string val = GetDefinition(def);
                    definitions.Add(Binary.StringTable[def.Item1], val);
                }
            }
        }

        /// <summary>
        /// Gets a flag from the Diannex Context.
        /// </summary>
        /// <remarks>
        /// NOTE: The Scene/Method where the flag is defined <b>MUST BE EXECUTED FIRST</b> before the flag will populate.
        /// </remarks>
        /// <param name="flag">The flag to get.</param>
        /// <returns>The flag as a <seealso cref="Value"/>.</returns>
        public Value GetFlag(string flag)
        {
            return Flags[flag];
        }

        /// <summary>
        /// Sets a flag from the Diannex Context.
        /// </summary>
        /// <remarks>
        /// If a flag is set before it is defined (see remark on <seealso cref="GetFlag(string)"/>), then the value will be treated as the default.
        /// <br/>
        /// <br/>
        /// WARNING: The value <b>MUST BE OF THE SAME TYPE</b> as it is defined the code, otherwise undefined behaviour may occur.
        /// </remarks>
        /// <param name="flag">The flag to set.</param>
        /// <param name="value">The <seealso cref="Value"/> to set the flag to.</param>
        public void SetFlag(string flag, Value value)
        {
            Flags[flag] = value;
        }

        /// <summary>
        /// Loads a translation file that's generated by the Diannex compiler.
        /// </summary>
        /// <remarks>
        /// NOTE: Loading a translation file will reload all the Definitions.
        /// </remarks>
        /// <param name="path">The path to the translation file.</param>
        public void LoadTranslationFile(string path)
        {
            using StreamReader reader = new StreamReader(File.OpenRead(path));
            Binary.TranslationTable =
                reader
                    .ReadToEnd()
                    .Split('\n')
                    .Where(s => !s.StartsWith('#') && !s.StartsWith('@') && !string.IsNullOrWhiteSpace(s))
                    .Select(s => s[1..^1])
                    .ToList();
            Binary.TranslationLoaded = true;
            foreach (var def in Binary.Definitions)
            {
                string name = Binary.StringTable[def.Item1];
                string val = GetDefinition(def);
                if (definitions.ContainsKey(name))
                {
                    definitions[name] = val;
                }
                else
                {
                    definitions.Add(name, val);
                }
            }
        }

        /// <summary>
        /// Begins execution of a scene from the Diannex code.
        /// </summary>
        /// <remarks>
        /// NOTE: If the scene has flags, this is where they'll be populated.
        /// </remarks>
        /// <param name="sceneName">The symbol name of the scene to run (e.g. "test.scene0")</param>
        public void RunScene(string sceneName)
        {
            if (!Binary.TranslationLoaded && Binary.TranslationTable.Count == 0)
            {
                Console.Error.WriteLine("[WARNING]: Currently no translations have been loaded! The program will crash when trying to run dialogue!");
            }

            var sceneId = LookupScene(sceneName);
            var scene = Binary.Scenes[sceneId];
            var bytecodeIndexes = scene.Item2;
            localVarStore = new LocalVariableStore(this);
            stack = new Stack<Value>();
            for (int i = 1, flagIndex = 0; i < bytecodeIndexes.Count; flagIndex++)
            {
                instructionPointer = bytecodeIndexes[i++];
                Paused = false;
                while (!Paused)
                    Update();
                var value = stack.Pop();

                instructionPointer = bytecodeIndexes[i++];
                Paused = false;
                while (!Paused)
                    Update();
                var name = stack.Pop();

                if (!Flags.ContainsKey(name.StringValue))
                {
                    SetFlag(name.StringValue, value);
                }
                localVarStore.FlagMap.Add(flagIndex, name.StringValue);
            }

            Paused = false;
            instructionPointer = bytecodeIndexes[0];
            CurrentScene = sceneName;
        }

        /// <summary>
        /// If <seealso cref="SelectChoice"/> is true, this will select the choice and resume the Interpreter.
        /// </summary>
        /// <remarks>
        /// WARNING: If this runs before <seealso cref="SelectChoice"/> is true, it will throw an exception!
        /// </remarks>
        /// <param name="idx">The 0 based index of choice, relative to <seealso cref="Choices"/>.</param>
        public void ChooseChoice(int idx)
        {
            if (idx >= choices.Count)
                throw new IndexOutOfRangeException($"Choice at index {idx} is outside of the range of choices.");
            var (ip,_) = choices[idx];
            instructionPointer = ip;
            SelectChoice = false;
            Paused = false;
        }

        /// <summary>
        /// Resumes the interpreter if it's <seealso cref="Paused"/>.
        /// </summary>
        public void Resume()
        {
            // TODO: Check for more invalid states
            if (RunningText) RunningText = false;
            if (CurrentScene == null || !Paused) return;

            if (!SelectChoice) Paused = false;
        }

        /// <summary>
        /// Steps through the Bytecode instruction by instruction.
        /// </summary>
        /// <remarks>
        /// NOTE: If <seealso cref="Paused"/> is true, this method will do nothing,
        /// so you don't need to check if the Interpreter is paused before running
        /// the method.
        /// </remarks>
        public void Update()
        {
            if (Paused) return;

            var opcode = (Opcode)Binary.Instructions[instructionPointer++];
            int arg1 = default, arg2 = default;
            double argDouble = default;

            switch (opcode)
            {
                case Opcode.FreeLocal:
                case Opcode.PushInt:
                case Opcode.PushString:
                case Opcode.PushBinaryString:
                case Opcode.MakeArray:
                case Opcode.SetVarGlobal:
                case Opcode.SetVarLocal:
                case Opcode.PushVarGlobal:
                case Opcode.PushVarLocal:
                case Opcode.Jump:
                case Opcode.JumpTruthy:
                case Opcode.JumpFalsey:
                case Opcode.ChoiceAdd:
                case Opcode.ChoiceAddTruthy:
                case Opcode.ChooseAdd:
                case Opcode.ChooseAddTruthy:
                    arg1 = BitConverter.ToInt32(Binary.Instructions, instructionPointer);
                    instructionPointer += 4;
                    break;
                case Opcode.PushInterpolatedString:
                case Opcode.PushBinaryInterpolatedString:
                case Opcode.Call:
                case Opcode.CallExternal:
                    arg1 = BitConverter.ToInt32(Binary.Instructions, instructionPointer);
                    instructionPointer += 4;
                    arg2 = BitConverter.ToInt32(Binary.Instructions, instructionPointer);
                    instructionPointer += 4;
                    break;
                case Opcode.PushDouble:
                    argDouble = BitConverter.ToDouble(Binary.Instructions, instructionPointer);
                    instructionPointer += 8;
                    break;
            }

            // Prepare for if statement fuck storm
            if (opcode == Opcode.Nop)
                return;

            #region Stack Instructions
            if (opcode == Opcode.FreeLocal)
                localVarStore.Delete(arg1);

            if (opcode == Opcode.Save)
                saveRegister = stack.Peek();
            if (opcode == Opcode.Load)
                stack.Push(new Value(saveRegister));

            if (opcode == Opcode.PushUndefined)
                stack.Push(new Value());
            if (opcode == Opcode.PushInt)
                stack.Push(new Value(arg1, Value.ValueType.Int32));
            if (opcode == Opcode.PushDouble)
                stack.Push(new Value(argDouble, Value.ValueType.Double));

            if (opcode == Opcode.PushString)
                stack.Push(new Value(Binary.TranslationTable[arg1], Value.ValueType.String));
            if (opcode == Opcode.PushInterpolatedString)
                stack.Push(new Value(Interpolate(Binary.TranslationTable[arg1], arg2), Value.ValueType.String));
            if (opcode == Opcode.PushBinaryString)
                stack.Push(new Value(Binary.StringTable[arg1], Value.ValueType.String));
            if (opcode == Opcode.PushBinaryInterpolatedString)
                stack.Push(new Value(Interpolate(Binary.StringTable[arg1], arg2), Value.ValueType.String));

            if (opcode == Opcode.MakeArray)
                stack.Push(ConstructArray(arg1));
            if (opcode == Opcode.PushArrayIndex)
            {
                var indx = stack.Pop();
                var arr = stack.Pop();
                stack.Push(arr.ArrayValue[indx.IntValue]);
            }
            if (opcode == Opcode.SetArrayIndex)
            {
                var val = stack.Pop();
                var indx = stack.Pop();
                var arr = stack.Pop();
                arr.ArrayValue[indx.IntValue] = val;
                stack.Push(arr);
            }

            if (opcode == Opcode.SetVarGlobal)
            {
                var val = stack.Pop();
                GlobalVariableStore[Binary.StringTable[arg1]] = val;
            }
            if (opcode == Opcode.SetVarLocal)
            {
                var val = stack.Pop();
                if (arg1 >= localVarStore.Count)
                {
                    int count = arg1 - localVarStore.Count - 1;
                    for (int i = 0; i < count; i++)
                        localVarStore.Add(new Value());
                    localVarStore.Add(val);
                }
                else
                {
                    localVarStore[arg1] = val;
                }
            }
            if (opcode == Opcode.PushVarGlobal)
                stack.Push(GlobalVariableStore[Binary.StringTable[arg1]]);
            if (opcode == Opcode.PushVarLocal)
                stack.Push(localVarStore[arg1]);

            if (opcode == Opcode.Pop)
                stack.Pop();
            if (opcode == Opcode.Duplicate)
            {
                var val = stack.Pop();
                stack.Push(val);
                stack.Push(val);
            }
            if (opcode == Opcode.Duplicate2)
            {
                var val1 = stack.Pop();
                var val2 = stack.Pop();
                stack.Push(val2);
                stack.Push(val1);
                stack.Push(val2);
                stack.Push(val1);
            }
            #endregion

            #region Value Modification
            if (opcode == Opcode.Addition)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 + val1);
            }
            if (opcode == Opcode.Subtraction)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 - val1);
            }
            if (opcode == Opcode.Multiply)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 * val1);
            }
            if (opcode == Opcode.Divide)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 / val1);
            }
            if (opcode == Opcode.Modulo)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 % val1);
            }
            if (opcode == Opcode.Negate)
                stack.Push(-stack.Pop());
            if (opcode == Opcode.Invert)
                stack.Push(!stack.Pop());

            if (opcode == Opcode.BitLeftShift)
            {
                int shift = stack.Pop().IntValue;
                var val = stack.Pop();
                stack.Push(val << shift);
            }
            if (opcode == Opcode.BitRightShift)
            {
                int shift = stack.Pop().IntValue;
                var val = stack.Pop();
                stack.Push(val >> shift);
            }
            if (opcode == Opcode.BitAnd)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 & val2);
            }
            if (opcode == Opcode.BitOr)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 | val2);
            }
            if (opcode == Opcode.BitExclusiveOr)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 ^ val2);
            }
            if (opcode == Opcode.BitNegate)
            {
                var val = stack.Pop();
                stack.Push(~val);
            }

            if (opcode == Opcode.Power)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(new Value(
                            Math.Pow(
                                val1.DoubleValue,
                                val2.DoubleValue),
                            Value.ValueType.Double));
            }
            #endregion

            #region Value Comparison
            if (opcode == Opcode.CompareEqual)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 == val2);
            }
            if (opcode == Opcode.CompareGreaterThan)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 > val2);
            }
            if (opcode == Opcode.CompareLessThan)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 < val2);
            }
            if (opcode == Opcode.CompareGreaterThanEqual)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 >= val2);
            }
            if (opcode == Opcode.CompareLessThanEqual)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 <= val2);
            }
            if (opcode == Opcode.CompareNotEqual)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 != val2);
            }
            #endregion

            #region Instruction Pointer Modification
            if (opcode == Opcode.Jump)
                instructionPointer += arg1;
            if (opcode == Opcode.JumpTruthy && (bool)stack.Pop())
                instructionPointer += arg1;
            if (opcode == Opcode.JumpFalsey && !((bool)stack.Pop()))
                instructionPointer += arg1;
            if (opcode == Opcode.Exit)
            {
                localVarStore.Clear();
                if (callStack.Count == 0)
                {
                    instructionPointer = -1;
                    Paused = true;
                    SceneCompleted = true;
                }
                else
                {
                    var cs = callStack.Pop();
                    instructionPointer = cs.Item1;
                    stack = cs.Item2;
                    localVarStore = cs.Item3;
                    stack.Push(new Value());
                }
            }
            if (opcode == Opcode.Return)
            {
                localVarStore.Clear();
                var returnVal = stack.Pop();
                var cs = callStack.Pop();
                instructionPointer = cs.Item1;
                stack = cs.Item2;
                localVarStore = cs.Item3;
                stack.Push(returnVal);
            }
            if (opcode == Opcode.Call)
            {
                Value[] val = new Value[arg2];
                for (int i = 0; i < arg2; i++)
                {
                    val[i] = stack.Pop();
                }
                
                var temp = new Stack<(int, Stack<Value>, LocalVariableStore)>(callStack);
                temp.Push((instructionPointer, stack, localVarStore));
                callStack.Clear();
                stack = new Stack<Value>();
                localVarStore = new LocalVariableStore(this);
                var bytecodeIndexes = Binary.Functions[arg1].Item2;
                for (int i = 1, flagIndex = 0; i < bytecodeIndexes.Count; i += 2, flagIndex++)
                {
                    instructionPointer = bytecodeIndexes[i];
                    Paused = false;
                    while (!Paused)
                    {
                        Update();
                    }
                    var value = stack.Pop();

                    instructionPointer = bytecodeIndexes[i + 1];
                    Paused = false;
                    while (!Paused)
                    {
                        Update();
                    }
                    var name = stack.Pop();

                    if (!Flags.ContainsKey(name.StringValue))
                    {
                        SetFlag(name.StringValue, value);
                    }
                    localVarStore.FlagMap.Add(flagIndex, name.StringValue);
                }

                Paused = false;
                callStack = temp;
                instructionPointer = bytecodeIndexes[0];

                for (int i = 0; i < arg2; i++)
                {
                    localVarStore.Add(val[i]);
                }
            }
            if (opcode == Opcode.CallExternal)
            {
                string name = Binary.StringTable[arg1];
                Value[] val = new Value[arg2];
                for (int i = 0; i < arg2; i++)
                {
                    val[i] = stack.Pop();
                }
                stack.Push(FunctionHandler.Invoke(name, val));
            }
            #endregion

            #region Choice/Choose
            if (opcode == Opcode.ChoiceBegin)
            {
                if (InChoice)
                    throw new InterpreterRuntimeException("Choice begins while another choice is being processed!");

                InChoice = true;
            }
            if (opcode == Opcode.ChoiceAdd)
            {
                if (!InChoice)
                    throw new InterpreterRuntimeException("Attempted to add a choice when no choice is being processed!");

                var chance = stack.Pop();
                var text = stack.Pop();
                if (ChanceCallback(chance.DoubleValue))
                    choices.Add((instructionPointer + arg1, text.StringValue));
            }
            if (opcode == Opcode.ChoiceAddTruthy)
            {
                if (!InChoice)
                    throw new InterpreterRuntimeException("Attempted to add a choice when no choice is being processed!");

                var chance = stack.Pop();
                var text = stack.Pop();
                var condition = stack.Pop();
                if ((bool)condition && ChanceCallback(chance.DoubleValue))
                {
                    choices.Add((instructionPointer + arg1, text.StringValue));
                }
            }
            if (opcode == Opcode.ChoiceSelect)
            {
                if (!InChoice)
                    throw new InterpreterRuntimeException("Attempted to wait for user choice when no choice is being processed!");
                if (choices.Count == 0)
                    throw new InterpreterRuntimeException("Attempted to wait for user choice when there's no choices to choose!");

                SelectChoice = true;
                Paused = true;
            }

            if (opcode == Opcode.ChooseAdd || opcode == Opcode.ChooseAddTruthy)
            {
                var chance = stack.Pop();
                if (opcode != Opcode.ChooseAddTruthy || (bool)stack.Pop())
                    chooseOptions.Add((chance.DoubleValue, instructionPointer + arg1));
            }

            if (opcode == Opcode.ChooseSel)
            {
                var selection = WeightedChanceCallback(chooseOptions.Select(t => t.Item1).ToArray());
                if (selection == -1 || selection >= chooseOptions.Count)
                    throw new IndexOutOfRangeException($"Selection returned by WeightedChanceCallback was out of bounds. Selection: {selection}");
                instructionPointer = chooseOptions[selection].Item2;
                chooseOptions.Clear();
            }

            if (opcode == Opcode.TextRun)
            {
                var text = stack.Pop();
                CurrentText = text.StringValue;
                RunningText = true;
                Paused = true;
            }
            #endregion
        }

        /// <summary>
        /// Retrieves a definition from the Diannex code
        /// </summary>
        /// <remarks>
        /// NOTE: Definitions are loaded when the Interpreter is constructed if the binary
        /// was created with no private/public translation files.<br/>Otherwise, they will be loaded
        /// whenever the <seealso cref="LoadTranslationFile(string)"/> method is called.
        /// <br/>
        /// <b>THIS MEANS INTERPOLATED STRINGS IN DEFINITIONS WON'T CHANGE UNTIL THEY'RE LOADED AGAIN.</b>
        /// </remarks>
        /// <param name="defname">The symbol name of the definition (e.g. "definitions.main")</param>
        /// <returns>The value of the definition, which will always be a string.</returns>
        public string GetDefinition(string defname)
        {
            if (definitions.ContainsKey(defname))
                return definitions[defname];
            return GetDefinition(LookupDefinition(defname));
        }

        private Value ConstructArray(int elementCount)
        {
            List<Value> values = new List<Value>();

            for (int i = 0; i < elementCount; i++)
            {
                values.Add(stack.Pop());
            }

            return new Value(values.ToArray(), Value.ValueType.Array);
        }

        private string Interpolate(string format, int exprCount)
        {
            object[] args = new object[exprCount];
            for (int i = 0; i < exprCount; i++)
            {
                var val = stack.Pop();
                args[i] = val.Type switch
                {
                    Value.ValueType.Undefined => null,
                    Value.ValueType.String => val.StringValue,
                    Value.ValueType.Int32 => val.IntValue,
                    Value.ValueType.Double => val.DoubleValue,
                    Value.ValueType.Array => val.ArrayValue,
                    _ => throw new NotImplementedException()
                };
            }

            // Replace an escaped interpolated ("\${}") with the escaped format equivalent ("{{}}")
            format = Regex.Replace(format, @"\\\$({.*?})", "{$1}");
            // Replace an interpolation ("${}") with the format equivalent ("{}")
            format = Regex.Replace(format, @"\$({.*?})", "$1");
            return string.Format(format, args);
        }

        private int LookupScene(string sceneName)
        {
            int id = LookupString(sceneName);
            if (id == -1)
            {
                throw new InterpreterRuntimeException("Scene could not be found!");
            }

            var scene = Binary.Scenes.FindIndex(s => s.Item1 == id);
            if (scene == -1)
            {
                throw new InterpreterRuntimeException("Scene could not be found!");
            }

            return scene;
        }

        private int LookupFunction(string funcName)
        {
            int id = LookupString(funcName);
            if (id == -1)
            {
                throw new InterpreterRuntimeException("Function could not be found!");
            }

            var func = Binary.Functions.FindIndex(s => s.Item1 == id);
            if (func == -1)
            {
                throw new InterpreterRuntimeException("Function could not be found!");
            }

            return func;
        }

        private (int, int, int) LookupDefinition(string defName)
        {
            int id = LookupString(defName);
            if (id == -1)
            {
                throw new InterpreterRuntimeException("Function could not be found!");
            }

            var def = Binary.Definitions.Find(s => s.Item1 == id);
            if (def == default)
            {
                throw new InterpreterRuntimeException("Function could not be found!");
            }

            return def;
        }

        private string GetDefinition((int, int, int) def)
        {
            var (_, strRef, bytecodeIndex) = def;
            string value = (strRef ^ (1 << 31)) == 0 ? Binary.StringTable[strRef & 0x7FFFFFFF] : Binary.TranslationTable[strRef];
            if (bytecodeIndex == -1) return value;
            var iptemp = instructionPointer;
            instructionPointer = bytecodeIndex;
            Paused = false;
            while (!Paused)
            {
                Update();
            }
            string ret = Interpolate(value, stack.Count);
            instructionPointer = iptemp;
            return ret;
        }

        private int LookupString(string str)
        {
            return Binary.StringTable.FindIndex(s => s == str);
        }

#if DEBUG
        public string Dissassemble(int idx)
        {
            StringBuilder d = new StringBuilder();
            while ((Opcode)Binary.Instructions[idx] != Opcode.Exit && (Opcode)Binary.Instructions[idx] != Opcode.Return)
            {
                var opcode = (Opcode)Binary.Instructions[idx++];
                int arg1 = default, arg2 = default;
                double argDouble = default;

                d.Append($"{idx-1,3}: ");
                d.Append(ToAssembledName(opcode));

                switch (opcode)
                {
                    case Opcode.FreeLocal:
                    case Opcode.PushInt:
                    case Opcode.PushString:
                    case Opcode.PushBinaryString:
                    case Opcode.MakeArray:
                    case Opcode.SetVarGlobal:
                    case Opcode.SetVarLocal:
                    case Opcode.PushVarGlobal:
                    case Opcode.PushVarLocal:
                    case Opcode.Jump:
                    case Opcode.JumpTruthy:
                    case Opcode.JumpFalsey:
                    case Opcode.ChoiceAdd:
                    case Opcode.ChoiceAddTruthy:
                    case Opcode.ChooseAdd:
                    case Opcode.ChooseAddTruthy:
                        arg1 = BitConverter.ToInt32(Binary.Instructions, idx);
                        idx += 4;
                        break;
                    case Opcode.PushInterpolatedString:
                    case Opcode.PushBinaryInterpolatedString:
                    case Opcode.Call:
                    case Opcode.CallExternal:
                        arg1 = BitConverter.ToInt32(Binary.Instructions, idx);
                        idx += 4;
                        arg2 = BitConverter.ToInt32(Binary.Instructions, idx);
                        idx += 4;
                        break;
                    case Opcode.PushDouble:
                        argDouble = BitConverter.ToDouble(Binary.Instructions, idx);
                        idx += 8;
                        break;
                }

                switch (opcode)
                {
                    case Opcode.PushInt:
                        d.Append($" #{arg1}");
                        break;
                    case Opcode.FreeLocal:
                    case Opcode.MakeArray:
                    case Opcode.SetVarLocal:
                    case Opcode.PushVarLocal:
                        d.Append($" #${arg1:X}");
                        break;
                    case Opcode.Jump:
                    case Opcode.JumpTruthy:
                    case Opcode.JumpFalsey:
                    case Opcode.ChooseAdd:
                    case Opcode.ChooseAddTruthy:
                    case Opcode.ChoiceAdd:
                    case Opcode.ChoiceAddTruthy:
                        d.Append($" ${idx + arg1}");
                        break;
                    case Opcode.PushDouble:
                        d.Append($" #{argDouble}");
                        break;
                    case Opcode.PushBinaryString:
                    case Opcode.PushBinaryInterpolatedString:
                    case Opcode.SetVarGlobal:
                    case Opcode.PushVarGlobal:
                        d.Append($" &\"{Binary.StringTable[arg1]}\"");
                        break;
                    case Opcode.Call:
                        d.Append($" {Binary.StringTable[Binary.Functions[arg1].Item1]}");
                        break;
                    case Opcode.CallExternal:
                        d.Append($" {Binary.StringTable[arg1]}");
                        break;
                    case Opcode.PushString:
                    case Opcode.PushInterpolatedString:
                        d.Append($" @\"{Binary.TranslationTable[arg1]}\"");
                        break;
                }

                switch (opcode)
                {
                    case Opcode.PushInterpolatedString:
                    case Opcode.PushBinaryInterpolatedString:
                    case Opcode.Call:
                    case Opcode.CallExternal:
                        d.Append($", #{arg2}");
                        break;
                }

                d.AppendLine();
            }
            d.AppendLine(ToAssembledName((Opcode)Binary.Instructions[idx]));
            return d.ToString();
        }

        public void DissassembleToFile(string path)
        {
            List<(int, List<int>)> list = new List<(int, List<int>)>();
            list.AddRange(Binary.Functions);
            list.AddRange(Binary.Scenes);
            list.Sort((t1, t2) => t1.Item2[0].CompareTo(t2.Item2[0]));

            OrderedDictionary result = new OrderedDictionary();

            foreach (var (symbol, strRef, index) in Binary.Definitions)
            {
                string str = (strRef ^ (1 << 31)) == 0 ? Binary.StringTable[strRef & 0x7FFFFFFF] : Binary.TranslationTable[strRef];
                if (index != -1)
                {
                    result.Add(Binary.StringTable[symbol] + $"@{str}", Dissassemble(index).Trim());
                }
                else
                {
                    result.Add(Binary.StringTable[symbol] + $"@{str}", "");
                }
            }

            foreach (var (symbol, funcPointers) in list)
            {
                for (int i = 0; i < funcPointers.Count; ++i)
                {
                    result.Add(Binary.StringTable[symbol] + $".{i}", Dissassemble(funcPointers[i]).Trim());
                }
            }

            using StreamWriter writer = new StreamWriter(path, false);
            foreach (DictionaryEntry elem in result)
            {
                writer.WriteLine($"{elem.Key}:\n{elem.Value}");
            }
        }

        private string ToAssembledName(Opcode op)
        {
            return op switch
            {
                Opcode.Nop => "nop",
                Opcode.FreeLocal => "freeloc",
                Opcode.Save => "save",
                Opcode.Load => "load",
                Opcode.PushUndefined => "pushu",
                Opcode.PushInt => "pushi",
                Opcode.PushDouble => "pushd",
                Opcode.PushString => "pushs",
                Opcode.PushInterpolatedString => "pushints",
                Opcode.PushBinaryString => "pushbs",
                Opcode.PushBinaryInterpolatedString => "pushbints",
                Opcode.MakeArray => "makearr",
                Opcode.PushArrayIndex => "pusharrind",
                Opcode.SetArrayIndex => "setarrind",
                Opcode.SetVarGlobal => "setvarglb",
                Opcode.SetVarLocal => "setvarloc",
                Opcode.PushVarGlobal => "pushvarglb",
                Opcode.PushVarLocal => "pushvarloc",
                Opcode.Pop => "pop",
                Opcode.Duplicate => "dup",
                Opcode.Duplicate2 => "dup2",
                Opcode.Addition => "add",
                Opcode.Subtraction => "sub",
                Opcode.Multiply => "mul",
                Opcode.Divide => "div",
                Opcode.Modulo => "mod",
                Opcode.Negate => "neg",
                Opcode.Invert => "inv",
                Opcode.BitLeftShift => "bitls",
                Opcode.BitRightShift => "bitrs",
                Opcode.BitAnd => "bitand",
                Opcode.BitOr => "bitor",
                Opcode.BitExclusiveOr => "bitxor",
                Opcode.BitNegate => "bitneg",
                Opcode.Power => "pow",
                Opcode.CompareEqual => "cmpeq",
                Opcode.CompareGreaterThan => "cmpgt",
                Opcode.CompareLessThan => "cmplt",
                Opcode.CompareGreaterThanEqual => "cmpgte",
                Opcode.CompareLessThanEqual => "cmplte",
                Opcode.CompareNotEqual => "cmpneq",
                Opcode.Jump => "jmp",
                Opcode.JumpTruthy => "jmpt",
                Opcode.JumpFalsey => "jmpf",
                Opcode.Exit => "exit",
                Opcode.Return => "ret",
                Opcode.Call => "call",
                Opcode.CallExternal => "callext",
                Opcode.ChoiceBegin => "choicebeg",
                Opcode.ChoiceAdd => "choiceadd",
                Opcode.ChoiceAddTruthy => "choiceaddt",
                Opcode.ChoiceSelect => "choicesel",
                Opcode.ChooseAdd => "chooseadd",
                Opcode.ChooseAddTruthy => "chooseaddt",
                Opcode.ChooseSel => "choosesel",
                Opcode.TextRun => "textrun",
                _ => "nop",
            };
        }
#endif

        /// <summary>
        /// Thrown whenever an error occurs during <seealso cref="Interpreter"/> execution.
        /// </summary>
        public class InterpreterRuntimeException : Exception
        {
            public InterpreterRuntimeException(string message) : base(message)
            {
            }
        }

        private sealed class LocalVariableStore
        {
            public Dictionary<int, Value> Variables = new Dictionary<int, Value>();
            public Dictionary<int, string> FlagMap = new Dictionary<int, string>();
            private Interpreter interpreter;

            public int Count => Variables.Count + FlagMap.Count;

            public Value this[int index]
            {
                get
                {
                    if (FlagMap.ContainsKey(index))
                    {
                        return interpreter.GetFlag(FlagMap[index]);
                    }
                    return Variables[index];
                }

                set
                {
                    if (FlagMap.ContainsKey(index))
                    {
                        interpreter.SetFlag(FlagMap[index], value);
                    }
                    else
                    {
                        Variables[index] = value;
                    }
                }
            }

            public LocalVariableStore(Interpreter interpreter)
            {
                this.interpreter = interpreter;
            }

            public void Add(Value value)
            {
                var index = Count;
                Variables.Add(index, value);
            }

            public void Delete(int index)
            {
                if (index < FlagMap.Count)
                {
                    FlagMap.Remove(index);
                }
                else if (Variables.ContainsKey(index))
                {
                    Variables.Remove(index);
                }
            }

            public void Clear()
            {
                Variables.Clear();
            }
        }
    }
}
