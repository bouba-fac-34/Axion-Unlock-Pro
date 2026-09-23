using Axion.Core.Models;
using Axion.Core.Services;

namespace Axion.Core.Protocols.MTK
{
    public class MtkProtocol
    {
        private readonly Action<string> _log;
        private readonly MtkService _mtk;

        public MtkProtocol(Action<string> log)
        {
            _log = log;
            _mtk = new MtkService(log);
        }

        public Task<OperationResult> RemoveFrpAsync(DeviceInfo device) =>
            _mtk.WipeFrpAsync(device);

        public Task<OperationResult> FormatFrpAsync(DeviceInfo device) =>
            _mtk.FormatFrpAsync(device);

        public Task<OperationResult> ReadGptAsync(DeviceInfo device) =>
            _mtk.ReadGptAsync(device);
    }
}
