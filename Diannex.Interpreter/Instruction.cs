using System;
using System.Collections.Generic;
using System.Text;

namespace Diannex.Interpreter
{
    // TODO: Comment better than just arguments
    public enum Opcode
    {
        Nop = 0x00,

        FreeLocal = 0x0A, // ID

        Save = 0x0B,
        Load = 0x0C,

        PushUndefined = 0x0F,
        PushInt = 0x10, // Value
        PushDouble = 0x11, // Value

        PushString = 0x12, // Index
        PushInterpolatedString = 0x13, // Index, Expr Count
        PushBinaryString = 0x14, // ID
        PushBinaryInterpolatedString = 0x15, // ID, Expr Count

        MakeArray = 0x16, // Size
        PushArrayIndex = 0x17,
        SetArrayIndex = 0x18,

        SetVarGlobal = 0x19, // Name
        SetVarLocal = 0x1A, // ID
        PushVarGlobal = 0x1B, // Name
        PushVarLocal = 0x1C, // ID

        Pop = 0x1D,
        Duplicate = 0x1E,
        Duplicate2 = 0x1F,

        Addition = 0x20,
        Subtraction = 0x21,
        Multiply = 0x22,
        Divide = 0x23,
        Modulo = 0x24,
        Negate = 0x25,
        Invert = 0x26,

        BitLeftShift = 0x27,
        BitRightShift = 0x28,
        BitAnd = 0x29,
        BitOr = 0x2A,
        BitExclusiveOr = 0x2B,
        BitNegate = 0x2C,

        Power = 0x2D,

        CompareEqual = 0x30,
        CompareGreaterThan = 0x31,
        CompareLessThan = 0x32,
        CompareGreaterThanEqual = 0x33,
        CompareLessThanEqual = 0x34,
        CompareNotEqual = 0x35,

        Jump = 0x40, // Relative address
        JumpTruthy = 0x41, // Relative address
        JumpFalsey = 0x42, // Relative address
        Exit = 0x43,
        Return = 0x44,
        Call = 0x45, // ID, Parameter count
        CallExternal = 0x46, // Name, Parameter count

        ChoiceBegin = 0x47,

        ChoiceAdd = 0x48, // Relative address
        ChoiceAddTruthy = 0x49, // Relative address
        ChoiceSelect = 0x4A,

        ChooseAdd = 0x4B, // Relative address
        ChooseAddTruthy = 0x4C, // Relative address
        ChooseSel = 0x4D,

        TextRun = 0x4E
    }

    public class Instruction
    {
        public Opcode Opcode;
        public int Arg1;
        public int Arg2;
        public double ArgDouble;

        // TODO: Figure out a cleaner way of doing this
        public Instruction(Opcode opcode)
        {
            Opcode = opcode;
        }

        public Instruction(Opcode opcode, int arg)
        {
            Opcode = opcode;
            Arg1 = arg;
            Arg2 = default;
            ArgDouble = default;
        }

        public Instruction(Opcode opcode, int arg1, int arg2)
        {
            Opcode = opcode;
            Arg1 = arg1;
            Arg2 = arg2;
            ArgDouble = default;
        }

        public Instruction(Opcode opcode, double arg)
        {
            Opcode = opcode;
            Arg1 = default;
            Arg2 = default;
            ArgDouble = arg;
        }
    }
}
