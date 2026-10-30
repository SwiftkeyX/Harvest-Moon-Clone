namespace Farm.Economy
{
    public class Wallet
    {
        public int Money { get; private set; }

        public Wallet(int startingMoney = 500)
        {
            Money = startingMoney;
        }

        public void Add(int amount) => Money += amount;

        /// <summary>Takes the money if there is enough; otherwise changes nothing.</summary>
        public bool TrySpend(int amount)
        {
            if (amount > Money) return false;
            Money -= amount;
            return true;
        }
    }
}
