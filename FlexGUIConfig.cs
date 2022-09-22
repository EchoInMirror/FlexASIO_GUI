namespace FlexASIOGUI;

public class FlexGuiConfig
{
    public string Backend { get; set; } = "Windows WASAPI";
    public int? BufferSizeSamples { get; set; }
    public FlexGuiConfigDeviceSection Input { get; }
    public FlexGuiConfigDeviceSection Output { get; }

    public FlexGuiConfig()
    {
        Input = new FlexGuiConfigDeviceSection();
        Output = new FlexGuiConfigDeviceSection();
    }

}
public class FlexGuiConfigDeviceSection
{
    public string Device { get; set; }
    public double? SuggestedLatencySeconds { get; set; }
    public bool? WasapiExclusiveMode { get; set; }
    public bool? WasapiAutoConvert { get; set; }
    public int? Channels { get; set; }
}