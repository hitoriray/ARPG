using System.Collections.Generic;
using Berry;
using FixMath;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// GOAP 地图管理器
    /// </summary>
    public class MapManager : SingletonMono<MapManager>
    {
        public Stack<Vector2Int> backupCells = new();
        public int currentRingNum = 0;
        public float cellSize = 2;

        private void Update()
        {
            UpdateSpawnRole();
        }

        private void CreateBackupCells()
        {
            currentRingNum += 1;
            int minX = -currentRingNum;
            int minY = -currentRingNum;
            int sideLen = currentRingNum * 2 + 1;

            // Left
            for (int y = minY; y < minY + sideLen; y++)
            {
                backupCells.Push(new Vector2Int(minX, y));
            }

            // Top
            int maxY = minY + sideLen - 1;
            for (int x = minX + 1; x < minX + sideLen; x++)
            {
                backupCells.Push(new Vector2Int(x, maxY));
            }

            // Right
            int maxX = minX + sideLen - 1;
            for (int y = maxY - 1; y >= minY; y--)
            {
                backupCells.Push(new Vector2Int(maxX, y));
            }

            // Bottom
            for (int x = maxX - 1; x > minX; x--)
            {
                backupCells.Push(new Vector2Int(x, minY));
            }
        }

        public Vector3 GetCellPosition(Vector2Int coord)
        {
            return new Vector3(coord.x * cellSize, 0, coord.y * cellSize);
        }

        public Vector2Int GetNextBuildCoord()
        {
            if (backupCells.Count == 0)
            {
                CreateBackupCells();
            }

            return backupCells.Pop();
        }

        #region 浆果

        [Header("浆果")] public Transform berryRoot;
        public GameObject berryPrefab;
        [ReadOnly] public HashSet<BerryController> ripeBerries = new(); // 成熟的浆果控制器
        [ReadOnly] public int ripeBerryCount => ripeBerries.Count;

        public BerryController SpawnBerry(Vector2Int coord)
        {
            BerryController berry = GameObject
                .Instantiate(berryPrefab, GetCellPosition(coord), Quaternion.identity, berryRoot)
                .GetComponent<BerryController>();

            return berry;
        }

        public void OnBerryRipe(BerryController berryController)
        {
            if (ripeBerries.Add(berryController))
            {
                RayDebug.Log($"成熟了一个浆果. 总成熟浆果数: {ripeBerryCount}");
                GOAPUIManager.Instance.SetRipeBerryCount(ripeBerryCount);

            }
        }

        public void RemoveBerryRipe(BerryController berryController)
        {
            if (ripeBerries.Remove(berryController))
            {
                RayDebug.Log($"移除一个成熟的浆果. 总成熟浆果数: {ripeBerryCount}");
                GOAPUIManager.Instance.SetRipeBerryCount(ripeBerryCount);
            }
        }

        public BerryController TryGetRipeBerry()
        {
            if (ripeBerryCount == 0) return null;
            BerryController berry = null;
            foreach (var item in ripeBerries)
            {
                berry = item;
                break;
            }

            RemoveBerryRipe(berry);
            return berry;
        }

        #endregion

        #region 食物

        private int reserveFoodCount = 0;

        public int ReserveFoodCount
        {
            get => reserveFoodCount;
            set
            {
                reserveFoodCount = value;
                RayDebug.Info($"食物数量: {reserveFoodCount}");
                GOAPUIManager.Instance.SetReserveFoodCount(reserveFoodCount);
                // TODO:
            }
        }

        #endregion

        #region 村民

        [Header("村民")] public GameObject rolePrefab;
        public Transform roleRoot;
        public int maxRoleCount = 10;
        public float spawnRoleInterval = 3f;
        private float spawnRoleTimer;
        private int roleCount;

        public int RoleCount
        {
            get => roleCount;
            set
            {
                roleCount = value;
                RayDebug.Info($"村民数量: {roleCount}");
                GOAPUIManager.Instance.SetRoleCount(roleCount);

            }
        }

        private void UpdateSpawnRole()
        {
            if (roleCount >= maxRoleCount) return;

            spawnRoleTimer -= Time.deltaTime;
            if (spawnRoleTimer <= 0f)
            {
                spawnRoleTimer = spawnRoleInterval;
                if (reserveFoodCount > roleCount * 3)
                {
                    Vector3 pos = new Vector3(TSRandom.Range(-10, 10), 0, TSRandom.Range(-10, 10));
                    GameObject.Instantiate(rolePrefab, pos, Quaternion.identity, roleRoot);
                    RoleCount += 1;
                }
            }
        }

        private void OnRoleDie()
        {
            RoleCount -= 1;
        }

        private void OnRoleEat()
        {
            ReserveFoodCount -= 1;
        }

    #endregion
    }
}