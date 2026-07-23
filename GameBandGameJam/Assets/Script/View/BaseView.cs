#nullable enable
using UnityEngine;

public abstract class BaseView : MonoBehaviour
{
    public bool IsActive => isActive;
    bool isActive;

    /// <summary>
    /// Called when the view is first pushed onto the stack and created.
    /// Use this for initialization and show animations.
    /// </summary>
    public virtual void OnPresent()
    {
        gameObject.SetActive(true);
        isActive = true;
    }

    /// <summary>
    /// Called when the view is popped from the stack and about to be destroyed.
    /// Use this for cleanup and hide animations.
    /// </summary>
    public virtual void OnDismiss()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when the view becomes the topmost view on the stack.
    /// This happens after OnPresent for new views, or when the view above is popped.
    /// </summary>
    public virtual void OnEnterForeground()
    {
        gameObject.SetActive(true);
        isActive = true;
    }

    /// <summary>
    /// Called when another view is pushed on top of this view.
    /// The view is still in the stack but no longer the topmost.
    /// </summary>
    public virtual void OnEnterBackground()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
}