using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragableObject : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public SlotType Type;
    public BodyPart HeldPart { get; private set; }
    private Transform _target { get { return HeldPart.transform; } }

    private Action<BodyPart> _onReleaseAction;

    private int _initialPointerId;
    private Camera _mainCamera;
    private Vector3 _screenPoint;
    private Vector3 _startLocation;

    public bool IsInUse { get { return HeldPart != null; } }

    private void Awake()
    {
        _initialPointerId = int.MaxValue;
        _mainCamera = Camera.main;

        _screenPoint = Vector3.zero;
        _screenPoint.z = 10;

        gameObject.SetActive(false);
    }

    public void SetTarget<T>(T element, Action<T> onRelease) where T : BodyPart
    {
        _onReleaseAction = onRelease as Action<BodyPart>;

        gameObject.SetActive(true);

        HeldPart = element;
        _target.SetParent(transform);
        _target.localPosition = Vector3.zero;
        
        _target.SetParent(null, true);
        _target.localScale = Vector3.one;
        _target.localPosition = new Vector3(_target.localPosition.x * 0.1f, _target.localPosition.y * 0.1f, 0);

        element.Enable();
    }

    private void RemoveTarget()
    {
        gameObject.SetActive(false);
        HeldPart = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_initialPointerId == int.MaxValue)
        {
            _startLocation = _target.position;
            _initialPointerId = eventData.pointerId;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_initialPointerId == eventData.pointerId)
        {
            _initialPointerId = int.MaxValue;

            if (false) //Todo : Check if we're on the correct drop area
            {
                //_target.position = _startLocation;
            }
            else
            {
                HeldPart.Disable();
                _onReleaseAction?.Invoke(HeldPart);
                RemoveTarget();
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_initialPointerId == eventData.pointerId)
        {
            _screenPoint.x = eventData.position.x;
            _screenPoint.y = eventData.position.y;
            _target.position = _mainCamera.ScreenToWorldPoint(_screenPoint);
        }
    }
}