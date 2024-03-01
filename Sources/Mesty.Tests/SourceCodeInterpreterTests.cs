using Kysect.CommonLib.BaseTypes.Extensions;
using Kysect.CommonLib.DependencyInjection.Logging;
using Mesty.Interpreter;
using Mesty.Interpreter.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Contracts;
using Mesty.SourceCodeDeclaration.Abstractions.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;
using Mesty.SourceCodeDeclaration.RoslynParser;
using Mesty.Tests.Tools;
using NUnit.Framework;
using System.Text;

namespace Mesty.Tests;

public class SourceCodeInterpreterTests
{
    private ISourceCodeClassDeclarationParser _parser;
    private ISourceCodeInterpreter _interpreter;

    [SetUp]
    public void Setup()
    {
        new ExecutionStatementComparator();
        _parser = new RoslynSourceCodeClassDeclarationParser(DefaultLoggerConfiguration.CreateConsoleLogger());
        _interpreter = new SourceCodeInterpreter(DefaultLoggerConfiguration.CreateConsoleLogger());
    }

    [Test]
    public void ExecuteSetMethod_ShouldReturnAllGeneration()
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

        SourceCodeClassDeclaration sourceCodeDeclaration = _parser.Parse(code);
        var entryPoint = new SourceCodeExecutionMethodStatementPointer("Set", 0);
        SourceCodeExecutionContext sourceCodeExecutionContext = _interpreter.Execute(sourceCodeDeclaration, entryPoint);

        if (!sourceCodeExecutionContext.TryGetGlobalVariable("_notEmptyEvent", out ISourceCodeVariableDeclaration? resetEvent))
            Assert.Fail("Variable _notEmptyEvent is not defined.");

        Assert.That(resetEvent is AutoResetEventSourceCodeVariableDeclaration { IsSet: true }, Is.True);

        IReadOnlyCollection<SourceCodeExecutionContext> allGenerations = sourceCodeExecutionContext.GetAllGenerations();
        Assert.That(allGenerations.Count, Is.EqualTo(12));

        return;
    }

    [Test]
    public void ExecuteWaitMethod_ShouldReturnAllGeneration()
    {
        string code = @"public class SampleClass
{
    private long _setCount = 0;
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

    public void Wait()
    {
        while (true)
        {
            long currCount = 0;
            long zeroValue = 0;
            while (currCount == zeroValue)
            {
                currCount = Interlocked.Read(ref _setCount);
                if (currCount == zeroValue)
                {
                    _notEmptyEvent.WaitOne();
                }
            }

            long decrementedValue = currCount - 1;
            long updatedValue;
            updatedValue = Interlocked.CompareExchange(ref _setCount, currCount, decrementedValue);
            if (_setCount == currCount)
            {
                return;
            }
        }
    }
}";

        SourceCodeClassDeclaration sourceCodeDeclaration = _parser.Parse(code);
        SourceCodeExecutionContext sourceCodeExecutionContext = _interpreter.Execute(sourceCodeDeclaration, new[]
        {
            new SourceCodeExecutionMethodStatementPointer("Set", 0),
            new SourceCodeExecutionMethodStatementPointer("Wait", 0),
        });

        IReadOnlyCollection<SourceCodeExecutionContext> allGenerations = sourceCodeExecutionContext.GetAllGenerations();
        string stringRepresentation = new SourceCodeExecutionContextPresenter().ConvertToString(allGenerations.ToList());
        // TODO: i am not sure about it
        Assert.That(allGenerations.Count, Is.EqualTo(39));

        return;
    }
}

public class SourceCodeExecutionContextPresenter
{
    public string ConvertToString(IReadOnlyList<SourceCodeExecutionContext> sourceCodeExecutionContexts)
    {
        sourceCodeExecutionContexts.ThrowIfNull();

        var stringBuilder = new StringBuilder();
        for (int i = 0; i < sourceCodeExecutionContexts.Count; i++)
        {
            stringBuilder.AppendLine($"State {i:000}");
            stringBuilder.AppendLine(ConvertToString(sourceCodeExecutionContexts[i]));
        }

        return stringBuilder.ToString();
    }

    public string ConvertToString(SourceCodeExecutionContext sourceCodeExecutionContext)
    {
        sourceCodeExecutionContext.ThrowIfNull();

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("\tGlobal variables:");
        foreach (ISourceCodeVariableDeclaration sourceCodeVariableDeclaration in sourceCodeExecutionContext.GlobalVariables)
        {
            stringBuilder.AppendLine($"\t\t{sourceCodeVariableDeclaration}");
        }


        stringBuilder.AppendLine("Threads");
        foreach (SourceCodeExecutionThreadContext sourceCodeExecutionThreadContext in sourceCodeExecutionContext.ThreadContexts)
        {
            stringBuilder.AppendLine($"\tThread-{sourceCodeExecutionThreadContext.ThreadId:00} call stack:");
            foreach ((string methodName, int statementIndex) in sourceCodeExecutionThreadContext.MethodStack)
                stringBuilder.AppendLine($"\t\t{methodName}:{statementIndex}");

            stringBuilder.AppendLine($"\tThread-{sourceCodeExecutionThreadContext.ThreadId:00} variables:");
            foreach (ISourceCodeVariableDeclaration sourceCodeVariableDeclaration in sourceCodeExecutionThreadContext.LocalVariables)
            {
                stringBuilder.AppendLine($"\t\t{sourceCodeVariableDeclaration}");
            }
        }

        return stringBuilder.ToString();
    }
}