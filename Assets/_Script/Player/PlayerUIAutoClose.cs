using UnityEngine;

public class PlayerUIAutoClose : MonoBehaviour
{
    [Header("Cài đặt khoảng cách đóng UI")]
    public float closeRadius = 2.0f; // Bằng interactRadius + 0.5f của ông
    public Vector3 checkOffset = new Vector3(0, 1f, 0);

    private void Update()
    {
        Vector3 checkPosition = transform.position + checkOffset;

        // 1. RƯƠNG ĐỒ
        if (InventoryManager.Instance != null && InventoryManager.Instance.currentOpenChest != null)
        {
            Collider chestCollider = InventoryManager.Instance.currentOpenChest.GetComponent<Collider>();
            float distanceToChest = chestCollider != null ? Vector3.Distance(checkPosition, chestCollider.ClosestPoint(checkPosition))
                                                          : Vector3.Distance(checkPosition, InventoryManager.Instance.currentOpenChest.transform.position);

            if (distanceToChest > closeRadius) InventoryManager.Instance.CloseChest();
        }

        // 2. Ô ĐẤT TRỒNG TRỌT
        if (FarmPlotUIManager.Instance != null && FarmPlotUIManager.Instance.IsOpen())
        {
            Transform pTransform = FarmPlotUIManager.Instance.GetCurrentPlotTransform();
            Collider pCollider = FarmPlotUIManager.Instance.GetCurrentPlotCollider();

            if (pTransform != null)
            {
                float distanceToPlot = pCollider != null
                    ? Vector3.Distance(checkPosition, pCollider.ClosestPoint(checkPosition))
                    : Vector3.Distance(checkPosition, pTransform.position);

                if (distanceToPlot > closeRadius) FarmPlotUIManager.Instance.ClosePlotUI();
            }
        }

        // 3. CỬA HÀNG (SHOP)
        if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsOpen())
        {
            Transform mTransform = ShopUIManager.Instance.GetCurrentMerchantTransform();
            Collider mCollider = ShopUIManager.Instance.GetCurrentMerchantCollider();

            if (mTransform != null)
            {
                float distanceToShop = mCollider != null
                    ? Vector3.Distance(checkPosition, mCollider.ClosestPoint(checkPosition))
                    : Vector3.Distance(checkPosition, mTransform.position);

                if (distanceToShop > closeRadius) ShopUIManager.Instance.CloseShop();
            }
        }

        // 4. BÀN CHẾ TẠO (BUILDER)
        if (BuilderUIManager.Instance != null && BuilderUIManager.Instance.IsOpen())
        {
            Transform bTransform = BuilderUIManager.Instance.GetCurrentTableTransform();
            Collider bCollider = BuilderUIManager.Instance.GetCurrentTableCollider();

            if (bTransform != null)
            {
                float distanceToBuilder = bCollider != null
                    ? Vector3.Distance(checkPosition, bCollider.ClosestPoint(checkPosition))
                    : Vector3.Distance(checkPosition, bTransform.position);

                if (distanceToBuilder > closeRadius) BuilderUIManager.Instance.CloseUI();
            }
        }

        // 5. CÔNG TRÌNH ĐANG XÂY (SITE)
        if (SiteConstructionUIManager.Instance != null && SiteConstructionUIManager.Instance.IsOpen())
        {
            Transform sTransform = SiteConstructionUIManager.Instance.GetCurrentSiteTransform();
            Collider sCollider = SiteConstructionUIManager.Instance.GetCurrentSiteCollider();

            if (sTransform != null)
            {
                float distanceToSite = sCollider != null
                    ? Vector3.Distance(checkPosition, sCollider.ClosestPoint(checkPosition))
                    : Vector3.Distance(checkPosition, sTransform.position);

                if (distanceToSite > closeRadius) SiteConstructionUIManager.Instance.CloseUI();
            }
        }
    }
}