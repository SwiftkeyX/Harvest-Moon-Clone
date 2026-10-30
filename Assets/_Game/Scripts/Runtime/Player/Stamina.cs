using UnityEngine;

namespace Farm.Player
{
    public class Stamina
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsExhausted => Current <= 0;

        public Stamina(int max = 100)
        {
            Max = max;
            Current = max;
        }

        public void Spend(int amount) => Current = Mathf.Max(0, Current - amount);

        public void RestoreForNewDay(bool passedOut) => Current = passedOut ? Max / 2 : Max;
    }
}
