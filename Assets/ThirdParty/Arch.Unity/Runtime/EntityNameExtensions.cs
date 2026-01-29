using Arch.Core;

namespace Arch.Unity
{
    public static class EntityNameExtensions
    {
        public static string GetName(this World world, Entity entity)
        {
            ref var name = ref world.Get<EntityName>(entity);
            return name.Value;
        }

        public static void SetName(this World world, Entity entity, string name)
        {
            world.Set<EntityName>(entity, new(name));
        }
    }
}