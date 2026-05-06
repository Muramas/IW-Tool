using System.Collections.Generic;
using UnityEngine;

public class CraftingLog : MonoBehaviour
{
	[SerializeField]
	private int max;

	[SerializeField]
	private Transform content;

	[SerializeField]
	private LogBlock prefab;

	private List<LogBlock> blocks;

	private Dictionary<string, LogBlock> mapBlock;

	private void Awake()
	{
		blocks = new List<LogBlock>();
		mapBlock = new Dictionary<string, LogBlock>();
	}

	private void OnEnable()
	{
		Clear();
	}

	public void Throw(Sprite sprite, BigNumber message, string postfix = null, string key = null, string format = "F0")
	{
		if (sprite == null && string.IsNullOrEmpty(key))
		{
			LogBlock free = GetFree();
			free.Show(sprite, message, postfix, format);
			blocks.Add(free);
		}
		if (string.IsNullOrEmpty(key))
		{
			key = sprite.name;
		}
		if (mapBlock != null)
		{
			LogBlock free;
			if (mapBlock.ContainsKey(key))
			{
				free = mapBlock[key];
				free.Refresh(message, postfix, format);
				return;
			}
			free = GetFree();
			free.Show(sprite, message, postfix, key, format);
			blocks.Add(free);
			mapBlock.Add(key, free);
		}
	}

	public void Clear()
	{
		if (blocks == null || blocks.Count == 0)
		{
			return;
		}
		foreach (LogBlock block in blocks)
		{
			Object.Destroy(block.gameObject);
		}
		blocks = new List<LogBlock>();
		mapBlock = new Dictionary<string, LogBlock>();
	}

	private LogBlock GetFree()
	{
		if (blocks == null)
		{
			blocks = new List<LogBlock>();
		}
		LogBlock logBlock;
		if (blocks.Count < max)
		{
			logBlock = Object.Instantiate(prefab, content);
			logBlock.transform.localScale = Vector3.one;
			return logBlock;
		}
		logBlock = blocks[0];
		if (mapBlock.ContainsValue(logBlock))
		{
			mapBlock.Remove(logBlock.key);
		}
		blocks.RemoveAt(0);
		return logBlock;
	}
}
