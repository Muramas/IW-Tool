using System.Collections.Generic;
using UnityEngine;

public class OrbWindow : MonoBehaviour
{
	public SpriteAtlas atlas;

	[SerializeField]
	private OrbChoose prefab;

	[SerializeField]
	private List<OrbChoose> blocks;

	[SerializeField]
	private Transform content;

	[SerializeField]
	private OrbPreview Preview;

	public void Init()
	{
		if (blocks != null && blocks.Count > 0)
		{
			return;
		}
		blocks = new List<OrbChoose>();
		foreach (KeyValuePair<int, OrbManager.Orb> orb in GameManager.Instance.Interior.Orbs.Orbs)
		{
			OrbChoose orbChoose = Object.Instantiate(prefab, content);
			orbChoose.Init(orb.Value, this);
			blocks.Add(orbChoose);
		}
		Preview.SetChoose(UpdateAll, UpdateAll);
	}

	private void OnEnable()
	{
		GameManager.Instance.Orb.VisualContainer.SetActive(value: false);
		GameManager.Instance.CorruptionManager.Visual.SetActive(value: false);
	}

	private void OnDisable()
	{
		GameManager.Instance.Orb.VisualContainer.SetActive(value: true);
		GameManager.Instance.CorruptionManager.Visual.SetActive(value: true);
	}

	public void Open()
	{
		Init();
		base.gameObject.SetActive(value: true);
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	public void OpenPreview(OrbChoose block)
	{
		Preview.Open(block.ID, block.GetSprite());
	}

	public void ClosePreview()
	{
		Preview.gameObject.SetActive(value: false);
	}

	private void UpdateAll(int id)
	{
		foreach (OrbChoose block in blocks)
		{
			block.UpdateVisual();
		}
	}
}
