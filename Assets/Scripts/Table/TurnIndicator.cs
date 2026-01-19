using UnityEngine;

public class TurnIndicator : MonoBehaviour
{
    public static TurnIndicator Instance { get; private set; }

    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);

    private Transform target;

    private void Awake()
    {
        Instance = this;
    }

    public void SetTarget(Transform t)
    {
        target = t;
        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}
