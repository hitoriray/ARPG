using System;
using Config.Skill;
using Player.Skill;
using UnityEngine;

namespace Player
{
    public class TestSkill : MonoBehaviour
    {
        public SkillPlayer skillPlayer;
        public SkillConfig skillConfig;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                skillPlayer.PlaySkill(skillConfig);
            }
        }
    }
}