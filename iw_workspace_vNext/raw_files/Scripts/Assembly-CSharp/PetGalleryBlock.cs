public class PetGalleryBlock : GalleryBlock
{
	public PetNames Key;

	public override void Open(bool isOpen)
	{
		if (!isOpen)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		base.gameObject.SetActive(value: true);
		for (int i = 0; i < Frames.Count; i++)
		{
			Frames[i].Refresh();
		}
	}
}
