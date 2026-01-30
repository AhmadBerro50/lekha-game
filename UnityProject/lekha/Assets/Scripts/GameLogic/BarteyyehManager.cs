using UnityEngine;
using Lekha.Core;

namespace Lekha.GameLogic
{
    /// <summary>
    /// Tracks Barteyyeh (best of 3) series state across multiple games.
    /// A team must win 2 out of 3 games to win the Barteyyeh.
    /// </summary>
    public class BarteyyehManager : MonoBehaviour
    {
        public static BarteyyehManager Instance { get; private set; }

        public int NorthSouthWins { get; private set; }
        public int EastWestWins { get; private set; }
        public int GamesPlayed { get; private set; }

        public const int WinsNeeded = 2;
        public const int MaxGames = 3;

        public bool IsBarteyyehComplete => NorthSouthWins >= WinsNeeded || EastWestWins >= WinsNeeded;

        public Team? BarteyyehWinner
        {
            get
            {
                if (NorthSouthWins >= WinsNeeded) return Team.NorthSouth;
                if (EastWestWins >= WinsNeeded) return Team.EastWest;
                return null;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RecordGameWin(Team winningTeam)
        {
            GamesPlayed++;
            if (winningTeam == Team.NorthSouth)
                NorthSouthWins++;
            else
                EastWestWins++;

            Debug.Log($"[BarteyyehManager] Game {GamesPlayed} won by {winningTeam}. Series: NS {NorthSouthWins} - {EastWestWins} EW");
        }

        public void ResetBarteyyeh()
        {
            NorthSouthWins = 0;
            EastWestWins = 0;
            GamesPlayed = 0;
            Debug.Log("[BarteyyehManager] Barteyyeh reset");
        }

        /// <summary>
        /// Get series score string like "1 - 0"
        /// </summary>
        public string GetSeriesScore()
        {
            return $"{NorthSouthWins} - {EastWestWins}";
        }

        /// <summary>
        /// Get series score relative to a team
        /// </summary>
        public string GetSeriesScoreForTeam(Team team)
        {
            if (team == Team.NorthSouth)
                return $"{NorthSouthWins} - {EastWestWins}";
            else
                return $"{EastWestWins} - {NorthSouthWins}";
        }

        public int GetWins(Team team)
        {
            return team == Team.NorthSouth ? NorthSouthWins : EastWestWins;
        }
    }
}
