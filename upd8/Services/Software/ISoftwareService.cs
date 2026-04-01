using upd8.Models.Software;

namespace upd8.Services.Software;

public interface ISoftwareService
{
    SoftwareSnapshot GetSnapshot();
}
