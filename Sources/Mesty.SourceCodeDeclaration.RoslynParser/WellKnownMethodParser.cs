using Kysect.CommonLib.BaseTypes.Extensions;
using Mesty.Core;
using Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;
using Mesty.SourceCodeDeclaration.RoslynParser.Tools;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Mesty.SourceCodeDeclaration.RoslynParser;

public class WellKnownMethodParser
{
    private readonly ILogger _logger;

    public WellKnownMethodParser(ILogger logger)
    {
        _logger = logger;
    }

    public bool TryParseWellKnown(string typeName, string methodName, InvocationExpressionSyntax invocationExpressionSyntax, string? resultVariable, [NotNullWhen(true)] out ISourceCodeMethodStatementDeclaration? result)
    {
        if (typeName == "Interlocked" && methodName == "Increment")
            return TryParseInterlockedIncrement(invocationExpressionSyntax, resultVariable, out result);

        if (typeName == "Interlocked" && methodName == "Read")
            return TryParseInterlockedRead(invocationExpressionSyntax, resultVariable, out result);

        if (typeName == "Interlocked" && methodName == "CompareExchange")
            return TryParseInterlockedCompareExchange(invocationExpressionSyntax, resultVariable, out result);
        result = null;
        return false;
    }

    public bool TryParseInterlockedIncrement(InvocationExpressionSyntax invocationExpressionSyntax, string? resultVariable, out ISourceCodeMethodStatementDeclaration? result)
    {
        invocationExpressionSyntax.ThrowIfNull();

        _logger.LogInformation("Parse method invocation as Interlock.Increment");
        if (invocationExpressionSyntax.ArgumentList.Arguments.Count != 1)
            throw new MestyException("Interlock.Increment must has one argument.");

        ArgumentSyntax argument = invocationExpressionSyntax.ArgumentList.Arguments.Single();
        if (argument.Expression is not IdentifierNameSyntax identifierName)
        {
            throw UnexpectedSyntaxNode.Handle(argument.Expression);
        }

        // TODO: null check?
        ArgumentNullException.ThrowIfNull(resultVariable);
        result = new InterlockedIncrementMethodStatementDeclaration(identifierName.Identifier.Text, resultVariable);
        return true;
    }

    public bool TryParseInterlockedRead(InvocationExpressionSyntax invocationExpressionSyntax, string? resultVariable, out ISourceCodeMethodStatementDeclaration result)
    {
        invocationExpressionSyntax.ThrowIfNull();

        _logger.LogInformation("Parse method invocation as Interlock.Read");
        if (invocationExpressionSyntax.ArgumentList.Arguments.Count != 1)
            throw new MestyException("Interlock.Read must has one argument.");

        ArgumentSyntax argument = invocationExpressionSyntax.ArgumentList.Arguments.Single();
        if (argument.Expression is not IdentifierNameSyntax identifierName)
        {
            throw UnexpectedSyntaxNode.Handle(argument.Expression);
        }

        // TODO: null check?
        ArgumentNullException.ThrowIfNull(resultVariable);
        result = new InterlockedReadMethodStatementDeclaration(identifierName.Identifier.Text, resultVariable);
        return true;
    }

    public bool TryParseInterlockedCompareExchange(InvocationExpressionSyntax invocationExpressionSyntax, string? resultVariable, out ISourceCodeMethodStatementDeclaration result)
    {
        invocationExpressionSyntax.ThrowIfNull();

        _logger.LogInformation("Parse method invocation as Interlock.CompareExchange");
        if (invocationExpressionSyntax.ArgumentList.Arguments.Count != 3)
            throw new MestyException("Interlock.CompareExchange must has 3 argument.");

        ArgumentSyntax locationArgument = invocationExpressionSyntax.ArgumentList.Arguments[0];
        if (locationArgument.Expression is not IdentifierNameSyntax locationIdentifier)
            throw UnexpectedSyntaxNode.Handle(locationArgument.Expression);

        ArgumentSyntax valueArgument = invocationExpressionSyntax.ArgumentList.Arguments[1];
        if (valueArgument.Expression is not IdentifierNameSyntax valueIdentifier)
            throw UnexpectedSyntaxNode.Handle(valueArgument.Expression);

        ArgumentSyntax comparandArgument = invocationExpressionSyntax.ArgumentList.Arguments[2];
        if (comparandArgument.Expression is not IdentifierNameSyntax comparandIdentifier)
            throw UnexpectedSyntaxNode.Handle(comparandArgument.Expression);

        // TODO: null check?
        ArgumentNullException.ThrowIfNull(resultVariable);
        result = new InterlockedCompareExchangeMethodStatementDeclaration(
            locationIdentifier.Identifier.Text,
            valueIdentifier.Identifier.Text,
            comparandIdentifier.Identifier.Text,
            resultVariable);
        return true;
    }
}