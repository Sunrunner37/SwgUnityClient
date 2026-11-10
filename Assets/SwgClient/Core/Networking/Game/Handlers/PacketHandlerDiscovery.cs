using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Swg.Client.Core.Networking.Game.Handlers
{
    internal static class PacketHandlerDiscovery
    {
        public static IEnumerable<T> Create<T>(ILifetimeScope services)
        {
            var types = typeof(GamePacketRouter).Assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(T).IsAssignableFrom(type)).OrderBy(type => type.FullName, StringComparer.Ordinal);
            foreach (var type in types)
            {
                var handler = (T)services.Resolve(type);
                yield return handler;
            }
        }

        public static Dictionary<uint, T> Map<T>(IEnumerable<T> handlers, Func<T, uint> key)
        {
            var result = new Dictionary<uint, T>();
            foreach (var handler in handlers)
            {
                uint opcode = key(handler);
                if (result.TryGetValue(opcode, out var existing))
                    throw new InvalidOperationException("Duplicate packet handler for 0x" + opcode.ToString("X8") + ": " + existing.GetType().FullName + " and " + handler.GetType().FullName);
                result.Add(opcode, handler);
            }

            return result;
        }
    }
}
