public interface IEffect
{
	void Apply();

	void SetEfficiency(Variable eff);

	void SetGilding(Variable eff);

	void Delete();

	void Update();

	string Preview(string key = "");
}
