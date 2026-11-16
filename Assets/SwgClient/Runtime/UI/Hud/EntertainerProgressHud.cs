using System;
using Swg.Client.Core.Characters;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Hud
{
    /// <summary>Optional advice below the buff HUD. Real buff icons remain entirely server-owned.</summary>
    public sealed class EntertainerProgressHud : MonoBehaviour
    {
        [Serializable]
        public sealed class Row
        {
            public GameObject panel;
            public TMP_Text heading;
            public TMP_Text progress;
            public TMP_Text advice;
            public Image duration;
            public Image strength;
        }
        public RectTransform buffPanel;
        public RectTransform panel;
        public Row music;
        public Row dance;
        ClientEntertainment entertainment;
        SceneObjectNameResolver names;
        double nextRefresh;
        readonly Vector3[] corners = new Vector3[4];
        void Awake()
        {
            entertainment = GameServices.Resolve<ClientEntertainment>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
        }
        void LateUpdate()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            entertainment.Refresh(now);
            panel.gameObject.SetActive(entertainment.Music != null || entertainment.Dance != null);
            if (!panel.gameObject.activeSelf) return;
            buffPanel.GetWorldCorners(corners);
            panel.position = corners[0];
            panel.anchoredPosition += new Vector2(buffPanel.rect.width, -8);
            if (now < nextRefresh) return;
            nextRefresh = now + .2;
            Show(music, entertainment.Music, "MUSIC", now);
            Show(dance, entertainment.Dance, "DANCE", now);
        }
        void Show(Row row, ClientEntertainment.Session session, string kind, double now)
        {
            row.panel.SetActive(session != null);
            if (session == null) return;
            var estimate = session.Estimate;
            row.heading.text = kind + " - " + names.Resolve(session.Performer);
            row.duration.fillAmount = estimate.Minutes / (float)EntertainerBuffEstimate.MaximumMinutes;
            row.strength.fillAmount = estimate.Strength / (float)EntertainerBuffEstimate.MasterStrength;
            row.progress.text = "Duration ~" + estimate.Minutes + " / 120 min\nStrength ~" + estimate.Strength + " / 125%";
            if (!session.Grouped)
                row.advice.text = "Not grouped - eligibility unknown.";
            else if (!estimate.MinimumReached(now))
                row.advice.text = "Wait at least " + Math.Ceiling(60 - estimate.Elapsed(now)) + "s before stopping.";
            else if (session.StrengthPerPulse <= 0)
                row.advice.text = "Performance data unavailable.";
            else if (estimate.Minutes == 120 && estimate.Strength == 125)
                row.advice.text = "Estimated full - stop to receive.";
            else
                row.advice.text = "Minimum met - full in ~" + TimeSpan.FromSeconds(estimate.SecondsToFull(now, session.StrengthPerPulse)).ToString(@"m\:ss") + ".";
            row.advice.text += "\n" + (int)estimate.Elapsed(now) + "s observed - " + estimate.Flourishes + " flourishes";
        }
    }
}
