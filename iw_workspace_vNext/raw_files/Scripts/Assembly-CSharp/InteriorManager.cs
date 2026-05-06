using UnityEngine;

public class InteriorManager
{
	public class SaveData
	{
		public BackgroundManager.SaveData back;

		public OrbManager.SaveData orb;

		public CatcherManager.SaveData hunter;

		public CustomCursorManager.SaveData cursors;
	}

	public BackgroundManager Backs;

	public OrbManager Orbs;

	public CustomCursorManager Cursors;

	[SerializeField]
	private BackgroundSpot BackSpot;

	public CatcherManager Hunter => GameManager.Instance.Shop.catcherManager;

	public void Init()
	{
		Backs = new BackgroundManager();
		Backs.Init(GlobalData.Backgroungs);
		Orbs = new OrbManager();
		Orbs.Init(GlobalData.Orbs);
		Cursors = new CustomCursorManager();
		Cursors.Init(GlobalData.Cursors, Resources.Load<CursorData>("Cursors"));
	}

	public SaveData Save()
	{
		return new SaveData
		{
			back = Backs.Save(),
			orb = Orbs.Save(),
			hunter = Hunter.SaveSkins(),
			cursors = Cursors.Save()
		};
	}

	public void Load(SaveData data)
	{
		if (data == null)
		{
			Backs.Load(null);
			Orbs.Load(null);
			Hunter.LoadSkins(null);
		}
		else
		{
			Backs.Load(data.back);
			Orbs.Load(data.orb);
			Hunter.LoadSkins(data.hunter);
			Cursors.Load(data.cursors);
		}
	}
}
