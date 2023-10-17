using Mesty.Core;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Methods;
using Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;
using Mesty.SourceCodeDeclaration.RoslynParser.Tools;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Kysect.CommonLib.BaseTypes.Extensions;

namespace Mesty.SourceCodeDeclaration.RoslynParser;

public class RoslynSourceCodeSyntaxParser
{
    private readonly ILogger _logger;
    private readonly WellKnownMethodParser _wellKnownMethodParser;

    public RoslynSourceCodeSyntaxParser(ILogger logger)
    {
        _logger = logger;
        _wellKnownMethodParser = new WellKnownMethodParser(logger);
    }

    public ExecutionMethod ParseMethodDeclarationSyntax(MethodDeclarationSyntax methodDeclarationSyntax)
    {
        methodDeclarationSyntax.ThrowIfNull();

        string methodName = methodDeclarationSyntax.Identifier.Text;
        if (methodDeclarationSyntax.Body == null)
            throw new MestyException($"Cannot parse method {methodName}. Method does not contains body.");

        _logger.LogInformation("Parse method {methodName} with {statementCount} statements.", methodName, methodDeclarationSyntax.Body.Statements.Count);
        IReadOnlyList<ISourceCodeMethodStatementDeclaration> sourceCodeContextMethodStatements = ParseBlockSyntax(methodDeclarationSyntax.Body);

        return new ExecutionMethod(methodName, sourceCodeContextMethodStatements);
    }

    private IReadOnlyList<ISourceCodeMethodStatementDeclaration> ParseBlockSyntax(BlockSyntax methodBlock)
    {
        var methodStatements = new List<ISourceCodeMethodStatementDeclaration>();

        foreach (StatementSyntax statementSyntax in methodBlock.Statements)
        {
            _logger.LogInformation("Parse statement {statementBody}", statementSyntax.ToFullString().Trim());
            methodStatements.AddRange(ParseStatement(statementSyntax));
        }

        return methodStatements;
    }

    private IEnumerable<ISourceCodeMethodStatementDeclaration> ParseStatement(StatementSyntax statementSyntax)
    {
        var methodStatements = new List<ISourceCodeMethodStatementDeclaration>();

        if (statementSyntax is LocalDeclarationStatementSyntax localDeclarationStatementSyntax)
        {
            VariableDescriptor variableDescriptor = VariableDeclarationParser.Instance.Parse(localDeclarationStatementSyntax.Declaration);
            methodStatements.Add(new VariableDeclarationSourceCodeMethodStatementDeclaration(ConvertToVariableDeclaration(variableDescriptor)));
            if (variableDescriptor.Initializer is not null)
                methodStatements.Add(ParseInitializerSyntax(variableDescriptor));

            return methodStatements;
        }

        if (statementSyntax is ExpressionStatementSyntax expressionStatementSyntax)
        {
            return ParseExpressionStatementSyntax(expressionStatementSyntax);
        }

        if (statementSyntax is IfStatementSyntax ifStatementSyntax)
        {
            return ParseIfStatement(ifStatementSyntax);
        }

        if (statementSyntax is WhileStatementSyntax whileStatement)
        {
            return ParseWhileStatement(whileStatement);
        }

        if (statementSyntax is ReturnStatementSyntax returnStatementSyntax)
        {
            return new[] { new ReturnStatementSourceCodeMethodStatementDeclaration() };
        }

        throw new MestyException($"Statement type {statementSyntax.GetType()} is not supported. Statement: {statementSyntax.ToFullString()}");
    }

    private List<ISourceCodeMethodStatementDeclaration> ParseExpressionStatementSyntax(ExpressionStatementSyntax expressionStatementSyntax)
    {
        var methodStatements = new List<ISourceCodeMethodStatementDeclaration>();

        if (expressionStatementSyntax.Expression is AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            if (assignmentExpressionSyntax.Left is not IdentifierNameSyntax resultVariable)
                throw new MestyException($"Only assignment into variables is supported.");

            if (assignmentExpressionSyntax.Right is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                methodStatements.Add(ParseMethodInvocationSyntax(invocationExpressionSyntax, resultVariable.Identifier.Text));
                return methodStatements;
            }

            throw new MestyException($"Expression with type {assignmentExpressionSyntax.Right.GetType()} is not supported. Statement: {assignmentExpressionSyntax.Right.ToFullString()}");
        }

        if (expressionStatementSyntax.Expression is InvocationExpressionSyntax methodInvocationExpressionSyntax)
        {
            methodStatements.Add(ParseMethodInvocationSyntax(methodInvocationExpressionSyntax, null));
            return methodStatements;
        }

        throw UnexpectedSyntaxNode.Handle(expressionStatementSyntax.Expression);
    }

    public IReadOnlyCollection<ISourceCodeMethodStatementDeclaration> ParseIfStatement(IfStatementSyntax ifStatementSyntax)
    {
        ifStatementSyntax.ThrowIfNull();

        (string? left, string? right) = ParseBinaryConditionOperands(ifStatementSyntax.Condition);

        // TODO: support expression that is not block
        if (ifStatementSyntax.Statement is not BlockSyntax ifBlock)
            throw UnexpectedSyntaxNode.Handle(ifStatementSyntax.Statement);

        // TODO: support else
        if (ifStatementSyntax.Else is not null)
            throw new MestyException($"Else clause in if statement is supported");

        IReadOnlyList<ISourceCodeMethodStatementDeclaration> onTrue = ParseBlockSyntax(ifBlock);
        IReadOnlyCollection<ISourceCodeMethodStatementDeclaration> onFalse = new List<ISourceCodeMethodStatementDeclaration>();

        var ifExecutionStatement = new IfSourceCodeStatementDeclaration(left, right, onTrue.Count);

        var result = new List<ISourceCodeMethodStatementDeclaration>();
        result.Add(ifExecutionStatement);
        result.AddRange(onTrue);
        if (onFalse.Count > 0)
        {
            result.Add(new SkipStatementSourceCodeMethodStatementDeclaration(onFalse.Count));
            result.AddRange(onFalse);
        }

        return result;
    }

    private IEnumerable<ISourceCodeMethodStatementDeclaration> ParseWhileStatement(WhileStatementSyntax whileStatement)
    {
        if (whileStatement.Statement is not BlockSyntax block)
            throw UnexpectedSyntaxNode.Handle(whileStatement.Statement);

        IReadOnlyList<ISourceCodeMethodStatementDeclaration> blockStatements = ParseBlockSyntax(block);

        ISourceCodeMethodStatementDeclaration conditionStatement;
        if (whileStatement.Condition is LiteralExpressionSyntax literalExpression && literalExpression.ToFullString() == "true")
        {
            conditionStatement = new WhileTrueSourceCodeStatementDeclaration(blockStatements.Count);
        }
        else
        {
            (string? left, string? right) = ParseBinaryConditionOperands(whileStatement.Condition);
            conditionStatement = new WhileSourceCodeStatementDeclaration(left, right, blockStatements.Count);
        }

        var result = new List<ISourceCodeMethodStatementDeclaration>();
        result.Add(conditionStatement);
        result.AddRange(blockStatements);
        result.Add(new SkipStatementSourceCodeMethodStatementDeclaration(-1 * (blockStatements.Count + 1)));

        return result;
    }

    private (string, string) ParseBinaryConditionOperands(ExpressionSyntax expressionSyntax)
    {
        if (expressionSyntax is not BinaryExpressionSyntax binaryExpressionSyntax)
            throw UnexpectedSyntaxNode.Handle(expressionSyntax);

        // TODO: check for not only ==
        if (binaryExpressionSyntax.OperatorToken.Text != "==")
            throw UnexpectedSyntaxNode.Handle(binaryExpressionSyntax);

        if (binaryExpressionSyntax.Left is not IdentifierNameSyntax leftIdentifierNameSyntax
            || binaryExpressionSyntax.Right is not IdentifierNameSyntax rightIdentifierNameSyntax)
        {
            throw UnexpectedSyntaxNode.Handle(binaryExpressionSyntax);
        }

        return (leftIdentifierNameSyntax.Identifier.Text, rightIdentifierNameSyntax.Identifier.Text);
    }

    private ISourceCodeMethodStatementDeclaration ParseMethodInvocationSyntax(InvocationExpressionSyntax invocationExpressionSyntax, string? resultVariable)
    {
        if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            throw new MestyException($"Unexpected variable initialization syntax with type {invocationExpressionSyntax.Expression.GetType()}.");
        }

        if (memberAccessExpressionSyntax.Expression is not IdentifierNameSyntax typeNameIdentifierNameSyntax)
        {
            throw new MestyException($"Unexpected variable initialization syntax with type {invocationExpressionSyntax.Expression.GetType()}.");
        }

        string typeName = typeNameIdentifierNameSyntax.Identifier.Text;
        string methodName = memberAccessExpressionSyntax.Name.Identifier.Text;

        if (_wellKnownMethodParser.TryParseWellKnown(typeName, methodName, invocationExpressionSyntax, resultVariable, out ISourceCodeMethodStatementDeclaration? wellKnownMethod))
            return wellKnownMethod;

        return new OtherMethodInvocationStatementDeclaration(typeName, methodName, resultVariable);
    }

    private ISourceCodeMethodStatementDeclaration ParseInitializerSyntax(VariableDescriptor variableDescriptor)
    {
        ISourceCodeMethodStatementDeclaration ParseBinary(BinaryExpressionSyntax binaryExpressionSyntax)
        {
            if (binaryExpressionSyntax.Left is IdentifierNameSyntax left)
            {
                return new SetValueToVariableWithDecrementStatementDeclaration(variableDescriptor.Name, left.Identifier.Text);
            }

            throw UnexpectedSyntaxNode.Handle(binaryExpressionSyntax.Left);
        }

        EqualsValueClauseSyntax initializer = variableDescriptor.Initializer.ThrowIfNull(nameof(variableDescriptor.Initializer));

        return initializer.Value switch
        {
            InvocationExpressionSyntax invocationExpressionSyntax => ParseMethodInvocationSyntax(invocationExpressionSyntax, variableDescriptor.Name),
            // TODO: validate that token is ok value source
            LiteralExpressionSyntax literalExpressionSyntax => new SetValueToVariableStatementDeclaration(variableDescriptor.Name, literalExpressionSyntax.Token.ToFullString()),
            BinaryExpressionSyntax binaryExpressionSyntax => ParseBinary(binaryExpressionSyntax),
            _ => throw UnexpectedSyntaxNode.Handle(initializer.Value)
        };
    }

    public ISourceCodeVariableDeclaration ConvertToVariableDeclaration(VariableDescriptor variableDescriptor)
    {
        variableDescriptor.ThrowIfNull();

        if (variableDescriptor.Type == "long")
        {
            if (variableDescriptor.Initializer is null)
                return new SimpleLongSourceCodeVariableDeclaration(variableDescriptor.Name, 0);

            if (long.TryParse(variableDescriptor.Initializer.Value.ToFullString(), out long result))
                return new SimpleLongSourceCodeVariableDeclaration(variableDescriptor.Name, result);

            if (variableDescriptor.Initializer.Value is BinaryExpressionSyntax binaryExpressionSyntax)
            {
                // TODO: meeeeeh
                if (binaryExpressionSyntax.Left is IdentifierNameSyntax left)
                    return new LongInitFromOtherSourceCodeVariableDeclaration(variableDescriptor.Name, left.Identifier.Text);
            }

            throw UnexpectedSyntaxNode.Handle(variableDescriptor.Initializer.Value);
        }

        if (variableDescriptor.Type == "AutoResetEvent")
        {
            _logger.LogWarning("Default value for AutoResetEvent is not supported.");

            // TODO: fetch initial value
            //if (variableDescriptor.Initializer is not null)
            //    throw new MestyException("Field declaration initialization is not supported.");
            return new AutoResetEventSourceCodeVariableDeclaration(variableDescriptor.Name, false);
        }

        throw new MestyException($"Field declaration type {variableDescriptor.Type} is not supported.");
    }
}