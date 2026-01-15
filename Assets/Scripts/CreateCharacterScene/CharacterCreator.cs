using System.Collections;
using System.Collections.Generic;
using Config;
using UnityEngine;
using JKFrame;
using Player;

public class CharacterCreator : SingletonMono<CharacterCreator>
{
    [SerializeField] private PlayerView playerView;
    [SerializeField] private Transform characterTransform;
    [SerializeField] private Animator animator;
    
    #region 职业相关
    // 不同职业的预览动画
    [SerializeField] private RuntimeAnimatorController[] animatorControllers;
    // 不同职业的武器
    [SerializeField] private GameObject[] warriorWeapons;
    [SerializeField] private GameObject[] assassinWeapons;
    [SerializeField] private GameObject[] archerWeapons;
    [SerializeField] private GameObject[] tankWeapons;

    private GameObject[] currentWeapons;
    #endregion

    private void Start()
    {
        base.Awake();
        playerView.Init();
    }

    /// <summary>
    /// 设置职业
    /// </summary>
    /// <param name="profession"></param>
    public void SetProfession(ProfessionType profession)
    {
        // 设置预览动画
        animator.runtimeAnimatorController = animatorControllers[(int)profession];

        SetCurrentWeaponActive(false);
        // 切换武器
        switch (profession)
        {
            case ProfessionType.Warrior:
                currentWeapons = warriorWeapons;
                break;
            case ProfessionType.Assassin:
                currentWeapons = assassinWeapons;
                break;
            case ProfessionType.Archer:
                currentWeapons = archerWeapons;
                break;
            case ProfessionType.Tank:
                currentWeapons = tankWeapons;
                break;
        }
        SetCurrentWeaponActive(true);
    }

    private void SetCurrentWeaponActive(bool active)
    {
        if (currentWeapons != null)
        {
            foreach (var weapon in currentWeapons)
            {
                weapon.SetActive(active);
            }
        }
    }

    /// <summary>
    /// 旋转玩家模型
    /// </summary>
    /// <param name="rotation"></param>
    public void RotateCharacter(Vector3 rotation)
    {
        characterTransform.Rotate(rotation);
    }

    /// <summary>
    /// 设置部位
    /// </summary>
    /// <param name="partConfig"></param>
    public void SetPart(CharacterPartConfigBase partConfig)
    {
        playerView.SetPart(partConfig);
    }

    public void SetSize(CharacterPartType partType, float value)
    {
        playerView.SetSize(partType, value);
    }

    public void SetHeight(CharacterPartType partType, float value)
    {
        playerView.SetHeight(partType, value);
    }

    public void SetColor1(CharacterPartConfigBase partConfig, Color color)
    {
        playerView.SetColor1(partConfig, color);
    }
    
    public void SetColor2(CharacterPartConfigBase partConfig, Color color)
    {
        playerView.SetColor2(partConfig, color);
    }
}
