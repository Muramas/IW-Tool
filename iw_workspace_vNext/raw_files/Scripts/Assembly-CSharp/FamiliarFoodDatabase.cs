using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[CreateAssetMenu(menuName = "ScriptableObjects/FamiliarFoodDatabase")]
public class FamiliarFoodDatabase : ScriptableObject
{
	[SerializeField]
	private List<FamiliarFood> allFoods = new List<FamiliarFood>();

	public FamiliarFood GetFoodForTag(FamiliarTags tag)
	{
		if (allFoods == null || allFoods.Count == 0)
		{
			return null;
		}
		List<FamiliarFood> list = allFoods.Where((FamiliarFood f) => f != null && f.Tag == tag).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		return list.FirstOrDefault();
	}

	public FamiliarFood GetFoodById(int id)
	{
		if (allFoods == null)
		{
			return null;
		}
		return allFoods.FirstOrDefault((FamiliarFood f) => f != null && f.Id == id);
	}
}
