using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Diannex.Interpreter
{
    public class Interpreter
    {
        public Binary Binary;
        public Dictionary<string, Value> GlobalVariableStore;

        public bool InChoice { get; private set; }
        public bool SelectChoice { get; private set; }
        public bool RunningText { get; private set; }
        public bool Paused { get; private set; }
        public bool SceneCompleted { get; private set; }
        public string CurrentScene { get; private set; }
        public string CurrentText { get; private set; }
        public List<(int /* Relative Address to jump to */, string /* Text to display to the user */)> Choices { get; private set; } // TODO: Promote to object

        private int instructionPointer;
        private Stack<Value> stack;
        private Value saveRegister;
        private List<Value> localVarStore;
        private Stack<(int, Stack<Value>, List<Value>)> callStack;
        private Dictionary<string, Func<Value[], Value>> externalFuncs;

        public Interpreter(Binary binary)
        {
            Binary = binary;
            GlobalVariableStore = new Dictionary<string, Value>();
            InChoice = false;
            SelectChoice = false;
            RunningText = false;

            CurrentScene = null;
            SceneCompleted = false;
            CurrentText = null;

            instructionPointer = 0;
            Paused = true;
            stack = new Stack<Value>();
            saveRegister = null;
            localVarStore = new List<Value>();
            callStack = new Stack<(int, Stack<Value>, List<Value>)>();
            Choices = new List<(int, string)>();
            externalFuncs = new Dictionary<string, Func<Value[], Value>>();
        }

        public void RegisterCommand(string cmdName, Func<Value[], Value> func)
        {
            // TODO: Sanitize cmdName?
            externalFuncs[cmdName] = func;
        }

        public void RunScene(string sceneName)
        {
            var sceneId = LookupScene(sceneName);
            var scene = Binary.Scenes[sceneId];
            instructionPointer = scene.Item2;
            CurrentScene = sceneName;
            Paused = false;
        }

        public void ChooseChoice(int idx)
        {
            // TODO: Decide whether nor not to stick to C# exceptions or use Interpreter*Exceptions
            var choice = Choices[idx];
            instructionPointer = choice.Item1;
            SelectChoice = false;
            Paused = false;
        }

        public void Resume()
        {
            // TODO: Check for more invalid states
            if (RunningText) RunningText = false;
            if (CurrentScene == null || !Paused) return;

            RunningText = false;
            if (!SelectChoice) Paused = false;
        }

        public void Update()
        {
            if (Paused) return;

            var ip = instructionPointer;
            var inst = Binary.Instructions[instructionPointer++];

            // Prepare for if statement fuck storm
            if (inst.Opcode == Opcode.Nop)
                return;

            #region Stack Instructions
            if (inst.Opcode == Opcode.Save)
                saveRegister = stack.Pop();
            if (inst.Opcode == Opcode.Load)
                stack.Push(saveRegister);

            if (inst.Opcode == Opcode.PushUndefined)
                stack.Push(new Value(null, Value.ValueType.Undefined));
            if (inst.Opcode == Opcode.PushInt)
                stack.Push(new Value(inst.Arg1, Value.ValueType.Int32));
            if (inst.Opcode == Opcode.PushDouble)
                stack.Push(new Value(inst.ArgDouble, Value.ValueType.Double));

            if (inst.Opcode == Opcode.PushString)
                stack.Push(new Value(Binary.TranslationTable[inst.Arg1], Value.ValueType.String));
            if (inst.Opcode == Opcode.PushInterpolatedString)
                stack.Push(new Value(Interpolate(Binary.TranslationTable[inst.Arg1], inst.Arg2), Value.ValueType.String));
            if (inst.Opcode == Opcode.PushBinaryString)
                stack.Push(new Value(Binary.StringTable[inst.Arg1], Value.ValueType.String));
            if (inst.Opcode == Opcode.PushBinaryInterpolatedString)
                stack.Push(new Value(Interpolate(Binary.StringTable[inst.Arg1], inst.Arg2), Value.ValueType.String));

            if (inst.Opcode == Opcode.MakeArray)
                stack.Push(ConstructArray(inst.Arg1));
            if (inst.Opcode == Opcode.PushArrayIndex)
            {
                var indx = stack.Pop();
                var arr = stack.Pop();
                stack.Push(((Value[])arr.Data)[indx.Data]);
            }
            if (inst.Opcode == Opcode.SetArrayIndex)
            {
                var arr = stack.Pop();
                var indx = stack.Pop();
                var val = stack.Pop();
                ((Value[])arr.Data)[indx.Data] = val;
                stack.Push(val);
            }

            if (inst.Opcode == Opcode.SetVarGlobal)
            {
                var val = stack.Pop();
                GlobalVariableStore[Binary.StringTable[inst.Arg1]] = val;
            }
            if (inst.Opcode == Opcode.SetVarLocal)
            {
                var val = stack.Pop();
                if (inst.Arg1 >= localVarStore.Count)
                    localVarStore.Add(val);
                else
                    localVarStore[inst.Arg1] = val;
            }
            if (inst.Opcode == Opcode.PushVarGlobal)
                stack.Push(GlobalVariableStore[Binary.StringTable[inst.Arg1]]);
            if (inst.Opcode == Opcode.PushVarLocal)
                stack.Push(localVarStore[inst.Arg1]);

            if (inst.Opcode == Opcode.Pop)
                stack.Pop();
            if (inst.Opcode == Opcode.Duplicate)
            {
                var val = stack.Pop();
                stack.Push(val);
                stack.Push(val);
            }
            if (inst.Opcode == Opcode.Duplicate2)
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
            if (inst.Opcode == Opcode.Addition)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 + val1);
            }
            if (inst.Opcode == Opcode.Subtraction)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 - val1);
            }
            if (inst.Opcode == Opcode.Multiply)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 * val1);
            }
            if (inst.Opcode == Opcode.Divide)
            {
                Value val1 = stack.Pop(), val2 = stack.Pop();
                stack.Push(val2 / val1);
            }
            if (inst.Opcode == Opcode.Negate)
                stack.Push(-stack.Pop());
            if (inst.Opcode == Opcode.Invert)
                stack.Push(!stack.Pop());

            if (inst.Opcode == Opcode.BitLeftShift)
            {
                int shift = stack.Pop().Data;
                var val = stack.Pop();
                stack.Push(val << shift);
            }
            if (inst.Opcode == Opcode.BitRightShift)
            {
                int shift = stack.Pop().Data;
                var val = stack.Pop();
                stack.Push(val >> shift);
            }
            if (inst.Opcode == Opcode.BitAnd)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 & val2);
            }
            if (inst.Opcode == Opcode.BitOr)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 | val2);
            }
            if (inst.Opcode == Opcode.BitExclusiveOr)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 ^ val2);
            }
            if (inst.Opcode == Opcode.BitNegate)
            {
                var val = stack.Pop();
                stack.Push(~val);
            }

            if (inst.Opcode == Opcode.Power)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(new Value(Math.Pow((double)val1.Data, (double)val2.Data), Value.ValueType.Double));
            }
            #endregion

            #region Value Comparison
            if (inst.Opcode == Opcode.CompareEqual)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 == val2);
            }
            if (inst.Opcode == Opcode.CompareGreaterThan)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 > val2);
            }
            if (inst.Opcode == Opcode.CompareLessThan)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 < val2);
            }
            if (inst.Opcode == Opcode.CompareGreaterThanEqual)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 >= val2);
            }
            if (inst.Opcode == Opcode.CompareLessThanEqual)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 <= val2);
            }
            if (inst.Opcode == Opcode.CompareNotEqual)
            {
                var val2 = stack.Pop();
                var val1 = stack.Pop();
                stack.Push(val1 != val2);
            }
            #endregion

            #region Instruction Pointer Modification
            if (inst.Opcode == Opcode.Jump)
                instructionPointer = ip + inst.Arg1;
            if (inst.Opcode == Opcode.JumpTruthy && (bool)stack.Pop())
                instructionPointer = ip + inst.Arg1;
            if (inst.Opcode == Opcode.JumpFalsey && !((bool)stack.Pop()))
                instructionPointer = ip + inst.Arg1;
            if (inst.Opcode == Opcode.Exit)
            {
                // TODO: Gracefully exit when at the end of the callstack
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
                    stack.Push(new Value(null, Value.ValueType.Undefined));
                }
            }
            if (inst.Opcode == Opcode.Return)
            {
                localVarStore.Clear();
                var returnVal = stack.Pop();
                var cs = callStack.Pop();
                instructionPointer = cs.Item1;
                stack = cs.Item2;
                localVarStore = cs.Item3;
                stack.Push(returnVal);
            }
            if (inst.Opcode == Opcode.Call)
            {
                Value[] val = new Value[inst.Arg2];
                for (int i = inst.Arg2-1; i >= 0; i--)
                {
                    val[i] = stack.Pop();
                }
                callStack.Push((instructionPointer, stack, localVarStore));
                instructionPointer = inst.Arg1;
                stack = new Stack<Value>();
                localVarStore = new List<Value>();
                for (int i = 0; i < inst.Arg2; i++)
                {
                    localVarStore.Add(val[i]);
                }
            }
            if (inst.Opcode == Opcode.CallExternal)
            {
                string name = Binary.StringTable[inst.Arg1];
                Value[] val = new Value[inst.Arg2];
                for (int i = inst.Arg2-1; i >= 0; i--)
                {
                    val[i] = stack.Pop();
                }
                stack.Push(externalFuncs[name](val));
            }
            #endregion

            #region Choice/Choose
            if (inst.Opcode == Opcode.ChoiceBegin)
            {
                if (InChoice)
                    throw new InterpreterRuntimeException("Choice begins while another choice is being processed!");

                InChoice = true;
            }
            if (inst.Opcode == Opcode.ChoiceAdd)
            {
                if (!InChoice)
                    throw new InterpreterRuntimeException("Attempted to add a choice when no choice is being processed!");

                // TODO: Yell at Colin for using a 1 in place of 100% when everything else using integers
                // ALSO TODO: Figure out why the interpreter is trying to run text
                var chance = stack.Pop();
                var text = stack.Pop();
                var rand = new Random();
                if ((int)chance.Data == 1 || rand.Next(0, 100) < (int)chance.Data)
                    Choices.Add((ip + inst.Arg1, text.Data));
            }
            if (inst.Opcode == Opcode.ChoiceAddTruthy)
            {
                if (!InChoice)
                    throw new InterpreterRuntimeException("Attempted to add a choice when no choice is being processed!");

                var chance = stack.Pop();
                var text = stack.Pop();
                var condition = stack.Pop();
                var rand = new Random();
                if ((bool)condition && ((int)chance.Data == 1 || rand.Next(0, 100) < (int)chance.Data))
                {
                    Choices.Add((ip + inst.Arg1, text.Data));
                }
            }
            if (inst.Opcode == Opcode.ChoiceSelect)
            {
                if (!InChoice)
                    throw new InterpreterRuntimeException("Attempted to wait for user choice when no choice is being processed!");
                if (Choices.Count == 0)
                    throw new InterpreterRuntimeException("Attempted to wait for user choice when there's no choices to choose!");

                SelectChoice = true;
                Paused = true;
            }

            if (inst.Opcode == Opcode.ChooseAdd || inst.Opcode == Opcode.ChooseAddTruthy || inst.Opcode == Opcode.ChooseSel)
            {
                throw new NotImplementedException("Choose not yet implemented!");
            }

            if (inst.Opcode == Opcode.TextRun)
            {
                var text = stack.Pop();
                CurrentText = text.Data;
                RunningText = true;
                Paused = true;
            }
            #endregion
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
                args[i] = stack.Pop().Data;
            }

            return string.Format(Regex.Replace(format, @"\$({.*?})", "$1"), args);
        }

        public int LookupScene(string sceneName)
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

        public int LookupFunction(string funcName)
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

        public int LookupDefinition(string defName)
        {
            int id = LookupString(defName);
            if (id == -1)
            {
                throw new InterpreterRuntimeException("Function could not be found!");
            }

            var def = Binary.Definitions.FindIndex(s => s.Item1 == id);
            if (def == -1)
            {
                throw new InterpreterRuntimeException("Function could not be found!");
            }

            return def;
        }

        public int LookupString(string str)
        {
            return Binary.StringTable.FindIndex(s => s == str);
        }

        public string Dissassemble(int idx)
        {
            StringBuilder d = new StringBuilder();
            while (Binary.Instructions[idx].Opcode != Opcode.Exit && Binary.Instructions[idx].Opcode != Opcode.Return)
            {
                var inst = Binary.Instructions[idx];
                // TODO: Properly check what arguments are actually valid, or if we're using arguments in general
                d.AppendLine($"{idx}: {inst.Opcode} [{inst.Arg1}, {inst.Arg2}] [{inst.ArgDouble}]");
                idx++;
            }
            d.AppendLine($"{idx}: {Binary.Instructions[idx].Opcode}");
            return d.ToString();
        }

        public class InterpreterRuntimeException : Exception
        {
            public InterpreterRuntimeException(string message) : base(message)
            {
            }
        }
    }
}
