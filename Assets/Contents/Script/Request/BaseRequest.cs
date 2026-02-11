using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// 정보 요청 최상위 클래스
/// </summary>
public class BaseRequest
{
	//protected string baseUri = "ec2-52-78-148-28.ap-northeast-2.compute.amazonaws.com:8002/";
	//protected string baseUri = "www.dicendeal.com:8002/";
	protected string baseUri = "ec2-52-78-148-28.ap-northeast-2.compute.amazonaws.com:8002/";
	//protected string baseUri = "https://ec2-52-78-148-28.ap-northeast-2.compute.amazonaws.com:8002/";
	//protected string baseUri = "dicendeal.com:8002/";
	//protected string baseUri = "52.78.148.28:8002/";
	protected string classValue = string.Empty;
    protected Dictionary<string, object> getValueDic = new Dictionary<string, object>();
    protected Dictionary<string, object> headerDic = new Dictionary<string, object>();
    protected string getValue = string.Empty;

    public WWWForm form = new WWWForm();
    public enum RequestType
    {
        POST,
        Get,
        Patch,
        Delete
    }
    public RequestType requestType = RequestType.Get;

    public UnityAction<BaseRespons> successOn = data=> { };
    public int requestStatus = 0;

    public BaseRequest()
    {

    }

    public void SetgetValueStr()
    {
        getValue = "";

        int index = 0;

        string currentStrValue = "";

        foreach (var dicValue in getValueDic)
        {
            if (index == 0)
            {
                currentStrValue += "?";
            }
            else 
            {
                currentStrValue += "&";
            }

            currentStrValue += string.Format("{0}={1}", dicValue.Key, dicValue.Value);

            index++;
        }

        getValue = currentStrValue;
    }

    public string GetUri()
    {
        return string.Format("{0}{1}{2}", baseUri, classValue, getValue);
    }

    public async virtual UniTask RequestOn()
    {
        if (NetworkCheck() == false)
        {
            Debug.LogWarning("인터넷 연결을 확인해주세요");
            return;
        }

        await Request();
    }

    bool NetworkCheck()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
        //return false;
    }

    async UniTask Request()
    {
        try
        {
            var result = await GetTextAsync();
            ResponsOn(result);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// UnityWebRequest를 async/await 에서 대기
    /// </summary>
    private async UniTask<string> GetTextAsync()
    {
        var uwr = requestType == RequestType.Get ? UnityWebRequest.Get(GetUri()) :
                                                UnityWebRequest.Post(GetUri(), form);

        if (requestType == RequestType.Patch)
        {
            uwr.method = "PATCH";
        }
        else if (requestType == RequestType.Delete)
        {
            uwr.method = "DELETE";
        }

        foreach (var header in headerDic)
        {
            uwr.SetRequestHeader(header.Key, (string)header.Value);
        }

        // SendWebRequest가 끝날 때까지 await 
        await uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            // 실패시 예외 throw
            //throw new Exception(uwr.error + " uri = "+GetUri());
        }

        return uwr.downloadHandler.text;
    }

    public T GetData<T>(string jsonData)
    {
        T data = default;

        try
        {
            data = JsonUtility.FromJson<T>(jsonData);
        }
        catch (Exception)
        {
            Debug.LogWarning("error = "+jsonData);
            //throw;
        }
        

        return data;
    }

    public T GetListData<T>(string jsonData)
    {
        T data = default;

        try
        {
            data = JsonConvert.DeserializeObject<T>(jsonData.ToString());
        }
        catch (Exception)
        {
            Debug.LogWarning("error = " + jsonData);
            throw;
        }


        return data;
    }

    public virtual void ResponsOn(string jsonData)
    {

    }
}

public class BaseRespons
{
    public bool success = false;
    public int id = 0;
    public string created_at = string.Empty;
    public string updated_at = string.Empty;
    public string deleted_at = string.Empty;
    public string message;
}

public class CommonRespons : BaseRespons
{
    public string resultStr;
    public int resultInt;
    public bool resultBool;
}

