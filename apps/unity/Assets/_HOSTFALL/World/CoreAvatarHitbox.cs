using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class CoreAvatarHitbox : MonoBehaviour
    {
        [SerializeField] private CoreAvatarEncounter encounter;
        [SerializeField] private CoreAvatarPart part;

        public void Configure(CoreAvatarEncounter owner, CoreAvatarPart component)
        {
            encounter = owner;
            part = component;
        }

        public void Hit(float amount) => encounter.Hit(amount, part);
    }
}
