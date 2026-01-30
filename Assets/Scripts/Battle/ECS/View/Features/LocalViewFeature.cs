using Battle.ECS.Core;
using Battle.ECS.System;

namespace Battle.ECS.Features
{
    /// <summary>
    /// 本地战斗表现Feature
    /// </summary>
    public sealed class LocalViewFeature : ViewFeatureBase
    {
        public LocalViewFeature(BattleContext context) : base(context)
        {
            Add(new VfxSpawnSystem(context));
            Add(new ViewSyncSystem(context));
            
            Add(new DeathSystem(context));
            Add(new DestroySystem(context));
        }
    }
}
