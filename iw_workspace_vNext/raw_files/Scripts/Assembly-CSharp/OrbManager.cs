using System;
using System.Collections.Generic;

public class OrbManager
{
	public class OrbFormat
	{
		public string id;

		public string name;

		public string cost;

		public string sprite;

		public string prefab;
	}

	public class Orb
	{
		public int Id;

		public string Name;

		public int Cost;

		public string Sprite;

		public string Prefab;

		public bool Unlocked;

		public Orb(OrbFormat data)
		{
			Id = int.Parse(data.id);
			Name = data.name;
			Cost = ((!string.IsNullOrEmpty(data.cost)) ? int.Parse(data.cost) : 0);
			Sprite = data.sprite;
			Prefab = data.prefab;
			if (Cost == 0)
			{
				Unlocked = true;
			}
		}
	}

	public class SaveData
	{
		public Dictionary<int, int> acitve;

		public List<int> unlocked;
	}

	public Dictionary<int, Orb> Orbs;

	public Dictionary<int, int> Selected;

	public Action OnChange;

	public void Init(List<OrbFormat> data)
	{
		Orbs = new Dictionary<int, Orb>();
		foreach (OrbFormat datum in data)
		{
			Orb orb = new Orb(datum);
			Orbs.Add(orb.Id, orb);
		}
		ResetSelected();
	}

	public int GetSelected()
	{
		int nameKey = (int)GameManager.Instance.CurrentHero.Hero.NameKey;
		if (Selected.ContainsKey(nameKey))
		{
			return Selected[(int)GameManager.Instance.CurrentHero.Hero.NameKey];
		}
		return 0;
	}

	public bool Buy(int id)
	{
		Orb orb = Orbs[id];
		VariableInt alterationSand = GameManager.Instance.AlterationSand;
		if (alterationSand.Value >= orb.Cost)
		{
			alterationSand.Change(-orb.Cost);
			orb.Unlocked = true;
			return true;
		}
		return false;
	}

	public void Set(int id)
	{
		Selected[(int)GameManager.Instance.CurrentHero.Hero.NameKey] = id;
		ActivateCurrent();
	}

	public void SetFake(int id)
	{
		if (Orbs.ContainsKey(id))
		{
			GameManager.Instance.Orb.SetOrbVisual(Orbs[id].Prefab);
			OnChange?.Invoke();
		}
	}

	public void Activate(int id)
	{
		if (Orbs[id].Unlocked)
		{
			Set(id);
		}
	}

	public void ActivateCurrent()
	{
		GameManager.Instance.Orb.SetOrbVisual(Orbs[GetSelected()].Prefab);
		OnChange?.Invoke();
	}

	public void ResetSelected()
	{
		Selected = new Dictionary<int, int>
		{
			{ 0, 0 },
			{ 2, 2 },
			{ 1, 1 },
			{ 5, 5 },
			{ 4, 4 },
			{ 3, 3 },
			{ 7, 7 },
			{ 6, 6 },
			{ 8, 8 },
			{ 9, 9 },
			{ 10, 10 },
			{ 11, 11 },
			{ 12, 12 },
			{ 13, 13 },
			{ 14, 14 },
			{ 15, 15 },
			{ 16, 16 },
			{ 17, 17 },
			{ 18, 18 }
		};
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.acitve = new Dictionary<int, int>();
		foreach (KeyValuePair<int, int> item in Selected)
		{
			if (item.Key != item.Value)
			{
				saveData.acitve.Add(item.Key, item.Value);
			}
		}
		saveData.unlocked = new List<int>();
		foreach (KeyValuePair<int, Orb> orb in Orbs)
		{
			if (orb.Value.Cost > 0 && orb.Value.Unlocked)
			{
				saveData.unlocked.Add(orb.Key);
			}
		}
		return saveData;
	}

	public void Load(SaveData data)
	{
		ResetSelected();
		foreach (KeyValuePair<int, Orb> orb in Orbs)
		{
			if (orb.Value.Unlocked && orb.Value.Cost > 0)
			{
				orb.Value.Unlocked = false;
			}
		}
		if (data == null)
		{
			return;
		}
		foreach (int item in data.unlocked)
		{
			Orbs[item].Unlocked = true;
		}
		foreach (KeyValuePair<int, int> item2 in data.acitve)
		{
			Selected[item2.Key] = item2.Value;
		}
		ActivateCurrent();
	}
}
