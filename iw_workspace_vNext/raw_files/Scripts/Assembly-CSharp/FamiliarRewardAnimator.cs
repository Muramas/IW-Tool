using System.Collections;
using UnityEngine;

public class FamiliarRewardAnimator : MonoBehaviour
{
	[SerializeField]
	private AnimationCurve scaleCurve;

	[SerializeField]
	private float duration = 0.35f;

	[SerializeField]
	private float scaleMultiplier = 1.15f;

	private Coroutine running;

	public void Play(Transform target, float offset = 1f)
	{
		if (!(target == null))
		{
			if (running != null)
			{
				StopCoroutine(running);
			}
			running = StartCoroutine(ScaleRoutine(target, offset));
		}
	}

	private IEnumerator ScaleRoutine(Transform target, float offset)
	{
		float targetScale = 1f + (scaleMultiplier - 1f) * (0.25f + offset * 0.75f);
		float time = 0f;
		while (time < duration)
		{
			float time2 = time / duration;
			float t = ((scaleCurve != null) ? scaleCurve.Evaluate(time2) : 1f);
			target.localScale = Vector3.one * Mathf.Lerp(1f, targetScale, t);
			time += Time.unscaledDeltaTime;
			yield return null;
		}
		target.localScale = Vector3.one;
		running = null;
	}
}
