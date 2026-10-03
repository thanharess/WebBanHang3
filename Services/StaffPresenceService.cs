using System.Collections.Concurrent;

namespace WebBanHang.Services
{
    public class StaffPresenceService
    {
        private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(90);
        private readonly ConcurrentDictionary<int, DateTime> _lastSeen = new();

        public void MarkOnline(int userId)
        {
            _lastSeen[userId] = DateTime.UtcNow;
        }

        public bool IsOnline(int userId)
        {
            return _lastSeen.TryGetValue(userId, out var lastSeen) &&
                DateTime.UtcNow - lastSeen <= OnlineWindow;
        }
    }
}
