using System;
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
        private float lastInputTime;
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
    }
    
    [Serializable]
    public class MouseKey
    {
        public int mouseButtonId;
        public bool isCache;
        public float cacheTime;
        private float lastInputTime;
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
    }

    [SerializeField] private Key[] skillKeys;
    [SerializeField] private MouseKey basicAttackKey;
    [SerializeField] private GameObject cineMachine;
    
    private bool characterControl;
    public bool CharacterControl
    {
        get => characterControl;
        set
        {
            characterControl = value;
            Cursor.lockState = characterControl ? CursorLockMode.Locked : CursorLockMode.None;
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

    public Key GetSkillKey(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skillKeys.Length)
            return null;
        return skillKeys[skillIndex];
    }

    public bool GetSkillKeyState(int skillIndex)
    {
        return characterControl && GetSkillKey(skillIndex).GetState();
    }
    
    public bool GetBasicAttackKeyState()
    {
        return characterControl && basicAttackKey.GetState();
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