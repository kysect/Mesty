using Mesty.Core;
using Mesty.Interpreter.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;
using System.Diagnostics.CodeAnalysis;
using Kysect.CommonLib.Collections.Extensions;

namespace Mesty.Interpreter;

public record SourceCodeExecutionThreadPointer(SourceCodeExecutionContext Context, int ThreadId)
{
    public SourceCodeExecutionContext ParentContext => Context;
    public IReadOnlyCollection<ISourceCodeVariableDeclaration> LocalVariables => GetThreadContext().LocalVariables;
    public IReadOnlyCollection<SourceCodeExecutionMethodStatementPointer> MethodStack => GetThreadContext().MethodStack;

    public bool TryGetLocalVariable(string variableName, [NotNullWhen(true)] out ISourceCodeVariableDeclaration? result)
    {
        result = LocalVariables.SingleOrDefault(v => v.Name == variableName);
        return result != null;
    }

    public ISourceCodeVariableDeclaration GetLocalOrGlobalVariable(string variableName)
    {
        if (TryGetLocalOrGlobalVariable(variableName, out ISourceCodeVariableDeclaration? result))
            return result;

        if (result is null)
            throw new MestyException($"Variable {variableName} is not defined");

        return result;
    }

    public bool TryGetLocalOrGlobalVariable(string variableName, [NotNullWhen(true)] out ISourceCodeVariableDeclaration? result)
    {
        result = LocalVariables.SingleOrDefault(v => v.Name == variableName);

        if (result is null)
            result = ParentContext.GlobalVariables.SingleOrDefault(v => v.Name == variableName);

        return result is not null;
    }

    public SourceCodeExecutionThreadPointer SetLocalOrGlobalVariable(string variableName, ISourceCodeVariableDeclaration newValue)
    {
        if (TryGetLocalVariable(variableName, out ISourceCodeVariableDeclaration? oldLocalValue))
        {
            return SetLocalVariable(oldLocalValue.UpdateValue(newValue));
        }

        if (ParentContext.TryGetGlobalVariable(variableName, out ISourceCodeVariableDeclaration? oldGlobalValue))
        {
            SourceCodeExecutionContext updatedExecutionContext = ParentContext.SetGlobalVariable(oldGlobalValue.UpdateValue(newValue));
            return this with { Context = updatedExecutionContext };
        }

        throw new MestyException($"Thread context does not have definition for variable {variableName}");
    }

    public SourceCodeExecutionThreadPointer SetLocalVariable(ISourceCodeVariableDeclaration variable)
    {
        SourceCodeExecutionThreadContext threadContext = GetThreadContext();
        var variables = threadContext.LocalVariables.CloneCollection().ToList();
        int index = variables.FindIndex(v => v.Name == variable.Name);
        if (index == -1)
            variables.Add(variable);
        else
            variables[index] = variable;

        threadContext = threadContext with { LocalVariables = variables };
        return this with { Context = ParentContext.UpdateThread(threadContext) };
    }

    public SourceCodeExecutionThreadPointer MovePointerToNextStatement(int shift = 1)
    {
        SourceCodeExecutionThreadContext currentContext = GetThreadContext();
        var newStack = currentContext.MethodStack.CloneCollection().ToList();
        newStack[^1] = new SourceCodeExecutionMethodStatementPointer(newStack[^1].MethodName, newStack[^1].StatementIndex + shift);
        var updatedContext = new SourceCodeExecutionThreadContext(currentContext.ThreadId, newStack, currentContext.LocalVariables);
        return this with { Context = ParentContext.UpdateThread(updatedContext) };
    }

    public SourceCodeExecutionThreadPointer ReturnFromLastMethod()
    {
        SourceCodeExecutionThreadContext currentContext = GetThreadContext();
        var updatedThreadContext = new SourceCodeExecutionThreadContext(currentContext.ThreadId, currentContext.MethodStack.Take(currentContext.MethodStack.Count - 1).ToList(), currentContext.LocalVariables);
        return this with { Context = ParentContext.UpdateThread(updatedThreadContext) };
    }

    private SourceCodeExecutionThreadContext GetThreadContext()
    {
        return Context.ThreadContexts.Single(t => t.ThreadId == ThreadId);
    }
}