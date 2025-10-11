using PurrNet;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] float _moveSpeed = 3f;
    [SerializeField] private InputActionReference _moveAction;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (_moveAction != null)
            _moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_moveAction != null)
            _moveAction.action.Disable();
    }

    private void FixedUpdate()
    {
        Vector2 input = _moveAction != null ? _moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        Vector3 move = new Vector3(input.x, 0f, input.y) * _moveSpeed;


        _rb.linearVelocity = new Vector3(move.x, _rb.linearVelocity.y, move.z);
    }
}

