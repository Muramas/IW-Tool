using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadingCompare : MonoBehaviour
{
	[SerializeField]
	private GameObject body;

	[SerializeField]
	private SaveLoadInfo cloud;

	[SerializeField]
	private SaveLoadInfo local;

	[SerializeField]
	private Image progress;

	[SerializeField]
	private TextMeshProUGUI header;

	[SerializeField]
	private TextMeshProUGUI theSameLabel;

	[SerializeField]
	private DisableCanvas canvas;

	[SerializeField]
	private float cloudProgress;

	private float localProgress;

	private bool isNotEmpty;

	private bool cloudLoaded;

	private bool localLoaded;

	private bool cloudStarted;

	private bool localStarted;

	public void On()
	{
		canvas.On();
		Wait(StartLoading);
		cloudProgress = 0f;
		localProgress = 0f;
		cloudLoaded = false;
		localLoaded = false;
		cloudStarted = false;
		localStarted = false;
		theSameLabel.text = string.Empty;
		Settings.BlockInput = true;
		Time.timeScale = 0f;
		base.gameObject.SetActive(value: true);
	}

	public void Off()
	{
		canvas.Off();
		GameManager.Instance.EnableAutoSave();
		Settings.BlockInput = false;
		if (Time.timeScale == 0f)
		{
			Time.timeScale = 1f;
		}
		base.gameObject.SetActive(value: false);
	}

	private void StartLoading()
	{
		if (GameManager.Instance.GetUserId() == "0" || string.IsNullOrEmpty(GameManager.Instance.GetUserId()))
		{
			string localSave = GameManager.Instance.SaveData.GetLocalSave();
			SaveData saveData = GameManager.Instance.SaveData.GetSaveData(localSave);
			if (saveData != null)
			{
				Load(saveData);
			}
			else
			{
				GameManager.Instance.LoadEmpty();
			}
			Off();
		}
		else
		{
			Debug.Log(GameManager.Instance.CustomCloud);
			StartCloudLoading();
			StartLocalLoading();
		}
	}

	private void StartCloudLoading()
	{
		if (!cloudStarted)
		{
			cloudStarted = true;
			Debug.Log("Starting cloud save loading...");
			GameManager.Instance.CustomCloud.GetUserData(OnLoadCloud, OnErrorCloud);
		}
	}

	private void StartLocalLoading()
	{
		if (!localStarted)
		{
			localStarted = true;
			Debug.Log("Starting local save loading...");
			Wait(GetLocal);
		}
	}

	private void Update()
	{
		progress.fillAmount = (cloudProgress + localProgress) / 2f;
		if (cloudLoaded && localLoaded)
		{
			CheckComparison();
		}
	}

	private void OnLoadCloud(string msg)
	{
		cloudProgress = 0.5f;
		if (string.IsNullOrEmpty(msg))
		{
			OnErrorCloud("empty save");
			return;
		}
		Wait(delegate
		{
			GetCloud(GameManager.Instance.CustomCloud.ConvertData(msg));
		});
	}

	private void GetCloud(string msg)
	{
		SaveData saveData = GameManager.Instance.SaveData.GetSaveData(msg);
		if (saveData == null)
		{
			OnErrorCloud(msg);
			return;
		}
		cloud.Open(saveData, msg);
		isNotEmpty = true;
		cloudLoaded = true;
		cloudProgress = 1f;
		Debug.Log("Cloud save loaded successfully");
		CheckComparisonStatus();
	}

	private void OnErrorCloud(string msg)
	{
		cloudLoaded = true;
		cloudProgress = 1f;
		Debug.Log("Cloud save loading failed: " + msg);
		CheckComparisonStatus();
	}

	private void GetLocal()
	{
		localProgress = 0.5f;
		string localSave = GameManager.Instance.SaveData.GetLocalSave();
		SaveData saveData = GameManager.Instance.SaveData.GetSaveData(localSave);
		if (saveData != null)
		{
			local.Open(saveData, localSave);
			isNotEmpty = true;
			Debug.Log("Local save loaded successfully");
		}
		else
		{
			Debug.Log("No local save found");
		}
		localLoaded = true;
		localProgress = 1f;
		CheckComparisonStatus();
	}

	private void CheckComparisonStatus()
	{
		if (!cloudLoaded || !localLoaded)
		{
			return;
		}
		if (local.gameObject.activeSelf)
		{
			local.ResetTextColor();
		}
		if (cloud.gameObject.activeSelf)
		{
			cloud.ResetTextColor();
		}
		if (local.gameObject.activeSelf && cloud.gameObject.activeSelf)
		{
			if (local.save == cloud.save)
			{
				theSameLabel.text = "SaveCompareIdentical".Translate();
				theSameLabel.color = Color.green;
				Debug.Log("Saves are identical");
				return;
			}
			theSameLabel.text = "SaveCompareConflict".Translate();
			theSameLabel.color = Color.red;
			Debug.Log("Save conflict detected");
			DateTime saveTime = local.SaveTime;
			DateTime saveTime2 = cloud.SaveTime;
			if (saveTime < saveTime2)
			{
				local.SetTextColor(Color.gray);
			}
			else if (saveTime2 < saveTime)
			{
				cloud.SetTextColor(Color.gray);
			}
		}
		else
		{
			theSameLabel.text = "";
		}
	}

	private void CheckComparison()
	{
		header.enabled = true;
		progress.transform.parent.gameObject.SetActive(value: false);
		if (!isNotEmpty)
		{
			GameManager.Instance.EnableAutoSave();
			Settings.BlockInput = false;
			Time.timeScale = 1f;
			GameManager.Instance.LoadEmpty();
		}
	}

	public void Load(SaveData data)
	{
		Settings.BlockInput = false;
		Time.timeScale = 1f;
		Wait(delegate
		{
			GameManager.Instance.SaveData.load_save(data);
		});
	}

	private void Wait(Action action)
	{
		StartCoroutine(wait(action));
	}

	private IEnumerator wait(Action action)
	{
		yield return null;
		action?.Invoke();
	}
}
