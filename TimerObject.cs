namespace CC_device_emulator
{
    public enum DeviceType
    {
        KITCHEN_DEVICE = 0,
        BATHROOM_DEVICE
    }

    public class TimerObject
    {
        public DeviceType DeviceType {  get; set; }        
        public string ClientId { get; set; } = null!;
        public string ClientPassword { get; set; } = null!;
        public string Topic { get; set; } = null!;

        public TimerObject() { }
    }
}
