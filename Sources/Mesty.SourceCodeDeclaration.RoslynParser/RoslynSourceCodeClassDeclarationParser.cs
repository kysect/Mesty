using Mesty.Core;
using Mesty.SourceCodeDeclaration.Abstractions.Contracts;
using Mesty.SourceCodeDeclaration.Abstractions.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Methods;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;
using Mesty.SourceCodeDeclaration.RoslynParser.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace Mesty.SourceCodeDeclaration.RoslynParser;

public class RoslynSourceCodeClassDeclarationParser : ISourceCodeClassDeclarationParser
{
    private readonly ILogger _logger;
    private readonly RoslynSourceCodeSyntaxParser _syntaxParser;

    public RoslynSourceCodeClassDeclarationParser(ILogger logger)
    {
        _logger = logger;

        _syntaxParser = new RoslynSourceCodeSyntaxParser(logger);
    }

    public SourceCodeClassDeclaration Parse(string sourceCode)
    {
        _logger.LogInformation("Start parsing source code string with length {length}", sourceCode.Length);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode);
        SyntaxNode root = tree.GetRoot();
        _logger.LogInformation("Tree root type is {type}", root.GetType());

        if (root is CompilationUnitSyntax compilationUnit)
            root = GetCompilationUnitSingleMember(compilationUnit);

        if (root is not ClassDeclarationSyntax classDeclarationSyntax)
            throw new MestyException("Only class as root node in supported");

        _logger.LogInformation("Parse class with name {className}", classDeclarationSyntax.Identifier.Text);
        var globalVariables = new List<ISourceCodeVariableDeclaration>();
        var sourceCodeContextMethods = new List<ExecutionMethod>();
        foreach (MemberDeclarationSyntax classMember in classDeclarationSyntax.Members)
        {
            if (classMember is FieldDeclarationSyntax fieldDeclarationSyntax)
            {
                _logger.LogInformation("Parse field [{fieldName}]", fieldDeclarationSyntax.ToString());
                VariableDescriptor variableDescriptor = VariableDeclarationParser.Instance.Parse(fieldDeclarationSyntax.Declaration);

                _logger.LogInformation("Field {name} with type {type}", variableDescriptor.Name, variableDescriptor.Type);
                globalVariables.Add(_syntaxParser.ConvertToVariableDeclaration(variableDescriptor));
            }

            if (classMember is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                sourceCodeContextMethods.Add(_syntaxParser.ParseMethodDeclarationSyntax(methodDeclarationSyntax));
            }
            else
            {
                _logger.LogInformation("Skip member {classMemberName} analyze", classMember.ToString());
            }
        }

        _logger.LogInformation("Type has {fieldCount} global variables", globalVariables.Count);
        _logger.LogInformation("Type has {methodCount} methods", sourceCodeContextMethods.Count);

        return new SourceCodeClassDeclaration(
            classDeclarationSyntax.Identifier.Text,
            globalVariables,
            sourceCodeContextMethods);
    }

    private SyntaxNode GetCompilationUnitSingleMember(CompilationUnitSyntax compilationUnit)
    {
        _logger.LogInformation("Try unwrap compilation unit.");
        if (compilationUnit.Members.Count != 1)
            throw new MestyException($"Cannot unwrap compilation unit. Unexpected member count: {compilationUnit.Members.Count}");

        MemberDeclarationSyntax member = compilationUnit.Members.Single();
        _logger.LogInformation("New analyzing type is {unwrappedType}", member.GetType());
        return member;
    }


}