using System;
using CashRegister;
using Gastronomy.Restaurant;
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

            if (!RegisterUtility.TryAddTab<ITab_Register_Restaurant>())
            {
                GenUI.ErrorDialog("ErrorRequiresCashRegister".Translate());
            }
        }
    }
}