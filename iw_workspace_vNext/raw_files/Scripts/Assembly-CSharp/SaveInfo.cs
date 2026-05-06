using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SaveInfo : MonoBehaviour
{
	public Text label;

	public string save_cloud = "Cloud save successful";

	public string save_complete = "Save complete";

	public string load_complete = "Load complete";

	public void Open(string text)
	{
		label.text = "";
		StopAllCoroutines();
		label.text = text.Translate();
		base.gameObject.SetActive(value: true);
		StartCoroutine(wait(2f, delegate
		{
			base.gameObject.SetActive(value: false);
		}));
	}

	private IEnumerator wait(float t, Action action)
	{
		float timer = 0f;
		while (timer < t)
		{
			yield return null;
			if (Time.timeScale != 0f)
			{
				timer += Time.unscaledDeltaTime;
			}
		}
		action();
	}
}
