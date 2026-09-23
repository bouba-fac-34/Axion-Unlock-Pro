using Axion.Core.Models;
using Axion.Core.Services;

namespace Axion.Core.Protocols.Qualcomm
{
    public class QualcommProtocol
    {
        private readonly Action<string> _log;
        private readonly EdlService _edl;

        public QualcommProtocol(Action<string> log)
        {
            _log = log;
            _edl = new EdlService(log);
        }

        public Task<OperationResult> EdlFrpAsync(DeviceInfo device) =>
            _edl.WipeFrpAsync(device);

        public Task<OperationResult> IdentifyAsync(DeviceInfo device) =>
            _edl.IdentifyAsync(device);

        public Task<OperationResult> WipeFrpAsync(DeviceInfo device) =>
            EdlFrpAsync(device);
    }
}
