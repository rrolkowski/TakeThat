using System.Collections;
using UnityEngine;

public class CardView : MonoBehaviour
{
    public CardId Card { get; private set; }

    [Header("Hover settings")]
    [SerializeField] private float hoverLift = 0.35f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private bool straightenOnHover = false;
    [SerializeField] private float hoverInSpeed = 10f;
    [SerializeField] private float hoverOutSpeed = 14f;

    [Header("Hover lock prfab")]
    [SerializeField] private GameObject hoverLockObject;

    Vector3 basePos;
    Quaternion baseRot;
    Vector3 baseScale;

    SpriteRenderer sr;
    Coroutine anim;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        if (hoverLockObject != null)
            hoverLockObject.SetActive(false);
    }

    public void Init(CardId card, Sprite sprite)
    {
        Card = card;
        sr.sprite = sprite;

        basePos = transform.localPosition;
        baseRot = transform.localRotation;

        if (anim != null) StopCoroutine(anim);
        anim = null;

        transform.localScale = baseScale;
        if (hoverLockObject != null) hoverLockObject.SetActive(false);
    }

    public void OnClicked()
    {
        if (GameSession.Instance == null) return;
        if (!GameSession.Instance.IsMyTurn()) return;

        if (PileThrowController.Instance != null)
        {
            PileThrowController.Instance.NotifyLocalClick(Card, transform);
        }

        if (Card.type == CardType.Number && LocalHandView.Instance != null)
        {
            int copies = LocalHandView.Instance.CountCopiesInHand(Card);
            if (copies >= 2)
            {
                GameSession.Instance.Server_RequestPlayMany(Card, copies);
                return;
            }
        }

        GameSession.Instance.Server_RequestPlay(Card);
    }

    public void OnHoverEnter()
    {
        if (hoverLockObject != null)
            hoverLockObject.SetActive(true);

        Vector3 liftDir = baseRot * Vector3.up;
        Vector3 targetPos = basePos + liftDir * hoverLift;
        Quaternion targetRot = straightenOnHover ? Quaternion.identity : baseRot;

        StartAnim(targetPos, targetRot, baseScale * hoverScale, hoverInSpeed);
    }

    public void OnHoverExit()
    {
        if (hoverLockObject != null)
            hoverLockObject.SetActive(false);

        StartAnim(basePos, baseRot, baseScale, hoverOutSpeed);
    }

    void StartAnim(Vector3 pos, Quaternion rot, Vector3 scale, float speed)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Animate(pos, rot, scale, speed));
    }

    IEnumerator Animate(Vector3 pos, Quaternion rot, Vector3 scale, float speed)
    {
        float t = 0f;

        Vector3 p0 = transform.localPosition;
        Quaternion r0 = transform.localRotation;
        Vector3 s0 = transform.localScale;

        while (t < 1f)
        {
            t += Time.deltaTime * speed;

            transform.localPosition = Vector3.Lerp(p0, pos, t);
            transform.localRotation = Quaternion.Slerp(r0, rot, t);
            transform.localScale = Vector3.Lerp(s0, scale, t);

            yield return null;
        }
    }

    public void ForceUnhover()
    {
        if (anim != null) StopCoroutine(anim);
        anim = null;

        if (hoverLockObject != null)
            hoverLockObject.SetActive(false);

        transform.localPosition = basePos;
        transform.localRotation = baseRot;
        transform.localScale = baseScale;
    }
}
