using UnityEngine;

public abstract class MapObjectBase : MonoBehaviour
{
    public Vector2Int coord;

    public virtual void Init(Vector2Int coord)
    {
        this.coord = coord;
    }
}