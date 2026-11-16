using System;
using System.Linq;
using System.Threading;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using UnityEngine;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Server-owned vehicle repair quotes. Core3 validates and performs accepted repairs.</summary>
    public sealed class ClientVehicleRepairs : IDisposable
    {
        public sealed class Quote
        {
            public uint PageId { get; }
            public string Title { get; }
            public string Prompt { get; }
            public string[] Rows { get; }
            internal Quote(SuiPageData page)
            {
                PageId = page.Id;
                Title = Property(page, "bg.caption.lblTitle");
                Prompt = Property(page, "Prompt.lblPrompt");
                Rows = page.Commands.Where(command => command.Type == 3
                    && command.NarrowParameters.Length >= 2
                    && command.NarrowParameters[0].StartsWith("List.dataList.", StringComparison.Ordinal)
                    && command.NarrowParameters[1] == "Text")
                    .Select(command => command.WideParameters.FirstOrDefault() ?? "").ToArray();
            }
        }
        readonly Action<SuiEventNotificationPacket> send;
        readonly SynchronizationContext context = SynchronizationContext.Current;
        bool disposed;
        public Quote CurrentQuote { get; private set; }
        public event Action Changed;
        public ClientVehicleRepairs(Action<SuiEventNotificationPacket> send) => this.send = send;
        static string Property(SuiPageData page, string widget) => page.Commands
            .Where(command => command.Type == 3 && command.NarrowParameters.Length >= 2
                && command.NarrowParameters[0] == widget && command.NarrowParameters[1] == "Text")
            .Select(command => command.WideParameters.FirstOrDefault() ?? "").LastOrDefault() ?? "";
        public void Receive(SuiPageData page) => context.Post(_ =>
        {
            if (disposed) return;
            if (page.RootPage != "Script.listBox" || Property(page, "bg.caption.lblTitle") != "@pet/pet_menu:confirm_repairs_t")
            {
                Debug.LogWarning("Unsupported server dialog: " + page.RootPage);
                return;
            }
            CurrentQuote = new Quote(page);
            Changed?.Invoke();
        }, null);
        public void Close(uint pageId) => context.Post(_ =>
        {
            if (!disposed && CurrentQuote?.PageId == pageId)
            {
                CurrentQuote = null;
                Changed?.Invoke();
            }
        }, null);
        public void Respond(bool accept)
        {
            if (CurrentQuote == null) return;
            var quote = CurrentQuote;
            CurrentQuote = null;
            // Core3's repair list box subscribes to SelectedRow and title, but does not require a row selection.
            send(new SuiEventNotificationPacket(quote.PageId, accept ? 0u : 1u, "-1", quote.Title));
            Changed?.Invoke();
        }
        public void Dispose() => disposed = true;
    }
}
