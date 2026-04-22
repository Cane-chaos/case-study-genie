using System;
using UnityEngine;

public static class EventManager
{
    // Sự kiện khi trạng thái Simulation thay đổi (Design -> Play)
    public static event Action<SimulationState> OnStateChanged;

    public static void TriggerStateChanged(SimulationState newState)
    {
        OnStateChanged?.Invoke(newState);
    }
}
