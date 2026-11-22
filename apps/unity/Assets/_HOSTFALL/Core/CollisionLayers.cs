namespace Hostfall.Core
{
    public static class CollisionLayers
    {
        public const int Player = 8;
        public const int PlayerMask = 1 << Player;
        public const int RobotHitbox = 9;
        public const int WeaponItem=10;
        public const int WorldMask = ~(PlayerMask | (1 << RobotHitbox) | (1 << WeaponItem));
        public const int InteractionMask=WorldMask|(1<<WeaponItem);
        public const int ShotMask = ~PlayerMask;
    }
}
