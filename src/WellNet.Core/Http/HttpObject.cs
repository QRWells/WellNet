using QRWells.WellNet.Core.Utils;

namespace QRWells.WellNet.Core.Http;

public abstract class HttpObject
{
    public MultiDictionary Headers { get; set; }
    public byte[] Body { get; set; }
}