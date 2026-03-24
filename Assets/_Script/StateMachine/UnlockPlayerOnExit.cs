using UnityEngine;

public class UnlockPlayerOnExit : StateMachineBehaviour
{
    // Bỏ qua OnStateEnter và OnStateUpdate luôn.
    // Chỉ dùng OnStateExit: Chạy đúng 100% xong thoát State thì mới mở khóa.

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.isActionLocked = false;
        }
    }
}