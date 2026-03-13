using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;
using System;

public class UserDataRequest : BaseRequest
{
    public string email = string.Empty;
    public int snsType = 0;
	public int freeCoin = 0;
	public int chargeCoin = 0;

	public async override UniTask RequestOn()
    {
        requestType = RequestType.POST;

        if (requestStatus == 0)
        {
            classValue = "getUserData";

            form.AddField("email", email);
            form.AddField("snsType", snsType);
        }
        else if (requestStatus == 1)
        {
            classValue = "updateCoinData";

            form.AddField("email", email);
			form.AddField("freeCoin", freeCoin);
			form.AddField("chargeCoin", chargeCoin);
		}

        await base.RequestOn();
    }

    public override void ResponsOn(string jsonData)
    {
        BaseRespons data = null;

        //Debug.LogWarning(jsonData);

        data = GetData<UserDataRespons>(jsonData);

        if (successOn != null)
        {
            successOn(data);
        }
    }
}

public class UserDataRespons : BaseRespons
{
    public string email = string.Empty;
    public int snsType = 0;
	public int freeCoin = 0;
	public int chargeCoin = 0;
}

public class UserData
{
	public string email = string.Empty;
	public int snsType = 0;
	public int freeCoin = 0;
	public int chargeCoin = 0;

	public ReactiveProperty<int> myCoin = new ReactiveProperty<int>(0);

	public UserData() { }
	public UserData(string email, int snsType, int freeCoin, int chargeCoin)
	{
		this.email = email;
		this.snsType = snsType;
		this.freeCoin = freeCoin;
		this.chargeCoin = chargeCoin;

		SetMyCoin();
	}

	public void AddCoin(int addFreeCoin , int addChargeCoin)
	{
		if (addFreeCoin < 0 )
		{
			int absFreeCoinValue = Mathf.Abs(addFreeCoin);

			if (freeCoin < absFreeCoinValue)
			{
				chargeCoin -= (absFreeCoinValue - freeCoin);
			}

			freeCoin = Math.Max(freeCoin + addFreeCoin, 0);
		}
		else 
		{
			freeCoin = freeCoin + addFreeCoin;
		}

		chargeCoin += addChargeCoin;

		SetMyCoin();
	}

	private void SetMyCoin()
	{
		myCoin.Value = freeCoin + chargeCoin;
	}
}