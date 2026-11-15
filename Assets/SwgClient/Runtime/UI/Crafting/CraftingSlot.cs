using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Swg.Client.Runtime.UI.Crafting
{
    public sealed class CraftingSlot : MonoBehaviour, IDropHandler
    {
        public TMP_Text label;
        public TMP_Text amount;
        public Image frame;
        public Image quality;
        public RawImage icon;
        public Image loadingIndicator;
        public Button button;
        public Action Insert;
        public Action Remove;
        bool populated;
        float lastClick = float.NegativeInfinity;
        Coroutine loading;
        void Awake() => button.onClick.AddListener(Click);
        void Click()
        {
            if (!populated) Insert?.Invoke();
            else if (Time.unscaledTime - lastClick < .3f) { Remove?.Invoke(); lastClick = float.NegativeInfinity; return; }
            lastClick = Time.unscaledTime;
        }
        public static Color QualityColor(float value) => value < .25f ? Color.red : value < .75f ? new Color(1, 1, 0) : Color.green;
        public void Show(Texture texture, int quantity, int required, bool qualifies, float score, bool busy, Color background)
        {
            populated = quantity > 0;
            amount.text = quantity + "/" + required;
            icon.texture = texture;
            frame.color = qualifies && quantity < required ? new Color(0, .65f, .25f, 1) : background;
            quality.fillAmount = Mathf.Clamp01(score);
            quality.color = QualityColor(score);
            button.interactable = !busy && (populated || qualifies);
        }
        public void BeginLoading()
        {
            if (loading != null) StopCoroutine(loading);
            loading = StartCoroutine(ShowLoading());
        }
        IEnumerator ShowLoading()
        {
            icon.enabled = false;
            loadingIndicator.gameObject.SetActive(true);
            for (float age = 0; age < .2f; age += Time.unscaledDeltaTime)
            {
                loadingIndicator.fillAmount = age / .2f;
                yield return null;
            }
            loadingIndicator.gameObject.SetActive(false);
            icon.enabled = true;
            loading = null;
        }
        void OnDisable()
        {
            if (loading != null) StopCoroutine(loading);
            loading = null;
            if (loadingIndicator != null) loadingIndicator.gameObject.SetActive(false);
            icon.enabled = true;
        }
        public void OnDrop(PointerEventData pointer)
        {
            if (button.interactable && pointer.pointerDrag != null && pointer.pointerDrag.GetComponent<CraftingIngredientDrag>() != null) Insert?.Invoke();
        }
    }
}
