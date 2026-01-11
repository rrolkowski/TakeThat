using UnityEngine;

public class HandFan_Debug : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform root;
    [SerializeField] private Sprite[] testSprites;

    [SerializeField] private float radius = 3.0f;
    [SerializeField] private float angleRange = 35f;

    private void Start()
    {
        SpawnTestHand(7);
    }

    private void SpawnTestHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float deg = Mathf.Lerp(-angleRange * 0.5f, angleRange * 0.5f, t);
            float rad = deg * Mathf.Deg2Rad;

            var pos = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius, 0f);
            var rot = Quaternion.Euler(0f, 0f, -deg);

            var c = Instantiate(cardPrefab, root);
            c.transform.localPosition = pos;
            c.transform.localRotation = rot;

            var sprite = testSprites.Length > 0 ? testSprites[i % testSprites.Length] : null;
            c.Init(i, sprite);
        }
    }
}
