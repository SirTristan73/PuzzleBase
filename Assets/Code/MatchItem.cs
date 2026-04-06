using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using EventManagment;

public class MatchItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _icon;
    public MatchItemData Data { get; private set; }
    private RectTransform _rect;

    public void Setup(int x, int y, int type, Sprite sprite)
    {
        Data = new MatchItemData(x, y, type);
        _icon.sprite = sprite;
        _rect = GetComponent<RectTransform>();
    }

    public void UpdateData(int x, int y) => Data = new MatchItemData(x, y, Data.Type);

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
        transform.DOScale(1.2f, 0.1f);
        // Сообщаем в шину, что нас схватили
        LevelController.Instance.EventBus.SendEvent(new GameEvent(Events.ItemDragStarted, this));
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPos);
        
        _rect.localPosition = localPos;
        // Посылаем текущую позицию курсора для расчетов в контроллере
        LevelController.Instance.EventBus.SendEvent(new GameEvent(Events.ItemPositionChanged, localPos));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.DOScale(1.0f, 0.1f);
        LevelController.Instance.EventBus.SendEvent(new GameEvent(Events.ItemDragEnded, this));
    }

    public Tween MoveTo(Vector2 targetPos, float duration = 0.15f)
    {
        return _rect.DOLocalMove(targetPos, duration).SetEase(Ease.OutQuad);
    }

    public void Despawn()
    {
        transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => Destroy(gameObject));
    }
}