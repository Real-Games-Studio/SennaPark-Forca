using UnityEngine;
using UnityEngine.InputSystem;

public class DebugInputProvider : MonoBehaviour, IInputProvider
{
    [Header("Valores combinados (lidos pelo GameManager)")]
    public float steering;
    public float brake;

    [Header("Configuracao")]
    [SerializeField] private float steeringSpeed = 3f;
    [SerializeField] private float brakeSpeed = 3f;
    [SerializeField] private float returnSpeed = 5f;

    // Teclado (A/D/Space)
    private float keyboardSteering;
    private float keyboardBrake;

    // Sliders do DebugPanel (escritos pelo DebugPanel)
    [HideInInspector] public float sliderSteering;
    [HideInInspector] public float sliderBrake;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Steering via teclado (A/D ou setas)
        float steerInput = 0f;
        if (kb[Key.D].isPressed || kb[Key.RightArrow].isPressed)
            steerInput = 1f;
        else if (kb[Key.A].isPressed || kb[Key.LeftArrow].isPressed)
            steerInput = -1f;

        if (steerInput != 0f)
            keyboardSteering = Mathf.MoveTowards(keyboardSteering, steerInput, steeringSpeed * Time.deltaTime);
        else
            keyboardSteering = Mathf.MoveTowards(keyboardSteering, 0f, returnSpeed * Time.deltaTime);

        // Brake via teclado (Space)
        if (kb[Key.Space].isPressed)
            keyboardBrake = Mathf.MoveTowards(keyboardBrake, 1f, brakeSpeed * Time.deltaTime);
        else
            keyboardBrake = Mathf.MoveTowards(keyboardBrake, 0f, returnSpeed * Time.deltaTime);

        // Combina teclado com slider - usa o de maior valor absoluto
        steering = Mathf.Abs(keyboardSteering) >= Mathf.Abs(sliderSteering) ? keyboardSteering : sliderSteering;
        brake = Mathf.Max(keyboardBrake, sliderBrake);
    }

    public SimulatorInputData GetInput()
    {
        return new SimulatorInputData
        {
            steering = Mathf.Clamp(steering, -1f, 1f),
            brake = Mathf.Clamp(brake, 0f, 1f)
        };
    }
}
