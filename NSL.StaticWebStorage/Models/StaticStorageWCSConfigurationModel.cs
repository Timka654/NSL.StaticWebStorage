namespace NSL.StaticWebStorage.Models
{
    public class StaticStorageWCSConfigurationModel
    {
        public string Host { get; set; } = "tcp://nsl.wcs.yarp:44560";

        public string ProjectName { get; set; } = "NSL.StaticWebStorage";
    }
}
