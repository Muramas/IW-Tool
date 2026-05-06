using System.Text;
using TMPro;
using UnityEngine;

public class StatisticVisual : MonoBehaviour
{
	public bool Active;

	public TextMeshProUGUI Label_names;

	public TextMeshProUGUI Label_content;

	public TextMeshProUGUI Label_names_gather;

	public TextMeshProUGUI Label_content_gather;

	public float update_rate_per_sec = 1f;

	private float timer;

	private StringBuilder str_base;

	private string separator = " / ";

	private void OnEnable()
	{
		str_base = new StringBuilder();
		str_base.AppendLine("Bought mana sources".Translate());
		str_base.AppendLine("Bought upgrades".Translate());
		str_base.AppendLine("Starting level".Translate());
		str_base.AppendLine("Level requirement".Translate());
		str_base.AppendLine("Offline".Translate());
		str_base.AppendLine("Collectables".Translate());
		str_base.AppendLine("Played time".Translate());
		str_base.AppendLine("Character time".Translate());
		str_base.AppendLine("Pet time".Translate());
		Label_names.text = str_base.ToString();
		str_base = new StringBuilder();
		str_base.AppendLine("Mana".Translate());
		str_base.AppendLine("Void Mana".Translate());
		str_base.AppendLine("Void Entities".Translate());
		str_base.AppendLine("Clicks".Translate());
		str_base.AppendLine("Autoclicks".Translate());
		str_base.AppendLine("Spells".Translate());
		str_base.AppendLine("Shards".Translate());
		Label_names_gather.text = str_base.ToString();
		fill_label_base();
		fill_label_gather();
	}

	private void fill_label_base()
	{
		str_base = new StringBuilder();
		str_base.Append(Statistic.TotalBuildings.ValueInt);
		str_base.AppendLine();
		str_base.Append(Statistic.BoughtUpgrades.ValueInt);
		str_base.AppendLine();
		str_base.AppendLine(GameManager.Instance.CurrentHero.StartingLevel.ValueInt.ToString());
		str_base.Append("- ");
		str_base.AppendLine(GameManager.Instance.LevelReduction.ValueInt.ToString());
		str_base.Append(((GameManager.Instance.OfflineProduction.Value - 1.0) * 100.0).ToReadableString());
		str_base.AppendLine("%");
		str_base.AppendLine(Statistic.Collectables.ValueInt.ToString());
		str_base.AppendLine(Statistic.time_to_string(Statistic.TimeSession.ValueInt));
		str_base.AppendLine(Statistic.time_to_string(GameManager.Instance.CurrentHero.PlayedTime.ValueInt));
		str_base.AppendLine(Statistic.time_to_string(GameManager.Instance.CurrentPet.PlayedTime.ValueInt));
		Label_content.text = str_base.ToString();
	}

	private void fill_label_gather()
	{
		str_base = new StringBuilder();
		str_base.AppendLine(Statistic.ManaSession.Value.ToReadableString() + separator + Statistic.ManaRealm.Value.ToReadableString());
		str_base.AppendLine(Statistic.VoidManaSession.Value.ToReadableString() + separator + Statistic.VoidManaRealm.Value.ToReadableString());
		str_base.AppendLine(Statistic.ClickableCollect.Value.ToReadableString("F0") + separator + Statistic.ClickableCollectRealm.Value.ToReadableString("F0"));
		str_base.AppendLine(Statistic.Clicks.Value.ToReadableString("F0") + separator + Statistic.ClicksRealm.Value.ToReadableString("F0"));
		str_base.AppendLine(Statistic.AutoClicks.Value.ToReadableString("F0") + separator + Statistic.AutoClicksRealm.Value.ToReadableString("F0"));
		str_base.AppendLine(Statistic.CastSpell.Value.ToReadableString("F0") + separator + Statistic.CastSpellRealm.Value.ToReadableString("F0"));
		str_base.AppendLine(Statistic.ShardsSession.Value.ToReadableString() + separator + Statistic.ShardsRealm.Value.ToReadableString());
		Label_content_gather.text = str_base.ToString();
	}

	private void Update()
	{
		if (Active)
		{
			timer += Time.unscaledDeltaTime;
			if (timer >= update_rate_per_sec)
			{
				timer = 0f;
				fill_label_base();
				fill_label_gather();
			}
		}
	}
}
