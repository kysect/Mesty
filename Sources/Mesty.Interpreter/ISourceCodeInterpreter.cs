using Mesty.Interpreter.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models;

namespace Mesty.Interpreter;

public interface ISourceCodeInterpreter
{
    SourceCodeExecutionContext Execute(SourceCodeClassDeclaration declaration, SourceCodeExecutionMethodStatementPointer startPointer);
    SourceCodeExecutionContext Execute(SourceCodeClassDeclaration declaration, IReadOnlyList<SourceCodeExecutionMethodStatementPointer> startPointers);
}