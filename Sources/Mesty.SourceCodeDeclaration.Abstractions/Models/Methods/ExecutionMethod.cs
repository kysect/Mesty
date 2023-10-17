using Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;

namespace Mesty.SourceCodeDeclaration.Abstractions.Models.Methods;

public record ExecutionMethod(string Name, IReadOnlyList<ISourceCodeMethodStatementDeclaration> Statements);