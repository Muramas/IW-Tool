using UnityEngine;

public class AdsRewardManager : MonoBehaviour
{
	public GameObject AdsReward;

	public AdsBonusUI BonusProfit;

	public AdsBonusUI BonusSpawn;

	public AdsBonusUI BonusShards;

	public AdsReward Profit;

	public AdsReward SpawnBonus;

	public AdsReward Shards;

	public bool AdsAvailable;

	private void Start()
	{
		Profit.effect = new SimpleEffect();
		Profit.effect.target = GameManager.Instance.Profit;
		Profit.effect.mult = 2.0;
		Profit.Duration = 14400f;
		Profit.Description = "Increase profit per sec x2 for 4 hours";
		BonusProfit.target = Profit;
		SpawnBonus.effect = new SimpleEffect();
		SpawnBonus.effect.target = GameManager.Instance.VoidManaManager.VoidCore.SpawnSpeed;
		SpawnBonus.effect.mult = 1.5;
		SpawnBonus.Duration = 3600f;
		SpawnBonus.Description = "Increase speed spawn bonus x1.5 for 1 hour";
		BonusSpawn.target = SpawnBonus;
		Shards.effect = new SimpleEffect();
		Shards.effect.target = GameManager.Instance.Scrolls.ShardsPassive;
		Shards.effect.add = 2.0;
		Shards.effect.mult = 1.2000000476837158;
		Shards.Duration = 900f;
		Shards.Description = "Increase passive generate shards per sec by 2 and 20 % for 15 minutes";
		BonusShards.target = Shards;
	}

	public void ActivateBonusProfit()
	{
		Profit.Activate();
		BonusProfit.gameObject.SetActive(value: true);
	}

	public void ActivateBonusSpawn()
	{
		SpawnBonus.Activate();
		BonusSpawn.gameObject.SetActive(value: true);
	}

	public void ActivateBonusShards()
	{
		Shards.Activate();
		BonusShards.gameObject.SetActive(value: true);
	}

	public bool CheckAvailable()
	{
		return false;
	}

	public AdsState getState()
	{
		return AdsState.None;
	}

	public void Watch()
	{
	}
}
