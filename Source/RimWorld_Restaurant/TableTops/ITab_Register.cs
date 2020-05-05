using RimWorld;
using UnityEngine;
using Verse;

namespace Restaurant.TableTops
{
    public class ITab_Register : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        public ITab_Register()
        {
            size = WinSize;
            labelKey = "TabRegister";
        }

        protected Building_CashRegister Register => (Building_CashRegister) SelThing;

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            Rect rectRadius = new Rect(rect) {height = 50};
            var listingRadius = new Listing_Standard();
            listingRadius.Begin(rectRadius);
            string strRadius = "TabRegisterRadius".Translate().Truncate(rectRadius.width * 0.6f);
            string strRadiusValue = Register.radius >= 999f ? "Unlimited".TranslateSimple().Truncate(rectRadius.width * 0.3f) : Register.radius.ToString("F0");
            listingRadius.Label(strRadius + ": " + strRadiusValue);
            float oldValue = Register.radius;
            Register.radius = listingRadius.Slider(Register.radius, 3f, 100f);
            if (Register.radius >= 100f)
            {
                Register.radius = 999f;
            }

            if (Register.radius != oldValue)
            {
                Register.ScanDiningSpots();
            }
            listingRadius.End();
        }

        public override void TabUpdate()
        {
            Register.DrawGizmos();
        }
    }
}
