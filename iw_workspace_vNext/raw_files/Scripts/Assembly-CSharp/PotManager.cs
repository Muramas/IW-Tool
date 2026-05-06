using System.Collections.Generic;
using UnityEngine;

public class PotManager : MonoBehaviour
{
	public List<PotVisual> Pots;

	[SerializeField]
	private List<Canvas> canvases;

	public static PotManager Instance;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			Init();
		}
	}

	private void Update()
	{
		if (Settings.BlockInput || Input.GetKey(KeyCode.P) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Y) || Input.GetKey(KeyCode.Q))
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Alpha8))
		{
			Pots.Find((PotVisual x) => x.Key == PotKey.Mana).Activate();
		}
		else if (Input.GetKeyDown(KeyCode.Alpha9))
		{
			Pots.Find((PotVisual x) => x.Key == PotKey.Void).Activate();
		}
		else if (Input.GetKeyDown(KeyCode.Alpha0))
		{
			Pots.Find((PotVisual x) => x.Key == PotKey.Production).Activate();
		}
	}

	public void Init()
	{
		foreach (Canvas canvase in canvases)
		{
			canvase.worldCamera = Camera.main;
		}
		foreach (PotVisual pot in Pots)
		{
			pot.Init(Create(pot.Key));
		}
	}

	public void Charge(PotKey key, BigNumber value)
	{
		Pots.Find((PotVisual x) => x.Key == key)?.Charge(value);
	}

	public void Reset()
	{
		foreach (PotVisual pot in Pots)
		{
			pot.Reset();
		}
	}

	public void StopAll()
	{
		foreach (PotVisual pot in Pots)
		{
			pot.Deactivate();
		}
	}

	private Pot Create(PotKey key)
	{
		return key switch
		{
			PotKey.Mana => new ManaPot(), 
			PotKey.Void => new VoidPot(), 
			PotKey.Production => new ProdPot(), 
			_ => new Pot(), 
		};
	}
}
