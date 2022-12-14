using Squirrel;
using System;

namespace CryptoMayhemLauncher.Interfaces
{
    public interface ISqurrielHandleEvents
    {
        void UpdateApp(UpdateManager manager, string newVersion);
        void SetDefaultConfiguration();
    }
}
