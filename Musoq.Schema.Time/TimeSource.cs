﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Time
{
    public class TimeSource : RowSourceBase<DateTimeOffset>
    {
        private readonly DateTimeOffset _startAt;
        private readonly DateTimeOffset _stopAt;
        private readonly string _resolution;

        public TimeSource(DateTimeOffset startAt, DateTimeOffset stopAt, string resolution)
        {
            _startAt = startAt;
            _stopAt = stopAt;
            _resolution = resolution;
        }

        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<DateTimeOffset>>> chunkedSource)
        {
            Func<DateTimeOffset, DateTimeOffset> modify;
            switch (_resolution)
            {
                case "seconds":
                    modify = offset => offset.AddSeconds(1);
                    break;
                case "minutes":
                    modify = offset => offset.AddMinutes(1);
                    break;
                case "hours":
                    modify = offset => offset.AddHours(1);
                    break;
                case "days":
                    modify = offset => offset.AddDays(1);
                    break;
                case "months":
                    modify = offset => offset.AddMonths(1);
                    break;
                case "years":
                    modify = offset => offset.AddYears(1);
                    break;
                default:
                    throw new NotSupportedException($"Chosen resolution '{_resolution}' is not supported.");
            }

            var listOfCalcTimes = new List<EntityResolver<DateTimeOffset>>();
            var currentTime = _startAt;
            var i = 0;

            while (currentTime <= _stopAt)
            {
                listOfCalcTimes.Add(new EntityResolver<DateTimeOffset>(currentTime, TimeHelper.TimeNameToIndexMap, TimeHelper.TimeIndexToMethodAccessMap));
                currentTime = modify(currentTime);

                if (i++ > 99)
                    continue;

                chunkedSource.Add(listOfCalcTimes);
                listOfCalcTimes = new List<EntityResolver<DateTimeOffset>>();
                i = 0;
            }

            chunkedSource.Add(listOfCalcTimes);
        }
    }
}