using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellSet : MonoBehaviour
{
	public List<Image> icons;

	public List<Image> frame;

	public TextMeshProUGUI label;

	public TMP_InputField nameField;

	[SerializeField]
	private int id;

	private SetsPanel panel;

	public void Init(int id)
	{
		this.id = id;
		label.text = (id + 1).ToString();
		panel = GameManager.Instance.SpellBook.SetsPanel;
		foreach (Image icon in icons)
		{
			icon.enabled = false;
		}
		foreach (Image item in frame)
		{
			item.sprite = panel.unlocked;
			item.enabled = true;
		}
	}

	public void Export()
	{
		if (panel.GetPreset().Get(id) != null)
		{
			panel.OpenExport(id);
		}
	}

	public void Save()
	{
		Preset<SpellPresetSlot> preset = new Preset<SpellPresetSlot>();
		List<Scroll> scrolls = GameManager.Instance.Scrolls.Scrolls;
		preset.id = id;
		for (int i = 0; i < scrolls.Count; i++)
		{
			Scroll scroll = scrolls[i];
			if (scroll.spell == null)
			{
				preset.slots.Add(new SpellPresetSlot(-1, 0));
			}
			else
			{
				preset.slots.Add(new SpellPresetSlot((int)GameManager.Instance.SpellBook.Enhancements.Downgrade(scroll.spell.NameKey), scroll.GetAutoMode()));
			}
		}
		if (!string.IsNullOrEmpty(nameField.text))
		{
			preset.name = nameField.text;
		}
		panel.GetPreset().Replace(preset);
		panel.Presets.SaveCurrent();
		panel.LoadVisualSet(id, preset);
		panel.OnSaveSet?.Invoke();
	}

	public void OnFocusNameField()
	{
		Settings.BlockInput = true;
	}

	public void OnChangeName()
	{
		Settings.BlockInput = false;
		nameField.text = nameField.text.Replace("#", string.Empty).Replace("@", string.Empty).Replace(";", string.Empty);
		if (panel.GetPreset().IsExist(id))
		{
			Preset<SpellPresetSlot> preset = panel.GetPreset().Get(id);
			preset.name = nameField.text;
			panel.GetPreset().Replace(preset);
			panel.Presets.SaveCurrent();
		}
	}

	public void Set()
	{
		GameManager.Instance.StartCoroutine(change());
	}

	private IEnumerator change()
	{
		Preset<SpellPresetSlot> s = panel.GetPreset().Get(id);
		List<Scroll> sc = GameManager.Instance.Scrolls.Scrolls;
		GameManager.Instance.Scrolls.ClearAutocast();
		if (s == null || s.slots.Count == 0)
		{
			if (GameManager.Instance.ChallengeManager.ActiveChallenge == null)
			{
				foreach (Scroll item in sc)
				{
					item.Clear();
				}
			}
			yield return null;
		}
		else
		{
			yield return null;
			for (int i = 0; i < sc.Count && i < s.slots.Count; i++)
			{
				Scroll scroll = sc[i];
				SpellPresetSlot spellPresetSlot = s.slots[i];
				if (scroll.spell != null)
				{
					if (scroll.spell.NameKey != spellPresetSlot.GetSpell())
					{
						sc[i].Clear(soft: true);
					}
					else
					{
						scroll.SetAutoMode(spellPresetSlot.autoMode);
					}
				}
			}
			if (sc.Count > s.slots.Count)
			{
				for (int j = s.slots.Count; j < sc.Count; j++)
				{
					sc[j].Clear();
				}
			}
			yield return null;
			SpellChoosePanel choosePanel = GameManager.Instance.Scrolls.ChoosePanel;
			for (int k = 0; k < sc.Count; k++)
			{
				Scroll scroll = sc[k];
				if (s.slots.Count <= k)
				{
					scroll.Clear();
					continue;
				}
				SpellPresetSlot spellPresetSlot = s.slots[k];
				if (scroll.spell != null || !scroll.gameObject.activeInHierarchy)
				{
					continue;
				}
				Spells spell = spellPresetSlot.GetSpell();
				if (GameManager.Instance.SpellBook.AvailableSpells.Any((Spell x) => x.NameKey == spell))
				{
					choosePanel.scroll = sc[k];
					SpellChoose spellChoose = GameManager.Instance.SpellBook.SpellChooses.Find((SpellChoose x) => x.Spell != null && x.Spell.NameKey == spell);
					if (spellChoose.Spell.AvailableToUse)
					{
						spellChoose.Choose();
						scroll.SetAutoMode(spellPresetSlot.autoMode);
					}
				}
			}
		}
		GameManager.Instance.Scrolls.ChangeSpells?.Invoke();
	}
}
