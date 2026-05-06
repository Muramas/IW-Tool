using System.Collections.Generic;
using UnityEngine;

public class HeroChoosePanel : MonoBehaviour
{
	public Sprite Normal;

	public Sprite Choosen;

	public List<HeroesChoose> Heroes;

	public HeroChooseWindow ChooseWindow;

	public AttributePanel Attributes;

	public Dictionary<HeroesNames, Hero> HeroMap;

	public DisableCanvas canvas;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Close();
		}
	}

	public void InitMap()
	{
		HeroMap = new Dictionary<HeroesNames, Hero>
		{
			{
				HeroesNames.Apprentice,
				new Apprentice()
			},
			{
				HeroesNames.Druid,
				new Druid()
			},
			{
				HeroesNames.Demonologist,
				new Demonologist()
			},
			{
				HeroesNames.Necromancer,
				new Necromancer()
			},
			{
				HeroesNames.Arcanist,
				new Arcanist()
			},
			{
				HeroesNames.Prodigy,
				new Prodigy()
			},
			{
				HeroesNames.Voidmancer,
				new Voidmancer()
			},
			{
				HeroesNames.Exorcist,
				new Exorcist()
			},
			{
				HeroesNames.Chronomancer,
				new Chronomancer()
			},
			{
				HeroesNames.Umbramancer,
				new Umbramancer()
			},
			{
				HeroesNames.Alchemist,
				new Alchemist()
			},
			{
				HeroesNames.Ironsoul,
				new Ironsoul()
			},
			{
				HeroesNames.Abolisher,
				new Abolisher()
			},
			{
				HeroesNames.Shaman,
				new Shaman()
			},
			{
				HeroesNames.Heretic,
				new Heretic()
			},
			{
				HeroesNames.Oni,
				new Oni()
			},
			{
				HeroesNames.Archon,
				new ArcT2()
			},
			{
				HeroesNames.Temporalist,
				new ProdT2()
			},
			{
				HeroesNames.Desolator,
				new Desolator()
			},
			{
				HeroesNames.Cryomancer,
				new Frostmage()
			},
			{
				HeroesNames.Nosferatu,
				new Nosferatu()
			},
			{
				HeroesNames.Artificer,
				new Artificer()
			},
			{
				HeroesNames.Shapeshifter,
				new Shapeshifter()
			},
			{
				HeroesNames.Archer,
				new Archer()
			}
		};
	}

	public void Init()
	{
		foreach (HeroesChoose hero in Heroes)
		{
			hero.Init();
		}
	}

	public void PostInit()
	{
		foreach (HeroesChoose hero in Heroes)
		{
			hero.Hero.PostInit();
		}
	}

	public void Restart()
	{
		foreach (HeroesChoose hero in Heroes)
		{
			hero.Restart();
		}
	}

	public HeroesChoose GetHero(HeroesNames key)
	{
		HeroesChoose result = Heroes[0];
		for (int i = 0; i < Heroes.Count; i++)
		{
			if (Heroes[i].Hero.NameKey == key)
			{
				result = Heroes[i];
				break;
			}
		}
		return result;
	}

	public void Open()
	{
		IClassChoose classChoose = null;
		classChoose = (GameManager.Instance.Ascension.IsActive ? ((IClassChoose)GameManager.Instance.Ascension.panel.Get(GameManager.Instance.Ascension.Choosen)) : ((IClassChoose)Heroes.Find((HeroesChoose x) => x.Hero == GameManager.Instance.CurrentHero.Hero)));
		ChooseWindow.Open(classChoose);
		foreach (HeroesChoose hero in Heroes)
		{
			hero.Check();
		}
		base.gameObject.SetActive(value: true);
		canvas.On();
		Attributes.gameObject.SetActive(value: false);
	}

	public void Close()
	{
		ChooseWindow.Close();
		base.gameObject.SetActive(value: false);
		canvas.Off();
	}

	public HeroesChoose Find(HeroesNames key)
	{
		return Heroes.Find((HeroesChoose x) => x.HeroName == key);
	}

	public void OpenEquipment()
	{
		Close();
		GameManager.Instance.Craft.window.Open();
	}
}
