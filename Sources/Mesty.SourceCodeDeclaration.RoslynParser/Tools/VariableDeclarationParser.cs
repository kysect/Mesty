using Kysect.CommonLib.BaseTypes.Extensions;
using Mesty.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mesty.SourceCodeDeclaration.RoslynParser.Tools;

public record VariableDescriptor(string Name, string Type, EqualsValueClauseSyntax? Initializer);

public class VariableDeclarationParser
{
    public static VariableDeclarationParser Instance { get; } = new VariableDeclarationParser();

    public VariableDescriptor Parse(VariableDeclarationSyntax variableDeclaration)
    {
        variableDeclaration.ThrowIfNull();

        if (variableDeclaration.Variables.Count != 1)
            throw new MestyException("Field declaration with multiple values is not supported.");

        VariableDeclaratorSyntax variableDeclaratorSyntax = variableDeclaration.Variables.Single();
        SyntaxToken fieldName = variableDeclaratorSyntax.Identifier;
        string type = ParseTypeFromSyntax(variableDeclaration.Type);

        return new VariableDescriptor(fieldName.Text, type, variableDeclaratorSyntax.Initializer);
    }

    private string ParseTypeFromSyntax(TypeSyntax typeSyntax)
    {
        return typeSyntax switch
        {
            PredefinedTypeSyntax predefined => predefined.Keyword.Text,
            IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.Identifier.Text,
            _ => throw new MestyException($"Type syntax {typeSyntax.GetType()} is not supported.")
        };
    }
}