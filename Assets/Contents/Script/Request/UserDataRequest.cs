using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class UserDataRequest : BaseRequest
{
    public string email = string.Empty;
    public int snsType = 0;
    public int coin = 0;

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
            form.AddField("coin", coin);
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
    public int coin = 0;
}