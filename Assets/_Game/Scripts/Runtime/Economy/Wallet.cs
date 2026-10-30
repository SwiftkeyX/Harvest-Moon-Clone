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
    }
}
