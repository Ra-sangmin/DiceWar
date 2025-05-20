using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;

public class IAPManager : MonoSingleton<IAPManager>, IStoreListener
{
    [Header("Product ID")]
    public readonly string productId_test_id = "coin_add_10";
    public readonly string productId_test_id2 = "coin_add_30";

    [Header("Cache")]
    private IStoreController storeController; //구매 과정을 제어하는 함수 제공자
    private IExtensionProvider storeExtensionProvider; //여러 플랫폼을 위한 확장 처리 제공자

    public bool initOn = false;

    public UnityAction<PurchaseEventArgs> processPurchaseOn = data => { };

    private void Start()
    {
        //InitUnityIAP(); //Start 문에서 초기화 필수
    }

    /* Unity IAP를 초기화하는 함수 */
    public void InitUnityIAP()
    {
        initOn = true;

        ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        /* 구글 플레이 상품들 추가 */
        builder.AddProduct(productId_test_id, ProductType.Consumable, new IDs() { { productId_test_id, GooglePlay.Name } });
        builder.AddProduct(productId_test_id2, ProductType.Consumable, new IDs() { { productId_test_id2, GooglePlay.Name } });

        UnityPurchasing.Initialize(this, builder);
    }

    /* 구매하는 함수 */
    public void Purchase(string productId)
    {
        Product product = storeController.products.WithID(productId); //상품 정의

        if (product != null && product.availableToPurchase) //상품이 존재하면서 구매 가능하면
        {
            storeController.InitiatePurchase(product); //구매가 가능하면 진행
        }
        else //상품이 존재하지 않거나 구매 불가능하면
        {
            Debug.Log("상품이 없거나 현재 구매가 불가능합니다");
        }
    }

    #region Interface
    /* 초기화 성공 시 실행되는 함수 */
    public void OnInitialized(IStoreController controller, IExtensionProvider extension)
    {
        Debug.Log("초기화에 성공했습니다");

        storeController = controller;
        storeExtensionProvider = extension;
    }

    /* 초기화 실패 시 실행되는 함수 */
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("초기화에 실패했습니다");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("초기화에 실패했습니다");
    }

    /* 구매에 실패했을 때 실행되는 함수 */
    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.Log("구매에 실패했습니다");
    }

    /* 구매를 처리하는 함수 */
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log("구매에 성공했습니다");

        //int addCoin = 0;

        //if (args.purchasedProduct.definition.id == productId_test_id)
        //{
        //    addCoin = 10;
        //    /* test_id 구매 처리 */
        //}
        //else if (args.purchasedProduct.definition.id == productId_test_id2)
        //{
        //    addCoin = 30;
        //    /* test_id2 구매 처리 */
        //}

        //DataManager.Instance.AddCoin(addCoin);

        return PurchaseProcessingResult.Complete;
    }

    #endregion
}
