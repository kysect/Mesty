namespace Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;

// TODO: add argument passing
public record OtherMethodInvocationStatementDeclaration(string TypeName, string MethodName, string? ResultVariable) : ISourceCodeMethodStatementDeclaration;