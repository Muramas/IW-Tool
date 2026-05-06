using System.Collections.Generic;
using UnityEngine;

public class FamiliarObj : MonoBehaviour
{
	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private List<Sprite> frames;

	[SerializeField]
	private float frameTime = 0.2f;

	[SerializeField]
	private AnimationCurve curve;

	[SerializeField]
	private float floatingAmplitude = 0.5f;

	[SerializeField]
	private float floatingPeriod = 2f;

	private float timer;

	private float floatingTimer;

	private int currentFrame;

	private bool movingForward = true;

	private void Start()
	{
		if (frames.Count > 0)
		{
			spriteRenderer.sprite = frames[0];
		}
		floatingPeriod = Random.Range(floatingPeriod - 0.025f, floatingPeriod + 0.025f);
		floatingTimer = Random.Range(0f, floatingPeriod);
	}

	private void Update()
	{
		timer += Time.unscaledDeltaTime;
		if (timer >= frameTime)
		{
			timer -= frameTime;
			AdvanceFrame();
		}
		floatingTimer += Time.unscaledDeltaTime;
		if (floatingTimer >= floatingPeriod)
		{
			floatingTimer -= floatingPeriod;
		}
		base.transform.localPosition = new Vector3(0f, floatingAmplitude * curve.Evaluate(floatingTimer / floatingPeriod), 0f);
	}

	private void AdvanceFrame()
	{
		int num = (movingForward ? 1 : (-1));
		int num2 = currentFrame + num;
		if (num2 >= frames.Count)
		{
			movingForward = false;
			num2 = currentFrame - 1;
		}
		else if (num2 < 0)
		{
			movingForward = true;
			num2 = currentFrame + 1;
		}
		currentFrame = num2;
		spriteRenderer.sprite = frames[currentFrame];
	}

	public void SetColor(Color color)
	{
		spriteRenderer.color = color;
	}
}
