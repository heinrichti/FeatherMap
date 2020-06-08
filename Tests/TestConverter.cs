using FeatherMap;

namespace Tests
{
    class TestConverter : IPropertyConverter<int, string>
    {
        public string Convert(int source) => source.ToString();

        public int ConvertBack(string target) => int.Parse(target);
    }
}
