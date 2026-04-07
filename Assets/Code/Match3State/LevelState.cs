public enum LevelState
{
    Idle,           // Ждём ввод
    Dragging,       // Игрок тащит
    Swapping,       // Происходит свап
    Resolving,      // Проверка матчей
    Animating,      // Падение/спавн
    Locked          // Полная блокировка
}