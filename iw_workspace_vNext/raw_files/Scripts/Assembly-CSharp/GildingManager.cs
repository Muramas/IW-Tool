using UnityEngine;

public class GildingManager : MonoBehaviour
{
	public class GildingSave
	{
		public BigNumber resource;

		public BigNumber resourceTotal;

		public BigNumber MentalizingExp;

		public VoidGilding.VoidSave Void;

		public CatalystGilding.CatalystGildingSave Catas;

		public BuildingGilding.SaveData Buildings;

		public SpellGilding.SaveData Spells;

		public PantheonGilding.SaveData Pantheon;
	}

	public VariableBignumber Resource;

	public VariableBignumber ResourceTotal;

	public VariableBignumber SplinterIncome;

	public VariableBignumber Trial;

	public VoidGilding Void;

	public CatalystGilding Catas;

	public BuildingGilding Buildings;

	public SpellGilding Spells;

	public PantheonGilding Pantheon;

	public BuildingGildingWindow BuildingWindow;

	public SpellGildingWindow SpellWindow;

	public PantheonGildingWindow PantheonWindow;

	public EchoSpawner EchoSpawner;

	public GildingWindow window;

	private Skill mentalizing;

	public void Init()
	{
		Void = new VoidGilding();
		Buildings = new BuildingGilding(GlobalData.BuildingGilding);
		BuildingWindow.Init();
		Spells = new SpellGilding();
		SpellWindow.Init();
		Pantheon = new PantheonGilding(GlobalData.PantheonGilding);
		PantheonWindow.Init();
		Resource = new VariableBignumber(0.0);
		ResourceTotal = new VariableBignumber(0.0);
		SplinterIncome = new VariableBignumber(1.0);
		Trial = new VariableBignumber(0.0);
		GameContext.ContextAddResource(ResourceBase.SplinterIncome.ToString(), SplinterIncome);
		GameContext.ContextAddResource("Gilding.SplintersTotal", ResourceTotal);
		GameContext.ContextAddResource("Gilding.Trial", Trial);
		EchoSpawner.Init(Void);
		Catas.Init();
		mentalizing = GameManager.Instance.SkillManager.Get(SkillManager.SkillNames.Mentalizing);
	}

	public BigNumber GetSplinterIncome()
	{
		return SplinterIncome.Value * mentalizing.GetBonus();
	}

	public BigNumber AddSplinter(BigNumber value)
	{
		BigNumber bigNumber = value * GetSplinterIncome();
		Resource.Change(bigNumber);
		ResourceTotal.Change(bigNumber);
		mentalizing.AddExp(bigNumber / 1000.0);
		return bigNumber;
	}

	public BigNumber AddSplinterConst(BigNumber value)
	{
		Resource.Change(value);
		ResourceTotal.Change(value);
		mentalizing.AddExp(value / 1000.0);
		return value;
	}

	public void RealmChange()
	{
		Clear(full: false);
		Spells.RealmChange();
		Buildings.Deactivate();
		Pantheon.PreExile();
	}

	public void Clear(bool full = true)
	{
		if (full)
		{
			Resource.SetValue(0.0);
			ResourceTotal.SetValue(0.0);
		}
		EchoSpawner.Restart();
		Void.Clear(full);
	}

	public GildingSave Save()
	{
		return new GildingSave
		{
			resource = Resource.Value,
			resourceTotal = ResourceTotal.Value,
			Void = Void.Save(),
			Catas = Catas.Save(),
			Buildings = Buildings.Save(),
			MentalizingExp = mentalizing.totalExp,
			Spells = Spells.Save(),
			Pantheon = Pantheon.Save()
		};
	}

	public void Load(GildingSave save)
	{
		Clear();
		if (save != null)
		{
			Resource.SetValue(save.resource);
			ResourceTotal.SetValue(save.resourceTotal);
			Void.Load(save.Void);
			Catas.Load(save.Catas);
			Buildings.Load(save.Buildings);
			mentalizing.Load(save.MentalizingExp);
			Spells.Load(save.Spells);
			Pantheon.Load(save.Pantheon);
		}
	}

	public void PreLoad()
	{
		Buildings.ResetFull();
		Catas.Restart();
		Void.Deactivate();
		Spells.PreLoad();
		Pantheon.ResetFull();
	}

	public void PostLoad()
	{
		GameManager.Instance.BonusSpawner.visual.SetActive(!GameManager.Instance.Paragon.GildingIsAvailable);
		EchoSpawner.visual.SetActive(GameManager.Instance.Paragon.GildingIsAvailable);
		BuildingWindow.PostLoad();
		CheckAvailable();
	}

	public void PreExile()
	{
		Paragons paragon = GameManager.Instance.Paragon;
		if (paragon.GildingBuildingsIsAvailable)
		{
			Buildings.Deactivate();
		}
		if (paragon.GildingIsAvailable)
		{
			Void.Deactivate();
		}
		if (paragon.GildingSpellsIsAvailable)
		{
			Spells.PreExile();
		}
		if (paragon.GildingPantheonIsAvailable)
		{
			Pantheon.PreExile();
		}
	}

	public void CheckAvailable(bool reset = false)
	{
		Paragons paragon = GameManager.Instance.Paragon;
		if (paragon.GildingIsAvailable)
		{
			Void.Activate();
		}
		if (paragon.GildingSpellsIsAvailable)
		{
			Spells.PostExile();
		}
		if (paragon.GildingBuildingsIsAvailable)
		{
			Buildings.Activate(reset);
		}
		if (paragon.GildingPantheonIsAvailable)
		{
			Pantheon.Activate(reset);
		}
	}

	public BigNumber GetOnTrial()
	{
		return AddSplinter(Trial.Value);
	}
}
