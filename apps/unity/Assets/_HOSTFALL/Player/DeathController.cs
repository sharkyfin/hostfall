using System;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Simulation;
using Hostfall.UI;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class DeathController : MonoBehaviour
    {
        private ControlState controls;
        private FacilityUI ui;
        private FacilitySimulation simulation;
        private WorldSaveController persistence;
        private PlayerMotor motor;

        private bool isDead;
        private float deathTime;
        private string deathCause = "Неизвестно";

        public bool IsDead => isDead;

        public void Initialize(ControlState state, FacilityUI interfaceUI, FacilitySimulation sim, WorldSaveController saveCtrl, PlayerMotor playerMotor)
        {
            controls = state;
            ui = interfaceUI;
            simulation = sim;
            persistence = saveCtrl;
            motor = playerMotor;
            isDead = false;
        }

        public void TriggerDeath(string cause)
        {
            if (isDead) return;
            isDead = true;
            deathCause = string.IsNullOrEmpty(cause) ? "Критическое разрушение корпуса" : cause;
            deathTime = Time.timeSinceLevelLoad;

            // Pause exploration controls
            controls?.SetMode(ControlMode.Paused);

            // Display Death screen
            if (ui != null)
            {
                ui.ShowDeathScreen(deathCause, deathTime);
            }
        }

        public void RestartRaid()
        {
            isDead = false;
            // Clean raid reset from scratch
            if (persistence != null)
            {
                string lang = persistence.Loaded ? "python" : "python";
                persistence.NewGame(lang);
            }
        }
    }
}

