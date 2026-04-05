using UnityEngine;

public class LandingSoundTrigger : StateMachineBehaviour
{
    // Hàm này tự động chạy DUY NHẤT 1 LẦN ngay khi Animator nhảy vào cục "Land"
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Land");
        }
    }
}