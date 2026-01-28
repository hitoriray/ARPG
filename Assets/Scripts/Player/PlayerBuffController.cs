using System.Collections.Generic;
using BuffSystem;
using JKFrame;
using UI;

namespace Player
{
    public class PlayerBuffController : BuffController
    {
        private Dictionary<Buff, UI_Buff_Slot> slotDict = new();
        protected override void OnBuffStart(Buff buff)
        {
            base.OnBuffStart(buff);
            UI_GameSceneMainWindow mainWindow = UISystem.GetWindow<UI_GameSceneMainWindow>();
            UI_Buff_Slot slot = mainWindow.AddBuff(buff.config);
            slot.UpdateStack(buff.stack);
            slot.UpdateMask(buff.destroyTimer / buff.config.duration);
            slotDict.Add(buff, slot);
        }

        protected override void OnBuffUpdate(Buff buff)
        {
            base.OnBuffUpdate(buff);
            var slot = slotDict[buff];
            slot.UpdateStack(buff.stack);
            slot.UpdateMask(buff.destroyTimer / buff.config.duration);
        }

        protected override void OnBuffEnd(Buff buff)
        {
            base.OnBuffEnd(buff);
            if (slotDict.Remove(buff, out var slot))
            {
                UI_GameSceneMainWindow mainWindow = UISystem.GetWindow<UI_GameSceneMainWindow>();
                mainWindow.RemoveBuff(slot);
            }
        }
    }
}