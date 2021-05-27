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
                GenUI.ErrorDialog(
                    "Gastronomy now requires the 'Cash Register' mod to be loaded.\n\nI had to create it in order to be able to work on a new feature. Sorry about this inconvenience.\n\nIf you add the 'Cash Register' mod, Gastronomy will continue working as before.\n\n\n-- Regards, Orion");
            }
        }
    }
}