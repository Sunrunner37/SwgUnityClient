using UnityEngine;
using Swg.Client.Runtime.Assets.UI;
using Swg.Client.Runtime.Commands;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Swg.Client.Runtime.UI.Commands
{
    public sealed class CommandButton : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        CommandCatalog catalog;
        CommandVisuals visuals;
        ICommandDispatcher dispatcher;

        public void Initialize(CommandCatalog catalog, CommandVisuals visuals, ICommandDispatcher dispatcher)
        {
            this.catalog = catalog;
            this.visuals = visuals;
            this.dispatcher = dispatcher;
        }

        public CommandDrag drag;
        public Image icon;
        public TMP_Text label;
        public bool browserItem;
        [SerializeField]
        string bindingKey = "";
        public string BindingKey => bindingKey;
        public CommandCatalog.Command Command { get; private set; }

        public void Bind(CommandCatalog.Command command)
        {
            bindingKey = command?.Key ?? "";
            Display(command);
        }

        public void RefreshBinding() => Display(catalog.Find(bindingKey));
        void Display(CommandCatalog.Command command)
        {
            Command = command;
            icon.sprite = visuals.GetIcon(command);
            icon.enabled = icon.sprite != null;
            if (label != null)
                label.text = command?.Label ?? "";
        }

        public void Activate()
        {
            if (Command == null)
                return;
            dispatcher.Execute(Command.Name, Command.Arguments, out string error);
            if (error != null)
                Debug.LogWarning(error, this);
        }
        public void OnPointerClick(PointerEventData pointer)
        {
            if (pointer.button == PointerEventData.InputButton.Left && (browserItem && pointer.clickCount == 2))
                Activate();
        }

        public void OnBeginDrag(PointerEventData pointer)
        {
            if (Command != null && pointer.button == PointerEventData.InputButton.Left)
                drag.Begin(Command, icon.sprite, pointer);
        }

        public void OnDrag(PointerEventData pointer)
        {
            if (drag.Command != null)
                drag.Move(pointer);
        }

        public void OnEndDrag(PointerEventData pointer) => drag.End();
        public void OnDrop(PointerEventData pointer)
        {
            if (!browserItem && drag.Command != null)
                Bind(drag.Command);
        }
    }
}
