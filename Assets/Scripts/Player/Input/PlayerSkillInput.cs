using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RayPlayer
{
    public class PlayerSkillInput : MonoBehaviour
    {
        [Serializable]
        private class InputBinding
        {
            public int skillIndex = -1;
            public InputActionReference action;
            public bool useBuffer = true;
            public float bufferTime = 0.2f;
        }

        [SerializeField] private InputBinding basicAttack;
        [SerializeField] private InputBinding[] skills;
        
        private readonly Dictionary<int, InputBuffer> skillBuffers = new Dictionary<int, InputBuffer>(8);
        private InputBuffer basicBuffer = new InputBuffer();

        public void Init()
        {
            basicBuffer.Unbind();
            foreach (var buffer in skillBuffers.Values)
                buffer.Unbind();
            skillBuffers.Clear();
            
            basicBuffer.Bind(basicAttack);
            if (skills == null)
                return;

            for (int i = 0; i < skills.Length; i++)
            {
                var binding = skills[i];
                if (binding == null || binding.action == null)
                    continue;

                var buffer = new InputBuffer();
                buffer.Bind(binding);
                skillBuffers[binding.skillIndex] = buffer;
            }
        }

        public bool GetBasicAttackState()
        {
            return basicBuffer.GetState();
        }

        public bool GetSkillState(int skillIndex)
        {
            return skillBuffers.TryGetValue(skillIndex, out InputBuffer buffer) && buffer.GetState();
        }

        public void ResetBasicBuffer()
        {
            basicBuffer.ResetBuffer();
        }

        public void ResetSkillBuffer(int skillIndex)
        {
            if (skillBuffers.TryGetValue(skillIndex, out InputBuffer buffer))
                buffer.ResetBuffer();
        }

        private void OnEnable()
        {
            basicBuffer.Enable();
            foreach (var buffer in skillBuffers.Values)
                buffer.Enable();
        }

        private void OnDisable()
        {
            basicBuffer.Disable();
            foreach (var buffer in skillBuffers.Values)
                buffer.Disable();
        }

        private void OnDestroy()
        {
            basicBuffer.Unbind();
            foreach (var buffer in skillBuffers.Values)
                buffer.Unbind();
        }

        private sealed class InputBuffer
        {
            private InputAction action;
            private bool useBuffer;
            private float bufferTime;
            private float lastPerformedTime = float.MinValue;

            public void Bind(InputBinding binding)
            {
                if (binding == null)
                    return;
                action = binding.action != null ? binding.action.action : null;
                useBuffer = binding.useBuffer;
                bufferTime = Mathf.Max(0f, binding.bufferTime);
                if (action != null)
                    action.performed += OnPerformed;
            }

            public void Enable()
            {
                if (action != null)
                    action.Enable();
            }

            public void Disable()
            {
                if (action != null)
                    action.Disable();
            }

            public void Unbind()
            {
                if (action != null)
                    action.performed -= OnPerformed;
                action = null;
            }

            public bool GetState()
            {
                if (action == null)
                    return false;

                if (!useBuffer)
                    return action.IsPressed();

                if (action.triggered)
                    return true;

                return Time.time - lastPerformedTime <= bufferTime;
            }

            public void ResetBuffer()
            {
                lastPerformedTime = float.MinValue;
            }

            private void OnPerformed(InputAction.CallbackContext ctx)
            {
                lastPerformedTime = Time.time;
            }
        }
    }
}