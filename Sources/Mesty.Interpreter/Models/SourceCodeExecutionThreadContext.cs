using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;

namespace Mesty.Interpreter.Models;

public record SourceCodeExecutionThreadContext(
    int ThreadId,
    IReadOnlyCollection<SourceCodeExecutionMethodStatementPointer> MethodStack,
    IReadOnlyCollection<ISourceCodeVariableDeclaration> LocalVariables)
{
    public static SourceCodeExecutionThreadContext StartNew(int threadId, SourceCodeExecutionMethodStatementPointer startPointer)
    {
        ISourceCodeVariableDeclaration[] executionThreadLocalVariables = Array.Empty<ISourceCodeVariableDeclaration>();
        return new SourceCodeExecutionThreadContext(threadId, new[] { startPointer }, executionThreadLocalVariables);
    }
}