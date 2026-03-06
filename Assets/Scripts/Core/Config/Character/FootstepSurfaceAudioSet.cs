using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum FootstepSurfaceType
{
    Default = 0,
    Stone = 1,
    Wood = 2,
    Grass = 3,
    Sand = 4,
    Dirt = 5,
    Metal = 6,
    Water = 7,
    Snow = 8,
}

[CreateAssetMenu(fileName = "FootstepAudioSet", menuName = "Config/Audio/Footstep Audio Set")]
public class FootstepSurfaceAudioSet : ScriptableObject
{
    [Serializable]
    public struct SurfaceEntry
    {
        [LabelText("地表类型")] public FootstepSurfaceType Surface;
        [LabelText("脚步声列表")] public AudioClip[] Clips;
    }

    [LabelText("默认脚步声")] public AudioClip[] DefaultClips;
    [LabelText("地表映射")] public SurfaceEntry[] SurfaceClips;

    [NonSerialized] private Dictionary<FootstepSurfaceType, AudioClip[]> clipLookup;

    private void OnEnable()
    {
        clipLookup = null;
    }

    public AudioClip[] GetClips(FootstepSurfaceType surfaceType)
    {
        EnsureLookup();
        if (clipLookup != null && clipLookup.TryGetValue(surfaceType, out var clips) && clips != null && clips.Length > 0)
            return clips;
        return DefaultClips;
    }

    private void EnsureLookup()
    {
        if (clipLookup != null)
            return;

        clipLookup = new Dictionary<FootstepSurfaceType, AudioClip[]>();
        if (SurfaceClips == null)
            return;

        foreach (var entry in SurfaceClips)
        {
            if (entry.Clips == null || entry.Clips.Length == 0)
                continue;
            clipLookup[entry.Surface] = entry.Clips;
        }
    }
}
