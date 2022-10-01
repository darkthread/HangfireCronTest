using System.Reflection;
using Hangfire;

Func<DateTime, long> toTimestamp = (d) => {
    TimeSpan ts = d - new DateTime(1970, 1, 1);
    return Convert.ToInt64(ts.TotalSeconds);
};
Func<long, DateTime> toDateTime = (l) => {
    return new DateTime(1970, 1, 1).AddSeconds(l);
};
var tzResolver = new DefaultTimeZoneResolver();
var recurJob = new Dictionary<string, string>
{
    ["Queue"] = "default",
    ["Cron"] = "0 16 * * *",
    ["TimeZoneId"] = "Taipei Standard Time",
    ["Job"] = "{}",
    ["CreatedAt"] = "",
    ["NextExecution"] = "",
    ["LastExecution"] = "",
    ["LastJobId"] = "8"
};
var jobEntType = typeof(RecurringJob).Assembly.GetType("Hangfire.RecurringJobEntity");
var jobEntConstructor = jobEntType.GetConstructor(new[] {
        typeof(string), typeof(IDictionary<string, string>),
        typeof(ITimeZoneResolver), typeof(DateTime) });
//ver 1.7.6
//bool TryGetNextExection(out DateTime? nextExecution, out Exception error)
//ver 1.7.31
//bool TryGetNextExection(bool scheduledChanged, out DateTime? nextExecution, out Exception error)
var trypGetNextExecution =
    jobEntType.GetMethod("TryGetNextExecution", BindingFlags.Instance | BindingFlags.NonPublic);

Action<bool, string, DateTime, DateTime?> test = (changed, cron, createdAt, lastExecution) => {
    Console.Write($"{(changed ? "Changed" : "Unchange")} / {cron} / {createdAt:HH:mm:ss} / {(lastExecution.HasValue ? lastExecution.Value.ToString("HH:mm:ss") : string.Empty)} => ");
    recurJob["Cron"] = cron;
    recurJob["CreatedAt"] = toTimestamp(createdAt.ToUniversalTime()).ToString();
    recurJob["LastExecution"] = lastExecution.HasValue ? toTimestamp(lastExecution.Value.ToUniversalTime()).ToString() : string.Empty;
    var jobEnt = jobEntConstructor.Invoke(new object[]
    {
        "Test", recurJob, tzResolver, DateTime.UtcNow
    });
    var param = new object[] { changed, null, null };
    trypGetNextExecution.Invoke(jobEnt, param);
    Console.WriteLine(((DateTime)param[1]).ToString());
};

var trigTime = DateTime.Now.AddMinutes(-1);
var cron = $"{trigTime.Minute} {trigTime.Hour} * * *";
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine($"Current Time={DateTime.Now:HH:mm:ss}");
Console.ResetColor();
//1.7.6
//RecurringJobEntity.ParseCronExpression(this.Cron)
//.GetNextOccurrence(this.LastExecution ?? this.CreatedAt.AddSeconds(-1.0), this.TimeZone, false);
//1.7.31
//RecurringJobEntity.ParseCronExpression(this.Cron)
//.GetNextOccurrence(scheduleChanged ? this._now.AddSeconds(-1.0) : (this.LastExecution ?? this.CreatedAt.AddSeconds(-1.0)), this.TimeZone, false);
//Changed -> Job、Cron、TimeZone、Queue different

//若排程有修改，下次執行時間由現在時間推算，若排程有修改下次執行時間由前次執行時間推算，若無前次執行時間，以建立時間推算
test(false, cron, trigTime.AddMinutes(1), null); //次日執行
test(false, cron, trigTime, null); //次日執行
test(false, cron, trigTime.AddMinutes(-1), null); //當日期執行

//有前次執行時間時，以前次時間推算
test(false, cron, trigTime.AddMinutes(-1), trigTime.AddMinutes(1)); //次日執行
test(false, cron, trigTime.AddMinutes(1), trigTime.AddMinutes(1)); //次日執行
test(false, cron, trigTime, trigTime); //次日執行
test(false, cron, trigTime, trigTime.AddMinutes(-1)); //當日期執行
test(false, cron, trigTime.AddMinutes(1), trigTime.AddMinutes(-1)); //當日期執行

//若排程異動，以現在時間推算
test(true, cron, trigTime.AddMinutes(-1), trigTime.AddMinutes(1)); //次日執行
test(true, cron, trigTime.AddMinutes(1), trigTime.AddMinutes(1)); //次日執行
test(true, cron, trigTime, trigTime); //次日執行
test(true, cron, trigTime, trigTime.AddMinutes(-1)); //次日執行
test(true, cron, trigTime.AddMinutes(1), trigTime.AddMinutes(-1)); //次日執行





