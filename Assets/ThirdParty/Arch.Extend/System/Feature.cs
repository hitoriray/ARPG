namespace Arch.Extend.System
{
#if UNITY_EDITOR
	public class Feature : DebugSystems
	{
	}
#elif XY_PROFILING
	public class Feature : ProfilingSystems
	{
	}
#else
	public class Feature : Systems
	{
	}
#endif
}
