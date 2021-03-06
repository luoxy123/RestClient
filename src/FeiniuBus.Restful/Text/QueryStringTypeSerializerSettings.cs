﻿using System;

namespace FeiniuBus.Restful.Text
{
    public class QueryStringTypeSerializerSettings
    {
        public QueryStringTypeSerializerSettings()
        {
            DateHandler = DateHandler.String;
            TreatEnumAsInteger = false;
            SkipDateTimeConversion = false;
        }

        public DateHandler DateHandler { get; set; }
        public bool TreatEnumAsInteger { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating if the framework should skip automatic <see cref="DateTime" /> conversions.
        ///     Dates will be handled literally, any included timezone encoding will be lost and the date will be treaded as
        ///     DateTimeKind.Local
        ///     Utc formatted input will result in DateTimeKind.Utc output. Any input without TZ data will be set
        ///     DateTimeKind.Unspecified
        ///     This will take precedence over other flags like AlwaysUseUtc
        /// </summary>
        public bool SkipDateTimeConversion { get; set; }
    }

    public enum DateHandler
    {
        Number,
        String
    }
}
