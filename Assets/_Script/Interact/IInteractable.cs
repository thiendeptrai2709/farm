using UnityEngine;

public interface IInteractable
{
    // Hàm dùng để lấy câu chữ hiện lên màn hình (VD: "Bấm E để thu hoạch")
    string GetInteractText();

    // Hàm thực thi hành động khi bấm nút
    void Interact();
}