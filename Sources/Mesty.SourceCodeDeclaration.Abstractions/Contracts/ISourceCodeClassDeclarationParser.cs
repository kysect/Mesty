using Mesty.SourceCodeDeclaration.Abstractions.Models;

namespace Mesty.SourceCodeDeclaration.Abstractions.Contracts;

public interface ISourceCodeClassDeclarationParser
{
    SourceCodeClassDeclaration Parse(string sourceCode);
}