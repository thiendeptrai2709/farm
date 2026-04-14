using UnityEngine;

public class BusStop : MonoBehaviour, IInteractable
{
    public BusVehicle myBus;
    public string GetInteractText()
    {
        return "[E] Xem lịch trình Xe Bus";
    }

    public void Interact()
    {
        if (BusUI.Instance != null)
        {
            BusUI.Instance.OpenUI(this.transform);
        }
    }
}