using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VectorDash
{
    /// <summary>
    /// Displays acquired power-ups in a bottom inventory bar.
    /// Supports direct mouse/touch clicking or keyboard hotkeys (1, 2, 3).
    /// Dynamically constructs clean UI slots if not pre-configured in the inspector.
    /// </summary>
    public class InventoryBarUI : MonoBehaviour
    {
        [System.Serializable]
        public class PowerSlot
        {
            public PowerUpType type;
            public Button button;
            public Image iconImage;
            public Image background;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI countText;
            public CanvasGroup canvasGroup;
        }

        [Header("Slot References")]
        public PowerSlot blastSlot;
        public PowerSlot healthSlot;
        public PowerSlot shieldSlot;

        private PlayerPowerController powerController;

        private void Start()
        {
            EnsurePlayerConnected();
            EnsureSlotsConstructed();
            RefreshAllSlots();
        }

        private void Update()
        {
            if (powerController == null)
            {
                EnsurePlayerConnected();
            }
        }

        private void EnsurePlayerConnected()
        {
            if (powerController != null) return;

            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                powerController = pc.GetComponent<PlayerPowerController>();
                if (powerController == null)
                {
                    powerController = pc.gameObject.AddComponent<PlayerPowerController>();
                }

                powerController.OnPowerInventoryChanged += OnInventoryChanged;
                RefreshAllSlots();
            }
        }

        private void OnDestroy()
        {
            if (powerController != null)
            {
                powerController.OnPowerInventoryChanged -= OnInventoryChanged;
            }
        }

        private void OnInventoryChanged(PowerUpType type, int count)
        {
            UpdateSlotUI(GetSlot(type), count);
        }

        private PowerSlot GetSlot(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.DoubleBlast: return blastSlot;
                case PowerUpType.HealthIncrease: return healthSlot;
                case PowerUpType.Protection: return shieldSlot;
                default: return null;
            }
        }

        public void RefreshAllSlots()
        {
            if (blastSlot != null)
            {
                int c = powerController != null ? powerController.blastCount : 0;
                UpdateSlotUI(blastSlot, c);
            }
            if (healthSlot != null)
            {
                int c = powerController != null ? powerController.healthCount : 0;
                UpdateSlotUI(healthSlot, c);
            }
            if (shieldSlot != null)
            {
                int c = powerController != null ? powerController.shieldCount : 0;
                UpdateSlotUI(shieldSlot, c);
            }
        }

        private void UpdateSlotUI(PowerSlot slot, int count)
        {
            if (slot == null) return;

            if (slot.countText != null)
            {
                slot.countText.text = count > 0 ? $"x{count}" : "0";
            }

            if (slot.canvasGroup != null)
            {
                slot.canvasGroup.alpha = count > 0 ? 1f : 0.4f;
                slot.canvasGroup.interactable = count > 0;
            }
            else if (slot.button != null)
            {
                slot.button.interactable = count > 0;
            }
        }

        public void OnClickUsePower(int typeIndex)
        {
            if (powerController == null) EnsurePlayerConnected();
            if (powerController != null)
            {
                powerController.UsePower((PowerUpType)typeIndex);
            }
        }

        /// <summary>
        /// Auto-generates clean UI layout if inspector slots are empty.
        /// </summary>
        private void EnsureSlotsConstructed()
        {
            if (blastSlot != null && blastSlot.button != null) return;

            // Horizontal layout container
            HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 25;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            blastSlot = CreateSlot(PowerUpType.DoubleBlast, "BLAST [1]", new Color(1f, 0.3f, 0.1f), 0);
            healthSlot = CreateSlot(PowerUpType.HealthIncrease, "HEAL [2]", new Color(0.1f, 1f, 0.4f), 1);
            shieldSlot = CreateSlot(PowerUpType.Protection, "SHIELD [3]", new Color(0f, 0.9f, 1f), 2);
        }

        private PowerSlot CreateSlot(PowerUpType type, string label, Color themeColor, int index)
        {
            GameObject slotObj = new GameObject($"Slot_{type}");
            slotObj.transform.SetParent(transform, false);

            RectTransform rt = slotObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(165, 150);

            CanvasGroup cg = slotObj.AddComponent<CanvasGroup>();

            // Background Card
            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.18f, 0.85f);

            Button btn = slotObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = themeColor;
            cb.pressedColor = Color.white * 0.8f;
            cb.disabledColor = new Color(1f, 1f, 1f, 0.3f);
            btn.colors = cb;
            btn.onClick.AddListener(() => OnClickUsePower((int)type));

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.62f);
            iconRt.anchorMax = new Vector2(0.5f, 0.62f);
            iconRt.sizeDelta = new Vector2(58 , 58);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = themeColor;
            iconImg.sprite = PowerUpCollectible.GetOrCreateCircleSprite();

            // Name Label
            GameObject nameObj = new GameObject("NameLabel");
            nameObj.transform.SetParent(slotObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(1, 0.35f);
            nameRt.offsetMin = new Vector2(4, 2);
            nameRt.offsetMax = new Vector2(-4, 0);
            TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = label;
            nameTxt.fontSize =26;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.color = themeColor;

            // Count Badge
            GameObject countObj = new GameObject("CountBadge");
            countObj.transform.SetParent(slotObj.transform, false);
            RectTransform countRt = countObj.AddComponent<RectTransform>();
            countRt.anchorMin = new Vector2(1, 1);
            countRt.anchorMax = new Vector2(1, 1);
            countRt.anchoredPosition = new Vector2(-12, -12);
            countRt.sizeDelta = new Vector2(42, 26);
            TextMeshProUGUI countTxt = countObj.AddComponent<TextMeshProUGUI>();
            countTxt.text = "0";
            countTxt.fontSize = 29;
            countTxt.fontStyle = FontStyles.Bold;
            countTxt.alignment = TextAlignmentOptions.Center;
            countTxt.color = Color.white;

            PowerSlot slot = new PowerSlot
            {
                type = type,
                button = btn,
                iconImage = iconImg,
                background = bg,
                nameText = nameTxt,
                countText = countTxt,
                canvasGroup = cg
            };

            return slot;
        }
    }
}
