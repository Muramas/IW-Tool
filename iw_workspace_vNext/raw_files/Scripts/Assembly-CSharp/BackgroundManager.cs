using System;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager
{
	public class BackgroundFormat
	{
		public string id;

		public string name;

		public string cost;

		public string sprite;

		public string prefab;

		public string premium;

		public bool IsPremium()
		{
			return !string.IsNullOrEmpty(premium);
		}
	}

	public class Backgound
	{
		public int Id;

		public string Name;

		public int Cost;

		public string Sprite;

		public string Prefab;

		public bool Unlocked;

		public string Premium;

		public bool IsPremium()
		{
			return !string.IsNullOrEmpty(Premium);
		}

		public Backgound(BackgroundFormat data)
		{
			Id = int.Parse(data.id);
			Name = data.name;
			Cost = ((!string.IsNullOrEmpty(data.cost)) ? int.Parse(data.cost) : 0);
			Sprite = data.sprite;
			Prefab = data.prefab;
			Premium = data.premium;
			if (Cost == 0 && !IsPremium())
			{
				Unlocked = true;
			}
		}
	}

	public class SaveData
	{
		public int active;

		public List<int> unlocked;
	}

	public Dictionary<int, Backgound> Backgrounds;

	public int Selected;

	public VariableInt Unlocked;

	private BackgroundSpot spot;

	public Action OnChange;

	public void Init(List<BackgroundFormat> data)
	{
		Unlocked = new VariableInt(0);
		GameContext.ContextAddResource("Gallery.Back", Unlocked);
		Backgrounds = new Dictionary<int, Backgound>();
		foreach (BackgroundFormat datum in data)
		{
			Backgound backgound = new Backgound(datum);
			Backgrounds.Add(backgound.Id, backgound);
		}
		spot = GameObject.FindGameObjectWithTag("BackSpot").GetComponent<BackgroundSpot>();
	}

	public bool Buy(int id)
	{
		if (!Backgrounds.ContainsKey(id))
		{
			return false;
		}
		Backgound backgound = Backgrounds[id];
		VariableInt alterationSand = GameManager.Instance.AlterationSand;
		if (alterationSand.Value >= backgound.Cost)
		{
			alterationSand.Change(-backgound.Cost);
			backgound.Unlocked = true;
			Unlocked.Change(1);
			return true;
		}
		return false;
	}

	public void Set(int id)
	{
		Selected = id;
		spot.Set(GetBackground(Selected));
		OnChange?.Invoke();
	}

	public void SetFake(int id)
	{
		spot.Set(GetBackground(id));
		OnChange?.Invoke();
	}

	public void ActivateCurrent()
	{
		spot.Set(GetBackground(Selected));
		OnChange?.Invoke();
	}

	public void Activate(int id)
	{
		if (GetBackground(id).Unlocked)
		{
			Set(id);
			OnChange?.Invoke();
		}
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.active = Selected;
		saveData.unlocked = new List<int>();
		foreach (KeyValuePair<int, Backgound> background in Backgrounds)
		{
			if (background.Value.Unlocked && (background.Value.Cost > 0 || background.Value.IsPremium()))
			{
				saveData.unlocked.Add(background.Key);
			}
		}
		return saveData;
	}

	public void Load(SaveData data)
	{
		ResetAll();
		int num = 0;
		if (data == null)
		{
			Activate(0);
			return;
		}
		foreach (int item in data.unlocked)
		{
			GetBackground(item).Unlocked = true;
			num++;
		}
		Activate(data.active);
		Unlocked.SetValue(num);
	}

	private void ResetAll()
	{
		Selected = 0;
		foreach (KeyValuePair<int, Backgound> background in Backgrounds)
		{
			if ((background.Value.Cost > 0 || background.Value.IsPremium()) && background.Value.Unlocked)
			{
				background.Value.Unlocked = false;
			}
		}
	}

	public void Unlock(int id)
	{
		if (Backgrounds.ContainsKey(id))
		{
			Backgound backgound = Backgrounds[id];
			if (backgound != null)
			{
				backgound.Unlocked = true;
			}
		}
	}

	public void Lock(int id)
	{
		if (!Backgrounds.ContainsKey(id))
		{
			return;
		}
		Backgound backgound = Backgrounds[id];
		if (backgound != null)
		{
			backgound.Unlocked = false;
			if (Selected == id)
			{
				Set(0);
			}
		}
	}

	private Backgound GetBackground(int id)
	{
		if (Backgrounds.ContainsKey(id))
		{
			return Backgrounds[id];
		}
		return Backgrounds[0];
	}

	public Color GetColor()
	{
		return spot.GetColor();
	}
}
