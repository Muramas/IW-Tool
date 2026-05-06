using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerVisual : MonoBehaviour
{
	public TopPanelLabel ManaLabel;

	public TopPanelLabel ManaPPS;

	public TopPanelLabel VoidManaLabel;

	public TopPanelLabel ShadowLabel;

	public TopPanelLabel MystsLabel;

	public TopPanelLabel ClickProfitLabel;

	public TopPanelLabel CritChanceLabel;

	public TopPanelLabel CritProfitLabel;

	public TopPanelLabel ShardsLabel;

	public TextMeshProUGUI VersionLabel;

	public GameObject LoadErrorWindow;

	public Image VoidIcon;

	public TextMeshProUGUI Tip;

	public GameObject CloudSaveMessage;

	private GameManager manager;

	private ShadowEnergyManager shadowManager;

	private bool Shadows;

	private float timer;

	private void Start()
	{
		manager = GameManager.Instance;
		VersionLabel.text = "Version " + manager.Version;
	}

	public void EnableShadows(ShadowEnergyManager shadowManager)
	{
		this.shadowManager = shadowManager;
		Shadows = true;
		ShadowLabel.gameObject.SetActive(value: true);
		VoidManaLabel.gameObject.SetActive(value: false);
	}

	public void DisableShadows()
	{
		Shadows = false;
		ShadowLabel.gameObject.SetActive(value: false);
		VoidManaLabel.gameObject.SetActive(value: true);
	}

	private void Update()
	{
		timer += Time.unscaledDeltaTime;
		if ((double)timer > 0.25)
		{
			update_text();
			timer = 0f;
		}
	}

	public void update_text()
	{
		ManaLabel.SetText(manager.Mana.Value.ToReadableString());
		ManaPPS.SetText(manager.PPSFromBuildings.ApplyModOnVar(manager.PPS.Value).ToReadableString());
		if (!Shadows)
		{
			VoidManaLabel.SetText(manager.VoidMana.Value.ToReadableString());
		}
		else
		{
			ShadowLabel.SetText(shadowManager.ShadowEnergy.Value.ToReadableString());
		}
		ClickProfitLabel.SetText(manager.Orb.click_profit.Value.ToReadableString());
		CritChanceLabel.SetText(manager.Orb.GetCritChange.ToString("F2", CultureInfo.InvariantCulture) + "%");
		CritProfitLabel.SetText((manager.Orb.crit_profit.Value * 100.0).ToReadableString() + "%");
		ShardsLabel.SetText(manager.Scrolls.ShardsPassive.Value.ToReadableString());
	}

	public void LoadError()
	{
		LoadErrorWindow.SetActive(value: true);
	}
}
