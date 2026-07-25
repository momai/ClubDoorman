using System.Reflection;
using ClubDoorman.Services.Handlers.Pipeline;
using Mono.Cecil;

namespace ClubDoorman.Test.Architecture;

/// <summary>
/// Discovers concrete <see cref="IMessageStep"/> types and inspects constructor / Telegram deps.
/// Nested types (async state machines) are folded into the outer step's Telegram ref set.
/// </summary>
internal static class PipelineStepDependencyInspector
{
    public const string StepsNamespace = "ClubDoorman.Services.Handlers.Pipeline.Steps";

    /// <summary>
    /// Known infrastructure / storage / mutable-config namespaces.
    /// Concrete constructor parameters from these are forbidden for pipeline steps.
    /// </summary>
    private static readonly string[] ForbiddenConcreteNamespaces =
    [
        "ClubDoorman.Services.Core.Configuration",
        "ClubDoorman.Services.UserManagement",
        "ClubDoorman.Services.SuspiciousUsers",
        "ClubDoorman.Infrastructure",
        "Microsoft.Extensions.Caching.Memory",
        "Microsoft.Extensions.Caching.Distributed",
        "System.Runtime.Caching"
    ];

    private static readonly HashSet<string> ForbiddenConcreteTypeFullNames =
        new(StringComparer.Ordinal)
        {
            "Microsoft.Extensions.Caching.Memory.MemoryCache",
            "System.Runtime.Caching.MemoryCache"
        };

    public static IReadOnlyDictionary<string, string[]> AllowedTelegramTypeBaseline =>
        PipelineStepTelegramBaseline.Allowed;

    public static bool IsInStepsNamespace(string? ns) =>
        ns == StepsNamespace ||
        (ns is not null && ns.StartsWith(StepsNamespace + ".", StringComparison.Ordinal));

    /// <summary>
    /// All concrete IMessageStep implementors in the production assembly (including via base class).
    /// </summary>
    public static IReadOnlyList<Type> GetConcreteMessageSteps()
    {
        return typeof(IMessageStep).Assembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                typeof(IMessageStep).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyDictionary<string, SortedSet<string>> GetDirectTelegramTypeRefs()
    {
        var stepFullNames = GetConcreteMessageSteps()
            .Select(t => t.FullName!)
            .ToHashSet(StringComparer.Ordinal);

        var assemblyPath = typeof(IMessageStep).Assembly.Location;
        using var module = ModuleDefinition.ReadModule(assemblyPath);
        var result = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var type in module.Types
                     .Where(t => t is { IsClass: true, IsNested: false } && stepFullNames.Contains(t.FullName))
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            result[type.FullName] = CollectTelegramRefs(type);
        }

        return result;
    }

    public static IEnumerable<string> GetForbiddenConstructorParameters(Type stepType)
    {
        foreach (var ctor in stepType.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
        {
            foreach (var parameter in ctor.GetParameters())
            {
                if (!IsForbiddenConstructorDependency(parameter.ParameterType))
                    continue;

                yield return $"{stepType.FullName} ctor param '{parameter.Name}': {parameter.ParameterType.FullName}";
            }
        }
    }

    /// <summary>
    /// Forbidden: concrete stateful app/infra services, storage, cache, mutable configuration.
    /// Allowed: interfaces, abstracts, value types, string, immutable/framework value objects, DTOs.
    /// </summary>
    private static bool IsForbiddenConstructorDependency(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsInterface || type.IsAbstract)
            return false;

        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            return false;

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition.IsInterface || definition.IsAbstract)
                return false;
        }

        var fullName = type.FullName ?? type.Name;
        if (ForbiddenConcreteTypeFullNames.Contains(fullName))
            return true;

        var ns = type.Namespace ?? string.Empty;
        foreach (var forbiddenNs in ForbiddenConcreteNamespaces)
        {
            if (ns == forbiddenNs || ns.StartsWith(forbiddenNs + ".", StringComparison.Ordinal))
                return true;
        }

        // Infrastructure-shaped ClubDoorman concretes outside the listed namespaces.
        if (ns.StartsWith("ClubDoorman", StringComparison.Ordinal) &&
            (type.Name.EndsWith("Storage", StringComparison.Ordinal) ||
             type.Name.EndsWith("Options", StringComparison.Ordinal) ||
             type.Name.EndsWith("Cache", StringComparison.Ordinal) ||
             type.Name.EndsWith("Index", StringComparison.Ordinal)))
        {
            return true;
        }

        // Telegram Bot client/API types (message DTOs under Types are allowed).
        if (ns.StartsWith("Telegram.Bot", StringComparison.Ordinal) &&
            !ns.StartsWith("Telegram.Bot.Types", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static SortedSet<string> CollectTelegramRefs(TypeDefinition type)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        CollectTelegramRefs(type, set);
        return set;
    }

    private static void CollectTelegramRefs(TypeDefinition type, SortedSet<string> set)
    {
        void Add(TypeReference? tr)
        {
            if (tr is null)
                return;

            switch (tr)
            {
                case GenericInstanceType gi:
                    Add(gi.ElementType);
                    foreach (var arg in gi.GenericArguments)
                        Add(arg);
                    return;
                case ArrayType or ByReferenceType or PointerType:
                    Add(tr.GetElementType());
                    return;
            }

            var name = tr.FullName;
            if (name.StartsWith("Telegram.", StringComparison.Ordinal))
                set.Add(name);
        }

        foreach (var field in type.Fields)
            Add(field.FieldType);

        foreach (var property in type.Properties)
            Add(property.PropertyType);

        foreach (var method in type.Methods)
        {
            Add(method.ReturnType);
            foreach (var parameter in method.Parameters)
                Add(parameter.ParameterType);

            if (!method.HasBody)
                continue;

            foreach (var variable in method.Body.Variables)
                Add(variable.VariableType);

            foreach (var instruction in method.Body.Instructions)
            {
                switch (instruction.Operand)
                {
                    case MethodReference methodRef:
                        Add(methodRef.DeclaringType);
                        Add(methodRef.ReturnType);
                        foreach (var parameter in methodRef.Parameters)
                            Add(parameter.ParameterType);
                        break;
                    case FieldReference fieldRef:
                        Add(fieldRef.DeclaringType);
                        Add(fieldRef.FieldType);
                        break;
                    case TypeReference typeRef:
                        Add(typeRef);
                        break;
                }
            }
        }

        // Async ExecuteAsync bodies live in nested state machines (<ExecuteAsync>d__*).
        foreach (var nestedType in type.NestedTypes)
            CollectTelegramRefs(nestedType, set);
    }
}
