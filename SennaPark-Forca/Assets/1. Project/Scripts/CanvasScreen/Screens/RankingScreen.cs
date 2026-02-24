using UnityEngine;
using TMPro;
using DG.Tweening;

public class RankingScreen : CanvasScreen
{
    [Header("Ranking UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text placeholderText;

    public override void TurnOn()
    {
        canvasgroup.interactable = true;
        canvasgroup.blocksRaycasts = true;
        canvasgroup.DOFade(1f, 0.3f);

        if (titleText != null)
            titleText.text = "Ranking Global";

        if (placeholderText != null)
            placeholderText.text = "Em breve!";
    }

    public override void TurnOff()
    {
        canvasgroup.interactable = false;
        canvasgroup.blocksRaycasts = false;
        canvasgroup.DOFade(0f, 0.3f);
    }
}
