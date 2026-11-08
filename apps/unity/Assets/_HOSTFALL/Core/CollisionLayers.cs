namespace Hostfall.Core
{
    public static class CollisionLayers
    {
        public const int Player = 8;
        public const int PlayerMask = 1 << Player;
        public const int WorldMask = ~PlayerMask;
    }
}
