using System.Globalization;

public class EffectReapPetExp : IEffect
{
	private SimpleEffect e;

	public float mod;

	private VariableBignumber v;

	private Variable efficiency;

	private Variable gilding;

	public BigNumber reap;

	public EffectReapPetExp(SimpleEffect effect, float k)
	{
		e = effect;
		mod = k;
		v = new VariableBignumber();
		e.parameter = v;
		reap = 0.0;
	}

	public void Apply()
	{
		reap = 0.0;
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			reap = GameManager.Instance.CurrentPet.Pet.TotalExp.Value * mod;
			GameManager.Instance.CurrentPet.Pet.TotalExp.Change(-reap);
			GameManager.Instance.CurrentPet.Pet.RecalculateLevel();
		}
		v.SetValue(reap);
		e.SetEfficiency(efficiency);
		e.SetGilding(gilding);
		e.Apply();
	}

	public void SetEfficiency(Variable eff = null)
	{
		efficiency = eff;
	}

	public void SetGilding(Variable eff = null)
	{
		gilding = eff;
	}

	public void Delete()
	{
		e.Delete();
	}

	public void Update()
	{
	}

	public string Preview(string key = "")
	{
		if (key == "t")
		{
			return (mod * 100f).ToString("F2", CultureInfo.InvariantCulture) + "%";
		}
		if (e.IsActive)
		{
			return e.Preview(key);
		}
		BigNumber value = 0.0;
		if (GameManager.Instance.CurrentPet.Pet != null)
		{
			value = GameManager.Instance.CurrentPet.Pet.TotalExp.Value * mod;
		}
		v.SetValue(value);
		e.SetEfficiency(efficiency);
		e.SetGilding(gilding);
		if (key == "t")
		{
			return (mod * 100f).ToString("F2", CultureInfo.InvariantCulture) + "%";
		}
		return e.Preview(key);
	}
}
