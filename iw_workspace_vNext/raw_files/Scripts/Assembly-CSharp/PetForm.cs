using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PetForm : DemonForm
{
	private SimpleEffect godAP;

	private BigNumber godMainAP;

	public override void Init()
	{
		base.Init();
		NameKey = HeroesNames.Dread;
		base.Name = "The Dread";
		RequedClasses.Add(HeroesNames.Temporalist);
		RequedClasses.Add(HeroesNames.Heretic);
		RequedClasses.Add(HeroesNames.Desolator);
		Feature = "Dread Feature";
		Description = "Dread Lore";
		ExpDescription = "Dread XP";
		spells = new List<Spells>
		{
			Spells.TrueSorcery,
			Spells.Voidbolt,
			Spells.ArtificialMuse,
			Spells.ManifestTwistedReality
		};
		godAP = new SimpleEffect(GameManager.Instance.Pantheon.MainAP);
		Skills.Add(godAP);
	}

	public override void ApplyEffects()
	{
		base.ApplyEffects();
		GameManager.Instance.CurrentPet.PetPanel.EnableSecond();
	}

	public override void DisableAll()
	{
		GameManager.Instance.CurrentPet.PetPanel.DisableSecond();
		base.DisableAll();
	}

	public override void update_effect()
	{
		BigNumber formPower = GetFormPower();
		godMainAP = 1.0 + 0.00800000037997961 * formPower;
		godAP.mult = godMainAP;
		godAP.Update();
	}

	public override BigNumber GetFormPower()
	{
		return Level.Value * GameManager.Instance.Ascension.AbilityPower.Value.Pow(0.5);
	}

	public override void update()
	{
		BigNumber bigNumber = 0.0;
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			bigNumber += GameManager.Instance.CurrentPet.Pet.Level.Value.Pow(0.5);
		}
		if (GameManager.Instance.CurrentPet.PetPanel.secondSlot.Pet != null)
		{
			bigNumber += GameManager.Instance.CurrentPet.PetPanel.secondSlot.Pet.Level.Value.Pow(0.5);
		}
		if (bigNumber > 1.0)
		{
			AddExp(1.36f * Time.unscaledDeltaTime * bigNumber);
		}
	}

	public override string TipText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.TipText());
		stringBuilder.AppendLine(string.Format("Dread Ability".Translate(), GetBonusMult(godMainAP)));
		stringBuilder.Append("AscensionAP".Translate());
		stringBuilder.Append(" ");
		stringBuilder.Append(GetBonusMult(GameManager.Instance.Ascension.AbilityPower.Value + 1.0));
		return stringBuilder.ToString();
	}
}
