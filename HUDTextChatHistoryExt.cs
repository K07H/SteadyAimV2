namespace SteadyAimV2
{
    public class HUDTextChatHistoryExt : HUDTextChatHistory
    {
        protected override void Awake()
        {
            base.Awake();
            P2PSession.Instance.UnregisterHandler(10, new P2PNetworkMessageDelegate(base.OnTextChat));
            P2PSession.Instance.RegisterHandler(10, new P2PNetworkMessageDelegate(SteadyAimV2.TextChatEvnt));
            P2PSession.Instance.RegisterHandler(10, new P2PNetworkMessageDelegate(base.OnTextChat));
        }

        protected override void OnDestroy()
        {
            P2PSession.Instance.UnregisterHandler(10, new P2PNetworkMessageDelegate(SteadyAimV2.TextChatEvnt));
            base.OnDestroy();
        }
    }
}