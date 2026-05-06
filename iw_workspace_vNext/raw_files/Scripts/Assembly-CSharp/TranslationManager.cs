using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

public class TranslationManager
{
	public enum Language
	{
		En = 0,
		Ru = 1,
		De = 2,
		Fr = 3,
		Pt = 4,
		Pl = 5,
		Da = 6,
		Es = 7
	}

	public class TranslationData
	{
		public string Key;

		public string En;

		public string Ru;

		public string De;

		public string Fr;

		public string Da;

		public string PtBR;

		public string Pl;

		public string Es;

		public string GetCurrent(Language lang)
		{
			switch (lang)
			{
			case Language.En:
				return En;
			case Language.Ru:
				if (!string.IsNullOrEmpty(Ru))
				{
					return Ru;
				}
				return En;
			case Language.De:
				if (!string.IsNullOrEmpty(De))
				{
					return De;
				}
				return En;
			case Language.Fr:
				if (!string.IsNullOrEmpty(Fr))
				{
					return Fr;
				}
				return En;
			case Language.Da:
				if (!string.IsNullOrEmpty(Da))
				{
					return Da;
				}
				return En;
			case Language.Pl:
				if (!string.IsNullOrEmpty(Pl))
				{
					return Pl;
				}
				return En;
			case Language.Pt:
				if (!string.IsNullOrEmpty(PtBR))
				{
					return PtBR;
				}
				return En;
			case Language.Es:
				if (!string.IsNullOrEmpty(Es))
				{
					return Es;
				}
				return En;
			default:
				return string.Empty;
			}
		}
	}

	private static TranslationManager instance;

	public Action OnChangeLanguage;

	private Language currentLanguage;

	private Dictionary<string, string> data;

	private static string fullPattern = "\\[(.*?)\\](\\{(.*?)\\})?";

	private Regex fullRx = new Regex(fullPattern, RegexOptions.Multiline);

	private static string pattern = "\\[(.*?)\\]";

	private Regex mainRx = new Regex(pattern, RegexOptions.Multiline);

	private static string subpattern = "\\{(.*?)\\}";

	private Regex subRx = new Regex(subpattern, RegexOptions.Multiline);

	public static TranslationManager Instance => instance ?? (instance = new TranslationManager());

	private TranslationManager()
	{
		if (PlayerPrefs.HasKey("lang"))
		{
			currentLanguage = (Language)PlayerPrefs.GetInt("lang");
		}
		LoadLanguage();
	}

	public Language GetCurrentLang()
	{
		return currentLanguage;
	}

	public void SetLanguage(int lang)
	{
		if (currentLanguage != (Language)lang)
		{
			currentLanguage = (Language)lang;
			PlayerPrefs.SetInt("lang", lang);
			LoadLanguage();
		}
	}

	public string Process(string text)
	{
		string empty = string.Empty;
		if (string.IsNullOrEmpty(text))
		{
			return empty;
		}
		if (fullRx.IsMatch(text))
		{
			return fullRx.Replace(text, ReplaceMatch);
		}
		return text.Translate();
	}

	private string ReplaceMatch(Match m)
	{
		string text = m.Groups[1].ToString().Translate();
		if (m.Groups.Count >= 3 && !string.IsNullOrEmpty(m.Groups[3].ToString()))
		{
			text = text.Replace("#", m.Groups[3].ToString());
		}
		return text;
	}

	public string Get(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			Debug.Log("key is null");
			return key;
		}
		if (data == null)
		{
			Debug.Log("translation data not found");
			return key;
		}
		if (!data.ContainsKey(key))
		{
			return key;
		}
		return data[key];
	}

	private void LoadLanguage()
	{
		TextAsset textAsset = Resources.Load<TextAsset>("Translation/" + currentLanguage);
		data = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
		OnChangeLanguage?.Invoke();
		Resources.UnloadAsset(textAsset);
		OnChangeLanguage?.Invoke();
	}
}
