using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class SpellChoosePanel : MonoBehaviour
{
	public Scroll scroll;

	public GameObject InfoGO;

	public TextMeshProUGUI info;

	public Switcher Info;

	public Switcher Sets;

	public Switcher Spells;

	public FlexTipSq Tip;

	public DisableCanvas canvas;

	public void Toggle()
	{
		if (base.gameObject.activeSelf)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public void Open()
	{
		Scroll scroll = GameManager.Instance.Scrolls.Scrolls.FirstOrDefault((Scroll x) => x.spell == null);
		if (scroll == null)
		{
			scroll = GameManager.Instance.Scrolls.Scrolls[0];
		}
		Open(scroll);
	}

	public void Open(Scroll _scroll)
	{
		if (scroll != null)
		{
			scroll.DeactiveChange();
		}
		if (_scroll != null)
		{
			_scroll.ActiveChange();
		}
		scroll = _scroll;
		canvas.On();
		base.gameObject.SetActive(value: true);
		OpenSpells();
		GameManager.Instance.Scrolls.OnOpen?.Invoke();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Close();
		}
	}

	public void Close()
	{
		if (scroll != null)
		{
			scroll.DeactiveChange();
		}
		base.gameObject.SetActive(value: false);
		canvas.Off();
	}

	public void OpenInfo()
	{
		Spells.Off();
		show_info();
		Info.On();
		Sets.Off();
		InfoGO.SetActive(value: true);
		GameManager.Instance.SpellBook.SetsPanel.gameObject.SetActive(value: false);
	}

	public void OpenSpells()
	{
		InfoGO.SetActive(value: false);
		Info.Off();
		Sets.Off();
		Spells.On();
		GameManager.Instance.SpellBook.SetsPanel.gameObject.SetActive(value: false);
	}

	public void OpenSets()
	{
		InfoGO.SetActive(value: false);
		Info.Off();
		Sets.On();
		Spells.Off();
		GameManager.Instance.SpellBook.SetsPanel.gameObject.SetActive(value: true);
	}

	private void show_info()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("PersistantDescr".Translate());
		stringBuilder.AppendLine();
		stringBuilder.Append("Bonuses".Translate());
		stringBuilder.AppendLine(":");
		BigNumber bigNumber = GameManager.Instance.Scrolls.SpellShardsCostReduction.Value;
		bool flag = false;
		if (bigNumber != 1.0)
		{
			stringBuilder.Append("SpellShardCost".Translate());
			stringBuilder.Append(": ");
			if (bigNumber < 0.05000000074505806)
			{
				bigNumber = 0.05000000074505806;
			}
			stringBuilder.Append(((1.0 - bigNumber) * 100.0).ToReadableString());
			stringBuilder.AppendLine("%");
			flag = true;
		}
		bigNumber = GameManager.Instance.Scrolls.SpellChargingCostReduction.Value;
		if (bigNumber != 1.0)
		{
			stringBuilder.Append("SpellChargeCost".Translate());
			stringBuilder.Append(": ");
			if (bigNumber < 0.05000000074505806)
			{
				bigNumber = 0.05000000074505806;
			}
			stringBuilder.Append(((1.0 - bigNumber) * 100.0).ToReadableString());
			stringBuilder.AppendLine("%");
			flag = true;
		}
		bigNumber = GameManager.Instance.Scrolls.SpellChargingSpeed.Value;
		if (bigNumber != 1.0)
		{
			stringBuilder.Append("SpellChargeSpeed".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append(((bigNumber - 1.0) * 100.0).ToReadableString());
			stringBuilder.AppendLine("%");
			flag = true;
		}
		bigNumber = GameManager.Instance.Scrolls.EvocationEfficiency.Value;
		if (bigNumber != 1.0)
		{
			stringBuilder.Append("EvoEff".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append((bigNumber * 100.0).ToReadableString());
			stringBuilder.AppendLine("%");
			flag = true;
		}
		bigNumber = GameManager.Instance.Scrolls.EvocationDurationReduction.Value;
		if (bigNumber != 1.0)
		{
			stringBuilder.Append("EvoDuration".Translate());
			stringBuilder.Append(": ");
			stringBuilder.AppendLine(bigNumber.ToReadableString());
			flag = true;
		}
		bigNumber = GameManager.Instance.Scrolls.IncantationEfficiency.Value;
		if (bigNumber != 1.0)
		{
			stringBuilder.Append("IncaEff".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append((bigNumber * 100.0).ToReadableString());
			stringBuilder.AppendLine("%");
			flag = true;
		}
		bigNumber = GameManager.Instance.Scrolls.IncantationDuration.ApplyModOnVar(1.0) / GameManager.Instance.Scrolls.IncantationDurationReduction.Value;
		if (bigNumber != 1.0)
		{
			if (bigNumber < 1.0)
			{
				stringBuilder.Append("IncaDurationDiv".Translate());
				stringBuilder.Append(": ");
				bigNumber = 1.0 / bigNumber;
				stringBuilder.AppendLine(bigNumber.ToReadableString("F2", bigNumber.Exponent < 0));
			}
			else
			{
				stringBuilder.Append("IncaDurationInc".Translate());
				stringBuilder.Append(": ");
				stringBuilder.Append((bigNumber * 100.0).ToReadableString());
				stringBuilder.AppendLine("%");
			}
			flag = true;
		}
		bigNumber = GameManager.Instance.Scrolls.SummoningEfficiency.Value;
		if (bigNumber != 1.0)
		{
			stringBuilder.Append("SummEff".Translate());
			stringBuilder.Append(": ");
			stringBuilder.Append((bigNumber * 100.0).ToReadableString());
			stringBuilder.AppendLine("%");
			flag = true;
		}
		bigNumber = GameManager.Instance.Scrolls.SummoningDurationReduction.Value;
		if (bigNumber != 1.0)
		{
			stringBuilder.Append("SummDuration".Translate());
			stringBuilder.Append(": ");
			stringBuilder.AppendLine(bigNumber.ToReadableString());
			flag = true;
		}
		if (!flag)
		{
			stringBuilder.AppendLine("-");
		}
		stringBuilder.Append("CastRate".Translate());
		stringBuilder.Append(": ");
		stringBuilder.AppendLine((GameManager.Instance.Scrolls.CastRate.ValueFloat * GameManager.Instance.Scrolls.CastRateMult.ValueFloat).ToString("F0"));
		List<Spell> allPersistentSpells = GameManager.Instance.SpellBook.GetAllPersistentSpells();
		if (allPersistentSpells.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("PersistantCasts".Translate());
			stringBuilder.AppendLine(":");
		}
		foreach (Spell item in allPersistentSpells)
		{
			stringBuilder.Append(item.Name.Translate());
			stringBuilder.Append(": ");
			stringBuilder.AppendLine(item.Use.Value.ToReadableString("F0"));
		}
		stringBuilder.AppendLine();
		stringBuilder.Append("AccumStart".Translate());
		stringBuilder.Append(": ");
		stringBuilder.AppendLine(GameManager.Instance.Scrolls.AccumulatedCasts.Value.ToReadableString());
		if (GameManager.Instance.Scrolls.AccumCastCount.Value > 0.0)
		{
			stringBuilder.Append("AccumAndPersistantCasts".Translate());
			stringBuilder.Append(": ");
			stringBuilder.AppendLine(GameManager.Instance.Scrolls.AccumCastCount.Value.ToReadableString("F0"));
		}
		if (GameManager.Instance.Scrolls.EvoCastCount.Value > 0.0)
		{
			stringBuilder.Append("EvoCasts".Translate());
			stringBuilder.Append(": ");
			stringBuilder.AppendLine(GameManager.Instance.Scrolls.EvoCastCount.Value.ToReadableString("F0"));
		}
		info.text = stringBuilder.ToString();
	}
}
