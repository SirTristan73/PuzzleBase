using UnityEngine;
using UnityEngine.UI; // Для работы с Image
using DG.Tweening;
using EventManagment;

public class CharacterJuicer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _characterImage; // Ссылка на картинку персонажа

    [Header("Animation Settings")]
    [SerializeField] private float _punchAmount = 0.15f;
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private int _vibrato = 8;

    private Vector3 _originalScale;
    private Tweener _currentTween;

    private void Start()
    {
        if (_characterImage == null)
        {
            Debug.LogError($"[CharacterJuicer] Ссылка на Image не назначена на {gameObject.name}!");
            return;
        }

        _originalScale = _characterImage.transform.localScale;

        // Подписываемся на глобальную шину через синглтон контроллера
        if (LevelController.Instance != null)
        {
            LevelController.Instance.EventBus.Subscribe(Events.ScoreAdded, OnScoreAdded);
        }
    }

    private void OnScoreAdded(GameEvent e)
    {
        // Нам плевать на данные в e.Data, важен сам факт события
        Jump();
    }

    private void Jump()
    {
        if (_characterImage == null) return;

        // Перезапуск анимации, если она уже идет
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
            _characterImage.transform.localScale = _originalScale;
        }

        // Панч-эффект именно для ТРАНСФОРМЫ картинки
        _currentTween = _characterImage.transform
            .DOPunchScale(Vector3.one * _punchAmount, _duration, _vibrato)
            .OnComplete(() => _characterImage.transform.localScale = _originalScale);
    }

    private void OnDestroy()
    {
        // Чистим за собой, чтобы не было утечек памяти и ошибок
        if (LevelController.Instance != null && LevelController.Instance.EventBus != null)
        {
            LevelController.Instance.EventBus.Unsubscribe(Events.ScoreAdded, OnScoreAdded);
        }
    }
}