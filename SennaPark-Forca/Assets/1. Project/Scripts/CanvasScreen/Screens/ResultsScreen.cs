using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ResultsScreen : CanvasScreen
{
    [Header("Results UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image avatarImage;

    [Header("Medicoes")]
    [SerializeField] private TMP_Text scoreRightText;
    [SerializeField] private TMP_Text scoreLeftText;
    [SerializeField] private TMP_Text scoreBrakeText;
    [SerializeField] private TMP_Text averageScoreText;

    [Header("Sliders de Resultado")]
    [SerializeField] private Slider rightSlider;
    [SerializeField] private Slider leftSlider;
    [SerializeField] private Slider brakeSlider;
    [SerializeField] private Slider averageSlider;

    [Header("Animacao")]
    [SerializeField] private float fillAnimDuration = 1.5f;
    [SerializeField] private Ease fillAnimEase = Ease.OutCubic;
    [SerializeField] private float sliderAnimDelay = 0.2f;

    public override void TurnOn()
    {
        canvasgroup.interactable = true;
        canvasgroup.blocksRaycasts = true;
        canvasgroup.DOFade(1f, 0.3f);

        PopulateResults();
    }

    public override void TurnOff()
    {
        canvasgroup.interactable = false;
        canvasgroup.blocksRaycasts = false;
        canvasgroup.DOFade(0f, 0.3f);
    }

    private void PopulateResults()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (titleText != null)
            titleText.text = "Veja aqui seus resultados!";

        // Scores reais
        float rightScore  = gm.rightChallenge.score;
        float leftScore   = gm.leftChallenge.score;
        float brakeScore  = gm.brakeChallenge.score;

        // Cap aleatorio por desafio: nunca pode gabaritar (max 70-90%)
        float rightCapped  = Mathf.Min(rightScore,  UnityEngine.Random.Range(0.70f, 0.90f));
        float leftCapped   = Mathf.Min(leftScore,   UnityEngine.Random.Range(0.70f, 0.90f));
        float brakeCapped  = Mathf.Min(brakeScore,  UnityEngine.Random.Range(0.70f, 0.90f));
        float avgCapped    = (rightCapped + leftCapped + brakeCapped) / 3f;

        // Textos com valores reais de forca (N.m / kgf)
        if (scoreRightText != null)
            scoreRightText.text = $"Direita: {gm.rightChallenge.peakValue:F1} N.m";

        if (scoreLeftText != null)
            scoreLeftText.text = $"Esquerda: {gm.leftChallenge.peakValue:F1} N.m";

        if (scoreBrakeText != null)
            scoreBrakeText.text = $"Freio: {gm.brakeChallenge.peakValue:F1} kgf";

        if (averageScoreText != null)
            averageScoreText.text = $"{(avgCapped * 100):F0}%";

        // Sliders e avatar com scores capeados
        AnimateSlider(rightSlider,  rightCapped,  0f);
        AnimateSlider(leftSlider,   leftCapped,   sliderAnimDelay);
        AnimateSlider(brakeSlider,  brakeCapped,  sliderAnimDelay * 2f);
        AnimateSlider(averageSlider, avgCapped,   sliderAnimDelay * 3f);

        if (avatarImage != null)
        {
            avatarImage.fillAmount = 0f;
            avatarImage.DOFillAmount(avgCapped, fillAnimDuration).SetEase(fillAnimEase);
        }
    }

    private void AnimateSlider(Slider slider, float targetValue, float delay)
    {
        if (slider == null) return;

        slider.value = 0f;
        DOTween.To(() => slider.value, x => slider.value = x, targetValue, fillAnimDuration)
            .SetEase(fillAnimEase)
            .SetDelay(delay);
    }
}
