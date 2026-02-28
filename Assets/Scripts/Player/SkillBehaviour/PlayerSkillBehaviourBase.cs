using Config;
using Data;
using JKFrame;
using Skill;
using Skill.Behaviour;
using UI;

namespace RayPlayer
{
    public abstract class PlayerSkillBehaviourBase : SkillBehaviourBase
    {
        protected PlayerController player;
        public override void Init(ICharacter owner, SkillConfig skillConfig, SkillBrainBase skillBrain, SkillPlayer skillPlayer,
            SkillLearnedData skillLearnedData, int skillIndex)
        {
            base.Init(owner, skillConfig, skillBrain, skillPlayer, skillLearnedData, skillIndex);
            player = owner as PlayerController;
        }

        protected override void UpdateSkillSlot()
        {
            if (TryGetSkillSlot(out var slot))
            {
                OnUpdateSkillSlot(slot);
            }
        }

        protected virtual bool TryGetSkillSlot(out UI_ShortcutSkill_Slot slot)
        {
            slot = null;
            if (owner is not ICharacter)
                return false;

            var window = UISystem.GetWindow<UI_GameSceneMainWindow>();
            if (window == null)
                return false;

            return window.TryGetShortcutSkillSlot(skillIndex, out slot);
        }

        protected virtual void OnUpdateSkillSlot(UI_ShortcutSkill_Slot slot)
        {
            float max = skillConfig.GetCdTimeByLv(SkillLv);
            float value = 0;
            if (max != 0) value = cdTimer / max;
            slot.UpdateCdTime(value);
            slot.UpdateSkillReleaseState(CheckRelease());
        }
    }
}
