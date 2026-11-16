using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.DataTable;

namespace Swg.Client.Core.World.Travel
{
    /// <summary>Native travel.iff fares. Core3 mirrors its upper triangle; zero means no direct route.</summary>
    public sealed class TravelRoutes
    {
        readonly Dictionary<(string, string), int> fares = new Dictionary<(string, string), int>();
        public IReadOnlyList<string> Planets
        {
            get;
        }
        public TravelRoutes(DataTableFile table)
        {
            Planets = table.Rows.Select(row => row.Get<string>(0)).ToArray();
            for (int i = 0; i < table.Rows.Count; i++)
                for (int j = i + 1; j < table.Columns.Count; j++)
                {
                    string destination = table.Columns[j].Name;
                    int fare = table.Rows[i].Get<int>(j);
                    fares[(Planets[i], destination)] = fare;
                    fares[(destination, Planets[i])] = fare;
                }
        }
        public int Fare(string departure, string arrival) => departure != null && arrival != null && fares.TryGetValue((departure, arrival), out int value) ? value : 0;
    }
}
