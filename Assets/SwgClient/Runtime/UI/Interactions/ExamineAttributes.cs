using Swg.Client.Runtime.UI.Theming;
using Swg.Client.Core.World;
using System;
using System.Text;
using System.Text.RegularExpressions;
using Swg.Client.Core;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using TMPro;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Interactions
{
    public sealed class ExamineAttributes : MonoBehaviour
    {
        public TMP_Text text;
        public UiTheme theme;
        ClientObjectInteractions interactions;
        StringTableLocalizer localizer;
        SwgId swgId;
        string description;
        static readonly Regex colors = new Regex(@"\\#(?:[0-9a-fA-F]{6}|\.)", RegexOptions.Compiled);
        void Awake()
        {
            interactions = GameServices.Resolve<ClientObjectInteractions>();
            localizer = GameServices.Resolve<StringTableLocalizer>();
        }
        void OnEnable()
        {
            interactions.AttributesChanged += OnAttributesChanged;
            theme.Changed += Refresh;
            Refresh();
        }
        void OnDisable()
        {
            interactions.AttributesChanged -= OnAttributesChanged;
            theme.Changed -= Refresh;
        }
        public void Show(SwgId target)
        {
            swgId = target;
            var state = GameServices.Resolve<WorldState>();
            description = state.SceneObjects.TryGet(target, out var sceneObject) ? GameServices.Resolve<SceneObjectNameResolver>().Description(sceneObject) : "";
            Refresh();
            interactions.RequestExamine(target);
        }
        void OnAttributesChanged(SwgId changedSwgId)
        {
            if (changedSwgId == swgId)
                Refresh();
        }
        static string Literal(string value) => "<noparse>" + colors.Replace(value, "") + "</noparse>";

        void Refresh()
        {
            var result = new StringBuilder();
            foreach (var attribute in interactions.GetAttributes(swgId))
            {
                string name = localizer.Lookup("obj_attr_n", attribute.Name);
                if (name.StartsWith("@", StringComparison.Ordinal))
                    name = attribute.Name.Replace('_', ' ');
                result.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(theme.attributeText)).Append('>')
                    .Append(Literal(name)).Append(":</color>    ").AppendLine(Literal(localizer.Localize(attribute.Value)));
            }
            if (!string.IsNullOrEmpty(description))
                result.AppendLine().AppendLine(Literal(description));
            text.text = result.ToString();
        }
    }
}
