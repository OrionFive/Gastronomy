using HugsLib;
using UnityEngine;
using Verse;

namespace Gastronomy
{
    [StaticConstructorOnStartup]
    public class ModBaseGastronomy : ModBase
    {
        public static Texture2D symbolTakeOrder;
        public static Texture2D symbolNoOrder;
        public static Texture2D symbolInsultPatron;
        public override string ModIdentifier => "Gastronomy";

        public override void MapLoaded(Map map)
        {
            symbolTakeOrder = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/TakeOrder");
            symbolNoOrder = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/NoOrder");
            symbolInsultPatron = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/Insult");
        }
    }
}