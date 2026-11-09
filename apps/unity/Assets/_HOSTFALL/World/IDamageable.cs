namespace Hostfall.World
{
    public interface IDamageable
    {
        string DamageableId { get; }
        bool IsDestroyed { get; }
        float Health { get; }
        void TakeDamage(float amount);
        void ApplyEmpStun(float duration = 6f);
    }
}

