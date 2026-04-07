using UnityEngine;
using EventManagment;

public class GameManager : PersistentSingleton<GameManager>
{   
    private EventBus _bus;
    public EventBus BUS => _bus;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        base.Awake();
        _bus = new EventBus(GameEvent => false);
        _bus.SendEvent(new GameEvent(Events.Debug));
    }


}
