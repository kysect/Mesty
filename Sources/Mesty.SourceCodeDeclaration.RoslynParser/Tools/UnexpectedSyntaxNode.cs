using Mesty.Core;
using Microsoft.CodeAnalysis;

namespace Mesty.SourceCodeDeclaration.RoslynParser.Tools;

public static class UnexpectedSyntaxNode
{
    public static MestyException Handle(SyntaxNode node)
    {
        return new MestyException($"Unexpected syntax with type {node.GetType()}. Statement: {node.ToFullString()}");
    }
}