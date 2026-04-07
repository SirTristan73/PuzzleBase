using UnityEngine;
using TMPro; // Если используешь TextMeshPro, если обычный Text — замени на UnityEngine.UI
using DG.Tweening;
using EventManagment;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text _scoreText; // Ссылка на компонент текста
    [SerializeField] private float _animateDuration = 0.5f;

    private int _currentTotalScore = 0;
    private int _displayedScore = 0;

    private void Start()
    {
        UpdateText();

        // Подписываемся на системную шину
        if (LevelController.Instance != null)
        {
            LevelController.Instance.EventBus.Subscribe(Events.ScoreAdded, OnScoreAdded);
        }
    }

    private void OnScoreAdded(GameEvent e)
    {
        // Извлекаем число из object Data
        if (e.Data is int addedScore)
        {
            _currentTotalScore += addedScore;
            AnimateScore();
        }
        else if (e.Data is float addedScoreFloat) // На случай, если прилетит float
        {
            _currentTotalScore += (int)addedScoreFloat;
            AnimateScore();
        }
    }

    private void AnimateScore()
    {
        // Красивое "тиканье" цифр от текущего значения до нового
        DOTween.To(() => _displayedScore, x => _displayedScore = x, _currentTotalScore, _animateDuration)
            .OnUpdate(UpdateText)
            .SetEase(Ease.OutQuad);
        
        // Небольшой "подпрыг" самого текста для сочности
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }

    private void UpdateText()
    {
        if (_scoreText != null)
        {
            _scoreText.text = _displayedScore.ToString();
        }
    }

    private void OnDestroy()
    {
        if (LevelController.Instance != null)
        {
            LevelController.Instance.EventBus.Unsubscribe(Events.ScoreAdded, OnScoreAdded);
        }
    }
}