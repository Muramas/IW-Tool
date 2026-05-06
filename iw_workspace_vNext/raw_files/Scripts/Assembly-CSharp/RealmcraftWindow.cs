using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RealmcraftWindow : Window
{
	[SerializeField]
	private DisableCanvas canvas;

	[SerializeField]
	private List<PlaneVisual> planes;

	private RealmcraftManager manager;

	[SerializeField]
	private Transform content;

	[SerializeField]
	private Slider zoom;

	[SerializeField]
	private float scrollSensitivity;

	[SerializeField]
	private float minZoom;

	[SerializeField]
	private float maxZoom;

	[SerializeField]
	private Image activeArrow;

	[SerializeField]
	private RealmHistory history;

	[SerializeField]
	private Button historyButton;

	private bool zoomScrolling = true;

	public Plane Selected { get; private set; }

	public PlaneVisual ActivePlane { get; private set; }

	private void Awake()
	{
		manager = GameManager.Instance.Realmcraft;
		string text = manager.Active?.ID ?? string.Empty;
		foreach (PlaneVisual plane in planes)
		{
			plane.Init(manager.Get(plane.Id), Select);
			plane.SetSelected(text == plane.Id);
			plane.SetActive(text == plane.Id);
		}
		Selected = manager.Get(text);
	}

	private void OnEnable()
	{
		string text = manager.Active?.ID ?? string.Empty;
		ActivePlane = planes[0];
		foreach (PlaneVisual plane in planes)
		{
			if (plane.Id == text)
			{
				ActivePlane = plane;
			}
			plane.SetActive(text == plane.Id);
		}
		content.transform.localPosition = -ActivePlane.transform.localPosition / content.transform.localScale.x;
		zoom.value = (content.localScale.x - minZoom) / (maxZoom - minZoom);
		historyButton.gameObject.SetActive(history.CheckAvailable());
		UpdateActiveArrow();
	}

	private void OnDisable()
	{
		historyButton.gameObject.SetActive(value: false);
	}

	public void DisableZoomScrolling()
	{
		zoomScrolling = false;
	}

	public void EnableZoomScrolling()
	{
		zoomScrolling = true;
	}

	protected override void Update()
	{
		base.Update();
		if (Input.mouseScrollDelta.y != 0f && zoomScrolling)
		{
			float value = content.localScale.x + Input.mouseScrollDelta.y * scrollSensitivity;
			value = Mathf.Clamp(value, minZoom, maxZoom);
			if (content.localScale.x != value)
			{
				content.localScale = Vector3.one * value;
				zoom.value = (value - minZoom) / (maxZoom - minZoom);
			}
		}
	}

	private void LateUpdate()
	{
		UpdateActiveArrow();
	}

	private void UpdateActiveArrow()
	{
		if (!(ActivePlane == null))
		{
			activeArrow.transform.localPosition = ActivePlane.GetActivePosition();
		}
	}

	public void OnChangeZoom()
	{
		float num = zoom.value * (maxZoom - minZoom) + minZoom;
		if (content.localScale.x != num)
		{
			content.localScale = Vector3.one * num;
		}
	}

	public void Select(string id)
	{
		Plane plane = manager.Get(id);
		if (!manager.IsAvailable(plane))
		{
			return;
		}
		id = ((plane == null) ? string.Empty : plane.ID);
		Selected = plane;
		foreach (PlaneVisual plane2 in planes)
		{
			plane2.SetSelected(plane2.Id == id);
		}
	}

	public void ChangeRealm()
	{
		if (!manager.IsAvailable(Selected))
		{
			Select(string.Empty);
			return;
		}
		bool num = manager.Active == null;
		bool flag = Selected != null;
		if (num)
		{
			GameManager.Instance.Realm.Convert();
			Close();
		}
		else
		{
			GameManager.Instance.Realmcraft.Convert();
			Close();
		}
	}

	public bool CheckAvailable()
	{
		return GameManager.Instance.Paragon.RealmsIsAvailable;
	}

	public override void Open()
	{
		base.gameObject.SetActive(value: true);
		canvas.On();
	}

	public override void Close()
	{
		canvas.Off();
		base.gameObject.SetActive(value: false);
	}

	public List<PlaneVisual> GetPlanes()
	{
		return planes;
	}
}
