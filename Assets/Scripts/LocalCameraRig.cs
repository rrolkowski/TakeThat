using PurrNet;
using Unity.Cinemachine;
using UnityEngine;

public class LocalCameraRig : MonoBehaviour
{
    [SerializeField] private Transform[] camPoints;
    [SerializeField] private CinemachineCamera vcam;

    private void LateUpdate()
    {
        if (!PlayerAvatar.TryGetLocal(out var local)) return;

        int seat = local.SeatIndex;
        if (seat < 0 || seat >= camPoints.Length) return;

        var p = camPoints[seat];
        if (p == null) return;

        vcam.transform.SetPositionAndRotation(p.position, p.rotation);

        enabled = false;
    }
}

