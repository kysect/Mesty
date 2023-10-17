using Kysect.CommonLib.DependencyInjection;
using Mesty.SourceCodeDeclaration.Abstractions.Contracts;
using Mesty.SourceCodeDeclaration.Abstractions.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Methods;
using Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;
using Mesty.SourceCodeDeclaration.RoslynParser;
using Mesty.Tests.Tools;
using NUnit.Framework;

namespace Mesty.Tests;

public class SourceCodeContextParserTests
{
    private ExecutionStatementComparator _executionStatementComparator;
    private ISourceCodeClassDeclarationParser _parser;

    [SetUp]
    public void Setup()
    {
        _executionStatementComparator = new ExecutionStatementComparator();
        _parser = new RoslynSourceCodeClassDeclarationParser(PredefinedLogger.CreateConsoleLogger());
    }

    [Test]
    public void ParseGlobalContext_ShouldReturnAllClassFields()
    {
        string code = @"public class SampleClass1
{
    private long _setCount;
    private readonly AutoResetEvent _notEmptyEvent = new(false);
}";

        SourceCodeClassDeclaration sourceCodeDeclaration = _parser.Parse(code);

        Assert.That(sourceCodeDeclaration.TypeName, Is.EqualTo("SampleClass1"));
        Assert.That(sourceCodeDeclaration.MemberVariables.Count, Is.EqualTo(2));
        Assert.Contains(new SimpleLongSourceCodeVariableDeclaration("_setCount", 0), (System.Collections.ICollection)sourceCodeDeclaration.MemberVariables);
        Assert.Contains(new AutoResetEventSourceCodeVariableDeclaration("_notEmptyEvent", false), (System.Collections.ICollection)sourceCodeDeclaration.MemberVariables);
    }

    [Test]
    public void ParseSetMethod_ShouldReturnAllStatements()
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

        Assert.That(sourceCodeDeclaration.TypeName, Is.EqualTo("SampleClass1"));
        Assert.That(sourceCodeDeclaration.Methods.Count, Is.EqualTo(1));

        ExecutionMethod sourceCodeContextMethod = sourceCodeDeclaration.Methods.Single();

        var expectedList = new List<ISourceCodeMethodStatementDeclaration>()
        {
            new VariableDeclarationSourceCodeMethodStatementDeclaration(new SimpleLongSourceCodeVariableDeclaration("newValue", 0)),
            new InterlockedIncrementMethodStatementDeclaration("_setCount", "newValue"),
            new VariableDeclarationSourceCodeMethodStatementDeclaration(new SimpleLongSourceCodeVariableDeclaration("tempValue", 1)),
            new SetValueToVariableStatementDeclaration("tempValue", "1"),
            new IfSourceCodeStatementDeclaration(
                "newValue",
                "tempValue",
                1),
            new OtherMethodInvocationStatementDeclaration("_notEmptyEvent", "Set", null)

        };

        _executionStatementComparator.CompareStatements(expectedList, sourceCodeContextMethod.Statements);
    }

    [Test]
    public void ParseWaitMethod_ShouldReturnAllStatements()
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
            if (updatedValue == currCount)
            {
                return;
            }
        }
    }
}";

        SourceCodeClassDeclaration sourceCodeDeclaration = _parser.Parse(code);

        ExecutionMethod sourceCodeContextMethod = sourceCodeDeclaration.Methods.Single(m => m.Name == "Wait");

        var expectedList = new List<ISourceCodeMethodStatementDeclaration>()
        {
            new WhileTrueSourceCodeStatementDeclaration(15),
            new VariableDeclarationSourceCodeMethodStatementDeclaration(new SimpleLongSourceCodeVariableDeclaration("currCount", 0)),
            new SetValueToVariableStatementDeclaration("currCount", "0"),
            new VariableDeclarationSourceCodeMethodStatementDeclaration(new SimpleLongSourceCodeVariableDeclaration("zeroValue", 0)),
            new SetValueToVariableStatementDeclaration("zeroValue", "0"),
            new WhileSourceCodeStatementDeclaration("currCount", "zeroValue", 3),
            new InterlockedReadMethodStatementDeclaration("_setCount", "currCount"),
            new IfSourceCodeStatementDeclaration("currCount", "zeroValue", 1),
            new OtherMethodInvocationStatementDeclaration("_notEmptyEvent", "WaitOne", null),
            new SkipStatementSourceCodeMethodStatementDeclaration(-4),
            new VariableDeclarationSourceCodeMethodStatementDeclaration(new LongInitFromOtherSourceCodeVariableDeclaration("decrementedValue", "currCount")),
            new SetValueToVariableWithDecrementStatementDeclaration("decrementedValue", "currCount"),
            new VariableDeclarationSourceCodeMethodStatementDeclaration(new SimpleLongSourceCodeVariableDeclaration("updatedValue", 0)),
            new InterlockedCompareExchangeMethodStatementDeclaration("_setCount", "currCount", "decrementedValue", "updatedValue"),
            new IfSourceCodeStatementDeclaration("updatedValue", "currCount", 1),
            new ReturnStatementSourceCodeMethodStatementDeclaration(),
            new SkipStatementSourceCodeMethodStatementDeclaration(-16),
        };

        _executionStatementComparator.CompareStatements(expectedList, sourceCodeContextMethod.Statements);
    }


}