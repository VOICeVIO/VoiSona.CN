using CsvHelper.Configuration.Attributes;
// ReSharper disable InconsistentNaming

namespace VoiSona.CN.Translator
{
    internal class Unit
    {
        [Index(0)]
        [Name("Origin")]
        public string Origin { set; get; }
        [Index(1)]
        [Name("Trans")]
        public string Trans { get; set; }

        //https://github.com/JoshClose/CsvHelper/issues/1078
        public Unit(string Origin, string Trans)
        {
            this.Origin = Origin;
            this.Trans = Trans;
        }
    }
}
