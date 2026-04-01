using upd8.Models.Info;

namespace upd8.Services.Info;

public interface IInfoService
{
    InfoSnapshot GetSnapshot();
}
