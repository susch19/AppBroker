namespace AppBroker.Core.DynamicUI;

public record HistoryPropertyInfo(string PropertyName, string XAxisName, string UnitOfMeasurement, string IconName, uint BrightThemeColor = 0xFFFFFFFF, uint DarkThemeColor = 0xFF000000, string ChartType = "line");
