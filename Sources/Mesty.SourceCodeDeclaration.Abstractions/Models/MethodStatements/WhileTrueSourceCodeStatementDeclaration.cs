namespace Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;

public record WhileTrueSourceCodeStatementDeclaration(int BlockStatementCount) : ISourceCodeMethodStatementDeclaration;

public record WhileSourceCodeStatementDeclaration(
    string LeftOperandName,
    string RightOperandName,
    int BlockStatementCount) : ISourceCodeMethodStatementDeclaration;