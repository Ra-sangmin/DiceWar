using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Threading;
using static Unity.VisualScripting.Member;
using UnityEngine.Assertions.Must;
using static GameResultPopup;

public class InGameControllerBase : MonoBehaviour
{
	public MapController mapController;
	public Button endTurnBtn;

	protected bool gameEndOn = false;

	protected CancellationTokenSource source = new CancellationTokenSource();

	protected virtual void Awake()
	{
		SetEvent();
	}

	protected virtual void SetEvent()
	{
		mapController.gameWinOn = GameWinOn;
		mapController.gameLoseOn = GameLoseOn;
		//mapController.turnOffOn = TurnOffOn;
		mapController.selectAreaOn = SelectAreaOn;
		mapController.SetEvent();
	}

	protected virtual void Start()
	{
		SoundManager.Instance.PlayBGM(BGMEnum.Game);

		PopupManager.Instance.SetCanvasParant(transform);

		mapController.CreateMapInit();

		endTurnBtn.interactable = false;

		if (DataManager.Instance.mapSelectionOn == false)
		{
			GameStartOn();
		}
	}

	void GameWinOn()
	{
		GameEndOn(GameResultEnum.Win);
	}

	void GameLoseOn()
	{
		GameEndOn(GameResultEnum.Lose);
	}

	void GameEndOn(GameResultEnum gameResultEnum)
	{
		if (gameEndOn)
			return;

		gameEndOn = true;

		DataManager.Instance.gameStart.SetValueAndForceNotify(false);

		TokenSourceInit();

		PopupManager.Instance.GameResultPopupOn(this, gameResultEnum, GetCoin(gameResultEnum));
	}

	public void TokenSourceInit()
	{
		source.Cancel();
		source = new CancellationTokenSource();
	}

	protected virtual int GetCoin(GameResultEnum gameResultEnum) { return 0; }

	protected virtual void GameStartOn()
	{
		gameEndOn = false;

		DataManager.Instance.gameStart.SetValueAndForceNotify(true);

		mapController.inGameBottomController.SetStatus(1);

		TurnCheck();

		DataManager.Instance.leaveEarlyPopupReadyOn = true;
	}

	public void GoMainOn()
	{
		DataManager.Instance.GameDataClearOn();

		if (DataManager.Instance.isMultiOn)
		{
			ServerManager.Instance.GameOutRequestOn();
		}

		SceneManager.LoadScene("Main");
	}

	public void SelectAreaOn(AreaData areaData)
	{
		//PlayerEnum currentPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

		////≥ª ∂•¿ª º±≈√«ﬂ¿ª∂ß
		//if (areaData.player == currentPlayerEnum) 
		//{
		//    Debug.LogWarning("≥ª ∂•");
		//}
		//else 
		//{
		//    Debug.LogWarning("≥≤¿« ∂•");
		//}
	}

	protected virtual void Update()
	{
		//if (DataManager.Instance.isMultiOn == false)
		//{
		//TurnCheckDelayCheck();
		//}   

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			//PopupManager.Instance.LandTradeApprovePopupOn();

			//yourTurnPopup.MyTurnActiveOn();

			//LeaveEarlyPopupOn();
		}

		//Debug.LogWarning("enum = "+DataManager.Instance.playerData.playerEnum);
	}

	void TurnCheckDelayCheck()
	{
		if (DataManager.Instance.gameStart.Value == false || DataManager.Instance.playOn)
		{
			return;
		}

		TurnCheck();
	}


	protected virtual async void TurnCheck()
	{
		DataManager.Instance.playOn = true;

		mapController.playerIconController.SetIconTurnEffect();

		bool myTurn = DataManager.Instance.IsMyTurn();

		if (myTurn)
		{
			PopupManager.Instance.YourTurnPopupOn();

			mapController.SelectClearOn();

			MyTurnPlayOn();
		}
		else
		{
			await OtherTurnPlayOn();
		}

		//SetEndTurnBtn();

		
	}

	protected virtual void MyTurnPlayOn(){}

	protected virtual async UniTask OtherTurnPlayOn() 
	{
		//SetEndTurnBtn();
		mapController.playerIconController.SetIconTurnEffect();

		PlayerEnum currentPlayer = DataManager.Instance.currentPlayer;
		PlayerIcon playerIcon = mapController.playerIconController.GetPlayerIcon(currentPlayer);

		if (playerIcon != null)
		{
			try
			{
				await AIPlayOn(currentPlayer, source);
			}
			catch (OperationCanceledException)
			{
				Debug.LogWarning("cancleOn");
			}
			
		}
		else
		{
			OtherPlayerNotOn();
		}
	}

	protected virtual void OtherPlayerNotOn(){}

	protected PlayerEnum GetNextPlayer()
	{
		PlayerEnum currentPlayer = DataManager.Instance.currentPlayer;

		PlayerEnum nextPlayer = currentPlayer;

		while (true)
		{
			nextPlayer = (PlayerEnum)(((int)nextPlayer) + 1);

			if ((int)nextPlayer == DataManager.Instance.num_player)
			{
				nextPlayer = PlayerEnum.Player_0;
			}

			PlayerIcon playerIcon = mapController.playerIconController.GetPlayerIcon(nextPlayer);

			if (playerIcon != null)
			{
				break;
			}
		}
		return nextPlayer;
	}

	protected virtual async UniTask AIPlayOn(PlayerEnum currentPlayer , CancellationTokenSource source)
	{
		await UniTask.Delay(100, cancellationToken: source.Token);
	}

	public void SetEndTurnBtn()
	{
		bool gameStart = DataManager.Instance.gameStart.Value;

		endTurnBtn.interactable = gameStart && DataManager.Instance.IsMyTurn();

		//Debug.LogWarning(DataManager.Instance.IsMyTurn());

	}

	public void EndTurnBtnClickOn()
	{
		EndTurnEventOn();
	}

	protected virtual void EndTurnEventOn()
	{
		endTurnBtn.interactable = false;

		mapController.SelectClearOn();
		TokenSourceInit();
	}

	public void TurnOffOn()
	{
		DataManager.Instance.TurnOffOn();
		mapController.playerIconController.SetIconTurnEffect();
		TurnCheck();
	}

	public void NewGameCheckOn()
	{
		if (DataManager.Instance.CheckNewGame())
		{
			NewGameOn();
		}
	}

	public void NewGameOn()
	{
		DataManager.Instance.gameStart.SetValueAndForceNotify(false);
		DataManager.Instance.playOn = false;
		DataManager.Instance.AddCoin(-DataManager.Instance.GetNeedCoin());

		if (DataManager.Instance.isMultiOn)
		{
			ServerManager.Instance.GameOutRequestOn();
			SceneManager.LoadScene("Loading");
		}
		else
		{
			mapController.NewGameOn();

			if (DataManager.Instance.mapSelectionOn == false)
			{
				GameStartOn();
			}
		}
	}
	public void ReStartOn()
	{
		RestartOnPlay().Forget();
	}

	public async UniTask RestartOnPlay() 
	{
		TokenSourceInit();

		await UniTask.Delay(100);

		mapController.ReStartOn();

		DataManager.Instance.playOn = false;

		GameStartOn();
	}

	public void OptionPopupOn()
	{
		PopupManager.Instance.OptionPopupOn(this);
	}

	public void LeaveEarlyPopupOn()
	{
		PopupManager.Instance.LeaveEarlyPopupOn(this , GetCoin(GameResultEnum.LeaveEarly));
	}

	public void LeaveEarlyFinishOn(bool finishOn)
	{
		DataManager.Instance.leaveEarlyPopupOpenOn = false;

		if (finishOn)
		{
			GameEndOn(GameResultEnum.LeaveEarly);
		}
	}

	public void TutorialPopupOn()
	{
		PopupManager.Instance.TutorialPopupOn();
	}
}