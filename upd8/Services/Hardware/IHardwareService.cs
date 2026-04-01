using upd8.Models.Hardware;

namespace upd8.Services.Hardware;

public interface IHardwareService
{
    HardwareSnapshot GetSnapshot();
}
