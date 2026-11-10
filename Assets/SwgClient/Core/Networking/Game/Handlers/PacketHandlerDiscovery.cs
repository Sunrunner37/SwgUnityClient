using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Swg.Client.Core.Networking.Game.Handlers
{
    internal static class PacketHandlerDiscovery
    {
        public static IEnumerable<T> Create<T>(GameWorldState world)
        {
            var types = typeof(GamePacketRouter).Assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(T).IsAssignableFrom(type)).OrderBy(type => type.FullName, StringComparer.Ordinal);
            foreach (var type in types)
            {
                var constructor = type.GetConstructor(new[] { typeof(GameWorldState) }) ?? type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new InvalidOperationException(type.FullName + " requires a public GameWorldState or parameterless constructor.");
                T handler;
                try
                {
                    handler = (T)constructor.Invoke(constructor.GetParameters().Length == 0 ? null : new object[] { world });
                }
                catch (TargetInvocationException error)
                {
                    throw new InvalidOperationException("Failed to construct packet handler " + type.FullName, error.InnerException ?? error);
                }

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
