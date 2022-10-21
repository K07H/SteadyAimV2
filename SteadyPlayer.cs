namespace SteadyAimV2
{
    internal class SteadyPlayer : Player
    {
        public override void StartShake(float power, float speed, float duration, float shake_mul)
        {
            if (!SteadyAimV2.SteadyAimV2Enabled || !(P2PSession.Instance.GetGameVisibility() == P2PGameVisibility.Singleplayer || ReplTools.AmIMaster()))
            {
                base.StartShake(power, speed, duration, shake_mul);
            }
            else
            {
                this.m_WantedShakePower = SteadyAimV2.Power * shake_mul;
                this.m_ShakeSpeed = SteadyAimV2.Speed;
                this.m_SetShakePowerDuration = SteadyAimV2.Duration;
            }
        }
    }
}
