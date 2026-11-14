using System;
using System.Collections;
using Swg.Client.Core.Characters;
using Swg.Client.Runtime.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.CharacterSheet
{
    public sealed class StatMigrationWindow : MonoBehaviour
    {
        [Serializable]
        public sealed class Row
        {
            public TMP_Text current;
            public TMP_Text target;
            public Slider slider;
        }
        public Row[] rows;
        public TMP_Text points;
        public TMP_Text error;
        public Button save;
        ClientCharacterSheet sheet;
        HamAttributes targets;
        bool initialized;
        readonly UnityAction<float>[] listeners = new UnityAction<float>[9];
        public int PointsRemaining => sheet.Data.MigrationBudget - targets.Total;
        void Awake() => sheet = GameServices.Resolve<ClientCharacterSheet>();
        void OnEnable()
        {
            initialized = false;
            error.text = "";
            save.interactable = false;
            points.text = "--";
            sheet.Data.Changed += Receive;
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].current.text = "--";
                rows[i].target.text = "--";
                rows[i].slider.interactable = false;
                int index = i;
                listeners[i] = value => Adjust(index, Mathf.RoundToInt(value));
                rows[i].slider.onValueChanged.AddListener(listeners[i]);
            }
            sheet.RequestMigration();
            StartCoroutine(Prepare());
        }
        IEnumerator Prepare()
        {
            yield return new WaitUntil(() => sheet.Loading.IsCompleted);
            if (sheet.Loading.IsFaulted) { Debug.LogException(sheet.Loading.Exception.GetBaseException(), this); yield break; }
            Receive(CharacterSheetChange.Migration);
        }
        void Receive(CharacterSheetChange changes)
        {
            if ((changes & CharacterSheetChange.Migration) == 0 || initialized || !sheet.Loading.IsCompletedSuccessfully || !sheet.Data.HasMigrationTargets || sheet.Player == null) return;
            targets = sheet.Data.MigrationTargets;
            var minimum = sheet.MigrationLimits(false);
            var maximum = sheet.MigrationLimits(true);
            var current = ClientCharacterSheet.Attributes(sheet.Player.Ham);
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].current.text = current[i].Base.ToString();
                rows[i].slider.minValue = minimum[i];
                rows[i].slider.maxValue = maximum[i];
                rows[i].slider.wholeNumbers = true;
                rows[i].slider.interactable = true;
            }
            initialized = true;
            Refresh();
        }
        public void Adjust(int index, int value)
        {
            if (!initialized) return;
            value = Mathf.Clamp(value, Mathf.RoundToInt(rows[index].slider.minValue), Mathf.RoundToInt(rows[index].slider.maxValue));
            targets[index] = Mathf.Min(value, targets[index] + Mathf.Max(0, PointsRemaining));
            Refresh();
        }
        void Refresh()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].slider.SetValueWithoutNotify(targets[i]);
                rows[i].target.text = targets[i].ToString();
            }
            points.text = PointsRemaining.ToString();
            save.interactable = PointsRemaining == 0;
        }
        public void Open() { gameObject.SetActive(true); transform.SetAsLastSibling(); }
        public void Save()
        {
            if (!initialized) return;
            if (sheet.SaveMigration(targets, out string message)) gameObject.SetActive(false);
            else error.text = message;
        }
        public void Cancel() { sheet.CancelMigration(); gameObject.SetActive(false); }
        void OnDisable()
        {
            sheet.Data.Changed -= Receive;
            for (int i = 0; i < rows.Length; i++) rows[i].slider.onValueChanged.RemoveListener(listeners[i]);
            StopAllCoroutines();
        }
    }
}
