using System.Diagnostics;
using UnityEngine.Profiling;

namespace Battle.ECS.Core.Helper
{
    public static class ProfilerHelper
    {
        [Conditional("UNITY_EDITOR"), Conditional("XY_PROFILING")]
        public static void Begin(string name)
        {
            Profiler.BeginSample(name);
        }

        [Conditional("UNITY_EDITOR"), Conditional("XY_PROFILING")]
        public static void End()
        {
            Profiler.EndSample();
        }
    }
}