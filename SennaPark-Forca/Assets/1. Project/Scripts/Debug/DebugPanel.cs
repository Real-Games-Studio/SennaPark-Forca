using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class DebugPanel : MonoBehaviour
{
    [Header("Painel")]
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Input Provider")]
    [SerializeField] private DebugInputProvider debugInput;

    [Header("Sliders de Input")]
    [SerializeField] private Slider steeringSlider;
    [SerializeField] private Slider brakeSlider;

    [Header("Textos de Valores")]
    [SerializeField] private TMP_Text steeringValueText;
    [SerializeField] private TMP_Text brakeValueText;
    [SerializeField] private TMP_Text torqueValueText;
    [SerializeField] private TMP_Text pressureValueText;

    [Header("Textos de Estado")]
    [SerializeField] private TMP_Text currentStateText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text holdProgressText;

    [Header("Botoes")]
    [SerializeField] private Button rfidButton;
    [SerializeField] private Button nextStateButton;
    [SerializeField] private Button resetButton;

    private bool isVisible = true;

    void Start()
    {
        // Configura sliders
        if (steeringSlider != null)
        {
            steeringSlider.minValue = -1f;
            steeringSlider.maxValue = 1f;
            steeringSlider.onValueChanged.AddListener(OnSteeringSliderChanged);
        }

        if (brakeSlider != null)
        {
            brakeSlider.minValue = 0f;
            brakeSlider.maxValue = 1f;
            brakeSlider.onValueChanged.AddListener(OnBrakeSliderChanged);
        }

        // Configura botoes
        if (rfidButton != null)
            rfidButton.onClick.AddListener(OnRFIDButtonClick);

        if (nextStateButton != null)
            nextStateButton.onClick.AddListener(OnNextStateButtonClick);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClick);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Toggle visibilidade (F12)
        if (kb[Key.F12].wasPressedThisFrame)
        {
            isVisible = !isVisible;
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = isVisible ? 1f : 0f;
                panelCanvasGroup.interactable = isVisible;
                panelCanvasGroup.blocksRaycasts = isVisible;
            }
        }

        // Atualiza displays
        UpdateDisplays();
    }

    private void UpdateDisplays()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Estado
        if (currentStateText != null)
            currentStateText.text = $"Estado: {gm.CurrentState}";

        if (timerText != null)
            timerText.text = $"Timer: {gm.StateTimer:F1}s";

        if (holdProgressText != null)
            holdProgressText.text = $"Hold: {(gm.CurrentHoldProgress * 100):F0}%";

        // Valores de input
        if (debugInput != null)
        {
            if (steeringValueText != null)
                steeringValueText.text = $"Steering: {debugInput.steering:F2}";

            if (brakeValueText != null)
                brakeValueText.text = $"Brake: {debugInput.brake:F2}";

            // Sincroniza sliders com o valor atual (inclusive teclado)
            if (steeringSlider != null)
                steeringSlider.SetValueWithoutNotify(debugInput.steering);

            if (brakeSlider != null)
                brakeSlider.SetValueWithoutNotify(debugInput.brake);
        }

        // Valores convertidos
        if (torqueValueText != null)
            torqueValueText.text = $"Torque: {gm.CurrentTorque:F1} N.m";

        if (pressureValueText != null)
            pressureValueText.text = $"Pressao: {gm.CurrentPressure:F1} kgf";
    }

    private void OnSteeringSliderChanged(float value)
    {
        if (debugInput != null)
            debugInput.sliderSteering = value;
    }

    private void OnBrakeSliderChanged(float value)
    {
        if (debugInput != null)
            debugInput.sliderBrake = value;
    }

    private void OnRFIDButtonClick()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRFIDScanned();
    }

    private void OnNextStateButtonClick()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ForceNextState();
    }

    private void OnResetButtonClick()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TransitionTo(GameState.Idle);
    }
}
