using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DesktopWindowModel))]
    [RequireComponent(typeof(DesktopWindowView))]
    public sealed class DesktopWindowController : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        [SerializeField] private DesktopWindowManager manager;
        [SerializeField] private DesktopWindowModel model;
        [SerializeField] private DesktopWindowView view;

        private readonly HashSet<DesktopWindowMovementLockToken> movementLocks = new HashSet<DesktopWindowMovementLockToken>();
        private int nextMovementLockId = 1;
        private Vector2 dragPointerOffset;
        private bool dragging;
        private bool pointerStartedOnTitleBar;

        public event Action<DesktopWindowController> DragStarted;
        public event Action<DesktopWindowController> Dragging;
        public event Action<DesktopWindowController> DragEnded;

        public DesktopWindowModel Model => model;
        public DesktopWindowView View => view;
        public DesktopWindowManager Manager => manager;
        public bool IsDragging => dragging;
        public bool MovementLocked => movementLocks.Count > 0;

        private void Awake()
        {
            ResolveReferences();
            ApplyModelToView();
        }

        private void OnEnable()
        {
            ResolveReferences();
            manager?.RegisterWindow(this);
            ApplyModelToView();
        }

        private void OnDisable()
        {
            CancelDragIfNeeded();
            manager?.UnregisterWindow(this);
        }

        private void OnDestroy()
        {
            CancelDragIfNeeded();
        }

        public void SetManager(DesktopWindowManager value)
        {
            if (manager == value)
            {
                return;
            }

            manager?.UnregisterWindow(this);
            manager = value;
            if (isActiveAndEnabled)
            {
                manager?.RegisterWindow(this);
            }
        }

        public DesktopWindowMovementLockToken AcquireMovementLock(string ownerName)
        {
            string safeOwnerName = string.IsNullOrWhiteSpace(ownerName) ? "Unnamed" : ownerName;
            DesktopWindowMovementLockToken token = new DesktopWindowMovementLockToken(nextMovementLockId++, safeOwnerName);
            movementLocks.Add(token);
            return token;
        }

        public bool ReleaseMovementLock(DesktopWindowMovementLockToken token)
        {
            return token.IsValid && movementLocks.Remove(token);
        }

        public void ClearMovementLocks()
        {
            movementLocks.Clear();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            manager?.FocusWindow(this);
            pointerStartedOnTitleBar = IsPointerInsideTitleBar(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag() || !pointerStartedOnTitleBar || manager == null || view.WindowRoot == null)
            {
                return;
            }

            RectTransform desktopBounds = manager.DesktopBounds;
            if (desktopBounds == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(desktopBounds, eventData.position, eventData.pressEventCamera, out Vector2 pointerLocalPosition))
            {
                return;
            }

            dragPointerOffset = pointerLocalPosition - view.WindowRoot.anchoredPosition;
            dragging = true;
            manager.NotifyDragStarted(this);
            DragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || manager == null || view.WindowRoot == null)
            {
                return;
            }

            RectTransform desktopBounds = manager.DesktopBounds;
            if (desktopBounds == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(desktopBounds, eventData.position, eventData.pressEventCamera, out Vector2 pointerLocalPosition))
            {
                return;
            }

            Vector2 desired = pointerLocalPosition - dragPointerOffset;
            Vector2 clamped = DesktopWindowGeometry.ClampAnchoredPosition(desktopBounds.rect, desired, view.WindowRoot.rect.size, view.WindowRoot.pivot);
            view.WindowRoot.anchoredPosition = clamped;
            model.SetPosition(clamped);
            manager.NotifyDragging(this);
            Dragging?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDrag();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerStartedOnTitleBar = false;
        }

        public void ApplyModelToView()
        {
            ResolveReferences();
            if (model == null || view == null)
            {
                return;
            }

            if (view.WindowRoot != null)
            {
                view.WindowRoot.anchoredPosition = model.CurrentPosition;
                view.WindowRoot.sizeDelta = model.CurrentSize;
            }

            view.ApplyTitle(model.Title);
            view.ApplyFocus(model.Focused);
            view.ApplyVisible(model.Visible);
        }

        public void SetFocused(bool focused)
        {
            ResolveReferences();
            if (model == null || view == null)
            {
                return;
            }

            model.SetFocused(focused);
            view.ApplyFocus(focused);
        }

        private bool CanDrag()
        {
            return model != null && model.DraggingAllowed && !MovementLocked;
        }

        private bool IsPointerInsideTitleBar(PointerEventData eventData)
        {
            return view != null
                && view.TitleBar != null
                && RectTransformUtility.RectangleContainsScreenPoint(view.TitleBar, eventData.position, eventData.pressEventCamera);
        }

        private void EndDrag()
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            manager?.NotifyDragEnded(this);
            DragEnded?.Invoke(this);
        }

        private void CancelDragIfNeeded()
        {
            EndDrag();
            pointerStartedOnTitleBar = false;
        }

        private void ResolveReferences()
        {
            if (model == null)
            {
                model = GetComponent<DesktopWindowModel>();
            }

            if (view == null)
            {
                view = GetComponent<DesktopWindowView>();
            }
        }
    }
}
