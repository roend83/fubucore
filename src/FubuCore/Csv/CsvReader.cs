using System.IO;
using FubuCore.Binding;
using FubuCore.Binding.Values;

namespace FubuCore.Csv
{
    // See the CsvReaderHarness for integration tests against this
    public class CsvReader : ICsvReader
    {
        private readonly IObjectResolver _resolver;

        public CsvReader(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void Read<T>(CsvRequest<T> request)
        {
            using (var stream = new FileStream(request.FileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    var headers = determineHeaders(reader, request);
                    processData(reader, headers, request);
                }
            }
        }

        private CsvData determineHeaders<T>(StreamReader reader, CsvRequest<T> request)
        {
            CsvData headers = null;
            if (request.HeadersExist)
            {
                var values = reader.ReadLine();
                if (values.IsEmpty()) return null;

                if (request.UseHeaderOrdering) headers = new CsvData(values);
            }

            return headers;
        }

        private void processData<T>(StreamReader reader, CsvData headers, CsvRequest<T> request)
        {
            string line;
            var mapping = request.Mapping.As<IColumnMapping>();
            while ((line = reader.ReadLine()) != null)
            {
                var source = valueSourceFor(line, headers, mapping);
                var result = _resolver.BindModel(typeof(T), source);

                request.Callback(result.Value.As<T>());
            }
        }

        private IValueSource valueSourceFor(string line, CsvData headers, IColumnMapping mapping)
        {
            var values = new CsvData(line);
            return headers == null
                       ? mapping.ValueSource(values)
                       : mapping.ValueSource(values, headers);
        }
    }
}