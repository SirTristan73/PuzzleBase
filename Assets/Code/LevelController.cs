using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using EventManagment;

public class LevelController : Singleton<LevelController>
{
    [Header("Board Settings")]
    [SerializeField] private int _width = 6;
    [SerializeField] private int _height = 6;
    [SerializeField] private int _itemTypesCount = 5;
    [SerializeField] private int _scorePerItem = 10;

    [Header("UI & Prefabs")]
    [SerializeField] private MatchItem _itemPrefab;
    [SerializeField] private MatchItemCatalog _catalog;
    [SerializeField] private RectTransform _gridParent;
    [SerializeField] private Vector2 _spacing = new Vector2(5, 5);

    private MatchItem[,] _grid;
    private MatchItem[,] _startSnapshot; // Состояние до начала хода
    private bool _isProcessing;
    private MatchItem _draggedItem;
    
    private Vector2 _cellSize;
    private Vector2 _startPosition;

    public EventBus EventBus { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        _grid = new MatchItem[_width, _height];
        _startSnapshot = new MatchItem[_width, _height];
        
        // Инициализируем твой ивент-бас
        EventBus = new EventBus(e => false); 
        
        // Подписки на строковые события
        EventBus.Subscribe(Events.ItemDragStarted, (e) => OnDragStarted((MatchItem)e.Data));
        EventBus.Subscribe(Events.ItemPositionChanged, (e) => OnPositionChanged((Vector2)e.Data));
        EventBus.Subscribe(Events.ItemDragEnded, (e) => OnDragEnded((MatchItem)e.Data));

        CalculateGrid();
    }

    private void Start() => SpawnInitialGrid();

    private void CalculateGrid()
    {
        Rect rect = _gridParent.rect;
        float size = Mathf.Min((rect.width - (_spacing.x * (_width - 1))) / _width, 
                              (rect.height - (_spacing.y * (_height - 1))) / _height);
        _cellSize = new Vector2(size, size);
        _startPosition = new Vector2(-rect.width / 2 + size / 2, -rect.height / 2 + size / 2);
    }

    private void SpawnInitialGrid()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int type = Random.Range(0, _itemTypesCount); // Упростим для примера
                SpawnItem(x, y, type);
            }
        }
    }

    private void SpawnItem(int x, int y, int type)
    {
        var visuals = _catalog.GetVisuals(type);
        var item = Instantiate(_itemPrefab, _gridParent);
        item.Setup(x, y, type, visuals.Sprite);
        _grid[x, y] = item;
        
        RectTransform rt = item.GetComponent<RectTransform>();
        rt.sizeDelta = _cellSize;
        rt.localPosition = GetPositionForCell(x, y);
    }

    public Vector2 GetPositionForCell(int x, int y) => 
        _startPosition + new Vector2(x * (_cellSize.x + _spacing.x), y * (_cellSize.y + _spacing.y));

    #region Input Handlers (EventBus)

    private void OnDragStarted(MatchItem item)
    {
        if (_isProcessing) return;
        _draggedItem = item;
        
        // Запоминаем КТО где стоял
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                _startSnapshot[x, y] = _grid[x, y];
    }

    private void OnPositionChanged(Vector2 localPos)
    {
        if (_draggedItem == null || _isProcessing) return;

        int targetX = Mathf.RoundToInt((localPos.x - _startPosition.x) / (_cellSize.x + _spacing.x));
        int targetY = Mathf.RoundToInt((localPos.y - _startPosition.y) / (_cellSize.y + _spacing.y));

        targetX = Mathf.Clamp(targetX, 0, _width - 1);
        targetY = Mathf.Clamp(targetY, 0, _height - 1);

        if (targetX != _draggedItem.Data.X || targetY != _draggedItem.Data.Y)
        {
            MatchItem targetItem = _grid[targetX, targetY];
            if (targetItem != null)
            {
                int oldX = _draggedItem.Data.X;
                int oldY = _draggedItem.Data.Y;

                // Перестановка по твоей схеме: таргет идет на место, где был тянущийся
                _grid[targetX, targetY] = _draggedItem;
                _grid[oldX, oldY] = targetItem;

                targetItem.UpdateData(oldX, oldY);
                targetItem.MoveTo(GetPositionForCell(oldX, oldY), 0.15f);
                
                _draggedItem.UpdateData(targetX, targetY);
            }
        }
    }

    private void OnDragEnded(MatchItem item)
    {
        if (_draggedItem == null) return;
        StartCoroutine(PostDragValidation());
    }

    private IEnumerator PostDragValidation()
    {
        _isProcessing = true;
        MatchItem item = _draggedItem;
        _draggedItem = null;

        // Сажаем «беглеца» в его новую ячейку
        yield return item.MoveTo(GetPositionForCell(item.Data.X, item.Data.Y), 0.1f).WaitForCompletion();

        var matches = FindAllMatches();
        if (matches.Count >= 3)
            yield return HandleMatchesRoutine(matches);
        else
            yield return RevertGrid(); // ОТКАТ

        _isProcessing = false;
    }

    #endregion

    private IEnumerator RevertGrid()
    {
        Sequence s = DOTween.Sequence();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                MatchItem original = _startSnapshot[x, y];
                _grid[x, y] = original;
                original.UpdateData(x, y);
                s.Join(original.MoveTo(GetPositionForCell(x, y), 0.2f));
            }
        }
        yield return s.WaitForCompletion();
    }

    private IEnumerator HandleMatchesRoutine(List<MatchItem> matches)
{
    while (matches.Count >= 3)
    {
        int pointsForMatch = matches.Count * _scorePerItem;

        foreach (var m in matches)
        {
            _grid[m.Data.X, m.Data.Y] = null;
            m.Despawn();
        }

        // 1. Оповещаем систему о нахождении матча (если нужно для чего-то еще)
        EventBus.SendEvent(new GameEvent(Events.MatchFound, matches.Count));
        
        // 2. ВОТ ЭТОТ ИВЕНТ ТЫ ИСКАЛ: Шлем очки в шину
        // Его поймают и ScoreDisplay, и CharacterJuicer
        EventBus.SendEvent(new GameEvent(Events.ScoreAdded, pointsForMatch));
        
        yield return new WaitForSeconds(0.2f);
        yield return CollapseColumns();
        matches = FindAllMatches();
    }
}

    private List<MatchItem> FindAllMatches()
    {
        HashSet<MatchItem> matched = new HashSet<MatchItem>();
        // Горизонтали
        for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width - 2; x++)
            {
                var t = _grid[x, y]?.Data.Type;
                if (t != null && _grid[x+1, y]?.Data.Type == t && _grid[x+2, y]?.Data.Type == t)
                { matched.Add(_grid[x,y]); matched.Add(_grid[x+1,y]); matched.Add(_grid[x+2,y]); }
            }
        // Вертикали
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height - 2; y++)
            {
                var t = _grid[x, y]?.Data.Type;
                if (t != null && _grid[x, y+1]?.Data.Type == t && _grid[x, y+2]?.Data.Type == t)
                { matched.Add(_grid[x,y]); matched.Add(_grid[x,y+1]); matched.Add(_grid[x,y+2]); }
            }
        return matched.ToList();
    }

    private IEnumerator CollapseColumns()
    {
        Sequence seq = DOTween.Sequence();
        for (int x = 0; x < _width; x++)
        {
            int empty = 0;
            for (int y = 0; y < _height; y++)
            {
                if (_grid[x, y] == null) empty++;
                else if (empty > 0)
                {
                    var item = _grid[x, y];
                    _grid[x, y - empty] = item;
                    _grid[x, y] = null;
                    item.UpdateData(x, y - empty);
                    seq.Join(item.MoveTo(GetPositionForCell(x, y - empty)));
                }
            }
            for (int i = 0; i < empty; i++)
            {
                int y = _height - empty + i;
                SpawnItem(x, y, Random.Range(0, _itemTypesCount));
                var item = _grid[x, y];
                item.transform.localPosition += new Vector3(0, 500, 0);
                seq.Join(item.MoveTo(GetPositionForCell(x, y)));
            }
        }
        yield return seq.WaitForCompletion();
    }
}