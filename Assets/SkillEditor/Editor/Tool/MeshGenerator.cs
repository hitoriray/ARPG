using UnityEngine;

namespace SkillEditor
{
    public static class MeshGenerator
    {
        private static int[] bottomIndices =
        {
            1, 2, 0,
            1, 3, 2,
        };
        private static int[] topIndices =
        {
            0, 2, 1,
            2, 3, 1,
        };
        
        /// <summary>
        /// 生成扇形网格
        /// </summary>
        /// <param name="insideRadius">内半径</param>
        /// <param name="outsideRadius">外半径</param>
        /// <param name="height">高度</param>
        /// <param name="angle">角度</param>
        /// <returns></returns>
        public static Mesh GenerateFanMesh(float insideRadius, float outsideRadius, float height, float angle)
        {
            Mesh fanMesh = new();
            Vector3 centerPos = Vector3.zero;
            Vector3 direction = Vector3.forward;
            Vector3 rightDir = Quaternion.AngleAxis(angle / 2, Vector3.up) * direction;
            float deltaAngle = 2.5f;
            int rectCnt = (int)(angle / deltaAngle);
            int lineCnt = rectCnt + 1;
            Vector3[] vertices = new Vector3[2 * lineCnt * 2];
            int[] triangles = new int[6 * rectCnt * 4 + 6 * 12];
            
            // 底面
            for (int i = 0; i < lineCnt; i++)
            {
                // 处理顶点
                Vector3 dir = Quaternion.AngleAxis(-deltaAngle * i, Vector3.up) * rightDir;
                Vector3 minPos = centerPos + dir * insideRadius;
                Vector3 maxPos = centerPos + dir * outsideRadius;
                vertices[i * 2] = minPos;
                vertices[i * 2 + 1] = maxPos;
                
                // 处理三角形（索引1,2,0 和 索引1,3,2）保证是正面
                if (i < lineCnt - 1)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        triangles[i * 6 + j] = i * 2 + bottomIndices[j];
                    }
                }
            }
            
            // 顶面
            for (int i = lineCnt; i < lineCnt * 2; i++)
            {
                // 处理顶点
                Vector3 dir = Quaternion.AngleAxis(-deltaAngle * (i - lineCnt), Vector3.up) * rightDir;
                Vector3 minPos = centerPos + dir * insideRadius;
                Vector3 maxPos = centerPos + dir * outsideRadius;
                minPos.y += height;
                maxPos.y += height;
                vertices[i * 2] = minPos;
                vertices[i * 2 + 1] = maxPos;
                
                // 处理三角形
                if (i < lineCnt * 2 - 1)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        triangles[i * 6 + j] = i * 2 + topIndices[j];
                    }
                }
            }
            
            // 右侧面
            // 0 2 1
            // 2 3 1
            int startIndex = lineCnt * 2 - 1;
            triangles[startIndex * 6 + 0] = 0;
            triangles[startIndex * 6 + 1] = lineCnt * 2;
            triangles[startIndex * 6 + 2] = 1;
            triangles[startIndex * 6 + 3] = lineCnt * 2;
            triangles[startIndex * 6 + 4] = lineCnt * 2 + 1;
            triangles[startIndex * 6 + 5] = 1;
        
            // 左侧面
            // 1 2 0
            // 1 3 2
            triangles[startIndex * 6 + 6] = (lineCnt - 1) * 2 + 1;
            triangles[startIndex * 6 + 7] = (lineCnt * 2 - 1) * 2;
            triangles[startIndex * 6 + 8] = (lineCnt - 1) * 2;
            triangles[startIndex * 6 + 9] = (lineCnt - 1) * 2 + 1;
            triangles[startIndex * 6 + 10] = (lineCnt * 2 - 1) * 2 + 1;
            triangles[startIndex * 6 + 11] = (lineCnt * 2 - 1) * 2;

            // 内侧面
            // 0 2 1
            // 2 3 1
            startIndex += 2;
            for (int i = 0; i < rectCnt; i++)
            {
                triangles[(startIndex + i) * 6 + 0] = i * 2 + 0;
                triangles[(startIndex + i) * 6 + 1] = i * 2 + 2;
                triangles[(startIndex + i) * 6 + 2] = lineCnt * 2 + i * 2;
                triangles[(startIndex + i) * 6 + 3] = i * 2 + 2;
                triangles[(startIndex + i) * 6 + 4] = lineCnt * 2 + i * 2 + 2;
                triangles[(startIndex + i) * 6 + 5] = lineCnt * 2 + i * 2;
            }
            
            // 外侧面
            // 1 2 0
            // 1 3 2
            startIndex += rectCnt;
            for (int i = 0; i < rectCnt; i++)
            {
                triangles[(startIndex + i) * 6 + 0] = lineCnt * 2 + i * 2 + 1;
                triangles[(startIndex + i) * 6 + 1] = i * 2 + 1 + 2;
                triangles[(startIndex + i) * 6 + 2] = i * 2 + 1;
                triangles[(startIndex + i) * 6 + 3] = lineCnt * 2 + i * 2 + 1;
                triangles[(startIndex + i) * 6 + 4] = lineCnt * 2 + i * 2 + 1 + 2;
                triangles[(startIndex + i) * 6 + 5] = i * 2 + 1 + 2;
            }
            
            fanMesh.vertices = vertices;
            fanMesh.triangles = triangles;
            fanMesh.RecalculateNormals();
            return fanMesh;
        }
    }
}