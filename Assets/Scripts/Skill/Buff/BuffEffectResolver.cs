using Config;
using UnityEngine;

namespace Buff
{
    public abstract class BuffEffectResolverBase : MonoBehaviour
    {
        public abstract void Resolve(Buff buff, BuffEffectDataBase effectData);
    }
}