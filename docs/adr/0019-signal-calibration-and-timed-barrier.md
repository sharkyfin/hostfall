# 0019 — Separate oscillator and equipment clocks

SIGNAL SYNC needs an interactive hold test and a physical reaction that continues when the player leaves the screen. A single timer would either advance the puzzle unattended or freeze the timed passage.

The oscillator, partial hold and calibration window advance while the app has focus. Heat, recharge and the 20-second passage window use world time. Circuit ventilation affects cooling. The north actuator supplies an eight-second return after initial calibration. The physical portal holds an existing opening while the player's bounds overlap it. Scene load reapplies the saved timers and checks the restored player pose.

The campaign saves both models and validates earned calibration receipts. Repeated final tuning opens another window. A single completion receipt publishes the archive entry. Reusing the existing gate and original guard keeps authored world and enemy IDs stable.
