using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurnDirectionUI : MonoBehaviour
{
    [SerializeField] private RectTransform spinner;
    [SerializeField] private float speed = 35f;

    private int direction = 1;

    void OnEnable()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.OnDirectionChanged += SetDirection;
            SetDirection(GameSession.Instance.Direction);
        }
    }

    void OnDisable()
    {
        if (GameSession.Instance != null)
            GameSession.Instance.OnDirectionChanged -= SetDirection;
    }

    void Update()
    {
        spinner.Rotate(0f, 0f, direction * speed * Time.deltaTime);
    }


    public void SetDirection(int serverDirection)
    {
        direction = serverDirection >= 0 ? 1 : -1;

        var s = spinner.localScale;
        s.x = Mathf.Abs(s.x) * (direction == 1 ? 1f : -1f);
        spinner.localScale = s;
    }
}
