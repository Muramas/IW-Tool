using System;
using System.Collections.Generic;
using UnityEngine;

public class CatcherManager : MonoBehaviour
{
	public class SaveData
	{
		public int active;

		public List<int> unlocked;
	}

	[Serializable]
	public class HunterData
	{
		public int id;

		public bool unlocked;

		public bool isPremium;

		public int cost;

		public RuntimeAnimatorController animator;

		public GameObject controller;
	}

	[SerializeField]
	private Catcher catcher;

	private int upgrade;

	[SerializeField]
	public List<HunterData> list;

	public int selectedId;

	public bool isActive { get; private set; }

	private void Awake()
	{
		base.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		if (GameManager.Instance.MBSpawner.bonus.activated)
		{
			catcher.Launch();
		}
	}

	public int GetUpgrade()
	{
		return upgrade;
	}

	public void SetFake(int id)
	{
		HunterData hunterData = list.Find((HunterData x) => x.id == id);
		if (hunterData != null)
		{
			catcher.SetAnimator(hunterData.animator);
			if (hunterData.controller != null)
			{
				catcher.SetController(hunterData.controller);
			}
		}
	}

	public void SetActive(int id)
	{
		if (selectedId == id)
		{
			return;
		}
		HunterData hunterData = list.Find((HunterData x) => x.id == id);
		if (hunterData != null)
		{
			selectedId = id;
			catcher.SetAnimator(hunterData.animator);
			if (hunterData.controller != null)
			{
				catcher.SetController(hunterData.controller);
			}
		}
	}

	public void Load(bool active, int upgrade = 0)
	{
		this.upgrade = upgrade;
		if (this.upgrade == 0)
		{
			GameManager.Instance.MBSpawner.DropQuantity.SetValue(1.0);
		}
		else
		{
			GameManager.Instance.MBSpawner.DropQuantity.SetValue(1.100000023841858);
		}
		if (active)
		{
			if (!base.transform.gameObject.activeSelf)
			{
				MovableBonusSpawner mBSpawner = GameManager.Instance.MBSpawner;
				mBSpawner.OnSpawn = (Action)Delegate.Combine(mBSpawner.OnSpawn, new Action(OnSpawn));
				MovableBonusSpawner mBSpawner2 = GameManager.Instance.MBSpawner;
				mBSpawner2.OnManualCollect = (Action)Delegate.Combine(mBSpawner2.OnManualCollect, new Action(OnCollect));
				MovableBonusSpawner mBSpawner3 = GameManager.Instance.MBSpawner;
				mBSpawner3.OnManualCollect = (Action)Delegate.Combine(mBSpawner3.OnManualCollect, new Action(catcher.Stop));
				base.gameObject.SetActive(value: true);
			}
		}
		else if (base.transform.gameObject.activeSelf)
		{
			MovableBonusSpawner mBSpawner4 = GameManager.Instance.MBSpawner;
			mBSpawner4.OnSpawn = (Action)Delegate.Remove(mBSpawner4.OnSpawn, new Action(OnSpawn));
			MovableBonusSpawner mBSpawner5 = GameManager.Instance.MBSpawner;
			mBSpawner5.OnManualCollect = (Action)Delegate.Remove(mBSpawner5.OnManualCollect, new Action(OnCollect));
			MovableBonusSpawner mBSpawner6 = GameManager.Instance.MBSpawner;
			mBSpawner6.OnManualCollect = (Action)Delegate.Remove(mBSpawner6.OnManualCollect, new Action(catcher.Stop));
			base.gameObject.SetActive(value: false);
		}
		isActive = active;
	}

	public void LoadSkins(SaveData data)
	{
		SetActive(0);
		foreach (HunterData item in list)
		{
			if (item.cost > 0 || item.isPremium)
			{
				item.unlocked = false;
			}
		}
		if (data == null)
		{
			return;
		}
		SetActive(data.active);
		int i;
		for (i = 0; i < data.unlocked.Count; i++)
		{
			HunterData hunterData = list.Find((HunterData x) => x.id == data.unlocked[i]);
			if (hunterData != null)
			{
				hunterData.unlocked = true;
			}
		}
	}

	public SaveData SaveSkins()
	{
		SaveData saveData = new SaveData();
		saveData.active = selectedId;
		saveData.unlocked = new List<int>();
		foreach (HunterData item in list)
		{
			if ((item.cost > 0 || item.isPremium) && item.unlocked)
			{
				saveData.unlocked.Add(item.id);
			}
		}
		return saveData;
	}

	public void OnSpawn()
	{
		catcher.Launch();
	}

	public void OnCollect()
	{
	}

	public bool Buy(int id)
	{
		HunterData hunterData = list.Find((HunterData x) => x.id == id);
		if (hunterData == null)
		{
			return false;
		}
		VariableInt alterationSand = GameManager.Instance.AlterationSand;
		if (alterationSand.Value >= hunterData.cost)
		{
			alterationSand.Change(-hunterData.cost);
			hunterData.unlocked = true;
			return true;
		}
		return false;
	}

	public void Unlock(int id)
	{
		HunterData hunterData = list.Find((HunterData x) => x.id == id);
		if (hunterData != null)
		{
			hunterData.unlocked = true;
		}
	}

	public void Lock(int id)
	{
		HunterData hunterData = list.Find((HunterData x) => x.id == id);
		if (hunterData != null)
		{
			hunterData.unlocked = false;
			if (selectedId == id)
			{
				SetActive(0);
			}
		}
	}
}
