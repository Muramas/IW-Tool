using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellChoose : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public Spell Spell;

	public Image Icon;

	public Button button;

	public bool LimitedCharges = true;

	[SerializeField]
	private TextMeshProUGUI nameLabel;

	private float timer;

	[SerializeField]
	private Image selected;

	[SerializeField]
	private Image fade;

	private bool tip_active;

	public Spells SpellName => Spell.NameKey;

	public bool CursedChallenge => Spell.CursedChallenge;

	public void Init()
	{
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		Spell = GameManager.Instance.SpellBook.GetSpell(SpellName);
		Icon.sprite = GameManager.Instance.SpellBook.atlas.Get(Spell.NameKey.ToString());
	}

	public void Init(Spell sp)
	{
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		Spell = sp;
		if (Spell == null)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		Icon.sprite = GameManager.Instance.SpellBook.atlas.Get(Spell.NameKey.ToString());
		UpdateSelected();
		nameLabel.text = Spell.GetNameString(showEnh: false);
		base.gameObject.SetActive(value: true);
	}

	private void OnEnable()
	{
		if (Spell != null)
		{
			Spell.SetEfficiency();
			UpdateSelected();
			nameLabel.text = Spell.GetNameString(showEnh: false);
		}
	}

	private void OnDisable()
	{
		HideTip();
	}

	private void UpdateSelected()
	{
		selected.enabled = Spell.choice;
	}

	public void Update()
	{
		if (Spell == null)
		{
			return;
		}
		button.interactable = Spell.AvailableToUse;
		fade.enabled = !button.interactable;
		if (timer > 1f)
		{
			if (tip_active)
			{
				update_tip();
			}
			UpdateSelected();
			timer = 0f;
		}
		timer += Time.deltaTime;
	}

	public void Choose()
	{
		if (Spell == null || !Spell.choice)
		{
			Choose(false);
			UpdateSelected();
		}
	}

	public void Choose(bool onLoad = false)
	{
		Scroll scroll = GameManager.Instance.Scrolls.ChoosePanel.scroll;
		if (scroll.spell != null)
		{
			GameManager.Instance.Scrolls.RemoveAutoCast(scroll);
		}
		scroll.Clear();
		if (!GameManager.Instance.Scrolls.Scrolls.Any((Scroll x) => x.spell != null && x.spell.NameKey == Spell.NameKey))
		{
			scroll.SetSpell(Spell, onLoad);
			scroll.Icon.sprite = Icon.sprite;
			if (Spell.ShardsBuilding && !Spell.IsMax && !GameManager.Instance.Scrolls.unfilled_scrolls.Contains(scroll))
			{
				GameManager.Instance.Scrolls.unfilled_scrolls.Add(scroll);
			}
			button.interactable = false;
			Spell.choice = true;
			if (Spell.ShardsBuilding)
			{
				scroll.ProgressBar.sprite = scroll.shards_progress;
			}
			else
			{
				scroll.ProgressBar.sprite = scroll.charging_progress;
			}
			scroll.ProgressBar.color = Color.white;
			scroll.Icon.color = Color.white;
			scroll.ChangeButton.gameObject.SetActive(value: true);
			scroll.CountLabel.text = new BigNumber(Spell.chargeCount).ToReadableString("F0");
			if (Spell.OnChoose != null)
			{
				Spell.OnChoose();
			}
			Spell.ApplyPassive();
			if (Settings.AutoClose)
			{
				HideTip();
				GameManager.Instance.Scrolls.ChoosePanel.Close();
			}
		}
	}

	private string getTip()
	{
		return string.Format("SpellChooseTooltip".Translate(), Spell.GetNameString(), Spell.LevelReq.ToString(), Spell.GetTooltipDescription());
	}

	private void update_tip()
	{
		GameManager.Instance.Tip.UpdateText(getTip());
	}

	public void ShowTip()
	{
		if (Spell != null)
		{
			GameManager.Instance.Scrolls.ChoosePanel.Tip.SetText(getTip(), base.transform, new Vector3(0.7f, 0.858f, 0f));
			tip_active = true;
		}
	}

	public void HideTip()
	{
		GameManager.Instance.Scrolls.ChoosePanel.Tip.Close();
		tip_active = false;
	}

	public void Restart()
	{
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		ShowTip();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		HideTip();
	}
}
