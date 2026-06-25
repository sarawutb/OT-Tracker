using System;
using System.Globalization;
using Newtonsoft.Json.Converters;

namespace OTTracker.Domain.Entities;

public sealed class InvariantDateTimeConverter : IsoDateTimeConverter
{
    public InvariantDateTimeConverter()
    {
        Culture = CultureInfo.InvariantCulture;
    }
}
