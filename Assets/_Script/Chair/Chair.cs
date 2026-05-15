using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public class Chair : MonoBehaviour, IInteractable
{
    [Header("Điểm neo")]
    [Tooltip("Kéo 1 hoặc nhiều điểm vào đây. 1 điểm = ghế đơn, nhiều điểm = ghế dài")]
    public Transform[] sitPoints;

    [Tooltip("Chỗ nhân vật sẽ xuất hiện sau khi đứng lên")]
    public Transform exitPoint;

    [HideInInspector] public bool[] occupiedSeats; // Chức năng: Lưu trạng thái trống/đầy của từng ghế

    public LocalizedString interactText;

    private void Awake()
    {
        // Khởi tạo mảng ghi nhớ bằng đúng số lượng điểm ngồi m kéo vào
        if (sitPoints != null && sitPoints.Length > 0)
        {
            occupiedSeats = new bool[sitPoints.Length];
        }
    }

    public string GetInteractText()
    {
        PlayerInteraction playerInteract = Object.FindFirstObjectByType<PlayerInteraction>();
        if (playerInteract != null && playerInteract.isSitting) return "";

        if (!HasEmptySeat()) return ""; // Hết chỗ thì tắt UI chữ E

        if (interactText != null && !interactText.IsEmpty)
        {
            return interactText.GetLocalizedString();
        }
        return "[E] Ngồi nghỉ";
    }

    public void Interact()
    {
        int emptyIndex = GetRandomEmptySeatIndex();
        if (emptyIndex != -1)
        {
            PlayerInteraction playerInteract = Object.FindFirstObjectByType<PlayerInteraction>();
            if (playerInteract != null)
            {
                playerInteract.SitDown(this, emptyIndex); // Báo cho Player biết nó ngồi slot số mấy
            }
        }
    }

    // --- HỆ THỐNG KIỂM TRA CHỖ NGỒI ---
    public bool HasEmptySeat()
    {
        if (occupiedSeats == null) return false;
        for (int i = 0; i < occupiedSeats.Length; i++)
        {
            if (!occupiedSeats[i]) return true; // Còn 1 ghế trống là trả về true
        }
        return false;
    }

    public int GetRandomEmptySeatIndex()
    {
        if (occupiedSeats == null) return -1;

        List<int> emptyIndices = new List<int>();
        for (int i = 0; i < occupiedSeats.Length; i++)
        {
            if (!occupiedSeats[i]) emptyIndices.Add(i);
        }

        if (emptyIndices.Count > 0)
        {
            int randomIndex = Random.Range(0, emptyIndices.Count);
            return emptyIndices[randomIndex];
        }
        return -1;
    }

    // Chức năng: NPC dò ghế và chiếm chỗ
    public int NPCTryOccupy()
    {
        int index = GetRandomEmptySeatIndex();
        if (index != -1)
        {
            occupiedSeats[index] = true; // NPC tự động khóa cái slot này lại
            return index; // Trả về số thứ tự ghế cho NPC nhớ
        }
        return -1;
    }

    // Chức năng: Trả lại ghế
    public void LeaveChair(int index)
    {
        if (occupiedSeats != null && index >= 0 && index < occupiedSeats.Length)
        {
            occupiedSeats[index] = false;
        }
    }
}