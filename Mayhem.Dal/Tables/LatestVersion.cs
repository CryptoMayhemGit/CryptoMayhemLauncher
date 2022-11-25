using Mayhem.Dal.Dto.Dtos;

namespace Mayhem.Dal.Tables
{
    public class LatestVersion
    {
        public BuildVersion Version { get; set; }
        public string BuildURL { get; set; }
    }
}
