using System;
using System.Collections;
using Scripts.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Scripts.Systems.Interface
{
    public class BetterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {

        [SerializeField] private UnityEvent OnClick;
        [SerializeField] private GameObject enabledOnHover;

        private bool _interactable;

        public void SetInteractable(bool isInteractable)
        {
            _interactable = isInteractable;
            if (!_interactable) StoppedHovering();
        }
        
        private Vector3 _startScale;

        private void Start()
        {
            _startScale = transform.localScale;
            if (enabledOnHover) enabledOnHover.SetActive(false);
            SetInteractable(true);
        }

        private void StartNewAnimation(float endSizeMultiplier)
        {
            StopAllCoroutines();
            StartCoroutine(Animation(endSizeMultiplier, Constants.BETTER_BUTTON_ANIMATION_DURATION));
        }
        
        private IEnumerator Animation(float endSizeMultiplier, float duration)
        {
            Vector3 currentScale = transform.localScale;
            Vector3 endScale = _startScale * endSizeMultiplier;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(currentScale, endScale, elapsed / duration);
                yield return null;
            }
            transform.localScale = endScale;
        }

        private bool _isHovering = false;
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable) return;
            _isHovering = true;
            if (enabledOnHover) enabledOnHover.SetActive(true);
            StartNewAnimation(1.15f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StoppedHovering();
        }

        private void StoppedHovering()
        {
            _isHovering = false;
            if (enabledOnHover) enabledOnHover.SetActive(false);
            StartNewAnimation(1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable) return;
            StartNewAnimation(.9f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StartNewAnimation(_isHovering ? 1.15f : 1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable) return;
            OnClick?.Invoke();
        }
    }
}