using HugsLib;
using Verse;

namespace Gastronomy
{
    [StaticConstructorOnStartup]
    public class ModBaseGastronomy : ModBase
    {
        public override string ModIdentifier => "Gastronomy";

        private static Settings settings;

        public override void DefsLoaded()
        {
            settings = new Settings(Settings);
        }

        public override void MapLoaded(Map map)
        {
            Symbols.Initialize();
        }
    }
}