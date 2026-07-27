using System;
using DemonLord.Application;

namespace DemonLord.Presentation.Exploration
{
    public readonly struct InGameHudState
    {
        public InGameHudState(
            string areaName,
            string roomName,
            bool hasCurrency,
            long currency,
            bool hasGameTime,
            int day,
            int hour,
            int minute)
        {
            AreaId = string.Empty;
            AreaName = areaName ?? string.Empty;
            RoomId = string.Empty;
            RoomName = roomName ?? string.Empty;
            FloorId = string.Empty;
            HasCurrency = hasCurrency;
            Currency = currency;
            HasGameTime = hasGameTime;
            Day = day;
            Hour = hour;
            Minute = minute;
        }

        public InGameHudState(
            string areaId,
            string areaName,
            string roomId,
            string roomName,
            string floorId,
            bool hasCurrency,
            long currency,
            bool hasGameTime,
            int day,
            int hour,
            int minute)
            : this(areaName, roomName, hasCurrency, currency, hasGameTime, day, hour, minute)
        {
            AreaId = areaId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            FloorId = floorId ?? string.Empty;
        }

        public string AreaId { get; }

        public string AreaName { get; }

        public string RoomName { get; }

        public string RoomId { get; }

        public string FloorId { get; }

        public bool HasCurrency { get; }

        public long Currency { get; }

        public bool HasGameTime { get; }

        public int Day { get; }

        public int Hour { get; }

        public int Minute { get; }
    }

    public interface IInGameHudStateSource
    {
        InGameHudState Current { get; }

        event Action<InGameHudState> Changed;
    }

    /// <summary>
    /// Combines a location feed with independent future wallet and clock read models. The source
    /// owns no simulated values: empty read models intentionally keep the HUD in placeholder mode.
    /// </summary>
    public sealed class InGameHudStateSource : IInGameHudStateSource, IDisposable
    {
        private readonly IWalletReadModel wallet;
        private readonly IGameTimeReadModel gameTime;
        private string areaName;
        private string roomName;
        private string areaId;
        private string roomId;
        private string floorId;
        private bool disposed;

        public InGameHudStateSource(
            string initialAreaName,
            string initialRoomName,
            IWalletReadModel wallet,
            IGameTimeReadModel gameTime)
        {
            areaName = initialAreaName ?? string.Empty;
            roomName = initialRoomName ?? string.Empty;
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.gameTime = gameTime ?? throw new ArgumentNullException(nameof(gameTime));
            this.wallet.Changed += NotifyChanged;
            this.gameTime.Changed += NotifyChanged;
        }

        public event Action<InGameHudState> Changed;

        public InGameHudState Current => new InGameHudState(
            areaId,
            areaName,
            roomId,
            roomName,
            floorId,
            wallet.HasCurrency,
            wallet.Currency,
            gameTime.HasGameTime,
            gameTime.Day,
            gameTime.Hour,
            gameTime.Minute);

        public void SetLocation(string nextAreaName, string nextRoomName)
        {
            SetLocation(areaId, nextAreaName, roomId, nextRoomName, floorId);
        }

        public void SetLocation(
            string nextAreaId,
            string nextAreaName,
            string nextRoomId,
            string nextRoomName,
            string nextFloorId)
        {
            string normalizedAreaId = nextAreaId ?? string.Empty;
            string normalizedArea = nextAreaName ?? string.Empty;
            string normalizedRoomId = nextRoomId ?? string.Empty;
            string normalizedRoom = nextRoomName ?? string.Empty;
            string normalizedFloorId = nextFloorId ?? string.Empty;
            if (string.Equals(areaId, normalizedAreaId, StringComparison.Ordinal)
                && string.Equals(areaName, normalizedArea, StringComparison.Ordinal)
                && string.Equals(roomId, normalizedRoomId, StringComparison.Ordinal)
                && string.Equals(roomName, normalizedRoom, StringComparison.Ordinal)
                && string.Equals(floorId, normalizedFloorId, StringComparison.Ordinal))
            {
                return;
            }

            areaId = normalizedAreaId;
            areaName = normalizedArea;
            roomId = normalizedRoomId;
            roomName = normalizedRoom;
            floorId = normalizedFloorId;
            NotifyChanged();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            wallet.Changed -= NotifyChanged;
            gameTime.Changed -= NotifyChanged;
        }

        private void NotifyChanged()
        {
            if (!disposed)
            {
                Changed?.Invoke(Current);
            }
        }
    }
}
