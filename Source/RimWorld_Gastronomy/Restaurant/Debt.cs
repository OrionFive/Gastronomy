using Verse;

namespace Gastronomy.Restaurant
{
	public class Debt : IExposable
	{
		public Pawn patron;
		public float amount;

		public void ExposeData()
		{
			Scribe_References.Look(ref patron, "patron");
			Scribe_Values.Look(ref amount, "amount");
		}
	}
}
