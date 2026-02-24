using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estado Atual")]
    [SerializeField] private GameState currentState = GameState.Idle;
    public GameState CurrentState => currentState;

    [Header("Input Provider (arraste o DebugInputProvider aqui)")]
    [SerializeField] private DebugInputProvider debugInputProvider;
    private IInputProvider inputProvider;

    [Header("Limites de Forca")]
    [SerializeField] private float maxTorque = 30f;
    [SerializeField] private float maxPressure = 80f;

    [Header("Thresholds dos Desafios")]
    [SerializeField] private float torqueThreshold = 30f;
    [SerializeField] private float pressureThreshold = 80f;

    [Header("Duracao dos Desafios (segundos)")]
    [SerializeField] private float steeringHoldDuration = 2f;
    [SerializeField] private float brakeHoldDuration = 2f;
    [SerializeField] private float challengeIntroDuration = 4f;

    [Header("Timers de Tela (segundos)")]
    [SerializeField] private float onboardingDuration = 10f;
    [SerializeField] private float challengeCompletionDelay = 4f;
    [SerializeField] private float resultsDuration = 5f;
    [SerializeField] private float rankingDuration = 5f;

    [Header("Nomes das Telas")]
    [SerializeField] private string idleScreenName = "Idle";
    [SerializeField] private string onboardingScreenName = "Onboarding";
    [SerializeField] private string challengeScreenName = "Challenge";
    [SerializeField] private string resultsScreenName = "Results";
    [SerializeField] private string rankingScreenName = "Ranking";

    // Dados dos desafios
    public ChallengeData rightChallenge = new ChallengeData();
    public ChallengeData leftChallenge = new ChallengeData();
    public ChallengeData brakeChallenge = new ChallengeData();

    // Propriedades lidas pela UI
    public float CurrentTorque { get; private set; }
    public float CurrentPressure { get; private set; }
    public float CurrentHoldProgress { get; private set; }
    public float StateTimer { get; private set; }

    // Fase do desafio (lida pelo ChallengeScreen)
    public ChallengePhase CurrentChallengePhase { get; private set; }
    public int CountdownValue { get; private set; }
    public string ChallengeNarrativeText { get; private set; }
    public string ChallengeActionText { get; private set; }

    // Eventos
    public event Action<GameState> OnStateChanged;
    public event Action<ChallengePhase> OnChallengePhaseChanged;
    public event Action OnChallengeCompleted;

    private Coroutine stateCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find se nao atribuido no Inspector
        if (debugInputProvider == null)
            debugInputProvider = FindAnyObjectByType<DebugInputProvider>();

        if (debugInputProvider != null)
            inputProvider = debugInputProvider;
        else
            Debug.LogWarning("[GameManager] DebugInputProvider nao encontrado! Inputs nao funcionarao.");
    }

    void Start()
    {
        // Escuta reset de inatividade do ScreenCanvasController
        ScreenManager.CallScreen += OnExternalScreenCall;
        TransitionTo(GameState.Idle);
        Debug.Log("[GameManager] Iniciado no estado IDLE");
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Atalhos de debug (funcionam mesmo sem DebugPanel configurado)
        if (kb[Key.F1].wasPressedThisFrame)
        {
            Debug.Log("[GameManager] F1 - Simulando RFID");
            OnRFIDScanned();
        }

        if (kb[Key.F2].wasPressedThisFrame)
        {
            Debug.Log($"[GameManager] F2 - Forcando proximo estado (atual: {currentState})");
            ForceNextState();
        }

        if (kb[Key.F3].wasPressedThisFrame)
        {
            Debug.Log("[GameManager] F3 - Reset para IDLE");
            TransitionTo(GameState.Idle);
        }
    }

    void OnDestroy()
    {
        ScreenManager.CallScreen -= OnExternalScreenCall;
    }

    // Detecta quando ScreenCanvasController reseta para Idle por inatividade
    private void OnExternalScreenCall(string screenName)
    {
        if (screenName == idleScreenName && currentState != GameState.Idle)
        {
            // Reset externo (inatividade) - sincroniza o estado
            StopCurrentState();
            currentState = GameState.Idle;
            CurrentHoldProgress = 0f;
            StateTimer = 0f;
            ResetChallengeData();
            OnStateChanged?.Invoke(currentState);
            stateCoroutine = StartCoroutine(RunIdle());
        }
    }

    public void TransitionTo(GameState newState)
    {
        Debug.Log($"[GameManager] Transicao: {currentState} -> {newState}");

        StopCurrentState();

        currentState = newState;
        CurrentHoldProgress = 0f;
        StateTimer = 0f;
        CurrentChallengePhase = ChallengePhase.Intro;

        // Inicia coroutine ANTES de notificar telas
        // Assim os textos sao setados antes do ChallengeScreen ler
        stateCoroutine = StartCoroutine(RunState(newState));

        // Notifica as telas (agora ChallengeNarrativeText ja foi setado pelo coroutine)
        ShowScreenForState(newState);
        OnStateChanged?.Invoke(newState);
    }

    private void StopCurrentState()
    {
        if (stateCoroutine != null)
        {
            StopCoroutine(stateCoroutine);
            stateCoroutine = null;
        }
    }

    private void ShowScreenForState(GameState state)
    {
        string screenName = state switch
        {
            GameState.Idle => idleScreenName,
            GameState.Onboarding => onboardingScreenName,
            GameState.ChallengeRight => challengeScreenName,
            GameState.ChallengeLeft => challengeScreenName,
            GameState.ChallengeBrake => challengeScreenName,
            GameState.Results => resultsScreenName,
            GameState.Ranking => rankingScreenName,
            _ => idleScreenName
        };

        ScreenManager.SetCallScreen(screenName);
    }

    private IEnumerator RunState(GameState state)
    {
        switch (state)
        {
            case GameState.Idle:
                yield return RunIdle();
                break;
            case GameState.Onboarding:
                yield return RunOnboarding();
                break;
            case GameState.ChallengeRight:
                yield return RunChallengeRight();
                break;
            case GameState.ChallengeLeft:
                yield return RunChallengeLeft();
                break;
            case GameState.ChallengeBrake:
                yield return RunChallengeBrake();
                break;
            case GameState.Results:
                yield return RunResults();
                break;
            case GameState.Ranking:
                yield return RunRanking();
                break;
        }
    }

    #region === ESTADOS ===

    private IEnumerator RunIdle()
    {
        ResetChallengeData();
        // Aguarda indefinidamente - OnRFIDScanned() fara a transicao
        while (true)
            yield return null;
    }

    private IEnumerator RunOnboarding()
    {
        StateTimer = onboardingDuration;

        while (StateTimer > 0f)
        {
            StateTimer -= Time.deltaTime;
            UpdateInputReadings();
            yield return null;
        }

        StateTimer = 0f;
        TransitionTo(GameState.ChallengeRight);
    }

    private IEnumerator RunChallengeRight()
    {
        rightChallenge.Reset();

        // === FASE 1: INTRO ===
        ChallengeNarrativeText = "Voce esta prestes a fazer uma curva acentuada a direita.\nVoce precisa atingir 30N.m de torque por 2 segundos!";
        ChallengeActionText = "Vire agora!";
        yield return RunChallengeIntro();

        // === FASE 2: COUNTDOWN ===
        yield return RunChallengeCountdown();

        // === FASE 3: MEDICAO ===
        SetChallengePhase(ChallengePhase.Measuring);
        float holdTimer = 0f;

        while (holdTimer < steeringHoldDuration)
        {
            UpdateInputReadings();

            if (CurrentTorque >= torqueThreshold)
                holdTimer += Time.deltaTime;
            else
                holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 0.5f);

            CurrentHoldProgress = Mathf.Clamp01(holdTimer / steeringHoldDuration);
            rightChallenge.peakValue = Mathf.Max(rightChallenge.peakValue, CurrentTorque);

            yield return null;
        }

        rightChallenge.holdTime = holdTimer;
        rightChallenge.score = Mathf.Clamp01(rightChallenge.peakValue / maxTorque);
        rightChallenge.completed = true;

        OnChallengeCompleted?.Invoke();
        yield return new WaitForSeconds(challengeCompletionDelay);
        TransitionTo(GameState.ChallengeLeft);
    }

    private IEnumerator RunChallengeLeft()
    {
        leftChallenge.Reset();

        // === FASE 1: INTRO ===
        ChallengeNarrativeText = "Voce esta prestes a fazer uma curva acentuada a esquerda.\nVoce precisa atingir 30N.m de torque por 2 segundos!";
        ChallengeActionText = "Vire agora!";
        yield return RunChallengeIntro();

        // === FASE 2: COUNTDOWN ===
        yield return RunChallengeCountdown();

        // === FASE 3: MEDICAO ===
        SetChallengePhase(ChallengePhase.Measuring);
        float holdTimer = 0f;

        while (holdTimer < steeringHoldDuration)
        {
            UpdateInputReadings();

            float absTorque = Mathf.Abs(CurrentTorque);
            if (CurrentTorque < 0 && absTorque >= torqueThreshold)
                holdTimer += Time.deltaTime;
            else
                holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 0.5f);

            CurrentHoldProgress = Mathf.Clamp01(holdTimer / steeringHoldDuration);
            if (CurrentTorque < 0)
                leftChallenge.peakValue = Mathf.Max(leftChallenge.peakValue, absTorque);

            yield return null;
        }

        leftChallenge.holdTime = holdTimer;
        leftChallenge.score = Mathf.Clamp01(leftChallenge.peakValue / maxTorque);
        leftChallenge.completed = true;

        OnChallengeCompleted?.Invoke();
        yield return new WaitForSeconds(challengeCompletionDelay);
        TransitionTo(GameState.ChallengeBrake);
    }

    private IEnumerator RunChallengeBrake()
    {
        brakeChallenge.Reset();

        // === FASE 1: INTRO ===
        ChallengeNarrativeText = "Voce precisa diminuir para entrar nos boxes.\nVoce precisa atingir 80kgf de pressao por 1 segundo!";
        ChallengeActionText = "Freie agora!";
        yield return RunChallengeIntro();

        // === FASE 2: COUNTDOWN ===
        yield return RunChallengeCountdown();

        // === FASE 3: MEDICAO ===
        SetChallengePhase(ChallengePhase.Measuring);
        float holdTimer = 0f;

        while (holdTimer < brakeHoldDuration)
        {
            UpdateInputReadings();

            if (CurrentPressure >= pressureThreshold)
                holdTimer += Time.deltaTime;
            else
                holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 0.5f);

            CurrentHoldProgress = Mathf.Clamp01(holdTimer / brakeHoldDuration);
            brakeChallenge.peakValue = Mathf.Max(brakeChallenge.peakValue, CurrentPressure);

            yield return null;
        }

        brakeChallenge.holdTime = holdTimer;
        brakeChallenge.score = Mathf.Clamp01(brakeChallenge.peakValue / maxPressure);
        brakeChallenge.completed = true;

        OnChallengeCompleted?.Invoke();
        yield return new WaitForSeconds(challengeCompletionDelay);
        TransitionTo(GameState.Results);
    }

    // Helpers de fase dos desafios
    private IEnumerator RunChallengeIntro()
    {
        SetChallengePhase(ChallengePhase.Intro);
        StateTimer = challengeIntroDuration;

        while (StateTimer > 0f)
        {
            StateTimer -= Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RunChallengeCountdown()
    {
        SetChallengePhase(ChallengePhase.Countdown);

        for (int i = 3; i >= 1; i--)
        {
            CountdownValue = i;
            OnChallengePhaseChanged?.Invoke(ChallengePhase.Countdown);
            yield return new WaitForSeconds(1f);
        }

        // "Ja!"
        CountdownValue = 0;
        OnChallengePhaseChanged?.Invoke(ChallengePhase.Countdown);
        yield return new WaitForSeconds(0.5f);
    }

    private void SetChallengePhase(ChallengePhase phase)
    {
        CurrentChallengePhase = phase;
        OnChallengePhaseChanged?.Invoke(phase);
        Debug.Log($"[GameManager] Fase do desafio: {phase}");
    }

    private IEnumerator RunResults()
    {
        StateTimer = resultsDuration;

        while (StateTimer > 0f)
        {
            StateTimer -= Time.deltaTime;
            yield return null;
        }

        TransitionTo(GameState.Ranking);
    }

    private IEnumerator RunRanking()
    {
        StateTimer = rankingDuration;

        while (StateTimer > 0f)
        {
            StateTimer -= Time.deltaTime;
            yield return null;
        }

        TransitionTo(GameState.Idle);
    }

    #endregion

    #region === METODOS PUBLICOS ===

    /// <summary>
    /// Chamado pelo hardware RFID ou pelo DebugPanel
    /// </summary>
    public void OnRFIDScanned()
    {
        if (currentState == GameState.Idle)
            TransitionTo(GameState.Onboarding);
    }

    /// <summary>
    /// Forca avanco para o proximo estado (debug)
    /// </summary>
    public void ForceNextState()
    {
        GameState next = currentState switch
        {
            GameState.Idle => GameState.Onboarding,
            GameState.Onboarding => GameState.ChallengeRight,
            GameState.ChallengeRight => GameState.ChallengeLeft,
            GameState.ChallengeLeft => GameState.ChallengeBrake,
            GameState.ChallengeBrake => GameState.Results,
            GameState.Results => GameState.Ranking,
            GameState.Ranking => GameState.Idle,
            _ => GameState.Idle
        };
        TransitionTo(next);
    }

    public float GetAverageScore()
    {
        return (rightChallenge.score + leftChallenge.score + brakeChallenge.score) / 3f;
    }

    #endregion

    #region === HELPERS ===

    private void UpdateInputReadings()
    {
        if (inputProvider == null) return;

        SimulatorInputData raw = inputProvider.GetInput();
        CurrentTorque = raw.steering * maxTorque;
        CurrentPressure = raw.brake * maxPressure;
    }

    private void ResetChallengeData()
    {
        rightChallenge.Reset();
        leftChallenge.Reset();
        brakeChallenge.Reset();
        CurrentTorque = 0f;
        CurrentPressure = 0f;
        CurrentHoldProgress = 0f;
    }

    #endregion
}
