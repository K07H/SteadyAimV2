using System;

namespace SteadyAimV2
{
    public class P2PSessionExt : P2PSession
    {
        public override void OnConnected(P2PConnection conn)
        {
            base.OnConnected(conn);
            try
            {
                if (!(this.GetGameVisibility() == P2PGameVisibility.Singleplayer || ReplTools.AmIMaster()) && conn != null && conn.m_Peer == this.GetSessionMaster(false))
                    SteadyAimV2.WaitAMinBeforeFirstRequest = DateTime.Now.Ticks / 10000000L;
            }
            catch (Exception ex)
            {
                ModAPI.Log.Write($"[{SteadyAimV2.ModName}:P2PSessionExtended.OnConnected] Exception caught on connection: [{ex.ToString()}]");
            }
        }

        public override void OnDisconnected(P2PConnection conn)
        {
            try
            {
                if (!(this.GetGameVisibility() == P2PGameVisibility.Singleplayer || ReplTools.AmIMaster()) && conn != null && conn.m_Peer == this.GetSessionMaster(false))
                    SteadyAimV2.RestorePermissionStateToOrig();
            }
            catch (Exception ex)
            {
                ModAPI.Log.Write($"[{SteadyAimV2.ModName}:P2PSessionExtended.OnDisconnected] Exception caught on disconnection: [{ex.ToString()}]");
            }
            base.OnDisconnected(conn);
        }

        public override void SendTextChatMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message) && message.StartsWith(SteadyAimV2.PermissionRequestBegin, StringComparison.InvariantCulture) && message.IndexOf(SteadyAimV2.PermissionRequestEnd, StringComparison.InvariantCulture) > 0)
                return;
            base.SendTextChatMessage(message);
        }
    }
}
