namespace QuickRemoting;

public static class InterfaceValidator<TService>
{
    static InterfaceValidator()
    {
        Validate(typeof(TService));
    }

    public static void Validate() { }

    private static void Validate(Type type)
    {
        var methods = type.GetMethods();

        ValidateUnambiguousMethodNames(type, methods);
        ValidateMethodReturnValues(type, methods);
    }

    private static void ValidateUnambiguousMethodNames(Type type, MethodInfo[] methods)
    {
        var dmr = (
            from m in methods
            group m by m.Name into g
            where g.Count() > 1
            select new { Name = g.Key, Count = g.Count() }
        ).FirstOrDefault();

        if (dmr != null)
        {
            Debugger.Break();
            throw new Exception($"Interface {type.Name} has {dmr.Count} methods with the same name {dmr.Name}, which is not supported.");
        }
    }

    private static void ValidateMethodReturnValues(Type type, MethodInfo[] methods)
    {
        var firstMethod = methods.FirstOrDefault(m => !typeof(Task).IsAssignableFrom(m.ReturnType));

        if (firstMethod != null)
        {
            Debugger.Break();
            throw new Exception($"Interface {type.Name} has method {firstMethod.Name} with return type {firstMethod.ReturnType.Name}. All return types must derive from System.Threading.Task.");
        }
    }
}
