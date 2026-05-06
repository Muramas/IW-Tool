using CardGame;
using UnityEngine;

public class ExperimentKeys : Experiment
{
	[SerializeField]
	private LootTable loot;

	[SerializeField]
	private Vector2 dropRange;

	public override void Init(CraftingMenu menu, int salt)
	{
		base.Init(menu, salt);
		loot.Init();
	}

	protected override void GiveReward()
	{
		int? random = loot.GetRandom(GetSeed());
		float num = (float)Random.Range((int)dropRange.x, (int)dropRange.y + 1) * ExpeditionManager.Instance.Keys.KeyIncome.ValueFloat * GameManager.Instance.Craft.ExperimentCraftEfficiency.Value.ToFloat();
		int num2;
		if (random >= 10)
		{
			num2 = 10;
			num *= 2.1f;
			if (random == 11)
			{
				num *= 2f;
			}
			else if (random == 12)
			{
				num *= 3f;
			}
		}
		else
		{
			num2 = random.Value;
		}
		ExpeditionManager.Instance.Keys.AddKey((Keys)num2, Mathf.FloorToInt(num));
		menu.log.Throw(menu.GetSprite(DustGambling.DropType.Key, num2), Mathf.FloorToInt(num));
	}

	protected override bool IsInteractable()
	{
		return CheckCost();
	}
}
