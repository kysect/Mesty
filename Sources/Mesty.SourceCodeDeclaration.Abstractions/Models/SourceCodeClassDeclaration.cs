using Mesty.SourceCodeDeclaration.Abstractions.Models.Methods;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;

namespace Mesty.SourceCodeDeclaration.Abstractions.Models;

public record SourceCodeClassDeclaration(
    string TypeName,
    IReadOnlyCollection<ISourceCodeVariableDeclaration> MemberVariables,
    IReadOnlyCollection<ExecutionMethod> Methods
);