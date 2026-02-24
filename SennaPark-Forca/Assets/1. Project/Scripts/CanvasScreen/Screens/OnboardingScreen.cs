using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class OnboardingScreen : CanvasScreen
{
    [Header("Onboarding UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Image avatarImage;

    [Header("Indicadores de Input (Free Play)")]
    [SerializeField] private Slider steeringIndicator;
    [SerializeField] private Slider brakeIndicator;
    [SerializeField] private TMP_Text torqueText;
    [SerializeField] private TMP_Text pressureText;

    private const string ONBOARDING_MESSAGE = "Experimente o equipamento e prepare-se!";

    public override void TurnOn()
    {
        canvasgroup.interactable = true;
        canvasgroup.blocksRaycasts = true;
        canvasgroup.DOFade(1f, 0.3f);

        if (instructionText != null)
            instructionText.text = ONBOARDING_MESSAGE;

        if (steeringIndicator != null)
        {
            steeringIndicator.minValue = -1f;
            steeringIndicator.maxValue = 1f;
            steeringIndicator.interactable = false;
        }

        if (brakeIndicator != null)
        {
            brakeIndicator.minValue = 0f;
            brakeIndicator.maxValue = 1f;
            brakeIndicator.interactable = false;
        }
    }

    public override void TurnOff()
    {
        canvasgroup.interactable = false;
        canvasgroup.blocksRaycasts = false;
        canvasgroup.DOFade(0f, 0.3f);
    }

    void Update()
    {
        if (!IsOn()) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // Countdown
        if (countdownText != null)
            countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, gm.StateTimer)).ToString();

        // Indicadores de free play
        float normalizedSteering = gm.CurrentTorque / 30f;
        float normalizedBrake = gm.CurrentPressure / 80f;

        if (steeringIndicator != null)
            steeringIndicator.value = normalizedSteering;

        if (brakeIndicator != null)
            brakeIndicator.value = normalizedBrake;

        if (torqueText != null)
            torqueText.text = $"{gm.CurrentTorque:F1} N.m";

        if (pressureText != null)
            pressureText.text = $"{gm.CurrentPressure:F1} kgf";
    }
}
