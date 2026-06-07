using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Scripts.Systems.Console
{
    public class ExpandConsole : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {

        [SerializeField] private RectTransform rectToExpand;
        [SerializeField] private float minimumSize;
        [SerializeField] private ConsoleManager consoleManager;
        [SerializeField] private RectTransform canvasRect;

        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
        }

        private bool _hasClicked;
        public void OnPointerDown(PointerEventData eventData)
        {
            _hasClicked = true;
        }

        private void Update()
        {
            if (!_hasClicked) return;
            //float maxSize = Screen.height;
            //float rawY = maxSize - Mouse.current.position.ReadValue().y;
            //Debug.Log($"Screen height: {maxSize}, rawY: {rawY}, mouseHeight: {Mouse.current.position.ReadValue().y}");
            //float newSize = Mathf.Lerp(maxSize, minimumSize, (rawY / maxSize));
            /*if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Mouse.current.position.ReadValue(),
                    _camera, out Vector2 localPoint))
            {
                float newSize = Mathf.Clamp(-localPoint.y, minimumSize, 1080f);
                rectToExpand.sizeDelta = new Vector2(rectToExpand.sizeDelta.x, newSize);
            }*/
            float canvasScale = canvasRect.localScale.y;
            float rawY = Screen.height - Mouse.current.position.ReadValue().y;
            float newSize = rawY / canvasScale;
            newSize = Mathf.Clamp(newSize, minimumSize, 1080f);
            rectToExpand.sizeDelta = new Vector2(rectToExpand.sizeDelta.x, newSize);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _hasClicked = false;
            consoleManager?.UpdateConsoleSize();
        }
    }
}