using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellGildingWindow : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI inkLabel;

	[SerializeField]
	private Transform content;

	[SerializeField]
	private SpellUpgradeVisual prefab;

	[SerializeField]
	private BuyInks buyInks;

	[SerializeField]
	private Toggle upgraded;

	[SerializeField]
	private Toggle active;

	[SerializeField]
	private TMP_Dropdown sortByType;

	[SerializeField]
	private ShardsPoolUpgradeVisual shardsPool;

	private List<SpellUpgradeVisual> list;

	[SerializeField]
	private SkillVisual skill;

	[SerializeField]
	private MasteryMilestonesListSwitch bonusesList;

	private void OnEnable()
	{
		UpdateInkLabel();
		UpdateList();
		bonusesList.UpdateText();
	}

	public bool CheckAvailable()
	{
		return GameManager.Instance.Paragon.GildingSpellsIsAvailable;
	}

	public void Init()
	{
		SpellGilding spells = GameManager.Instance.Gilding.Spells;
		list = new List<SpellUpgradeVisual>();
		shardsPool.Init(spells.shardsPool);
		VariableBignumber ink = spells.ink;
		ink.OnChange = (Action)Delegate.Combine(ink.OnChange, new Action(OnChangeInk));
		skill.SetSkill(spells.mastery.Skill);
		Skill obj = spells.mastery.Skill;
		obj.OnEarnExp = (Action)Delegate.Combine(obj.OnEarnExp, new Action(skill.UpdateLabel));
		bonusesList.Init(spells.mastery.Bonuses);
		MasteryBonuses bonuses = spells.mastery.Bonuses;
		bonuses.OnCheck = (Action)Delegate.Combine(bonuses.OnCheck, new Action(bonusesList.UpdateText));
	}

	public void UpdateList()
	{
		List<Spells> spellList = GameManager.Instance.CurrentHero.Hero.SpellList;
		spellList = GameManager.Instance.SpellBook.Enhancements.Replace(spellList);
		List<Spell> list = new List<Spell>();
		SpellBook spellBook = GameManager.Instance.SpellBook;
		if (active.isOn)
		{
			foreach (Scroll scroll in GameManager.Instance.Scrolls.Scrolls)
			{
				if (scroll.spell != null)
				{
					Spell spell = scroll.spell;
					if (!spellBook.MemeticBanList.Contains(spell.NameKey) && spell.Type != SpellTypeGroup.None)
					{
						list.Add(spell);
					}
				}
			}
		}
		else
		{
			foreach (Spells item in spellList)
			{
				if (!spellBook.MemeticBanList.Contains(item))
				{
					Spell spell = spellBook.GetSpell(item);
					if (spell.Type != SpellTypeGroup.None)
					{
						list.Add(spell);
					}
				}
			}
		}
		if (upgraded.isOn)
		{
			SpellGilding spells = GameManager.Instance.Gilding.Spells;
			List<Spell> list2 = new List<Spell>();
			foreach (Spell item2 in list)
			{
				if (spells.Map.ContainsKey(item2.NameKey) && spells.Map[item2.NameKey].Level != 0)
				{
					list2.Add(item2);
				}
			}
			list = list2;
		}
		if (sortByType.value > 0)
		{
			SpellTypeGroup type = SpellTypeGroup.Evocation;
			if (sortByType.value == 2)
			{
				type = SpellTypeGroup.Incantation;
			}
			else if (sortByType.value == 3)
			{
				type = SpellTypeGroup.Summoning;
			}
			list.RemoveAll((Spell x) => x.Type != type);
		}
		list.Sort((Spell x, Spell y) => (x.level_req >= y.level_req) ? 1 : (-1));
		int count = list.Count;
		SpellUpgradeVisual spellUpgradeVisual = null;
		for (int num = 0; num < count; num++)
		{
			if (this.list.Count < num + 1)
			{
				spellUpgradeVisual = UnityEngine.Object.Instantiate(prefab, content);
				this.list.Add(spellUpgradeVisual);
			}
			else
			{
				spellUpgradeVisual = this.list[num];
			}
			spellUpgradeVisual.Init(list[num].NameKey);
		}
		for (int num2 = count; num2 < this.list.Count; num2++)
		{
			this.list[num2].gameObject.SetActive(value: false);
		}
	}

	public void Open()
	{
		UpdateInkLabel();
		UpdateList();
		buyInks.UpdateAmountToBuy();
		base.gameObject.SetActive(value: true);
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	public void ResetAll()
	{
		SpellGilding manager = GameManager.Instance.Gilding.Spells;
		if (manager.ink == manager.inkTotal)
		{
			return;
		}
		if (GameManager.Instance.Nullifier.ValueInt >= 100)
		{
			GameManager.Instance.ConfirmWindow.Open("This action costs 100 <sprite=4>\nProceed?", delegate
			{
				GameManager.Instance.Nullifier.Change(-100);
				manager.ResetSoft();
			});
		}
		else
		{
			GameManager.Instance.ConfirmWindow.Open("You don't have enough <sprite=4> left.", delegate
			{
				GameManager.Instance.Shop.Open();
			});
		}
	}

	private void UpdateInkLabel()
	{
		SpellGilding spells = GameManager.Instance.Gilding.Spells;
		inkLabel.text = NumberUtils.BigNumberToReadableStringTruncated(spells.ink.Value, 2, "F0") + "/" + NumberUtils.BigNumberToReadableStringTruncated(spells.inkTotal.Value, 2, "F0");
	}

	private void OnChangeInk()
	{
		UpdateInkLabel();
		foreach (SpellUpgradeVisual item in list)
		{
			item.UpdateButton();
		}
		shardsPool.UpdateButton();
	}
}
