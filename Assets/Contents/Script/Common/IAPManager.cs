using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoSingleton<IAPManager>, IDetailedStoreListener
{
    [Header("Product ID")]
    private const string coin_add_10 = "coin_add_10";
    private const string coin_add_30 = "coin_add_30";

    [Header("Cache")]
    private IStoreController storeController; //구매 과정을 제어하는 함수 제공자
    private IExtensionProvider extensionProvider; //여러 플랫폼을 위한 확장 처리 제공자

    public bool initOn = false;

    public UnityAction<PurchaseEventArgs> processPurchaseOn = data => { };

    private void Start()
    {
        InitUnityIAP(); //Start 문에서 초기화 필수
    }

    /* Unity IAP를 초기화하는 함수 */
    public void InitUnityIAP()
    {
		if (IsInitialized())
			return;

		ConfigurationBuilder builder = null;
		
#if UNITY_IOS
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance(AppStore.AppleAppStore));
#elif UNITY_ANDROID
		var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
#endif

	    if (builder != null)
	    {
		    builder.AddProduct(coin_add_10, ProductType.Consumable);
		    builder.AddProduct(coin_add_30, ProductType.Consumable);

		    UnityPurchasing.Initialize(this, builder);    
	    }
	}

	private bool IsInitialized()
	{
		return storeController != null && extensionProvider != null;
	}

    #region Interface
    /* 초기화 성공 시 실행되는 함수 */
    public void OnInitialized(IStoreController controller, IExtensionProvider extension)
    {
        Debug.Log("초기화에 성공했습니다");

        storeController = controller;
		extensionProvider = extension;
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

	public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
	{
		Debug.Log("구매에 실패했습니다");
	}

	#endregion
}
