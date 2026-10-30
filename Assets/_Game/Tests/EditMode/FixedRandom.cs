namespace Farm.Tests
{
    /// <summary>
    /// A random source that always rolls the same value, so chance-based rules are predictable in
    /// tests: 0.0 makes every chance happen, 0.99 makes almost none happen.
    /// </summary>
    public class FixedRandom : System.Random
    {
        private readonly double roll;

        public FixedRandom(double roll) => this.roll = roll;

        public override double NextDouble() => roll;

        protected override double Sample() => roll;

        public override int Next(int maxValue) => (int)(roll * maxValue);

        public override int Next(int minValue, int maxValue) => minValue + (int)(roll * (maxValue - minValue));
    }
}
