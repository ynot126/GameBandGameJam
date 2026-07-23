using UnityEngine;
using System;
using System.Collections.Generic;

public class ViewManager : Singleton<ViewManager>
{
    
    [SerializeField] Transform _viewContainer;

    readonly Stack<BaseView> _viewStack = new Stack<BaseView>();

    public BaseView TopView => _viewStack.Count > 0 ? _viewStack.Peek() : null;
    public int StackCount => _viewStack.Count;
    
    public override void Initialize()
    {
        
    }
    /// <summary>
    /// Pushes a view by instantiating the given prefab.
    /// The current top view receives OnEnterBackground.
    /// The new view receives OnPresent followed by OnEnterForeground.
    /// </summary>
    public void PushView(BaseView view)
    {

        // Send current top to background
        if (_viewStack.Count > 0)
        {
            BaseView currentTop = _viewStack.Peek();
            currentTop.OnEnterBackground();
        }
        // Instantiate and push the new view
        _viewStack.Push(view);
        view.OnPresent();
        Debug.Log($"[ViewManager] Pushed instance: {view.GetType().Name} | Stack count: {_viewStack.Count}");
    }
    
    public void PopView()
    {
        if (_viewStack.Count == 0)
        {
            Debug.LogWarning("[ViewManager] Cannot pop. View stack is empty.");
            return;
        }

        BaseView poppedView = _viewStack.Pop();
        
        poppedView.OnDismiss();
        
        Debug.Log($"[ViewManager] Popped: {poppedView.GetType().Name} | Stack count: {_viewStack.Count}");
        

        // Bring previous view to foreground
        if (_viewStack.Count > 0)
        {
            BaseView newTop = _viewStack.Peek();
            newTop.OnEnterForeground();
        }
    }

    /// <summary>
    /// Removes all views from the stack without restoring a previous top view.
    /// Call before switching scenes so the persistent ViewManager does not retain stale references.
    /// </summary>
    public void ClearStack()
    {
        int clearedCount = _viewStack.Count;
        while (_viewStack.Count > 0)
        {
            BaseView view = _viewStack.Pop();
            if (view)
                view.OnDismiss();
        }
        Debug.Log($"[ViewManager] Cleared stack ({clearedCount} view(s)).");
    }
}