namespace Battle
{
    public interface IDamageNumberService
    {
        void Spawn(float damage, bool isCritical, bool isHeal, UnityEngine.Vector3 worldPos);
    }
}