using UnityEngine;

public enum SimulationState { MainMenu, Designer, Playing }

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;
    public SimulationState CurrentState = SimulationState.MainMenu;

    void Awake()
    {
        // Đảm bảo chỉ có 1 SimulationManager duy nhất trong Game
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void ChangeState(SimulationState newState)
    {
        CurrentState = newState;
        Debug.Log($"Trạng thái đã đổi sang: {newState}");
        
        // Gọi EventManager để báo cho UI biết mà cập nhật
        EventManager.TriggerStateChanged(newState);
    }
}
