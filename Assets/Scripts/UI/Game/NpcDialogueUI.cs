using System;
using JKFrame;
using Manager;
using PixelCrushers.DialogueSystem;
using UI;
using UnityEngine;

/// <summary>
/// 将 Pixel Crushers Dialogue System 的 UI 回调桥接到 JKFrame UISystem。
/// 挂载在 Dialogue Manager 所在的 GameObject 上，并在
/// Dialogue System Controller → Dialogue UI 槽位引用本组件。
/// </summary>
[AddComponentMenu("ARPG/Dialogue/Npc Dialogue UI Bridge")]
public class NpcDialogueUI : MonoBehaviour, IDialogueUI
{
    // IDialogueUI 必须实现的 isOpen 标记
    public bool isOpen { get; set; }

    // 玩家选择分支时，插件通过此事件接收回调
    public event System.EventHandler<SelectedResponseEventArgs> SelectedResponseHandler;

    // ─── 对话开启 / 关闭 ───────────────────────────────────────────

    /// <summary>对话开始，打开你自己的 UI 窗口</summary>
    public void Open()
    {
        isOpen = true;
        PlayerService.Instance?.SetCharacterControl(false);
        PlayerService.Instance?.PushUICursor();

        var window = UISystem.Show<UI_ConversationWindow>();
        window?.SetBridge(this);
    }

    /// <summary>对话结束，关闭 UI 窗口</summary>
    public void Close()
    {
        isOpen = false;
        PlayerService.Instance?.SetCharacterControl(true);
        PlayerService.Instance?.PopUICursor();

        UISystem.Close<UI_ConversationWindow>();
    }

    // ─── 台词显示 ──────────────────────────────────────────────────

    /// <summary>显示当前说话角色的台词（NPC 或 PC 均走这里）</summary>
    public void ShowSubtitle(Subtitle subtitle)
    {
        if (subtitle == null) return;
        GetWindow()?.ShowSubtitle(subtitle);
    }

    public void HideSubtitle(Subtitle subtitle)
    {
        // 大多数实现中台词在切下一句前不手动隐藏，留空即可
    }

    // ─── 继续按钮 ──────────────────────────────────────────────────

    public void ShowContinueButton(Subtitle subtitle)
    {
        GetWindow()?.ShowContinueButton();
    }

    public void HideContinueButton(Subtitle subtitle)
    {
        GetWindow()?.HideContinueButton();
    }

    // ─── 玩家分支选项 ───────────────────────────────────────────────

    public void ShowResponses(Subtitle subtitle, Response[] responses, float timeout)
    {
        GetWindow()?.ShowResponses(responses, OnResponseSelected);
    }

    public void HideResponses()
    {
        GetWindow()?.HideResponses();
    }

    // ─── 提示文字（Alert）──────────────────────────────────────────

    public void ShowAlert(string message, float duration)
    {
        // 可选：接入你的 Toast / 提示系统
        Debug.Log($"[DialogueAlert] {message}");
    }

    public void HideAlert() { }
    public void HideAllAlerts() { }

    // ─── QTE（一般用不到，留空）──────────────────────────────────

    public void ShowQTEIndicator(int index) { }
    public void HideQTEIndicator(int index) { }

    // ─── 头像更新 ──────────────────────────────────────────────────

    public void SetPCPortrait(Sprite portraitSprite, string portraitName)
    {
        GetWindow()?.SetPCPortrait(portraitSprite, portraitName);
    }

    public void SetActorPortraitSprite(string actorName, Sprite portraitSprite)
    {
        GetWindow()?.SetActorPortrait(actorName, portraitSprite);
    }

    // ─── 点击事件（通过按钮 OnClick 回调；不走此路径时可留空）──────

    public void OnClick(object data) { }

    // ─── Continue 按钮被点击（由窗口调用）──────────────────────────

    /// <summary>
    /// 窗口里的"继续"按钮点击时调用此方法，通知插件推进对话
    /// </summary>
    public void OnContinue()
    {
        if (!isOpen) return;
        DialogueManager.instance?.SendMessage(
            DialogueSystemMessages.OnConversationContinue,
            (IDialogueUI)this,
            SendMessageOptions.DontRequireReceiver);
    }

    // ─── 玩家选择了一个分支 ──────────────────────────────────────

    private void OnResponseSelected(Response response)
    {
        SelectedResponseHandler?.Invoke(this, new SelectedResponseEventArgs(response));
    }

    // ─── 工具方法 ─────────────────────────────────────────────────

    private static UI_ConversationWindow GetWindow()
    {
        return UISystem.GetWindow<UI_ConversationWindow>();
    }
}
