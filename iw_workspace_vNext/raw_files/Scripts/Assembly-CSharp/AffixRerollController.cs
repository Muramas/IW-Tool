using System;
using System.Collections.Generic;
using System.Text;
using ModelShark;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AffixRerollController : MonoBehaviour
{
	[SerializeField]
	private AffixHolder Effects;

	[SerializeField]
	private AffixHolder Rerolls;

	[SerializeField]
	private Button RerollButton;

	[SerializeField]
	private GameObject EditBlock;

	[SerializeField]
	private Button AcceptButton;

	[SerializeField]
	private TextMeshProUGUI RerollCost;

	[SerializeField]
	private ModelShark.TooltipTrigger tooltip;

	private MythicItem item;

	private MythicCrafter crafter;

	private const string cost = " # <sprite=4>";

	private void Awake()
	{
		MasterworkController masterwork = GameManager.Instance.Craft.window.forge.masterwork;
		masterwork.OnUpgrade = (Action)Delegate.Combine(masterwork.OnUpgrade, new Action(SetPossibleOutcomes));
	}

	public void Open(MythicItem item, MythicCrafter crafter)
	{
		this.item = item;
		this.crafter = crafter;
		base.gameObject.SetActive(value: true);
		Effects.Init(item.GetAffixes());
		SetStateReroll(state: false);
		SetPossibleOutcomes();
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	private void SetPossibleOutcomes()
	{
		tooltip.SetText("BodyText", getAffixListDescription());
	}

	private void Update()
	{
		AcceptButton.interactable = Rerolls.GetSelected() != null;
		RerollButton.interactable = Effects.GetSelected() != null && GameManager.Instance.Nullifier.ValueInt >= GetCost();
	}

	public void Reroll()
	{
		if (Effects.GetSelected() == null || GameManager.Instance.Nullifier.ValueInt < GetCost())
		{
			return;
		}
		GameManager.Instance.Nullifier.Change(-GetCost());
		item.ClearItsSlot();
		List<Affix> list = new List<Affix>();
		foreach (Affix affix3 in item.GetAffixes())
		{
			if (affix3 != Effects.GetSelected())
			{
				list.Add(affix3);
			}
		}
		List<Affix> list2 = new List<Affix> { Effects.GetSelected() };
		MythicTier tier = item.GetTier();
		Affix affix = crafter.RerollAffix(item, list);
		affix.SetGilding(tier.GetGilding());
		list.Add(affix);
		list2.Add(affix);
		affix.SetEfficiency(item.Efficiency);
		affix.SetGilding(tier.GetGilding());
		Affix affix2 = crafter.RerollAffix(item, list);
		affix2.SetGilding(tier.GetGilding());
		affix2.SetEfficiency(item.Efficiency);
		affix2.SetGilding(tier.GetGilding());
		list2.Add(affix2);
		Rerolls.Init(list2, list2[0]);
		item.Rerolls++;
		SetStateReroll(state: true);
	}

	public void Accept()
	{
		if (Rerolls.GetSelected() != null)
		{
			SetStateReroll(state: false);
			item.ReplaceAffix(Effects.GetSelected(), Rerolls.GetSelected());
			Effects.Init(item.GetAffixes());
			Affix affix = item.GetAffixes().Find((Affix x) => x.GetTarget() == Rerolls.GetSelected().GetTarget());
			Effects.OnSelect(affix);
		}
	}

	public void Decline()
	{
		SetStateReroll(state: false);
	}

	private void SetStateReroll(bool state)
	{
		Rerolls.gameObject.SetActive(state);
		RerollButton.gameObject.SetActive(!state);
		EditBlock.SetActive(state);
		Effects.SetBlocking(state);
		if (!state)
		{
			RerollCost.text = "Reroll".Translate() + " # <sprite=4>".Replace("#", GetCost().ToString());
		}
	}

	private int GetCost()
	{
		return Mathf.Clamp(item.Rerolls + 1, 0, 10);
	}

	private string getAffixListDescription()
	{
		List<AffixData> affixes = GlobalData.Affixes;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (AffixData item in affixes)
		{
			bool isInt = item.IsInt();
			stringBuilder.AppendLine(TranslationManager.Instance.Process(item.Description).Replace("#1", "(" + formatValue(item.GetMinValue(this.item.Efficiency, this.item.GetTier().GetGilding(), isInt), isInt) + "-" + formatValue(item.GetMaxValue(this.item.Efficiency, this.item.GetTier().GetGilding(), isInt), isInt) + ")"));
		}
		return stringBuilder.ToString();
	}

	private string formatValue(BigNumber value, bool isInt)
	{
		string text = ((!isInt) ? (((value - 1.0) * 100.0).ToReadableString() + "%") : value.ToInt().ToString());
		if (Settings.ColoredTips)
		{
			return "<color=#e2b018>" + text + "</color>";
		}
		return text;
	}
}
