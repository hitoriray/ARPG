using JKFrame;
using UI;
using UnityEngine;
using Config;

namespace Item
{
    /// <summary>
    /// 酒馆 NPC 交互入口：玩家进入范围后按交互键打开对应 NPC 对话
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TavernNpcInteractable : MonoBehaviour
    {
        [Header("优先使用角色卡里的 NpcId")]
        [SerializeField] private TavernCharacterCard characterCard;
        [SerializeField] private string npcId = "";
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool logOpen = false;

        private bool playerInside;
        private bool lastInteractive;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void Update()
        {
            if (!playerInside)
            {
                lastInteractive = false;
                return;
            }

            bool interactive = InputService.Instance != null && InputService.Instance.Interactive;
            if (interactive && !lastInteractive)
            {
                OpenDialog();
            }
            lastInteractive = interactive;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            playerInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            playerInside = false;
            lastInteractive = false;
        }

        private void OpenDialog()
        {
            string resolvedNpcId = ResolveNpcId();
            var window = UISystem.Show<UI_DialogWindow>();
            window?.Show(resolvedNpcId);

            if (logOpen)
            {
                RayDebug.Info($"[TavernNpcInteractable] 打开对话：npcId={resolvedNpcId}");
            }
        }

        private string ResolveNpcId()
        {
            if (characterCard != null && !string.IsNullOrWhiteSpace(characterCard.NpcId))
                return characterCard.NpcId;
            if (!string.IsNullOrWhiteSpace(npcId))
                return npcId.Trim();
            return "tavern_default";
        }
    }
}
