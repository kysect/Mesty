using Mesty.Interpreter.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace Mesty.Interpreter;

public class SourceCodeInterpreter : ISourceCodeInterpreter
{
    private readonly ILogger _logger;
    private readonly IStatementExecutor _statementExecutor;

    public SourceCodeInterpreter(ILogger logger)
    {
        _logger = logger;
        _statementExecutor = new StatementExecutor(_logger);
    }

    public SourceCodeExecutionContext Execute(SourceCodeClassDeclaration declaration, SourceCodeExecutionMethodStatementPointer startPointer)
    {
        var executionInterpretingContext = new SourceCodeExecutionContext(
            declaration,
            new List<SourceCodeExecutionThreadContext>(),
            declaration.MemberVariables.ToList(),
            null);

        // TODO: add threadId
        const int threadId = 1;
        executionInterpretingContext = executionInterpretingContext.StartThread(threadId, startPointer);
        var threadPointer = new SourceCodeExecutionThreadPointer(executionInterpretingContext, threadId);

        while (_statementExecutor.CanExecute(threadPointer))
        {
            threadPointer = _statementExecutor.ExecuteThreadAction(threadPointer);
        }

        SourceCodeExecutionContext threadContextParentContext = threadPointer.Context;
        return threadContextParentContext;
    }

    public SourceCodeExecutionContext Execute(SourceCodeClassDeclaration declaration, IReadOnlyList<SourceCodeExecutionMethodStatementPointer> startPointers)
    {
        var executionInterpretingContext = new SourceCodeExecutionContext(
            declaration,
            new List<SourceCodeExecutionThreadContext>(),
            declaration.MemberVariables.ToList(),
            null);

        for (int threadId = 0; threadId < startPointers.Count; threadId++)
        {
            executionInterpretingContext = executionInterpretingContext.StartThread(threadId, startPointers[threadId]);
            var threadPointer = new SourceCodeExecutionThreadPointer(executionInterpretingContext, threadId);

            while (_statementExecutor.CanExecute(threadPointer))
            {
                threadPointer = _statementExecutor.ExecuteThreadAction(threadPointer);
            }

            executionInterpretingContext = threadPointer.Context;
        }

        return executionInterpretingContext;
    }
}