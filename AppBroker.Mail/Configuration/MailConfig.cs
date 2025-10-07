using AppBroker.Core.Configuration;


namespace AppBroker.Windmill.Configuration;

public class MailConfig : IConfig
{
    public string Name => "Mail";

    public string Host { get; set; } 
    public ushort Port {get;set;} 
    public bool UseSsl {get;set;} 
    public string Username {get;set;} 
    public string Password {get;set;}
}
