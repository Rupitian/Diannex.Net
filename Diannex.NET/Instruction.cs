namespace Diannex.NET
{
    public enum Opcode
    {
        /// <summary>
        /// No Operation. Does nothing.
        /// </summary>
        Nop = 0x00,

        /// <summary>
        /// Frees a local variable from the stack frame if it exists.
        /// </summary>
        /// <remarks>Operands: ID</remarks>
        FreeLocal = 0x0A,

        /// <summary>
        /// Copies the value on the top of the stack into the <b>save</b> register.
        /// </summary>
        Save = 0x0B,
        /// <summary>
        /// Pushes the value from the <b>save</b> register onto the top of the stack.
        /// </summary>
        Load = 0x0C,

        /// <summary>
        /// Pushes undefined value to stack
        /// </summary>
        PushUndefined = 0x0F,
        /// <summary>
        /// Pushes DiannexInt32 to stack
        /// </summary>
        /// <remarks>Operands: Value</remarks>
        PushInt = 0x10,
        /// <summary>
        /// Pushes DiannexDouble to stack
        /// </summary>
        /// <remarks>Operands: Value</remarks>
        PushDouble = 0x11,

        /// <summary>
        /// Pushes a translatable string to stack
        /// </summary>
        /// <remarks>Operands: Index</remarks>
        PushString = 0x12,
        /// <summary>
        /// Pushes an interpolated translatable string to stack
        /// </summary>
        /// <remarks>Operands: Index, Expression count</remarks>
        PushInterpolatedString = 0x13,
        /// <summary>
        /// Pushes an internal string to stack
        /// </summary>
        /// <remarks>Operands: ID</remarks>
        PushBinaryString = 0x14,
        /// <summary>
        /// Pushes an interpolated internal string to stack
        /// </summary>
        /// <remarks>Operands: ID, Expression count</remarks>
        PushBinaryInterpolatedString = 0x15, // ID, Expr Count

        /// <summary>
        /// Constructs an array based off the stack
        /// </summary>
        /// <remarks>Operands: Size</remarks>
        MakeArray = 0x16,
        /// <summary>
        /// Extracts a single value out of an array, remove the array from stack (uses stack for index)
        /// </summary>
        PushArrayIndex = 0x17,
        /// <summary>
        /// Sets a value in an array on the top of the stack (uses stack for index and value)
        /// </summary>
        SetArrayIndex = 0x18,

        /// <summary>
        /// Sets a global variable from the stack
        /// </summary>
        /// <remarks>Operands: Name</remarks>
        SetVarGlobal = 0x19,
        /// <summary>
        /// Sets a local variable from the stack
        /// </summary>
        /// <remarks>Operands: ID</remarks>
        SetVarLocal = 0x1A,
        /// <summary>
        /// Pushes a global variable to the stack
        /// </summary>
        /// <remarks>Operands: Name</remarks>
        PushVarGlobal = 0x1B,
        /// <summary>
        /// Pushes a local variable to the stack
        /// </summary>
        /// <remarks>Operands: ID</remarks>
        PushVarLocal = 0x1C,

        /// <summary>
        /// Discards the value on the top of the stack
        /// </summary>
        Pop = 0x1D,
        /// <summary>
        /// Duplicates the value on the top of the stack
        /// </summary>
        Duplicate = 0x1E,
        /// <summary>
        /// Duplicates the values on the top two slots of the stack
        /// </summary>
        Duplicate2 = 0x1F,

        /// <summary>
        /// Adds the two values on the top of the stack, popping them, pushing the result
        /// </summary>
        Addition = 0x20,
        /// <summary>
        /// Same as <see cref="Addition"/> but subtract.
        /// </summary>
        Subtraction = 0x21,
        /// <summary>
        /// Same as <see cref="Addition"/> but multiply.
        /// </summary>
        Multiply = 0x22,
        /// <summary>
        /// Same as <see cref="Addition"/> but divide.
        /// </summary>
        Divide = 0x23,
        /// <summary>
        /// Same as <see cref="Addition"/> but modulo.
        /// </summary>
        Modulo = 0x24,
        /// <summary>
        /// Negates the value on the top of the stack, popping it, pushing the result
        /// </summary>
        Negate = 0x25,
        /// <summary>
        /// Same as <see cref="Negate"/> but inverts a boolean
        /// </summary>
        Invert = 0x26,

        /// <summary>
        /// Performs bitwise left-shift using the top two values of stack, popping them, pushing the result
        /// </summary>
        BitLeftShift = 0x27,
        /// <summary>
        /// Same as <see cref="BitLeftShift"/> but right-shift
        /// </summary>
        BitRightShift = 0x28,
        /// <summary>
        /// Same as <see cref="BitLeftShift"/> but and
        /// </summary>
        BitAnd = 0x29,
        /// <summary>
        /// Same as <see cref="BitLeftShift"/> but or
        /// </summary>
        BitOr = 0x2A,
        /// <summary>
        /// Same as <see cref="BitLeftShift"/> but xor
        /// </summary>
        BitExclusiveOr = 0x2B,
        /// <summary>
        /// Same as <see cref="BitLeftShift"/> but negate (~)
        /// </summary>
        BitNegate = 0x2C,

        /// <summary>
        /// Power binary operation using top two values of stack
        /// </summary>
        Power = 0x2D,

        /// <summary>
        /// Compares the top two values of stack to check if they are equal, popping them, pushing the result
        /// </summary>
        CompareEqual = 0x30,
        /// <summary>
        /// Same as <see cref="CompareEqual"/> but Greater Than
        /// </summary>
        CompareGreaterThan = 0x31,
        /// <summary>
        /// Same as <see cref="CompareEqual"/> but Less Than
        /// </summary>
        CompareLessThan = 0x32,
        /// <summary>
        /// Same as <see cref="CompareEqual"/> but Greater Than or Equal
        /// </summary>
        CompareGreaterThanEqual = 0x33,
        /// <summary>
        /// Same as <see cref="CompareEqual"/> but Less Than or Equal
        /// </summary>
        CompareLessThanEqual = 0x34,
        /// <summary>
        /// Same as <see cref="CompareEqual"/> but Not Equal
        /// </summary>
        CompareNotEqual = 0x35,

        /// <summary>
        /// Jumps to an instruction
        /// </summary>
        /// <remarks>Operands: Relative address</remarks>
        Jump = 0x40,
        /// <summary>
        /// <inheritdoc cref="Jump"/> if the value on the top of the stack is truthy (which it pops off)
        /// </summary>
        /// <remarks>Operands: Relative address</remarks>
        JumpTruthy = 0x41,
        /// <summary>
        /// <inheritdoc cref="Jump"/>  if the value on the top of the stack is NOT truthy (which it pops off)
        /// </summary>
        /// <remarks>Operands: Relative address</remarks>
        JumpFalsey = 0x42,
        /// <summary>
        /// Exits the current stack frame
        /// </summary>
        Exit = 0x43,
        /// <summary>
        /// <inheritdoc cref="Exit"/> returning a value (from the stack, popping it off)
        /// </summary>
        Return = 0x44,
        /// <summary>
        /// Calls a function defined in the code
        /// </summary>
        /// <remarks>Operands: ID, Parameter count</remarks>
        Call = 0x45,
        /// <summary>
        /// Calls a function defined by a game.<br/>
        /// Check: <see cref="FunctionHandler"/>
        /// </summary>
        /// <remarks>Operands: Name, Parameter count</remarks>
        CallExternal = 0x46,

        /// <summary>
        /// Switches to the choice state in the interpreter<br/>
        /// No other choices can run and only one textrun can
        /// execute until after choicesel is executed.
        /// </summary>
        ChoiceBegin = 0x47,

        /// <summary>
        /// Adds a choice, using the stack for the text and the % chance of appearing
        /// </summary>
        /// <remarks>Operands: Relative address</remarks>
        ChoiceAdd = 0x48,
        /// <summary>
        /// Same as <seealso cref="ChoiceAdd"/> but also if an additional stack value is truthy
        /// </summary>
        /// <remarks>Operands: Relative address</remarks>
        ChoiceAddTruthy = 0x49,
        /// <summary>
        /// Pauses the interpreter, waiting for user input to select one of the choices, then jumps to one of them, resuming
        /// </summary>
        ChoiceSelect = 0x4A,

        /// <summary>
        /// Adds a new address to one of the possible next statements, using stack for chances
        /// </summary>
        /// <remarks>Operands: Relative address</remarks>
        ChooseAdd = 0x4B,
        /// <summary>
        /// Same as <seealso cref="ChooseAdd"/> but also if an additional stack value is truthy
        /// </summary>
        /// <remarks>Operands: Relative address</remarks>
        ChooseAddTruthy = 0x4C,
        /// <summary>
        /// Jumps to one of the choices, using the addresses and chances/requirement values on the stack
        /// </summary>
        ChooseSel = 0x4D,

        /// <summary>
        /// Pauses the interpreter, running a line of text from the stack
        /// </summary>
        TextRun = 0x4E
    }
}
