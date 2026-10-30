using UnityEngine;

namespace Farm.View
{
    /// <summary>Lets game time pass in real time, in 10-minute steps.</summary>
    public class TimeController : MonoBehaviour
    {
        private const int StepMinutes = 10;

        [SerializeField] private float realSecondsPerGameHour = 40f;
        [SerializeField] private FarmGame game;
        [SerializeField] private PlayerActions playerActions;

        private float elapsed;

        private void Update()
        {
            if (game.ShopOpen) return;
            float secondsPerStep = realSecondsPerGameHour * StepMinutes / 60f;
            elapsed += UnityEngine.Time.deltaTime;
            if (elapsed < secondsPerStep) return;
            elapsed -= secondsPerStep;

            if (game.DayCycle.PassTime(StepMinutes))
            {
                elapsed = 0f;
                game.Say("Passed out!");
                playerActions.Refresh();
            }
        }
    }
}
