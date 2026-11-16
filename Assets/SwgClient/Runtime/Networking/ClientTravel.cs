using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Networking.Game.Packets.Player;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World.Travel;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Travel selection uses native fares and the server's current incoming-travel/starport lists. The server owns purchasing.</summary>
    public sealed class ClientTravel : IDisposable
    {
        public sealed class Point
        {
            public string Name
            {
                get;
            }
            public Vector3 SwgPosition
            {
                get;
            }
            public uint Tax
            {
                get;
            }
            public bool Interplanetary
            {
                get;
            }
            public Point(string name, Vector3 position, uint tax, bool interplanetary)
            {
                Name = name;
                SwgPosition = position;
                Tax = tax;
                Interplanetary = interplanetary;
            }
        }
        public sealed class TicketSelection
        {
            public uint PageId { get; }
            public string[] Destinations { get; }
            internal TicketSelection(SuiPageData page)
            {
                PageId = page.Id;
                Destinations = page.Commands.Where(command => command.Type == 3 && command.NarrowParameters.Length >= 2
                    && command.NarrowParameters[0].StartsWith("List.dataList.", StringComparison.Ordinal)
                    && command.NarrowParameters[1] == "Text").Select(command => command.WideParameters.FirstOrDefault() ?? "").ToArray();
            }
        }
        public TicketSelection Tickets { get; private set; }
        readonly Action<SuiEventNotificationPacket> respond;
        readonly CurrentPlayer player;
        readonly ClientCommands commands;
        readonly Action<PlanetTravelPointListRequestPacket> request;
        readonly SynchronizationContext context;
        readonly Dictionary<string, Point[]> lists = new Dictionary<string, Point[]>();
        readonly HashSet<string> requested = new HashSet<string>();
        readonly Task<TravelRoutes> routesRead;
        public Task Ready => routesRead;
        TravelRoutes routes;
        bool disposed;
        public bool IsOpen
        {
            get; private set;
        }
        public bool Purchasing
        {
            get; private set;
        }
        public bool PurchaseComplete
        {
            get; private set;
        }
        public bool RoundTrip
        {
            get; private set;
        }
        public string DeparturePlanet
        {
            get; private set;
        }
        public string DeparturePoint
        {
            get; private set;
        }
        public string SelectedPlanet
        {
            get; private set;
        }
        public string SelectedPoint
        {
            get; private set;
        }
        public event Action Changed;
        Point Origin => lists.TryGetValue(DeparturePlanet ?? "", out var points) ? points.FirstOrDefault(p => p.Name == DeparturePoint) : null;
        public IEnumerable<string> Planets => routes == null ? Enumerable.Empty<string>() : routes.Planets.Where(p => p == DeparturePlanet || Origin?.Interplanetary == true && routes.Fare(DeparturePlanet, p) > 0 && DestinationsFor(p).Any());
        IEnumerable<Point> DestinationsFor(string planet) => lists.TryGetValue(planet ?? "", out var points) ? points.Where(p => planet == DeparturePlanet ? p.Name != DeparturePoint : p.Interplanetary) : Enumerable.Empty<Point>();
        public IReadOnlyList<Point> Destinations => DestinationsFor(SelectedPlanet).OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
        public Point Selected => DestinationsFor(SelectedPlanet).FirstOrDefault(p => p.Name == SelectedPoint);
        public int Cost => Selected == null || routes == null ? 0 : checked((routes.Fare(DeparturePlanet, SelectedPlanet) + (int)(Origin?.Tax ?? 0)) * (RoundTrip ? 2 : 1));
        public bool CanPurchase => IsOpen && !Purchasing && Selected != null && routes?.Fare(DeparturePlanet, SelectedPlanet) > 0;
        public ClientTravel(TreRuntimeAssets assets, CurrentPlayer player, ClientCommands commands, Action<PlanetTravelPointListRequestPacket> request, Action<SuiEventNotificationPacket> respond)
        {
            this.respond = respond;
            this.player = player;
            this.commands = commands;
            this.request = request;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create travel on Unity's main thread.");
            routesRead = assets.ReadAsync(loader => new TravelRoutes(DataTableFile.Parse(loader.Read("datatables/travel/travel.iff"))));
        }
        public void Receive(EnterTicketPurchaseModePacket packet) => context.Post(async _ =>
        {
            if (disposed)
                return;
            lists.Clear();
            requested.Clear();
            IsOpen = true;
            RoundTrip = false;
            Purchasing = false;
            PurchaseComplete = false;
            DeparturePlanet = packet.Planet;
            DeparturePoint = packet.PointName;
            SelectedPlanet = DeparturePlanet;
            SelectedPoint = null;
            Changed?.Invoke();
            try
            {
                routes = await routesRead;
                if (disposed || !IsOpen)
                    return;
                Request(DeparturePlanet);
            }
            catch (Exception error) { Debug.LogException(error); Close(); }
        }, null);
        void Request(string planet)
        {
            if (requested.Add(planet))
                request(new PlanetTravelPointListRequestPacket(player.SwgId, planet));
        }
        public void Receive(PlanetTravelPointListResponsePacket packet) => context.Post(_ =>
        {
            if (disposed || !IsOpen || !requested.Contains(packet.Planet))
                return;
            lists[packet.Planet] = packet.Names.Select((name, i) => new Point(name, packet.Positions[i], packet.Taxes[i], packet.Starports[i] != 0)).Where(p => !string.IsNullOrEmpty(p.Name)).ToArray();
            if (packet.Planet == DeparturePlanet && Origin?.Interplanetary == true)
                foreach (var planet in routes.Planets.Where(p => routes.Fare(DeparturePlanet, p) > 0))
                    Request(planet);
            if (Selected == null)
                SelectedPoint = Destinations.FirstOrDefault()?.Name;
            Changed?.Invoke();
        }, null);
        public void SelectPlanet(string planet)
        {
            if (!Planets.Contains(planet))
                return;
            SelectedPlanet = planet;
            SelectedPoint = Destinations.FirstOrDefault()?.Name;
            Changed?.Invoke();
        }
        public void SelectPoint(string name)
        {
            if (!DestinationsFor(SelectedPlanet).Any(p => p.Name == name))
                return;
            SelectedPoint = name;
            Changed?.Invoke();
        }
        public void SetRoundTrip(bool value)
        {
            RoundTrip = value;
            Changed?.Invoke();
        }
        public void Purchase()
        {
            if (!CanPurchase)
                return;
            string Token(string value) => value.Replace(' ', '_');
            string args = Token(DeparturePlanet) + " " + Token(DeparturePoint) + " " + Token(SelectedPlanet) + " " + Token(SelectedPoint) + (RoundTrip ? " round" : " single");
            Purchasing = true;
            Changed?.Invoke();
            commands.Enqueue("purchaseTicket", args, completed: result =>
            {
                Purchasing = false;
                if (result.ErrorType != 0)
                    Debug.LogWarning("Ticket purchase failed (server error " + result.ErrorType + ":" + result.ErrorCode + ").");
                Changed?.Invoke();
            });
        }
        // purchaseTicket is immediate: Core3 confirms success with this SUI message box,
        // rather than removing an action from the command queue.
        public bool TryReceive(SuiPageData page)
        {
            if (page.RootPage == "Script.listBox" && page.Commands.Any(command => command.Type == 3
                && command.NarrowParameters.Length >= 2 && command.NarrowParameters[0] == "bg.caption.lblTitle"
                && command.NarrowParameters[1] == "Text" && command.WideParameters.Contains("Select Destination")))
            {
                context.Post(_ =>
                {
                    if (disposed) return;
                    Tickets = new TicketSelection(page);
                    Changed?.Invoke();
                }, null);
                return true;
            }
            if (page.RootPage != "Script.messageBox" || !page.Commands.Any(command => command.Type == 3
                && command.NarrowParameters.Length >= 2
                && command.NarrowParameters[0] == "Prompt.lblPrompt"
                && command.NarrowParameters[1] == "Text"
                && command.WideParameters.Contains("@travel:ticket_purchase_complete")))
                return false;
            context.Post(_ =>
            {
                if (disposed)
                    return;
                Purchasing = false;
                PurchaseComplete = true;
                Changed?.Invoke();
            }, null);
            return true;
        }
        public void UseTicket(int row)
        {
            if (Tickets == null || row < 0 || row >= Tickets.Destinations.Length) return;
            var pageId = Tickets.PageId;
            Tickets = null;
            // SuiListBox subscribes to SelectedRow followed by the title; Core3 maps the row to a ticket ID.
            respond(new SuiEventNotificationPacket(pageId, 0, row.ToString(System.Globalization.CultureInfo.InvariantCulture), "Select Destination"));
            Changed?.Invoke();
        }
        public void CancelTickets()
        {
            if (Tickets == null) return;
            var pageId = Tickets.PageId;
            Tickets = null;
            respond(new SuiEventNotificationPacket(pageId, 1, "-1", "Select Destination"));
            Changed?.Invoke();
        }
        public void CloseTickets(uint pageId) => context.Post(_ =>
        {
            if (disposed || Tickets?.PageId != pageId) return;
            Tickets = null;
            Changed?.Invoke();
        }, null);

        public void AcknowledgePurchase()
        {
            PurchaseComplete = false;
            Changed?.Invoke();
        }
        public void Close()
        {
            IsOpen = false;
            Changed?.Invoke();
        }
        public void Dispose()
        {
            disposed = true;
            Changed = null;
        }
    }
}
