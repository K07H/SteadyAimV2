using UnityEngine;

namespace SteadyAimV2
{
    public class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject("__SteadyAimV2Mod__").AddComponent<SteadyAimV2>();
        }
    }
}
