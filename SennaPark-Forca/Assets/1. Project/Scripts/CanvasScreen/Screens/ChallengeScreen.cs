using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ChallengeScreen : CanvasScreen
{
    [Header("Challenge UI")]
    [SerializeField] private TMP_Text challengeTitle;
    [SerializeField] private TMP_Text challengeInstruction;
    [SerializeField] private TMP_Text countdownText;

    [Header("Icone do Desafio (fora dos containers)")]
    [SerializeField] private Image challengeDisplayIcon;
    [SerializeField] private Image challengeDisplayFill;

    [Header("Volante (Desafios de Steering)")]
    [SerializeField] private Image steeringWheelImage;
    [SerializeField] private TMP_Text valueDisplayOutside;
    [SerializeField] private float maxWheelRotation = 90f;
    [SerializeField] private float wheelSmoothSpeed = 8f;
    [SerializeField] private float shakeIntensity = 2f;

    [Header("Setas de Direcao (Filled)")]
    [SerializeField] private Image directionIcon;
    [SerializeField] private Image directionArrowFill;

    [Header("Freio (Slider)")]
    [SerializeField] private Slider brakeProgressBar;
    [SerializeField] private Image brakeFill;
    [SerializeField] private TMP_Text brakeValueDisplay;

    [Header("Containers de Fase")]
    [SerializeField] private GameObject introContainer;
    [SerializeField] private GameObject measureContainer;
    [SerializeField] private GameObject countdownContainer;

    [Header("Containers de Tipo (Steering vs Brake)")]
    [SerializeField] private GameObject steeringContainer;
    [SerializeField] private GameObject brakeContainer;

    [Header("Sprites dos Desafios")]
    [SerializeField] private Sprite arrowRightSprite;
    [SerializeField] private Sprite arrowLeftSprite;
    [SerializeField] private Sprite brakeSprite;

    [Header("Cores")]
    [SerializeField] private Color steeringColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color brakeColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Efeito de Limite (nunca chega a 100%)")]
    [SerializeField] private float oscillationSpeed = 12f;
    [SerializeField] private float oscillationAmount = 0.025f;
    [SerializeField] private float brakeMaxPressure = 80f;
    private float maxFillAmount = 0.85f;

    [Header("Popup de Conclusao")]
    [SerializeField] private GameObject completionPopup;
    [SerializeField] private TMP_Text completionText;
    [SerializeField] private CanvasGroup popupCanvasGroup;

    // Tracking para polling
    private ChallengePhase lastKnownPhase = (ChallengePhase)(-1);
    private GameState lastKnownState = (GameState)(-1);
    private int lastKnownCountdown = -1;
    private bool subscribedToEvents;

    // Volante - estado interno
    private float currentWheelAngle;
    private float targetWheelAngle;
    private bool wasAboveThreshold;

    public override void OnEnable()
    {
        base.OnEnable();
        TrySubscribeEvents();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeEvents();
    }

    private void TrySubscribeEvents()
    {
        if (subscribedToEvents) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        GameManager.Instance.OnChallengePhaseChanged += OnChallengePhaseChanged;
        GameManager.Instance.OnChallengeCompleted += OnChallengeCompletedHandler;
        subscribedToEvents = true;
    }

    private void UnsubscribeEvents()
    {
        if (!subscribedToEvents) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        GameManager.Instance.OnChallengePhaseChanged -= OnChallengePhaseChanged;
        GameManager.Instance.OnChallengeCompleted -= OnChallengeCompletedHandler;
        subscribedToEvents = false;
    }

    public override void CallScreenListner(string screenName)
    {
        if (screenName == this.data.screenName)
        {
            if (!IsOn())
                TurnOn();
            else
                RefreshForCurrentChallenge();
        }
        else
        {
            TurnOff();
        }
    }

    public override void TurnOn()
    {
        canvasgroup.interactable = true;
        canvasgroup.blocksRaycasts = true;
        canvasgroup.DOFade(1f, 0.3f);

        lastKnownPhase = (ChallengePhase)(-1);
        lastKnownState = (GameState)(-1);
        lastKnownCountdown = -1;
        currentWheelAngle = 0f;
        wasAboveThreshold = false;

        HidePopup();

        RefreshForCurrentChallenge();
    }

    public override void TurnOff()
    {
        canvasgroup.interactable = false;
        canvasgroup.blocksRaycasts = false;
        canvasgroup.DOFade(0f, 0.3f);
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (IsOn())
            RefreshForCurrentChallenge();
    }

    private void OnChallengePhaseChanged(ChallengePhase phase)
    {
        if (!IsOn()) return;
        lastKnownPhase = phase;
        UpdatePhaseVisuals(phase);
    }

    private void OnChallengeCompletedHandler()
    {
        if (!IsOn()) return;

        var gm = GameManager.Instance;
        string message = gm.CurrentState switch
        {
            GameState.ChallengeRight => "<b><size=200>Muito bem!</size></b>\nCurva a direita dominada!",
            GameState.ChallengeLeft  => "<b><size=200>Excelente!</size></b>\nCurva a esquerda dominada!",
            GameState.ChallengeBrake => "<b><size=200>Perfeito!</size></b>\nFreada executada com maestria!",
            _                        => "<b><size=200>Muito bem!</size></b>"
        };

        if (completionText != null)
            completionText.text = message;

        if (popupCanvasGroup != null)
        {
            DOTween.Kill(popupCanvasGroup);
            popupCanvasGroup.blocksRaycasts = true;
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.DOFade(1f, 0.35f).SetEase(Ease.OutCubic);
        }

        if (completionPopup != null)
        {
            completionPopup.transform.localScale = Vector3.one * 0.8f;
            completionPopup.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }
    }

    private void HidePopup()
    {
        if (popupCanvasGroup != null)
        {
            DOTween.Kill(popupCanvasGroup);
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.blocksRaycasts = false;
        }

        if (completionPopup != null)
            DOTween.Kill(completionPopup.transform);
    }

    private void RefreshForCurrentChallenge()
    {
        if (GameManager.Instance == null) return;

        HidePopup();

        // Novo desafio = novo limite aleatorio (nunca chega exatamente no mesmo lugar)
        maxFillAmount = UnityEngine.Random.Range(0.73f, 0.89f);

        var gm = GameManager.Instance;
        var state = gm.CurrentState;

        switch (state)
        {
            case GameState.ChallengeRight:
                SetupSteeringChallenge(arrowRightSprite, (int)Image.OriginHorizontal.Left);
                break;
            case GameState.ChallengeLeft:
                SetupSteeringChallenge(arrowLeftSprite, (int)Image.OriginHorizontal.Right);
                break;
            case GameState.ChallengeBrake:
                SetupBrakeChallenge();
                break;
        }

        lastKnownPhase = gm.CurrentChallengePhase;
        lastKnownState = state;
        currentWheelAngle = 0f;
        wasAboveThreshold = false;
        UpdatePhaseVisuals(gm.CurrentChallengePhase);

        // Reset
        if (brakeProgressBar != null)
            brakeProgressBar.value = 0f;

        if (directionArrowFill != null)
            directionArrowFill.fillAmount = 0f;

        if (challengeDisplayFill != null)
            challengeDisplayFill.fillAmount = 0f;

        if (steeringWheelImage != null)
            steeringWheelImage.transform.localEulerAngles = Vector3.zero;
    }

    private void SetupSteeringChallenge(Sprite arrowSprite, int fillOrigin)
    {
        // Mostra volante, esconde freio
        SetContainerActive(steeringContainer, true);
        SetContainerActive(brakeContainer, false);

        // Icone global do desafio (sempre visivel, cor original)
        if (challengeDisplayIcon != null && arrowSprite != null)
            challengeDisplayIcon.sprite = arrowSprite;

        // Fill global do desafio (acompanha progresso)
        if (challengeDisplayFill != null)
        {
            if (arrowSprite != null)
                challengeDisplayFill.sprite = arrowSprite;

            challengeDisplayFill.type = Image.Type.Filled;
            challengeDisplayFill.fillMethod = Image.FillMethod.Horizontal;
            challengeDisplayFill.fillOrigin = fillOrigin;
            challengeDisplayFill.fillAmount = 0f;
            challengeDisplayFill.color = steeringColor;
        }

        // Seta de direcao (icone normal, cor original)
        if (directionIcon != null && arrowSprite != null)
            directionIcon.sprite = arrowSprite;

        // Seta filled (acompanha progresso)
        if (directionArrowFill != null)
        {
            if (arrowSprite != null)
                directionArrowFill.sprite = arrowSprite;

            directionArrowFill.type = Image.Type.Filled;
            directionArrowFill.fillMethod = Image.FillMethod.Horizontal;
            directionArrowFill.fillOrigin = fillOrigin;
            directionArrowFill.fillAmount = 0f;
            directionArrowFill.color = steeringColor;
        }
    }

    private void SetupBrakeChallenge()
    {
        // Esconde volante, mostra freio
        SetContainerActive(steeringContainer, false);
        SetContainerActive(brakeContainer, true);

        // Icone global do desafio (sempre visivel, cor original)
        if (challengeDisplayIcon != null && brakeSprite != null)
            challengeDisplayIcon.sprite = brakeSprite;

        // Fill global do desafio (vertical, de baixo pra cima)
        if (challengeDisplayFill != null)
        {
            if (brakeSprite != null)
                challengeDisplayFill.sprite = brakeSprite;

            challengeDisplayFill.type = Image.Type.Filled;
            challengeDisplayFill.fillMethod = Image.FillMethod.Vertical;
            challengeDisplayFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            challengeDisplayFill.fillAmount = 0f;
            challengeDisplayFill.color = brakeColor;
        }

        // Seta de direcao (icone normal, cor original)
        if (directionIcon != null && brakeSprite != null)
            directionIcon.sprite = brakeSprite;

        if (brakeFill != null)
            brakeFill.color = brakeColor;
    }

    private void UpdatePhaseVisuals(ChallengePhase phase)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        switch (phase)
        {
            case ChallengePhase.Intro:
                SetContainerActive(introContainer, true);
                SetContainerActive(measureContainer, false);
                SetContainerActive(countdownContainer, false);

                if (challengeTitle != null)
                    challengeTitle.text = "Desafio";

                if (challengeInstruction != null)
                    challengeInstruction.text = gm.ChallengeNarrativeText ?? "";
                break;

            case ChallengePhase.Countdown:
                SetContainerActive(introContainer, false);
                SetContainerActive(measureContainer, false);
                SetContainerActive(countdownContainer, true);

                if (countdownText != null)
                {
                    countdownText.text = gm.CountdownValue > 0 ? gm.CountdownValue.ToString() : "Ja!";
                    countdownText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 1, 0.5f);
                }
                break;

            case ChallengePhase.Measuring:
                SetContainerActive(introContainer, false);
                SetContainerActive(measureContainer, true);
                SetContainerActive(countdownContainer, false);

                if (challengeTitle != null)
                    challengeTitle.text = gm.ChallengeActionText ?? "";
                break;
        }
    }

    private void SetContainerActive(GameObject container, bool active)
    {
        if (container != null)
            container.SetActive(active);
    }

    void Update()
    {
        if (!IsOn()) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (!subscribedToEvents)
            TrySubscribeEvents();

        // Polling: detecta mudanca de fase ou estado
        if (gm.CurrentChallengePhase != lastKnownPhase)
        {
            lastKnownPhase = gm.CurrentChallengePhase;
            UpdatePhaseVisuals(lastKnownPhase);
        }

        if (gm.CurrentState != lastKnownState)
        {
            lastKnownState = gm.CurrentState;
            RefreshForCurrentChallenge();
        }

        // Countdown
        if (gm.CurrentChallengePhase == ChallengePhase.Countdown && gm.CountdownValue != lastKnownCountdown)
        {
            lastKnownCountdown = gm.CountdownValue;
            if (countdownText != null)
            {
                countdownText.text = gm.CountdownValue > 0 ? gm.CountdownValue.ToString() : "Ja!";
                countdownText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 1, 0.5f);
            }
        }

        // Medicao
        if (gm.CurrentChallengePhase == ChallengePhase.Measuring)
        {
            if (gm.CurrentState == GameState.ChallengeBrake)
                UpdateBrakeVisuals(gm);
            else
                UpdateSteeringVisuals(gm);
        }
    }

    private void UpdateSteeringVisuals(GameManager gm)
    {
        float torque = gm.CurrentTorque;
        float absTorque = Mathf.Abs(torque);
        float rawNormalized = absTorque / 30f;
        float normalizedClamped = Mathf.Clamp01(rawNormalized);
        float visualFill = ComputeVisualFill(rawNormalized);

        // === VOLANTE ===
        if (steeringWheelImage != null)
        {
            targetWheelAngle = -(torque / 30f) * maxWheelRotation;
            currentWheelAngle = Mathf.Lerp(currentWheelAngle, targetWheelAngle, wheelSmoothSpeed * Time.deltaTime);

            float shake = 0f;
            if (absTorque > 5f)
                shake = Random.Range(-shakeIntensity, shakeIntensity) * normalizedClamped;

            steeringWheelImage.transform.localEulerAngles = new Vector3(0f, 0f, currentWheelAngle + shake);
        }

        // === SETA FILLED ===
        if (directionArrowFill != null)
            directionArrowFill.fillAmount = visualFill;

        // === DISPLAY ICON FILL ===
        if (challengeDisplayFill != null)
            challengeDisplayFill.fillAmount = visualFill;

        // === VALOR FORA DO VOLANTE ===
        if (valueDisplayOutside != null)
            valueDisplayOutside.text = $"{absTorque:F1} N.m";

        // === FEEDBACK ao cruzar threshold ===
        bool isAboveThreshold = absTorque >= 30f;
        if (isAboveThreshold && !wasAboveThreshold)
        {
            // Acabou de cruzar - punch no volante
            if (steeringWheelImage != null)
                steeringWheelImage.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f, 2, 0.5f);

            // Seta fica totalmente colorida
            if (directionArrowFill != null)
                directionArrowFill.DOColor(Color.white, 0.15f).SetLoops(2, LoopType.Yoyo);
        }
        wasAboveThreshold = isAboveThreshold;
    }

    // Comprime o fill para nunca chegar a 100%.
    // Quando rawNormalized >= 1 (no limite), oscila levemente simulando o impacto.
    private float ComputeVisualFill(float rawNormalized)
    {
        if (rawNormalized < 1f)
            return rawNormalized * maxFillAmount;

        return maxFillAmount + Mathf.Sin(Time.time * oscillationSpeed) * oscillationAmount;
    }

    private void UpdateBrakeVisuals(GameManager gm)
    {
        float rawPressureNorm = gm.CurrentPressure / brakeMaxPressure;

        // Slider de freio (mostra hold progress com efeito de limite)
        if (brakeProgressBar != null)
            brakeProgressBar.value = ComputeVisualFill(gm.CurrentHoldProgress);

        // Display icon fill (vertical, de baixo pra cima, baseado na pressao)
        if (challengeDisplayFill != null)
            challengeDisplayFill.fillAmount = ComputeVisualFill(rawPressureNorm);

        // Valor de pressao
        if (brakeValueDisplay != null)
            brakeValueDisplay.text = $"{gm.CurrentPressure:F1} kgf";
    }
}
