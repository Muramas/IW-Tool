using System;
using System.Collections.Generic;

public static class ConditionFactory
{
	public delegate bool condition_function(string parameter, BigNumber argument, ref Variable result);

	public static Condition Create(condition_function func)
	{
		return new Condition
		{
			condition = func
		};
	}

	public static bool More(string parameter, BigNumber argument, ref Variable result)
	{
		bool result2 = false;
		Variable resource = GameContext.GetResource(parameter);
		if (resource != null)
		{
			result2 = resource.Value >= argument;
		}
		return result2;
	}

	public static bool Less(string parameter, BigNumber argument, ref Variable result)
	{
		bool result2 = false;
		Variable resource = GameContext.GetResource(parameter);
		if (resource != null)
		{
			result2 = resource.Value <= argument;
		}
		return result2;
	}

	public static bool Max(string parameter, BigNumber argument, ref Variable result)
	{
		bool result2 = false;
		List<BuildingVisual> buildings = GameManager.Instance.Buildings;
		if (buildings.Count > 0)
		{
			string text = "Building.";
			Variable variable = GameContext.GetResource(text + buildings[0].building.Name + "." + parameter);
			for (int i = 1; i < buildings.Count; i++)
			{
				Variable resource = GameContext.GetResource(text + buildings[i].building.Name + "." + parameter);
				if (resource.Value > variable.Value)
				{
					variable = resource;
				}
			}
			result = variable;
			result2 = true;
		}
		return result2;
	}

	public static bool Min(string parameter, BigNumber argument, ref Variable result)
	{
		bool result2 = false;
		List<BuildingVisual> buildings = GameManager.Instance.Buildings;
		if (buildings.Count > 0)
		{
			string text = "Building.";
			Variable variable = GameContext.GetResource(text + buildings[0].building.Name + "." + parameter);
			for (int i = 1; i < buildings.Count; i++)
			{
				Variable resource = GameContext.GetResource(text + buildings[i].building.Name + "." + parameter);
				if (resource.Value < variable.Value)
				{
					variable = resource;
				}
			}
			result = variable;
			result2 = true;
		}
		return result2;
	}

	public static bool PetLevel(string parameter, BigNumber argument, ref Variable result)
	{
		bool result2 = false;
		PetSlot currentPet = GameManager.Instance.CurrentPet;
		result = currentPet.Level;
		if (currentPet.Pet != null && currentPet.Pet.NameKey.ToString() == parameter && result.Value >= argument)
		{
			result2 = true;
		}
		return result2;
	}

	public static bool HeroLevel(string parameter, BigNumber argument, ref Variable result)
	{
		bool result2 = false;
		HeroSlot currentHero = GameManager.Instance.CurrentHero;
		result = currentHero.Level;
		if (currentHero.Hero.NameKey.ToString() == parameter && result.Value >= argument)
		{
			result2 = true;
		}
		return result2;
	}

	public static bool GodLevel(string parameter, BigNumber argument, ref Variable result)
	{
		God god = GameManager.Instance.Pantheon.GetGod((Gods)Enum.Parse(typeof(Gods), parameter));
		if (god == null)
		{
			return false;
		}
		result = god.Level;
		return god.Level.Value >= argument;
	}

	public static bool AscentionLevel(string parameter, BigNumber argument, ref Variable result)
	{
		HeroesNames key = (HeroesNames)Enum.Parse(typeof(HeroesNames), parameter);
		if (GameManager.Instance.Ascension.Forms.ContainsKey(key))
		{
			DemonForm demonForm = GameManager.Instance.Ascension.Forms[key];
			result = demonForm.Level;
			return demonForm.Level.Value >= argument;
		}
		return false;
	}

	public static bool SpellUse(string parameter, BigNumber argument, ref Variable result)
	{
		bool result2 = false;
		Spell spell = GameManager.Instance.SpellBook.SpellList.Find((Spell x) => x.NameKey.ToString() == parameter);
		if (spell != null)
		{
			result = spell.Use;
			if (result.Value >= argument)
			{
				result2 = true;
			}
		}
		return result2;
	}

	public static bool Achieve(string parameter, BigNumber argument, ref Variable result)
	{
		return GameManager.Instance.AchievManager.AllAchievs.Find((Achievement x) => x.Key.ToString() == parameter && x.Level == argument.ToInt())?.Unlocked ?? false;
	}
}
