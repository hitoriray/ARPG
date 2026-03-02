using System;
using System.Reflection;
using UnityEngine;

public static class MonoSingletonRuntimeInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAllMonoSingletonStatics()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            if (types == null) continue;

            foreach (var type in types)
            {
                if (type == null || type.IsAbstract) continue;
                if (!IsSubclassOfRawGeneric(typeof(MonoSingleton<>), type)) continue;

                var baseType = type.BaseType;
                if (baseType == null) continue;

                var instanceField = baseType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
                if (instanceField != null) instanceField.SetValue(null, null);

                var quittingField = baseType.GetField("isQuitting", BindingFlags.Static | BindingFlags.NonPublic);
                if (quittingField != null) quittingField.SetValue(null, false);
            }
        }
    }

    private static bool IsSubclassOfRawGeneric(Type rawGeneric, Type toCheck)
    {
        var current = toCheck;
        while (current != null && current != typeof(object))
        {
            var cur = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
            if (rawGeneric == cur) return true;
            current = current.BaseType;
        }

        return false;
    }
}
