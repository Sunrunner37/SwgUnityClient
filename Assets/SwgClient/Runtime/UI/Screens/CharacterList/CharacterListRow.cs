using System;
using UnityEngine;
using UnityEngine.UI;
using CharacterInfo = Swg.Client.Core.Networking.Login.CharacterInfo;

namespace Swg.Client.Runtime.UI.Screens.CharacterList
{
    public sealed class CharacterListRow : MonoBehaviour
    {
        public Text characterName;
        public Text galaxy;
        public Text planet;
        public Text server;
        public Button button;
        public Image background;
        public Outline selectionOutline;
        public Color normalColor = new Color(0, .84f, .98f, .2f);
        public Color selectedColor = new Color32(53, 203, 215, 255);
        public Color textColor = new Color32(150, 244, 252, 255);
        public Color selectedTextColor = new Color32(0, 56, 66, 255);
        public CharacterInfo Character { get; private set; }

        public void Bind(CharacterInfo character, string galaxyName, Action select)
        {
            Character = character;
            characterName.text = character.Name;
            galaxy.text = galaxyName;
            button.onClick.AddListener(() => select());
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            background.color = selected ? selectedColor : normalColor;
            if (selectionOutline != null)
                selectionOutline.enabled = selected;
            var labels = new[]
            {
                characterName,
                galaxy,
                planet,
                server
            };
            foreach (var label in labels)
            {
                if (label != null)
                {
                    label.color = selected ? selectedTextColor : textColor;
                }
            }
        }
    }
}
