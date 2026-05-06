using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorruptionManager : MonoBehaviour
{
	public class SaveData
	{
		public float timer;

		public int id;

		public int charge;

		public int amount;
	}

	public GameObject Visual;

	[SerializeField]
	private List<CorruptionSpot> spots;

	private CorruptionSpot activeSpot;

	private CorruptionReward rewards;

	public VariableFloat SpawnRate;

	public Action OnCollect;

	private float spawnPeriod = 8640f;

	private float spawnTimer;

	public void Init()
	{
		if (rewards == null)
		{
			rewards = new CorruptionReward();
			SpawnRate = new VariableFloat(1f);
			rewards.Init();
		}
	}

	private void Update()
	{
		spawnTimer += Time.unscaledDeltaTime * SpawnRate.ValueFloat;
		if (spawnTimer >= spawnPeriod)
		{
			Spawn();
		}
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.timer = spawnTimer;
		if (activeSpot == null)
		{
			saveData.id = -1;
			saveData.charge = 0;
		}
		else
		{
			saveData.id = activeSpot.ID;
			saveData.charge = activeSpot.Charges;
		}
		saveData.amount = rewards.Amount;
		return saveData;
	}

	public void Load(SaveData data)
	{
		StopAllCoroutines();
		Init();
		if (activeSpot != null)
		{
			activeSpot.Disable();
			activeSpot = null;
		}
		spawnTimer = 0f;
		if (data == null)
		{
			return;
		}
		if (data.id >= 0)
		{
			activeSpot = spots.Find((CorruptionSpot x) => x.ID == data.id);
			activeSpot.Add(data.charge);
		}
		rewards.Amount = data.amount;
		spawnTimer = data.timer;
	}

	public void Offline(float time)
	{
		spawnTimer += time;
		while (spawnTimer >= spawnPeriod)
		{
			Spawn();
		}
	}

	private void Spawn()
	{
		if (activeSpot == null)
		{
			activeSpot = spots[UnityEngine.Random.Range(0, spots.Count)];
		}
		activeSpot.Add();
		spawnTimer -= spawnPeriod;
	}

	public void Collect()
	{
		if (!(activeSpot == null))
		{
			StopAllCoroutines();
			StartCoroutine(collect());
		}
	}

	private IEnumerator collect()
	{
		List<MovableBonusSpawner.DropType> dropList = rewards.getDropList();
		float timer = 0f;
		while (activeSpot.Charges > 0)
		{
			if (timer >= 0.35f)
			{
				rewards.Get(dropList, activeSpot);
				timer = 0f;
			}
			yield return null;
			timer += Time.unscaledDeltaTime;
		}
		activeSpot = null;
	}
}
