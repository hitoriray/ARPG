using Config;
using Skill;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public class TestSkill : MonoBehaviour
    {
        public SkillPlayer skillPlayer;
        [FormerlySerializedAs("skillConfig")] public SkillClip skillClip;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                // skillPlayer.PlaySkill(skillConfig);
            }
        }
    }
}