using System;
using Manager;
using UnityEngine;

namespace Berry
{
    public class BerryController : MapObjectBase
    {
        public MeshRenderer meshRenderer;
        public Material normalMaterial;
        public Material ripeMaterial;
        private bool isRipe;
        public bool IsRipe
        {
            get => isRipe;
            set
            {
                isRipe = value;
                meshRenderer.material = isRipe ? ripeMaterial : normalMaterial;
                if (isRipe)
                    MapManager.Instance.OnBerryRipe(this);
                else
                    MapManager.Instance.RemoveBerryRipe(this);
            }
        }
        
        public float time;
        private float timer;
        public float ripeSpeed;

        private void Update()
        {
            if (!isRipe)
            {
                timer -= Time.deltaTime * ripeSpeed;
                if (timer <= 0f)
                {
                    IsRipe = true;
                }
            }
        }

        public override void Init(Vector2Int coord)
        {
            base.Init(coord);
            IsRipe = true;
        }

        public void OnPick()
        {
            IsRipe = false;
        }
    }
}