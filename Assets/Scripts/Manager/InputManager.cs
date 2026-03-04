using System;
using System.Collections.Generic;
using Data;
using JKFrame;
using UnityEngine;

public class InputManager : SingletonMono<InputManager>
{
    [Serializable]
    public class Key
    {
        public KeyCode keyCode;
        public bool isCache;
        public float cacheTime;
        private float lastInputTime = float.MinValue;  // 修复：初始化为极小值，避免游戏启动时误判
        public bool valid;

        public bool GetState()
        {
            if (!isCache)
                return Input.GetKey(keyCode);
            return Input.GetKey(keyCode) || Time.time - lastInputTime < cacheTime;
        }

        public void Update()
        {
            if (!isCache)
                return;

            if (Input.GetKey(keyCode))
            {
                lastInputTime = Time.time;
            }
            valid = GetState();
        }

        public void ResetCacheTimer()
        {
            lastInputTime = float.MinValue;
        }
    }
    
    [Serializable]
    public class MouseKey
    {
        public int mouseButtonId;
        public bool isCache;
        public float cacheTime;
        private float lastInputTime = float.MinValue;  // 修复：初始化为极小值，避免游戏启动时误判
        public bool valid;

        public bool GetState()
        {
            if (!isCache)
                return Input.GetMouseButton(mouseButtonId);
            return Input.GetMouseButton(mouseButtonId) || Time.time - lastInputTime < cacheTime;
        }

        public void Update()
        {
            if (!isCache)
                return;

            if (Input.GetMouseButton(mouseButtonId))
            {
                lastInputTime = Time.time;
            }
            valid = GetState();
        }
        
        public void ResetCacheTimer()
        {
            lastInputTime = float.MinValue;
        }
    }

    [SerializeField] private Key[] skillKeys;
    [SerializeField] private MouseKey basicAttackKey;
    [SerializeField] private GameObject cineMachine;
    // Key: 角色配置中技能的索引，而不是目前角色已学技能列表中的索引
    private Dictionary<int, Key> skillKeyMap = new();
    private bool characterControl;
    public bool CharacterControl
    {
        get => characterControl;
        set
        {
            characterControl = value;
            Cursor.lockState = characterControl ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !characterControl;
            cineMachine.SetActive(characterControl);
        }
    }

    public void Init(bool characterControl)
    {
        this.characterControl = characterControl;
    }
    
    private void Update()
    {
        basicAttackKey.Update();
        for (int i = 0; i < skillKeys.Length; i++)
        {
            skillKeys[i].Update();
        }
    }

    /// <summary>
    /// 将keyCodeIndex绑定到第skillIndex个技能
    /// </summary>
    /// <param name="keyCodeIndex"></param>
    /// <param name="skillIndex"></param>
    public void BindSkillKeyCode(int keyCodeIndex, int skillIndex)
    {
        skillKeyMap[skillIndex] = skillKeys[keyCodeIndex];
    }

    public Key GetSkillKey(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skillKeys.Length)
            return null;
        return skillKeys[skillIndex];
    }

    public bool GetSkillKeyState(int skillIndex)
    {
        if (skillKeyMap.TryGetValue(skillIndex, out var skillKey))
        {
            return characterControl && skillKey.GetState();
        }
        return false;
    }
    
    public bool GetBasicAttackKeyState()
    {
        return characterControl && basicAttackKey.GetState();
    }

    public void ResetSkillKeyCodeCacheTimer(int skillIndex)
    {
        if (skillKeyMap.TryGetValue(skillIndex, out var skillKey))
        {
            skillKey.ResetCacheTimer();
        }
    }
    
    public void ResetBasicAttackKeyCodeCacheTimer()
    {
        basicAttackKey.ResetCacheTimer();
    }

    public Vector2 GetMoveInput()
    {
        if (characterControl)
        {
            // 检测玩家输入
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector2 dir = new Vector2(h, v);
            if (dir.magnitude > 1)
            {
                dir.Normalize();
            }
            return dir;
        }
        return Vector2.zero;
    }
}