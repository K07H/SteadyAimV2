using UnityEngine;

namespace SteadyAimV2
{
    public class PlayerExt : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject("__SteadyAimV2Mod__").AddComponent<SteadyAimV2>();
        }

        public override void StartShake(float power, float speed, float duration, float shake_mul)
        {
            if (SteadyAimV2.SteadyAimV2Enabled && ((P2PSession.Instance.GetGameVisibility() == P2PGameVisibility.Singleplayer || ReplTools.AmIMaster()) || (SteadyAimV2.PermissionGranted && !SteadyAimV2.PermissionDenied)))
            {
                this.m_WantedShakePower = SteadyAimV2.Power * shake_mul;
                this.m_ShakeSpeed = SteadyAimV2.Speed;
                this.m_SetShakePowerDuration = SteadyAimV2.Duration;
            }
            else
                base.StartShake(power, speed, duration, shake_mul);
        }
    }
}
