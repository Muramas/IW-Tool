using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroChooseWindow : MonoBehaviour
{
	public Button Accept;

	public Transform Arrow;

	public IClassChoose hero;

	public TextMeshProUGUI Description;

	public TextMeshProUGUI Reqs;

	public Gallery Gallery;

	[SerializeField]
	private Button DescrButton;

	[SerializeField]
	private Button SkinsButton;

	[SerializeField]
	private GameObject Descr;

	[SerializeField]
	private GameObject Skins;

	public TextMeshProUGUI Label;

	private float timer;

	private void OnEnable()
	{
		if (!(hero == null))
		{
			StartCoroutine(alignArrow());
		}
	}

	public void Open(IClassChoose h)
	{
		hero = h;
		UpdateReqs();
		if (Skins.activeSelf)
		{
			OpenSkins();
		}
		else
		{
			OpenDescription();
		}
		HeroesNames heroName = hero.HeroName;
		if (GameManager.Instance.CurrentHero.HeroPanel.HeroMap.ContainsKey(heroName))
		{
			Label.text = GameManager.Instance.CurrentHero.HeroPanel.HeroMap[heroName].Name;
		}
		else
		{
			Label.text = GameManager.Instance.Ascension.Forms[heroName].Name;
		}
		update();
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(alignArrow());
		}
	}

	public void Close()
	{
		hero = null;
	}

	private void Update()
	{
		if (Time.timeScale != 0f)
		{
			timer += Time.deltaTime / Time.timeScale;
			if (timer > 1f)
			{
				update();
				timer = 0f;
			}
		}
	}

	public void OpenDescription()
	{
		SkinsButton.interactable = true;
		DescrButton.interactable = false;
		UpdateDescription();
		Skins.SetActive(value: false);
		Descr.SetActive(value: true);
	}

	public void OpenSkins()
	{
		SkinsButton.interactable = false;
		DescrButton.interactable = true;
		Gallery.Open(hero.HeroName);
		Skins.SetActive(value: true);
		Descr.SetActive(value: false);
	}

	private void update()
	{
		if (!(hero == null))
		{
			Accept.interactable = hero.CheckAvailable();
		}
	}

	private void UpdateDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Feature".Translate());
		stringBuilder.AppendLine(": ");
		stringBuilder.AppendLine(hero.GetFeature());
		stringBuilder.AppendLine();
		stringBuilder.Append("Lore".Translate());
		stringBuilder.AppendLine(": ");
		stringBuilder.AppendLine(hero.GetDescription());
		Description.text = stringBuilder.ToString();
	}

	private void UpdateReqs()
	{
		Reqs.text = hero.GetDescriptionReqText();
	}

	public void Choose()
	{
		if (Settings.ConfirmMessageChoose)
		{
			GameManager.Instance.ConfirmWindow.Open("ConfirmChangeClass".Translate(), delegate
			{
				hero.Choose();
			});
		}
		else
		{
			hero.Choose();
		}
	}

	private IEnumerator alignArrow()
	{
		yield return null;
		Arrow.position = hero.transform.position + new Vector3(0f, 0.35f, 0f);
	}
}
