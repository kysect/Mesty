using Kysect.CommonLib.DependencyInjection;
using Mesty.Interpreter;
using Mesty.Interpreter.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models;
using Mesty.SourceCodeDeclaration.RoslynParser;

namespace Mesty.Playground;

internal class Program
{
    private static void Main()
    {
        string code = @"public class SampleClass1
{
    private long _setCount;
    private readonly AutoResetEvent _notEmptyEvent = new(false);

    public void Set()
    {
        long newValue;
        newValue = Interlocked.Increment(ref _setCount);
        long tempValue = 1;
        if (newValue == tempValue)
        {
            _notEmptyEvent.Set();
        }
    }
}";
        var _parser = new RoslynSourceCodeClassDeclarationParser(PredefinedLogger.CreateConsoleLogger());
        var _interpreter = new SourceCodeInterpreter(PredefinedLogger.CreateConsoleLogger());


        SourceCodeClassDeclaration sourceCodeDeclaration = _parser.Parse(code);
        var entryPoint = new SourceCodeExecutionMethodStatementPointer("Set", 0);
        SourceCodeExecutionContext sourceCodeExecutionContext = _interpreter.Execute(sourceCodeDeclaration, entryPoint);

        IReadOnlyList<SourceCodeExecutionContext> allGenerations = sourceCodeExecutionContext.GetAllGenerations().ToList();

        for (int i = 0; i < allGenerations.Count; i++)
        {
            SourceCodeExecutionContext codeExecutionContext = allGenerations[i];
            Console.WriteLine($"Generation {i}");
            Console.WriteLine("Global variables: " + string.Join(", ", codeExecutionContext.GlobalVariables));
            Console.WriteLine("Threads: ");
            foreach (SourceCodeExecutionThreadContext sourceCodeExecutionThreadContext in codeExecutionContext.ThreadContexts)
            {
                SourceCodeExecutionMethodStatementPointer? statementPointer = sourceCodeExecutionThreadContext.MethodStack.LastOrDefault();
                Console.WriteLine($"\tThread {sourceCodeExecutionThreadContext.ThreadId}");
                if (statementPointer is null)
                    Console.WriteLine($"\tCurrent statement: <finished>");
                else
                    Console.WriteLine($"\tCurrent statement: {statementPointer.MethodName}:{statementPointer.StatementIndex}");

                Console.WriteLine($"\tVariables: " + string.Join(", ", sourceCodeExecutionThreadContext.LocalVariables));
            }

            Console.WriteLine();
        }
    }
}