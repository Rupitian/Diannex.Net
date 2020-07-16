using System;
using Diannex.Interpreter;
using DiannexInterpreter = Diannex.Interpreter.Interpreter;

namespace Diannex.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            // Hello, World!\n or no balls
            Console.WriteLine("Hello, World!");

            // Sure would be weird if I had to do some additional shit when jumping haha *HA*
            Binary b = Binary.ReadFromFile("out.dxb");
            DiannexInterpreter interpreter = new DiannexInterpreter(b);
            Console.WriteLine("textFunc:");
            Console.WriteLine(interpreter.Dissassemble(interpreter.Binary.Functions[interpreter.LookupFunction("textFunc")].Item2));
            Console.WriteLine();
            Console.WriteLine("test.main:");
            Console.WriteLine(interpreter.Dissassemble(interpreter.Binary.Scenes[interpreter.LookupScene("test.main")].Item2));
            interpreter.RunScene("test.main");

            while (!interpreter.SceneCompleted)
            {
                interpreter.Update();

                if (interpreter.RunningText)
                {
                    Console.WriteLine(interpreter.CurrentText);
                    Console.ReadLine();
                    interpreter.Resume();
                }
                else if (interpreter.SelectChoice && interpreter.Paused)
                {
                    for (int i = 0; i < interpreter.Choices.Count; i++)
                    {
                        Console.WriteLine($"[{i}]: {interpreter.Choices[i].Item2}");
                    }
                bad_practice:
                    Console.Write("Enter a number to pick a choice: ");
                    var key = Console.ReadKey();
                    Console.WriteLine();
                    if (int.TryParse($"{key.KeyChar}", out int choice) && choice < interpreter.Choices.Count)
                    {
                        interpreter.ChooseChoice(choice);
                    }
                    else
                    {
                        // One sec
                        goto bad_practice;
                    }
                }
            }
        }
    }
}
