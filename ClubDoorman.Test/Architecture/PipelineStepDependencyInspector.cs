using System.Reflection;
using ClubDoorman.Services.Handlers.Pipeline;
using Mono.Cecil;

namespace ClubDoorman.Test.Architecture;

/// <summary>
/// Locates pipeline step types and inspects their direct Telegram type references.
/// Uses Mono.Cecil so base-class implementors of <see cref="IMessageStep"/> are not missed.
/// </summary>
internal static class PipelineStepDependencyInspector
{
    public const string StepsNamespace = "ClubDoorman.Services.Handlers.Pipeline.Steps";

    /// <summary>
    /// Frozen direct Telegram.* type refs per step FullName.
    /// Steps absent from this map must not introduce any Telegram type references.
    /// Expand deliberately when a legacy step needs more Types access; never wholesale-exempt a type.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> AllowedTelegramTypeBaseline =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.AlreadyApprovedStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.FirstMessageLogStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.PrivateSkipStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Enums.ChatType",
                "Telegram.Bot.Types.Message"
            ],
            ["ClubDoorman.Services.Handlers.Pipeline.Steps.SystemOrBotMessageStep"] =
            [
                "Telegram.Bot.Types.Chat",
                "Telegram.Bot.Types.Message",
                "Telegram.Bot.Types.User"
            ]
        };

    public static IReadOnlyList<Type> GetPipelineStepTypes()
    {
        return typeof(IMessageStep).Assembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false, IsNested: false } &&
                t.Namespace is not null &&
                t.Namespace.StartsWith(StepsNamespace, StringComparison.Ordinal) &&
                !t.Name.Contains('<', StringComparison.Ordinal)) // skip async state machines
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyDictionary<string, SortedSet<string>> GetDirectTelegramTypeRefs()
    {
        var assemblyPath = typeof(IMessageStep).Assembly.Location;
        using var module = ModuleDefinition.ReadModule(assemblyPath);
        var result = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var type in module.Types
                     .Where(t =>
                         t is { IsClass: true, IsNested: false } &&
                         t.Namespace is not null &&
                         t.Namespace.StartsWith(StepsNamespace, StringComparison.Ordinal))
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            result[type.FullName] = CollectTelegramRefs(type);
        }

        return result;
    }

    public static IEnumerable<string> GetForbiddenConstructorParameters(Type stepType)
    {
        foreach (var ctor in stepType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var parameter in ctor.GetParameters())
            {
                var parameterType = parameter.ParameterType;
                if (IsAllowedConstructorDependency(parameterType))
                    continue;

                yield return $"{stepType.FullName} ctor param '{parameter.Name}': {parameterType.FullName}";
            }
        }
    }

    private static bool IsAllowedConstructorDependency(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsInterface || type.IsAbstract)
            return true;

        // Framework / BCL value plumbing only — no ClubDoorman concrete infra.
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            return true;

        if (type.Namespace is not null &&
            (type.Namespace.StartsWith("System", StringComparison.Ordinal) ||
             type.Namespace.StartsWith("Microsoft.Extensions", StringComparison.Ordinal)))
            return true;

        return false;
    }

    private static SortedSet<string> CollectTelegramRefs(TypeDefinition type)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);

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

        return set;
    }
}
