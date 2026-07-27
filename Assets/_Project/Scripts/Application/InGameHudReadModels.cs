using System;

namespace DemonLord.Application
{
    /// <summary>Read-only extension point for a future wallet system.</summary>
    public interface IWalletReadModel
    {
        bool HasCurrency { get; }

        long Currency { get; }

        event Action Changed;
    }

    /// <summary>Read-only extension point for a future in-game calendar and clock.</summary>
    public interface IGameTimeReadModel
    {
        bool HasGameTime { get; }

        int Day { get; }

        int Hour { get; }

        int Minute { get; }

        event Action Changed;
    }

    public sealed class EmptyWalletReadModel : IWalletReadModel
    {
        public bool HasCurrency => false;

        public long Currency => 0L;

        public event Action Changed
        {
            add { }
            remove { }
        }
    }

    public sealed class EmptyGameTimeReadModel : IGameTimeReadModel
    {
        public bool HasGameTime => false;

        public int Day => 0;

        public int Hour => 0;

        public int Minute => 0;

        public event Action Changed
        {
            add { }
            remove { }
        }
    }
}
