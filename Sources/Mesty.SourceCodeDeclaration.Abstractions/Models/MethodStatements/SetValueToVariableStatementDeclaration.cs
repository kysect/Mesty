namespace Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;

public record SetValueToVariableStatementDeclaration(string VariableName, string VariableValue) : ISourceCodeMethodStatementDeclaration;

//TODO: meeeh
public record SetValueToVariableWithDecrementStatementDeclaration(string VariableName, string VariableValue) : ISourceCodeMethodStatementDeclaration;