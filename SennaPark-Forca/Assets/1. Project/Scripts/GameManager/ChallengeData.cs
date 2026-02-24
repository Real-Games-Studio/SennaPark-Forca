[System.Serializable]
public class ChallengeData
{
    public float peakValue;  // Valor maximo atingido (torque ou pressao)
    public float holdTime;   // Tempo que manteve acima do threshold
    public float score;      // Pontuacao normalizada 0-1
    public bool completed;   // Completou o desafio?

    public void Reset()
    {
        peakValue = 0f;
        holdTime = 0f;
        score = 0f;
        completed = false;
    }
}
