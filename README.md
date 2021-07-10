# Diannex.Net
Diannex.Net is a C# Bytecode Interpreter for the Diannex dialogue language (the compiler of which can be found [here](https://github.com/Rupitian/diannex)). It's meant for use in games, but it can also be used in other applications if so desired.

It's built with .NET Core 3.1 and has no depedencies.

# Usage
Since it's meant to be made with games, which could have very different control flow, usage of the library will be different from project to project.
In general however, there are 4 steps which must be followed.
1. You need to construct an `Interpreter`, which takes in a `Binary` and a [`FunctionHandler`](#using-the-functionhandler) (and optionally a `Choice/WeightedChoiceHandler`).
2. You must run a scene with `Interpreter.RunScene(string)`, which takes in the name of a scene preceded by any namespaces (e.g. `main.scene1`).
3. You must run `Interpreter.Update()` ever frame/tick of your engine to progress the `Interpreter`.
4. Whenever [`Interpreter.RunningText`](#handling-interpreterrunningtext) or [`Interpreter.SelectChoice`](#handling-interpreterselectchoice) is true, you must handle them then run `Interpreter.Resume()` when finished to continue progressing the `Interpreter`.

# Using the FunctionHandler
The `FunctionHandler` class is how you will get your Diannex code to execute methods inside of your Engine/Application. There are 2 main ways to use it.
Through the `DiannexFunction`/`DiannexProperty` Attributes, or the `FunctionHandler.RegisterFunction` method.

## Using the DiannexFunction/DiannexProperty Attributes
Diannex.Net provides 2 attributes to make defining a function (or property!) that's callable through your Diannex code easier.
The caviate with this method however is that your functions/properties must be `static`.
To uses these Attributes, the `withAttributes` parameter of the `FunctionHandler` constructor is `true` (which it is by default.)

The DiannexFunction attribute takes in a string, which is the name of the method in your diannex code.
You can make a method a DiannexFunction like so:
```cs
[DiannexFunction("totallyNotAdding")]
public static int TotallyNotAdding(int x, int y)
{
  return x + y;
}
```
And this will define the method in your diannex code that takes in 2 integers, and returns 1 integer.
The best part about this method is that you don't have to deal with converting/constructing a `Value`! Diannex.Net does it for you!

Diannex.Net can convert the following types to a Value
```cs
double
int
string
```

Aside from exposing methods, you can also expose properties with the `DiannexProperty` attribute.
This attribute takes in 2 strings, one of them optional. The first string is the name of the `getter` in your Diannex script. The second is an optional `setter` in your Diannex script.

Here's an example on how you can define a `DiannexProperty`:
```cs
[DiannexProperty("getPlayerName")
public static string PlayerName => Engine.PlayerName;
```
The types that Diannex.Net can convert from your Properties is the same as methods.

## Using the RegisterFunction method
If you don't want to use the attributes, or if you don't want to use static methods, then the `FunctionHandler.RegisterFunction` method is for you. This method takes in a `string` and a `Func<Value[], Value>`.
The `string` is the name of the method in your Diannex script, and the `Func<Value[], Value>` is your method.
Unfortunately, this method won't convert the Values to C# types and doesn't check if all the arguments have been supplied, so you'll have to do this yourself.

So let's rewrite the `int TotallyNotAdding(int, int)` method so that it can be used:
```cs
public Value TotallyNotAdding(Value[] args)
{
  if (args.Length != 2 || args[0].Type != Value.ValueType.Int32 || args[1].Type != Value.ValueType.Int32)
  {
    // Log your error, or throw an exception if you wish, I'll just return 0.
    return new Value(0, Value.ValueType.Int32);
  }
  
  return args[0] + args[1]; // The Value class already has all the operators built in!
}

// Somewhere in your Engine/Application's init
var functionHandler = new FunctionHandler(withAttributes: false);
functionHandler.RegisterFunction("totallyNotAdding", this.TotallyNotAdding);
```
And there you go, the method does essentially the same thing as before from the perspective of your Diannex script!

# Handling Interpreter.RunningText
When the `Interpreter` is ready to display dialogue to the user, it will set `Interpreter.RunningText` to true, and will pause execution of the bytecode. 
The text to be displayed is located in `Interpreter.CurrentText`.
How you display this text is ultimately up to you, but typically this would be something like showing a dialogue box in your engine.

When you're done displaying the dialogue (in the example given above, this would be when your user presses a key to continue), you can resume the Interpreter by running `Interpreter.Resume()`

# Handling Interpreter.SelectChoice
Whenever the `Interpreter` comes across a point in your Diannex script where multiple choices are presented, it will set `Interpreter.SelectChoice` to true, and pause execution of the bytecode.
The choices are located in `Interpreter.Choices`, which is a `List<string`.
Just like with displaying text, how you handle choices is ultimately up to you, but however you handle it, you must run `Interpreter.ChooseChoice`.

`Interpreter.ChooseChoice` takes in an `int`, which is an index within the `Interpreter.Choices` list chosen by the user.
When you run this method, the Interpreter will automatically `Resume`, so you won't have to.

# Samples
As mentioned before, since this library is meant to be used in games, the control flow for your dialogue is likely to be different.
As such there isn't a quick sample I can place here aside from follow the steps outlined in (Usage)[#usage].

Eventually a full sample application will be made, at which point I'll replace this section with the link to it.
For now however, you can follow the [Issue](https://github.com/Rupitian/Diannex.Net/issues/1) about it.
