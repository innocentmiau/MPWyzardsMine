using TMPro;
using UnityEngine;

namespace Scripts.Systems.Console
{
    public class LinePanel : MonoBehaviour
    {

        private RectTransform _rect;
        private TMP_Text _tmp;
        private void Start()
        {
            _tmp = GetComponentInChildren<TMP_Text>();
            _rect = GetComponent<RectTransform>();
        }
        
        
        public void UpdateText(string newText, float newWidth)
        {
            if (!_tmp) _tmp = GetComponentInChildren<TMP_Text>();
            if (!_rect) _rect = GetComponent<RectTransform>();
            if (_tmp) _tmp.text = newText;
            if (_rect) _rect.sizeDelta = new Vector2(newWidth, _rect.sizeDelta.y);
        }
    }
}