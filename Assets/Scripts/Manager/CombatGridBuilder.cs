using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class CombatGridBuilder : MonoBehaviour
{
    [Header("Grid")]
    [Min(4)] public int cellsPerAxis = 40;
    [Min(0.1f)] public float cellSize = 1f;
    public Material lightCellMat;
    public Material darkCellMat;

    [Header("Distance Labels")]
    public bool showDistanceLabels = true;
    public Font customFont;
    [Min(1)] public int labelEveryNCells = 5;
    public float labelHeight = 0.02f;
    public Color labelColor = new Color(0f, 0f, 0f, 0.65f);
    [Min(8)] public int labelFontSize = 36;
    [Min(0.01f)] public float labelCharacterSize = 0.12f;
    public int labelSortingOrder = 10;

    [Header("Movement Course")]
    public Material obstacleMat;
    public Vector3 courseOrigin = new Vector3(0f, 0f, 10f);
    [Min(0.5f)] public float rampWidth = 4f;
    [Min(0.5f)] public float rampLength = 6f;
    [Min(0.1f)] public float rampThickness = 0.4f;
    [Min(0.1f)] public float platformWidth = 4f;
    [Min(0.1f)] public float platformDepth = 4f;
    [Min(0.1f)] public float platformThickness = 0.5f;

    [Header("Performance")]
    public bool useCombinedFloorMesh = true;
    public bool disableCellShadows = true;
    public bool disableObstacleShadows = true;
    public bool disableLabelShadows = true;

    [ContextMenu("Rebuild All")]
    public void RebuildAll()
    {
        RebuildGrid();
        RebuildMovementCourse();
    }

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
        if (lightCellMat == null || darkCellMat == null)
        {
            Debug.LogWarning("CombatGridBuilder: assign both lightCellMat and darkCellMat first.");
            return;
        }

        Transform floorRoot = GetOrCreateChild("_Floor");
        Transform labelRoot = GetOrCreateChild("_Labels");

        ClearChildren(floorRoot);

        BuildCells(floorRoot);
        BuildFloorCollider(floorRoot);

        if (showDistanceLabels)
        {
            BuildDistanceLabels(labelRoot);
        }
        else
        {
            SetLabelObjectsActive(labelRoot, false);
        }
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        Transform floorRoot = transform.Find("_Floor");
        Transform labelRoot = transform.Find("_Labels");

        if (floorRoot != null) SafeDestroy(floorRoot.gameObject);
        if (labelRoot != null) SafeDestroy(labelRoot.gameObject);
    }

    [ContextMenu("Rebuild Movement Course")]
    public void RebuildMovementCourse()
    {
        Transform courseRoot = GetOrCreateChild("_Course");
        ClearChildren(courseRoot);

        // Ramps for slope-limit and downhill behavior tests.
        BuildRamp(courseRoot, "Ramp_25deg", courseOrigin + new Vector3(-8f, 0f, 0f), 25f);
        BuildRamp(courseRoot, "Ramp_35deg", courseOrigin + new Vector3(0f, 0f, 0f), 35f);
        BuildRamp(courseRoot, "Ramp_45deg", courseOrigin + new Vector3(8f, 0f, 0f), 45f);

        // Platforms for jump/climb threshold tests.
        BuildPlatform(courseRoot, "Platform_1.2m", courseOrigin + new Vector3(-8f, 0f, 11f), 1.2f);
        BuildPlatform(courseRoot, "Platform_1.8m", courseOrigin + new Vector3(0f, 0f, 11f), 1.8f);
        BuildPlatform(courseRoot, "Platform_2.4m", courseOrigin + new Vector3(8f, 0f, 11f), 2.4f);
    }

    [ContextMenu("Clear Movement Course")]
    public void ClearMovementCourse()
    {
        Transform courseRoot = transform.Find("_Course");
        if (courseRoot != null) SafeDestroy(courseRoot.gameObject);
    }

    private void BuildCells(Transform floorRoot)
    {
        if (useCombinedFloorMesh)
        {
            BuildCombinedFloorMesh(floorRoot);
        }
        else
        {
            BuildCellObjects(floorRoot);
        }
    }

    private void BuildCellObjects(Transform floorRoot)
    {
        float half = cellsPerAxis * cellSize * 0.5f;

        for (int z = 0; z < cellsPerAxis; z++)
        {
            for (int x = 0; x < cellsPerAxis; x++)
            {
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.name = $"Cell_{x}_{z}";
                cell.transform.SetParent(floorRoot, false);

                float px = -half + (x + 0.5f) * cellSize;
                float pz = -half + (z + 0.5f) * cellSize;

                cell.transform.localPosition = new Vector3(px, 0f, pz);
                cell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);

                Renderer renderer = cell.GetComponent<Renderer>();
                renderer.sharedMaterial = ((x + z) & 1) == 0 ? lightCellMat : darkCellMat;
                ApplyRendererPerformance(renderer, disableCellShadows);

                // Keep a single box collider on _Floor to reduce collider count.
                Collider col = cell.GetComponent<Collider>();
                if (col != null) SafeDestroy(col);
            }
        }
    }

    private void BuildCombinedFloorMesh(Transform floorRoot)
    {
        float half = cellsPerAxis * cellSize * 0.5f;

        GameObject meshGo = new GameObject("GridMesh");
        meshGo.transform.SetParent(floorRoot, false);
        meshGo.transform.localPosition = Vector3.zero;
        meshGo.transform.localRotation = Quaternion.identity;
        meshGo.transform.localScale = Vector3.one;

        MeshFilter meshFilter = meshGo.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshGo.AddComponent<MeshRenderer>();

        int quadCount = cellsPerAxis * cellsPerAxis;
        Vector3[] vertices = new Vector3[quadCount * 4];
        Vector3[] normals = new Vector3[quadCount * 4];
        Vector2[] uvs = new Vector2[quadCount * 4];
        int[] lightTriangles = new int[quadCount * 6];
        int[] darkTriangles = new int[quadCount * 6];

        int vertexOffset = 0;
        int lightTriOffset = 0;
        int darkTriOffset = 0;

        for (int z = 0; z < cellsPerAxis; z++)
        {
            for (int x = 0; x < cellsPerAxis; x++)
            {
                float x0 = -half + x * cellSize;
                float x1 = x0 + cellSize;
                float z0 = -half + z * cellSize;
                float z1 = z0 + cellSize;

                vertices[vertexOffset + 0] = new Vector3(x0, 0f, z0);
                vertices[vertexOffset + 1] = new Vector3(x1, 0f, z0);
                vertices[vertexOffset + 2] = new Vector3(x1, 0f, z1);
                vertices[vertexOffset + 3] = new Vector3(x0, 0f, z1);

                normals[vertexOffset + 0] = Vector3.up;
                normals[vertexOffset + 1] = Vector3.up;
                normals[vertexOffset + 2] = Vector3.up;
                normals[vertexOffset + 3] = Vector3.up;

                uvs[vertexOffset + 0] = new Vector2(0f, 0f);
                uvs[vertexOffset + 1] = new Vector2(1f, 0f);
                uvs[vertexOffset + 2] = new Vector2(1f, 1f);
                uvs[vertexOffset + 3] = new Vector2(0f, 1f);

                bool lightCell = ((x + z) & 1) == 0;
                if (lightCell)
                {
                    lightTriangles[lightTriOffset + 0] = vertexOffset + 0;
                    lightTriangles[lightTriOffset + 1] = vertexOffset + 2;
                    lightTriangles[lightTriOffset + 2] = vertexOffset + 1;
                    lightTriangles[lightTriOffset + 3] = vertexOffset + 0;
                    lightTriangles[lightTriOffset + 4] = vertexOffset + 3;
                    lightTriangles[lightTriOffset + 5] = vertexOffset + 2;
                    lightTriOffset += 6;
                }
                else
                {
                    darkTriangles[darkTriOffset + 0] = vertexOffset + 0;
                    darkTriangles[darkTriOffset + 1] = vertexOffset + 2;
                    darkTriangles[darkTriOffset + 2] = vertexOffset + 1;
                    darkTriangles[darkTriOffset + 3] = vertexOffset + 0;
                    darkTriangles[darkTriOffset + 4] = vertexOffset + 3;
                    darkTriangles[darkTriOffset + 5] = vertexOffset + 2;
                    darkTriOffset += 6;
                }

                vertexOffset += 4;
            }
        }

        int[] lightTrimmed = new int[lightTriOffset];
        int[] darkTrimmed = new int[darkTriOffset];
        System.Array.Copy(lightTriangles, lightTrimmed, lightTriOffset);
        System.Array.Copy(darkTriangles, darkTrimmed, darkTriOffset);

        Mesh mesh = new Mesh();
        if (vertices.Length > 65535)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }
        mesh.name = "CombinedCombatGrid";
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.subMeshCount = 2;
        mesh.SetTriangles(lightTrimmed, 0);
        mesh.SetTriangles(darkTrimmed, 1);
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterials = new[] { lightCellMat, darkCellMat };
        ApplyRendererPerformance(meshRenderer, disableCellShadows);
    }

    private void BuildFloorCollider(Transform floorRoot)
    {
        BoxCollider box = floorRoot.GetComponent<BoxCollider>();
        if (box == null) box = floorRoot.gameObject.AddComponent<BoxCollider>();

        box.center = Vector3.zero;
        box.size = new Vector3(cellsPerAxis * cellSize, 0.2f, cellsPerAxis * cellSize);
    }

    private void BuildDistanceLabels(Transform labelRoot)
    {
        float half = cellsPerAxis * cellSize * 0.5f;
        int halfCells = cellsPerAxis / 2;
        Font font = customFont;
        if (font == null)
        {
            Debug.LogWarning("CombatGridBuilder: customFont is required for distance labels.");
            SetLabelObjectsActive(labelRoot, false);
            return;
        }

        int labelIndex = 0;
        for (int i = -halfCells; i <= halfCells; i += labelEveryNCells)
        {
            float meter = i * cellSize;
            string text = $"{meter:+0;-0;0}m";

            // Bottom edge X axis labels.
            CreateOrUpdateTextLabel(
                labelRoot,
                labelIndex++,
                $"Label_X_{text}",
                text,
                new Vector3(meter, labelHeight, -half + 0.25f),
                Quaternion.Euler(90f, 0f, 0f),
                font);

            // Left edge Z axis labels.
            CreateOrUpdateTextLabel(
                labelRoot,
                labelIndex++,
                $"Label_Z_{text}",
                text,
                new Vector3(-half + 0.25f, labelHeight, meter),
                Quaternion.Euler(90f, 90f, 0f),
                font);
        }

        DeactivateUnusedLabelObjects(labelRoot, labelIndex);
    }

    private void CreateOrUpdateTextLabel(
        Transform parent,
        int labelIndex,
        string name,
        string text,
        Vector3 localPos,
        Quaternion localRot,
        Font font)
    {
        Transform existing = labelIndex < parent.childCount ? parent.GetChild(labelIndex) : null;
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            go.transform.SetParent(parent, false);
        }

        go.name = name;
        go.SetActive(true);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        TextMesh tm = go.GetComponent<TextMesh>();
        if (tm == null)
        {
            tm = go.AddComponent<TextMesh>();
        }

        tm.text = text;
        tm.font = font;
        tm.fontSize = labelFontSize;
        tm.characterSize = labelCharacterSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = labelColor;

        MeshRenderer mr = tm.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = font.material;
            mr.sortingOrder = labelSortingOrder;
            ApplyRendererPerformance(mr, disableLabelShadows);
        }
    }

    private static void SetLabelObjectsActive(Transform parent, bool active)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(active);
        }
    }

    private static void DeactivateUnusedLabelObjects(Transform parent, int usedCount)
    {
        for (int i = usedCount; i < parent.childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void BuildRamp(Transform parent, string name, Vector3 basePoint, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float centerY = 0.5f * rampThickness * Mathf.Cos(rad) + 0.5f * rampLength * Mathf.Sin(rad);
        float centerZ = -0.5f * rampThickness * Mathf.Sin(rad) + 0.5f * rampLength * Mathf.Cos(rad);

        Vector3 center = new Vector3(basePoint.x, basePoint.y + centerY, basePoint.z + centerZ);
        Vector3 size = new Vector3(rampWidth, rampThickness, rampLength);
        Quaternion rotation = Quaternion.Euler(-angleDeg, 0f, 0f);

        CreateObstacleCube(parent, name, center, rotation, size);
    }

    private void BuildPlatform(Transform parent, string name, Vector3 groundCenter, float topHeight)
    {
        float centerY = topHeight - platformThickness * 0.5f;
        Vector3 center = new Vector3(groundCenter.x, centerY, groundCenter.z);
        Vector3 size = new Vector3(platformWidth, platformThickness, platformDepth);

        CreateObstacleCube(parent, name, center, Quaternion.identity, size);
    }

    private void CreateObstacleCube(
        Transform parent,
        string name,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && obstacleMat != null)
        {
            renderer.sharedMaterial = obstacleMat;
        }
        ApplyRendererPerformance(renderer, disableObstacleShadows);
    }

    private static void ApplyRendererPerformance(Renderer renderer, bool disableShadows)
    {
        if (renderer == null)
        {
            return;
        }

        if (disableShadows)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private Transform GetOrCreateChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(root.GetChild(i).gameObject);
        }
    }

    private static void SafeDestroy(Object obj)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(obj);
        }
        else
        {
            Object.DestroyImmediate(obj);
        }
    }
}
