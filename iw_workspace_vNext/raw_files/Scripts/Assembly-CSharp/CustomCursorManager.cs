using System;
using System.Collections.Generic;
using UnityEngine;

public class CustomCursorManager
{
	public class CursorFormat
	{
		public string id;

		public string name;

		public string cost;

		public string sprite;
	}

	public class CursorModel
	{
		public int Id;

		public string Name;

		public int Cost;

		public string Sprite;

		public bool Unlocked;

		public CursorModel(CursorFormat data)
		{
			Id = int.Parse(data.id);
			Name = data.name;
			Cost = ((!string.IsNullOrEmpty(data.cost)) ? int.Parse(data.cost) : 0);
			Sprite = data.sprite;
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

	public Dictionary<int, CursorModel> Cursors;

	public Dictionary<int, int> Selected;

	public Action OnChange;

	private CursorData map;

	public void Init(List<CursorFormat> data, CursorData map)
	{
		this.map = map;
		Cursors = new Dictionary<int, CursorModel>();
		foreach (CursorFormat datum in data)
		{
			CursorModel cursorModel = new CursorModel(datum);
			Cursors.Add(cursorModel.Id, cursorModel);
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

	public void UpdateCursor()
	{
		_ = GameManager.Instance.CurrentHero.Hero.NameKey;
		SetCursor(map.Get(Cursors[GetSelected()].Sprite));
	}

	public Texture2D GetSprite(int id)
	{
		return map.Get(Cursors[id].Sprite);
	}

	public void Remove()
	{
		SetCursor(null);
	}

	public void SetSelected(int id)
	{
		Selected[(int)GameManager.Instance.CurrentHero.Hero.NameKey] = id;
		if (Settings.Cursor)
		{
			GameManager.Instance.Interior.Cursors.UpdateCursor();
		}
	}

	public bool Buy(int id)
	{
		CursorModel cursorModel = Cursors[id];
		VariableInt alterationSand = GameManager.Instance.AlterationSand;
		if (alterationSand.Value >= cursorModel.Cost)
		{
			alterationSand.Change(-cursorModel.Cost);
			cursorModel.Unlocked = true;
			return true;
		}
		return false;
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
		foreach (KeyValuePair<int, CursorModel> cursor in Cursors)
		{
			if (cursor.Value.Cost > 0 && cursor.Value.Unlocked)
			{
				saveData.unlocked.Add(cursor.Key);
			}
		}
		return saveData;
	}

	public void Load(SaveData data)
	{
		ResetSelected();
		foreach (KeyValuePair<int, CursorModel> cursor in Cursors)
		{
			if (cursor.Value.Unlocked && cursor.Value.Cost > 0)
			{
				cursor.Value.Unlocked = false;
			}
		}
		if (data == null)
		{
			return;
		}
		foreach (int item in data.unlocked)
		{
			Cursors[item].Unlocked = true;
		}
		foreach (KeyValuePair<int, int> item2 in data.acitve)
		{
			Selected[item2.Key] = item2.Value;
		}
	}

	private void SetCursor(Texture2D t)
	{
		Cursor.SetCursor(t, Vector2.zero, CursorMode.Auto);
	}
}
