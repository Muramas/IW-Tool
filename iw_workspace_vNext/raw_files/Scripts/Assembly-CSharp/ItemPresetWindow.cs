using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ItemPresetWindow : MonoBehaviour
{
	public class SaveData
	{
		public Dictionary<string, string> prime;

		public Dictionary<string, string> quasi;

		public SaveData()
		{
			prime = new Dictionary<string, string>();
			quasi = new Dictionary<string, string>();
		}
	}

	public class ItemPresetMap
	{
		public Dictionary<string, PresetList> map;

		public ItemPresetMap()
		{
			map = new Dictionary<string, PresetList>();
		}

		public void Add(string key, PresetList preset)
		{
			if (map.ContainsKey(key))
			{
				map[key] = preset;
			}
			else
			{
				map.Add(key, preset);
			}
		}

		public PresetList Get(string key)
		{
			if (!map.ContainsKey(key))
			{
				return null;
			}
			return map[key];
		}
	}

	public class PresetList
	{
		public List<Preset> list;

		public PresetList()
		{
			list = new List<Preset>();
		}

		public PresetList Clone()
		{
			PresetList presetList = new PresetList();
			foreach (Preset item in list)
			{
				Preset preset = new Preset(item.id);
				foreach (PresetSlotLegacy item2 in item.list)
				{
					preset.Add(new PresetSlotLegacy(item2.item));
				}
				presetList.Add(preset);
			}
			return presetList;
		}

		public void Add(Preset preset)
		{
			int num = list.FindIndex((Preset x) => x.id == preset.id);
			if (num < 0)
			{
				list.Add(preset);
			}
			else
			{
				list[num] = preset;
			}
		}

		public Preset Get(int id)
		{
			return list.Find((Preset x) => x.id == id);
		}
	}

	public class Preset
	{
		public int id = -1;

		public string name;

		public List<PresetSlotLegacy> list;

		public Preset(int id)
		{
			this.id = id;
			list = new List<PresetSlotLegacy>();
		}

		public void Add(PresetSlotLegacy slot)
		{
			list.Add(slot);
		}

		public bool IsEmpty()
		{
			bool flag = true;
			foreach (PresetSlotLegacy item in list)
			{
				flag = item.item < 0;
				if (!flag)
				{
					break;
				}
			}
			return flag;
		}
	}

	public class PresetSlotLegacy
	{
		public int item = -1;

		public PresetSlotLegacy(int item)
		{
			this.item = item;
		}
	}

	public PresetController<PresetSlot> Presets;

	public List<ItemPreset> sets;

	public ItemPresetMap map;

	[SerializeField]
	private ExportWindow exportWindow;

	[SerializeField]
	private string exportMsg;

	[SerializeField]
	private string importMsg;

	[SerializeField]
	private Transform textPosition;

	[SerializeField]
	private Button copyPrime;

	public Func<string, string> OnShowName;

	public Action OnSaveSet;

	[DllImport("__Internal")]
	private static extern void ShowOverlayItems(string exportedGameSave);

	[DllImport("__Internal")]
	private static extern void HideOverlay();

	public void ImportFromWebOverlay(string importStr)
	{
		Import(importStr);
		HideOverlay();
		Settings.BlockInput = false;
	}

	private void ShowImportExportOverlay(string gameSave)
	{
		ShowOverlayItems(gameSave);
		Settings.BlockInput = true;
	}

	public void OnEnable()
	{
		copyPrime.gameObject.SetActive(GameManager.Instance.Realmcraft.IsActive);
		LoadPresetVisual();
	}

	public void OnDisable()
	{
		exportWindow.Close();
		Settings.BlockInput = false;
	}

	public void Init()
	{
		Presets = new PresetController<PresetSlot>();
		Presets.Init();
		foreach (ItemPreset set in sets)
		{
			set.Init(OnChangeSet, OpenExport);
		}
		HeroSlot currentHero = GameManager.Instance.CurrentHero;
		currentHero.OnChange = (Action)Delegate.Combine(currentHero.OnChange, new Action(UpdateCurrentPreset));
		LoadPresetVisual();
	}

	private void UpdateCurrentPreset()
	{
		Presets.UpdateCurrent();
		LoadPresetVisual();
	}

	public SaveData Save()
	{
		return new SaveData
		{
			prime = Presets.prime,
			quasi = Presets.quasi
		};
	}

	public void Load(SaveData data, ItemPresetMap legacyData)
	{
		if (data == null)
		{
			data = new SaveData();
		}
		if (legacyData != null && legacyData.map != null && GameManager.Instance.SaveData.SaveVersion < 70)
		{
			foreach (KeyValuePair<string, PresetList> item in legacyData.map)
			{
				if (data.prime.ContainsKey(item.Key))
				{
					data.prime[item.Key] = ConvertExportFullLegacy(item.Value);
				}
				else
				{
					data.prime.Add(item.Key, ConvertExportFullLegacy(item.Value));
				}
			}
		}
		Presets.prime = data.prime;
		Presets.quasi = data.quasi;
		UpdateCurrentPreset();
		LoadPresetVisual();
	}

	public void LoadPresetVisual()
	{
		PresetList<PresetSlot> preset = GetPreset();
		if (preset.sets == null || preset.sets.Count == 0)
		{
			for (int i = 0; i < sets.Count; i++)
			{
				sets[i].Clear();
			}
			return;
		}
		for (int j = 0; j < sets.Count; j++)
		{
			ItemPreset itemPreset = sets[j];
			Preset<PresetSlot> preset2 = preset.Get(itemPreset.id);
			itemPreset.Set(preset2);
		}
	}

	public void OnChangeSet(ItemPreset set)
	{
		PresetList<PresetSlot> preset = GetPreset();
		Preset<PresetSlot> preset2 = new Preset<PresetSlot>();
		preset2.id = set.id;
		foreach (ItemPresetFrame frame in set.frames)
		{
			preset2.slots.Add(new PresetSlot((frame.item == null) ? (-1) : frame.item.ID));
		}
		preset2.name = set.GetName();
		preset.Replace(preset2);
		Presets.SaveCurrent();
		UpdateCurrentPreset();
	}

	public void OpenExport(int id)
	{
		OpenExport(GetExport(id));
	}

	public void OpenExportFull()
	{
		OpenExport(Presets.GetForRealm());
	}

	public void OpenExport(string msg)
	{
		exportWindow.Open(exportMsg, msg);
	}

	public void OpenImport()
	{
		exportWindow.Open(importMsg, null, isExport: false);
	}

	public void Import(string str)
	{
		PresetList<PresetSlot> presetList = new PresetList<PresetSlot>(str);
		PresetList<PresetSlot> preset = GetPreset();
		foreach (KeyValuePair<int, Preset<PresetSlot>> set in presetList.sets)
		{
			preset.Replace(set.Value);
		}
		Presets.SaveCurrent();
		exportWindow.Close();
		UpdateCurrentPreset();
	}

	public void Import()
	{
		Import(exportWindow.GetText());
	}

	public string GetExport(int id)
	{
		return GetPreset().Get(id).ToString();
	}

	private PresetList<PresetSlot> GetPreset()
	{
		return Presets.GetCurrent;
	}

	public void CopyPrimePreset()
	{
		Presets.SetPrime();
		LoadPresetVisual();
	}

	public void CheckInputPresets()
	{
		if (Settings.BlockInput || (!Input.GetKey(KeyCode.P) && !Input.GetKey(KeyCode.Q)))
		{
			return;
		}
		int num = -1;
		if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
		{
			num = 0;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
		{
			num = 1;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
		{
			num = 2;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
		{
			num = 3;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
		{
			num = 4;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
		{
			num = 5;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
		{
			num = 6;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
		{
			num = 7;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
		{
			num = 8;
		}
		else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
		{
			num = 9;
		}
		if (num == -1)
		{
			return;
		}
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !Input.GetKey(KeyCode.Q))
		{
			if (sets.Count > num)
			{
				sets[num].Save();
			}
		}
		else if (sets.Count > num)
		{
			ItemPreset itemPreset = sets[num];
			itemPreset.Load();
			string text = itemPreset.GetName();
			if (string.IsNullOrEmpty(text))
			{
				text = "Set " + (num + 1);
			}
			if (OnShowName != null)
			{
				text = OnShowName(text);
			}
			GameManager.Instance.AnimatedText.TextUp(text, textPosition.position, Color.white, -1f, 1f, ignoreLimits: true, ignoreTimeScale: true);
		}
	}

	public string ConvertExportFullLegacy(PresetList preset)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < sets.Count; i++)
		{
			stringBuilder.Append(ConvertExportLegacy(preset, i));
		}
		return stringBuilder.ToString();
	}

	public string ConvertExportLegacy(PresetList preset, int id)
	{
		Preset preset2 = preset.Get(id);
		if (preset2 == null || preset2.list == null || preset2.IsEmpty())
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("#");
		stringBuilder.Append(id);
		stringBuilder.Append("#");
		stringBuilder.Append(preset2.name);
		stringBuilder.Append("@");
		foreach (PresetSlotLegacy item in preset2.list)
		{
			stringBuilder.Append(item.item);
			stringBuilder.Append(';');
		}
		stringBuilder.Remove(stringBuilder.Length - 1, 1);
		return stringBuilder.ToString();
	}

	public ItemPresetMap SaveSetsLegacy()
	{
		return map;
	}

	public void LoadSetsLegacy(ItemPresetMap data)
	{
		if (data == null)
		{
			data = new ItemPresetMap();
		}
		map = data;
	}
}
