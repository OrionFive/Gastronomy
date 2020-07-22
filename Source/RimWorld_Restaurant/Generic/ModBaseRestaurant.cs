using HugsLib;
using UnityEngine;
using Verse;

namespace Restaurant
{
    [StaticConstructorOnStartup]
    public class ModBaseRestaurant : ModBase
    {
        public static Texture2D symbolTakeOrder;
        public static Texture2D symbolInsultPatron;
        public override string ModIdentifier => "Restaurant";

        public override void MapLoaded(Map map)
        {
            symbolTakeOrder = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/TakeOrder");
            symbolInsultPatron = ContentFinder<Texture2D>.Get("Things/Mote/SpeechSymbols/Insult");
        }
    }
}