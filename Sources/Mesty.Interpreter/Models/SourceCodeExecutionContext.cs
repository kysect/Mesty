using Mesty.SourceCodeDeclaration.Abstractions.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Methods;
using Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;
using System.Diagnostics.CodeAnalysis;
using Kysect.CommonLib.Collections.Extensions;

namespace Mesty.Interpreter.Models;

public record SourceCodeExecutionContext(
    SourceCodeClassDeclaration CodeDeclaration,
    IReadOnlyList<SourceCodeExecutionThreadContext> ThreadContexts,
    IReadOnlyList<ISourceCodeVariableDeclaration> GlobalVariables,
    SourceCodeExecutionContext? Previous)
{
    public SourceCodeExecutionContext StartThread(int threadId, SourceCodeExecutionMethodStatementPointer startPointer)
    {
        var threadContexts = ThreadContexts.CloneCollection().ToList();
        SourceCodeExecutionContext newContext = this with { ThreadContexts = threadContexts, Previous = this };

        var sourceCodeExecutionThreadContext = SourceCodeExecutionThreadContext.StartNew(threadId, startPointer);
        threadContexts.Add(sourceCodeExecutionThreadContext);

        return newContext;
    }

    public SourceCodeExecutionContext UpdateThread(SourceCodeExecutionThreadContext updatedThread)
    {
        var threadContexts = ThreadContexts.CloneCollection().ToList();
        SourceCodeExecutionContext newContext = this with { ThreadContexts = threadContexts, Previous = this };

        int oldThreadIndex = threadContexts.FindIndex(t => t.ThreadId == updatedThread.ThreadId);
        if (oldThreadIndex == -1)
            throw new ArgumentException($"Thread with id {updatedThread.ThreadId} was not found in context.");

        threadContexts[oldThreadIndex] = updatedThread;
        return newContext;
    }

    public ISourceCodeMethodStatementDeclaration GetStatement(string methodName, int index)
    {
        ExecutionMethod executionMethod = CodeDeclaration.Methods.Single(m => m.Name == methodName);
        ISourceCodeMethodStatementDeclaration sourceCodeMethodStatementDeclaration = executionMethod.Statements[index];
        return sourceCodeMethodStatementDeclaration;
    }

    public bool HasNextStatement(string methodName, int index)
    {
        ExecutionMethod executionMethod = CodeDeclaration.Methods.Single(m => m.Name == methodName);
        return executionMethod.Statements.Count > index + 1;
    }

    public bool HasGlobalVariable(string variableName)
    {
        return GlobalVariables.Any(v => v.Name == variableName);
    }

    public bool TryGetGlobalVariable(string variableName, [NotNullWhen(true)] out ISourceCodeVariableDeclaration? result)
    {
        result = GlobalVariables.SingleOrDefault(v => v.Name == variableName);
        return result != null;
    }

    public SourceCodeExecutionContext SetGlobalVariable(ISourceCodeVariableDeclaration newValue)
    {
        var variables = GlobalVariables.CloneCollection().ToList();
        int index = variables.FindIndex(v => v.Name == newValue.Name);
        if (index == -1)
            variables.Add(newValue);
        else
            variables[index] = newValue;

        return this with { GlobalVariables = variables };
    }

    public IReadOnlyCollection<SourceCodeExecutionContext> GetAllGenerations()
    {
        var result = new List<SourceCodeExecutionContext>();

        SourceCodeExecutionContext? current = this;
        while (current is not null)
        {
            result.Add(current);
            current = current.Previous;
        }

        result.Reverse();
        return result;
    }
}