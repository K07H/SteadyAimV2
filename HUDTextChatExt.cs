using System;
using UnityEngine;

namespace SteadyAimV2
{
    public class HUDTextChatExt : HUDTextChat
    {
        public override void ConstantUpdate()
        {
            base.ConstantUpdate();
            if (SteadyAimV2.DoRequestPermission)
            {
                SteadyAimV2.DoRequestPermission = false;
                if (SteadyAimV2.WaitAMinBeforeFirstRequest > 0L)
                {
                    if (((DateTime.Now.Ticks / 10000000L) - SteadyAimV2.WaitAMinBeforeFirstRequest) > 59L)
                        SteadyAimV2.WaitAMinBeforeFirstRequest = -1L;
                    else
                    {
                        SteadyAimV2.ShowHUDError("You need to wait one minute before doing your first permission request.");
                        return;
                    }
                }
                if (P2PSession.Instance.GetGameVisibility() == P2PGameVisibility.Singleplayer || ReplTools.AmIMaster() || ReplTools.IsPlayingAlone())
                {
                    SteadyAimV2.ShowHUDError("Cannot request permission because you are the host or in singleplayer mode.");
                    return;
                }
                if (HUDNameAnimal.Get().IsActive() || HUDNameAnimal.Get().GetTimeSinceDeactivation() < 0.5f)
                {
                    SteadyAimV2.ShowHUDError("Cannot request permission because you are currently giving a name to an animal.");
                    return;
                }
                if (SteadyAimV2.PermissionGranted)
                {
                    SteadyAimV2.ShowHUDInfo("Host already gave you permission.");
                    return;
                }
                if (SteadyAimV2.PermissionDenied)
                {
                    SteadyAimV2.ShowHUDError("Host has denied permission or did not reply. Please restart the game to ask permission again.");
                    return;
                }
                if (SteadyAimV2.NbPermissionRequests >= 3)
                {
                    SteadyAimV2.ShowHUDError("You've reached the maximum amount of permission requests. Please restart the game to ask permission again.");
                    return;
                }
                if (SteadyAimV2.WaitingPermission)
                {
                    SteadyAimV2.ShowHUDError("You've requested permission less than a minute ago. Please wait one minute to ask again.");
                    return;
                }
                if (SteadyAimV2.OtherWaitingPermission)
                {
                    SteadyAimV2.ShowHUDError("Another player requested permission less than a minute ago. Please wait one minute to ask permission.");
                    return;
                }
                try
                {
                    if (this.m_ShouldBeVisible || InputsManager.Get().m_TextInputActive)
                    {
                        this.m_Field.text = string.Empty;
                        this.m_Field.DeactivateInputField();
                        this.m_Field.text = string.Empty;
                        this.m_ShouldBeVisible = false;
                        InputsManager.Get().m_TextInputActive = false;
                        if (this.m_ShouldBeVisible || InputsManager.Get().m_TextInputActive)
                        {
                            SteadyAimV2.ShowHUDError("Sending permission request failed (a chat window is active). Please try again.");
                            return;
                        }
                    }
                    P2PNetworkWriter writer = new P2PNetworkWriter();
                    writer.StartMessage(10);
                    writer.Write(SteadyAimV2.PermissionRequestFinal);
                    writer.FinishMessage();
                    P2PSession.Instance.SendWriterToAll(writer, 1);

                    SteadyAimV2.NbPermissionRequests = (SteadyAimV2.NbPermissionRequests + 1);
                    SteadyAimV2.PermissionAskTime = DateTime.Now.Ticks / 10000000L;
                    SteadyAimV2.WaitingPermission = true;
                    if (this.m_History)
                        this.m_History.StoreMessage(SteadyAimV2.PermissionRequestFinal, ReplTools.GetLocalPeer().GetDisplayName(), new Color?(ReplicatedLogicalPlayer.s_LocalLogicalPlayer ? ReplicatedLogicalPlayer.s_LocalLogicalPlayer.GetPlayerColor() : HUDTextChatHistory.NormalColor));
                    SteadyAimV2.ShowHUDInfo("Permission has been requested. Please wait one minute for the host to reply.");
                }
                catch (Exception ex)
                {
                    ModAPI.Log.Write($"[{SteadyAimV2.ModName}:HUDTextChatExtended.ConstantUpdate] Exception caught while trying to send permission request: [{ex.ToString()}]");
                }
            }
        }
    }
}
