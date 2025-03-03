namespace AIStorm.Core.Common;

using System;

public static class Tools
{
    public static DateTime ParseAsUtc(string dateTimeString)
    {
        DateTime dateTime = DateTime.Parse(dateTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind);
        dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        return dateTime;
    }
    
    public static string UtcToString(DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime must be in UTC format", nameof(dateTime));
        }
        
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ss");
    }
}
