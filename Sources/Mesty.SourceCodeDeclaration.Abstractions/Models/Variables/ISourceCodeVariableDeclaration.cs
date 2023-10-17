namespace Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;

public interface ISourceCodeVariableDeclaration
{
    string Name { get; }
    ISourceCodeVariableDeclaration UpdateValue(ISourceCodeVariableDeclaration newValue);
    bool ValueEquals(ISourceCodeVariableDeclaration other);
}