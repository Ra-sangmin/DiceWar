using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static bool _isQuitting = false;
	private static T _instance = null;

	public static T Instance
	{
		get
		{
			if (_isQuitting)
			{
				return null;
			}

			if (_instance == null)
			{
				_instance = Object.FindFirstObjectByType<T>();

				if (_instance == null)
				{
					// [수정] 가독성을 위해 Name을 클래스명으로 설정
					_instance = new GameObject(typeof(T).Name).AddComponent<T>();
					DontDestroyOnLoad(_instance.gameObject);
				}
			}

			return _instance;
		}
	}

	protected virtual void Awake()
	{
		// 인스턴스가 이미 있는데 다른 오브젝트가 또 생성되었다면 파괴 (중복 방지)
		if (_instance != null && _instance != this)
		{
			Destroy(this.gameObject);
			return;
		}

		_instance = this.GetComponent<T>();

		// 부모가 없을 때만 DontDestroyOnLoad 가능
		if (transform.parent == null)
		{
			DontDestroyOnLoad(this.gameObject);
		}

		Init();
	}

	public virtual void Init() { }

	// [추가] 앱이 종료될 때 플래그를 true로 설정합니다.
	protected virtual void OnApplicationQuit()
	{
		_isQuitting = true;
	}

	// [추가] 오브젝트가 파괴될 때 참조를 비워줍니다.
	protected virtual void OnDestroy()
	{
		// _instance = null; // 필요에 따라 해제 (일반적으로 종료 시점엔 _isQuitting으로 충분함)
	}
}