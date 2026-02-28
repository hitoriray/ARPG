using UnityEngine;

namespace Battle.ECS.Component
{
    /// <summary>
    /// View引用组件 - 关联逻辑实体和GameObject
    /// </summary>
    public struct ViewReference
    {
        public GameObject ViewObject;
        
        public ViewReference(GameObject viewObject)
        {
            ViewObject = viewObject;
        }
    }
}