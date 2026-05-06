using System;
using System.Collections.Generic;
using Mounts;

public class MountManager
{
	public class SaveData
	{
		public Dictionary<int, float> Progress;
	}

	public class MountFormat
	{
		public string ID;

		public string Name;

		public string Sprite;

		public string Description;

		public string Source;

		public string Enchant;

		public string EValue;

		public string Lore;
	}

	private List<Mount> mounts;

	private Mount trial;

	private Mount expedition;

	private Mount experiment;

	private const string SourceCraft = "craft";

	private const string SourceTrial = "trial";

	private const string SourceExperiment = "experiment";

	private const string SourceExpedition = "expedition";

	private const string SourceEvent = "event";

	private bool isLoaded;

	public void Init()
	{
		mounts = new List<Mount>();
		Add(new Zenith());
		Add(new Basilisk());
		Add(new RubedoEngine());
		Add(new FeyGlider());
		Add(new BlessedTapestry());
		Add(new Nulldragon());
		Add(new RitualDisk());
		Add(new Broom());
		Add(new Spidey());
		Add(new Echo());
		TrialManager trials = GameManager.Instance.Trials;
		trials.OnComplete = (Action<Trial>)Delegate.Combine(trials.OnComplete, new Action<Trial>(OnTrial));
		CraftingMenu craftingMenu = GameManager.Instance.Craft.window.craftingMenu;
		craftingMenu.OnExperimentCost = (Action<Dictionary<CraftResource, BigNumber>>)Delegate.Combine(craftingMenu.OnExperimentCost, new Action<Dictionary<CraftResource, BigNumber>>(OnExperiment));
		ExpeditionManager instance = ExpeditionManager.Instance;
		instance.OnWin = (Action<int>)Delegate.Combine(instance.OnWin, new Action<int>(OnExpedition));
		AchievementManager achievManager = GameManager.Instance.AchievManager;
		achievManager.OnTriumphUnlock = (Action<Triumph>)Delegate.Combine(achievManager.OnTriumphUnlock, new Action<Triumph>(OnTriumph));
	}

	public Mount GetMount(int id)
	{
		return mounts.Find((Mount x) => x.ID == id);
	}

	public void UnlockItem(Mount m)
	{
		m.unlockProgress = 1f;
		GameManager.Instance.Craft.UnlockItem(m);
		GameManager.Instance.Craft.window.ChangeFilter();
		GameManager.Instance.AchievManager.unlock_message.Unlock(string.Format("MountUnlock".Translate(), m.Name), m.Icon);
	}

	public void ReUnlockItems()
	{
		foreach (Mount mount in mounts)
		{
			if (mount.unlockProgress >= 1f && !GameManager.Instance.Craft.AvailableItems.Contains(mount))
			{
				UnlockItem(mount);
			}
		}
	}

	public void OnTriumph(Triumph t)
	{
		if (!isLoaded)
		{
			return;
		}
		if (t.Key == AchievementKey.T_Speedrun_4)
		{
			Mount mount = mounts.Find((Mount x) => x.ID == 2006);
			if (mount.unlockProgress < 1f)
			{
				UnlockItem(mount);
			}
		}
		if (t.Key == AchievementKey.T_Memory)
		{
			Mount mount2 = mounts.Find((Mount x) => x.ID == 2007);
			if (mount2.unlockProgress < 1f)
			{
				UnlockItem(mount2);
			}
		}
	}

	public void OnTrial(Trial t)
	{
		if (trial == null || !GameManager.Instance.Paragon.MountIsAvailable)
		{
			return;
		}
		float num = 0f;
		num = ((t.ID == 3) ? 1f : ((t.ID == 0) ? 0.5f : ((t.ID == 1) ? 0.3f : ((t.ID != 4) ? 0.1f : 0.25f))));
		num *= 0.1f;
		trial.unlockProgress += num;
		if (trial.unlockProgress >= 1f)
		{
			UnlockItem(trial);
			trial = mounts.Find((Mount x) => x.source == "trial" && x.unlockProgress < 1f);
		}
	}

	public void OnExperiment(Dictionary<CraftResource, BigNumber> e)
	{
		if (experiment == null || !GameManager.Instance.Paragon.MountIsAvailable)
		{
			return;
		}
		float num = (e[CraftResource.Red] * 2.0 + e[CraftResource.Blue] * 1.5 + e[CraftResource.Yellow] + e[CraftResource.Green]).ToFloat() / 200000f;
		experiment.unlockProgress += num;
		if (experiment.unlockProgress >= 1f)
		{
			UnlockItem(experiment);
			experiment = mounts.Find((Mount x) => x.source == "experiment" && x.unlockProgress < 1f);
		}
	}

	public void OnExpedition(int lvl)
	{
		if (expedition == null || !GameManager.Instance.Paragon.MountIsAvailable)
		{
			return;
		}
		float num = 0f;
		if ((float)ExpeditionManager.Instance.Character.Level - 10f <= (float)lvl)
		{
			num = 0.002f * (float)lvl / (float)ExpeditionManager.Instance.Character.Level;
		}
		expedition.unlockProgress += num;
		if (expedition.unlockProgress >= 1f)
		{
			UnlockItem(expedition);
			expedition = mounts.Find((Mount x) => x.source == "expedition" && x.unlockProgress < 1f);
		}
	}

	private void Add(Mount mount)
	{
		mount.Init();
		GameManager.Instance.Craft.AllItems.Add(mount);
		mounts.Add(mount);
	}

	public void CheckCrafted()
	{
		CraftManager craft = GameManager.Instance.Craft;
		if (mounts == null)
		{
			return;
		}
		foreach (Mount mount in mounts)
		{
			if (mount.source == "craft" && mount.Tier < 5 && craft.window.doll.SlotIsAvailable(SlotKey.Mount) && !craft.AvailableItems.Contains(mount))
			{
				craft.DropList.Add(mount);
			}
		}
	}

	public SaveData Save()
	{
		SaveData saveData = new SaveData();
		saveData.Progress = new Dictionary<int, float>();
		foreach (Mount mount in mounts)
		{
			if (mount.unlockProgress > 0f)
			{
				saveData.Progress.Add(mount.ID, mount.unlockProgress);
			}
		}
		return saveData;
	}

	public void Load(SaveData data)
	{
		ResetProgress();
		if (data != null)
		{
			foreach (KeyValuePair<int, float> v in data.Progress)
			{
				Mount mount = mounts.Find((Mount x) => x.ID == v.Key);
				if (mount != null)
				{
					mount.unlockProgress = v.Value;
				}
			}
		}
		trial = mounts.Find((Mount x) => x.source == "trial" && x.unlockProgress < 1f);
		expedition = mounts.Find((Mount x) => x.source == "expedition" && x.unlockProgress < 1f);
		experiment = mounts.Find((Mount x) => x.source == "experiment" && x.unlockProgress < 1f);
		isLoaded = true;
	}

	private void ResetProgress()
	{
		foreach (Mount mount in mounts)
		{
			mount.unlockProgress = 0f;
		}
	}
}
