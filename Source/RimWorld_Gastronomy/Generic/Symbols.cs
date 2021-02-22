using UnityEngine;
using Verse;

namespace Gastronomy
{
	public static class Symbols
	{
		public static Texture2D symbolTakeOrder;
		public static Texture2D symbolNoOrder;
		public static Texture2D symbolInsultPatron;

		public static void Initialize()
		{
			symbolTakeOrder = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/TakeOrder");
			symbolNoOrder = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/NoOrder");
			symbolInsultPatron = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/Insult");
		}
	}
}
