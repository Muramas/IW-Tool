using UnityEngine;

public class EffectArcherTention : IEffect
{
	public TentionStepCheck Tension;

	public EffectArcherTention()
	{
		Tension = (GameManager.Instance.CurrentHero.HeroPanel.HeroMap[HeroesNames.Archer] as Archer).shooting.Tension;
	}

	public void Apply()
	{
		Tension.Reset();
	}

	public void SetEfficiency(Variable eff = null)
	{
	}

	public void SetGilding(Variable eff = null)
	{
	}

	public void Delete()
	{
	}

	public void Update()
	{
		Tension.AddProgress(Time.deltaTime);
	}

	public string Preview(string key = "")
	{
		return Tension.GetScaledProgress().ToReadableString();
	}
}
