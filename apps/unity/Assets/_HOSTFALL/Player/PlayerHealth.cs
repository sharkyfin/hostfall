using System;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.UI;
using Hostfall.World;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float invulnerabilityDuration = 0.6f;

        private float invulnerabilityTimer;
        private ControlState controls;
        private FacilityUI ui;
        private DeathController deathController;

        public string DamageableId => "operator-chassis";
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float Health => currentHealth;
        public float HealthPercent => Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsDestroyed => currentHealth <= 0f;
        public bool IsStunned => false;

        public event Action<float, float> HealthChanged;
        public event Action<string> PlayerDied;

        public void Initialize(ControlState state, FacilityUI interfaceUI, DeathController death)
        {
            controls = state;
            ui = interfaceUI;
            deathController = death;
            currentHealth = maxHealth;
            invulnerabilityTimer = 0f;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, "Кинетический удар");
        }

        public void TakeDamage(float amount, string source)
        {
            if (currentHealth <= 0f || invulnerabilityTimer > 0f) return;

            invulnerabilityTimer = invulnerabilityDuration;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (ui != null)
            {
                ui.ShowActionNotice($"Целостность корпуса: {Mathf.CeilToInt(currentHealth)}%");
                ui.TriggerDamageFlash();
            }

            if (currentHealth <= 0f)
            {
                PlayerDied?.Invoke(source);
                if (deathController != null)
                {
                    deathController.TriggerDeath(source);
                }
            }
        }

        public void Heal(float amount)
        {
            if (currentHealth <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);
            if (ui != null)
            {
                ui.ShowActionNotice($"Ремонт корпуса: {Mathf.CeilToInt(currentHealth)}%");
            }
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            invulnerabilityTimer = 0f;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public PlayerHealthStateData Capture() => new() { present = true, current = currentHealth, invulnerability = Mathf.Max(0f, invulnerabilityTimer) };
        public void Restore(PlayerHealthStateData saved)
        {
            if (saved == null || !saved.present) return;
            currentHealth = Mathf.Clamp(saved.current, 0f, maxHealth);
            invulnerabilityTimer = Mathf.Clamp(saved.invulnerability, 0f, invulnerabilityDuration);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyEmpStun(float duration)
        {
            // Player chassis has built-in Faraday shielding; EMP causes brief sensor static
            if (ui != null)
            {
                ui.ShowActionNotice("ЭМИ-разряд · Сенсоры перегружены");
                ui.TriggerDamageFlash();
            }
        }

        private void Update()
        {
            if (invulnerabilityTimer > 0f)
            {
                invulnerabilityTimer -= Time.deltaTime;
            }
        }
    }
}
