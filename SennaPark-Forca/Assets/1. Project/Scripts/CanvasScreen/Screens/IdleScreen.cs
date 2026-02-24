using UnityEngine;
using TMPro;
using DG.Tweening;

public class IdleScreen : CanvasScreen
{
    [Header("Idle UI")]
    [SerializeField] private TMP_Text instructionText;

    private const string IDLE_MESSAGE = "Vamos treinar nossa forca?\nEscaneie sua credencial para comecar";

    public override void TurnOn()
    {
        canvasgroup.interactable = true;
        canvasgroup.blocksRaycasts = true;
        canvasgroup.DOFade(1f, 0.3f);

        if (instructionText != null)
            instructionText.text = IDLE_MESSAGE;
    }

    public override void TurnOff()
    {
        canvasgroup.interactable = false;
        canvasgroup.blocksRaycasts = false;
        canvasgroup.DOFade(0f, 0.3f);
    }
}
