using UnityEngine;

namespace Hostfall.World
{
    public sealed class MinibossComponent : MonoBehaviour
    {
        [SerializeField] private FacilityMiniboss boss;
        public void Configure(FacilityMiniboss owner) => boss = owner;
        public void Hit(float damage) => boss.Hit(damage, true);
    }
}
