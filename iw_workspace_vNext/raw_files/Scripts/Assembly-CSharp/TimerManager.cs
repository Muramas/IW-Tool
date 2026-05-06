using System;
using System.Collections.Generic;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
	public List<Timer> active;

	public List<Timer> pool;

	public void Init()
	{
		active = new List<Timer>();
		pool = new List<Timer>();
	}

	public Timer GetOneShot(float time, Action func, bool ignore_scale = false)
	{
		Timer timer = pool.Find((Timer x) => x is TimerOneShot);
		if (timer == null)
		{
			timer = new TimerOneShot(this);
		}
		timer.Start(time, func, ignore_scale);
		return timer;
	}

	public Timer GetPeriodical(float _period, Action func, bool ignore_scale = false)
	{
		Timer timer = pool.Find((Timer x) => x is TimerPeriodical);
		if (timer == null)
		{
			timer = new TimerPeriodical(this);
		}
		timer.Start(_period, func, ignore_scale);
		return timer;
	}

	public Timer GetPeriodical(float time, float _period, Action func, Action off, bool ignore_scale = false)
	{
		Timer timer = pool.Find((Timer x) => x is TimerPeriodicalLimited);
		if (timer == null)
		{
			timer = new TimerPeriodicalLimited(this);
		}
		(timer as TimerPeriodicalLimited).Start(time, _period, func, off, ignore_scale);
		return timer;
	}

	private void LateUpdate()
	{
		List<Timer> list = active;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].Update();
		}
	}
}
