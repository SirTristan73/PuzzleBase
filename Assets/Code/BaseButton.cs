using UnityEngine;
using UnityEngine.UI;

public abstract class BaseButton : MonoBehaviour
{
    [SerializeField] private Button _button;

    private void OnEnable()
    {
        Init();
    }

    private void OnDisable()
    {
        
    }

    public virtual void Init()
    {
        _button?.onClick.AddListener(OnClick);
    }

    public virtual void DeInit()
    {
        _button?.onClick.RemoveListener(OnClick);
    }

    public virtual void OnClick()
    {
        
    }
}
