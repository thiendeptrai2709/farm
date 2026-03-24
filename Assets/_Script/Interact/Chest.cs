using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chest : MonoBehaviour, IInteractable
{
    [Header("Cài đặt Rương")]
    public int chestSize = 16;
    public List<InventorySlot> chestSlots = new List<InventorySlot>();
    public List<InventorySlot> startingItems = new List<InventorySlot>();
    [Header("Hiệu ứng nắp rương")]
    public Transform lidTransform;         // Kéo cái object NẮP RƯƠNG vào đây
    public float openAngle = -90f;         // Góc xoay mở nắp (Thường là -90 hoặc 90 độ)
    public float animationDuration = 0.4f; // Mở trong 0.4 giây cho chân thực

    private bool isOpen = false;
    private Coroutine animationCoroutine;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        // LÀM SẠCH VÀ NẠP ĐỒ TỪ LÚC KHỞI CHẠY
        chestSlots.Clear();

        for (int i = 0; i < chestSize; i++)
        {
            // SỬA Ở ĐÂY: Truyền thẳng biến vào trong ngoặc tròn ()
            if (i < startingItems.Count && startingItems[i].item != null)
            {
                chestSlots.Add(new InventorySlot(startingItems[i].item, startingItems[i].amount));
            }
            else
            {
                // SỬA Ở ĐÂY: Truyền null và 0 cho các ô trống
                chestSlots.Add(new InventorySlot(null, 0));
            }
        }

        // Lưu lại góc ban đầu của nắp
        if (lidTransform != null)
        {
            closedRotation = lidTransform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(openAngle, 0, 0);
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RegisterChest(this);
        }
    }
    private void OnDestroy()
    {
        // Xóa tên khỏi sổ đăng ký để Manager không bị lỗi Null khi tìm đồ
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.UnregisterChest(this);
        }
    }
    public string GetInteractText()
    {
        // Đang mở rồi thì giấu chữ [E] đi cho đỡ rối mắt
        return isOpen ? "" : "[E] Open Chest";
    }

    public void Interact()
    {
        if (isOpen) return; // Nếu đang mở rồi thì không làm gì cả

        isOpen = true;
        InventoryManager.Instance.OpenChest(this);

        // Kích hoạt hiệu ứng từ từ mở nắp
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateLid(openRotation));
    }

    // Hàm này để InventoryManager gọi khi đóng rương (Bấm TAB hoặc đi ra xa)
    public void CloseChestVisuals()
    {
        isOpen = false;

        // Kích hoạt hiệu ứng từ từ đóng nắp
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateLid(closedRotation));
    }

    // Logic Toán học giúp nắp xoay mượt mà không bị giật cục
    private IEnumerator AnimateLid(Quaternion targetRotation)
    {
        if (lidTransform == null) yield break;

        Quaternion startRotation = lidTransform.localRotation;
        float timeElapsed = 0f;

        while (timeElapsed < animationDuration)
        {
            // Lerp là hàm làm mượt, xoay từ từ A sang B
            lidTransform.localRotation = Quaternion.Lerp(startRotation, targetRotation, timeElapsed / animationDuration);
            timeElapsed += Time.deltaTime;
            yield return null; // Chờ frame tiếp theo
        }
        lidTransform.localRotation = targetRotation; // Ép vào góc chuẩn cuối cùng
    }
}