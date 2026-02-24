public enum GameState
{
    Idle,
    Onboarding,
    ChallengeRight,
    ChallengeLeft,
    ChallengeBrake,
    Results,
    Ranking
}

public enum ChallengePhase
{
    Intro,      // Narrativa do desafio
    Countdown,  // 3, 2, 1, Ja!
    Measuring   // Medicao da forca
}
