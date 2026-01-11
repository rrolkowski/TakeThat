using UnityEngine;
using UnityEngine.InputSystem;

public class Debug_start : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.Server_StartGame();
                Debug.Log("Game Started!");
            }
        }
          
    }
}
