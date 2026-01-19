using TMPro;
using UnityEngine;

public class PlayerEffectIndicator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text plusText;

    [Header("Animator triggers")]
    [SerializeField] private string triggerShowPlus = "ShowPlus";
    [SerializeField] private string triggerShowBlock = "ShowBlock";

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        plusText = GetComponentInChildren<TMP_Text>();
    }

    public void ShowBlock()
    {
        if (animator != null)
            animator.SetTrigger(triggerShowBlock);
    }

    public void ShowPlus(int value)
    {
        if (plusText != null)
            plusText.text = $"+{value}";

        if (animator != null)
            animator.SetTrigger(triggerShowPlus);
    }
}
