using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Extension;
using AppBroker.Zigbee2Mqtt.Devices;

using AppBrokerASP;

using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;


using MimeKit;

using NLog;

using System.Runtime.InteropServices;

namespace AppBroker.Zigbee2Mqtt;

internal class Plugin : IPlugin
{
    private Logger logger;
    private Dictionary<long, Timer> lastReceivedTimer = new Dictionary<long, Timer>();
    public int LoadOrder => int.MinValue;

    public string Name => "Mail";


    public bool Initialize(LogFactory logFactory)
    {
        logger = logFactory.GetCurrentClassLogger();
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;

        return true;
    }

    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        if (!IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(e.Id, out var device))
            return;

        if (device is not Zigbee2MqttDevice || !device.TypeNames.Contains("WSDCGQ11LM"))
            return;

        ref var timer = ref CollectionsMarshal.GetValueRefOrAddDefault(lastReceivedTimer, e.Id, out var exists);

        if (!exists)
            timer = new Timer(SendMail, device, TimeSpan.FromMinutes(120), Timeout.InfiniteTimeSpan);
        else
            timer!.Change(TimeSpan.FromMinutes(120), Timeout.InfiniteTimeSpan);
    }

    private void SendMail(object? state)
    {
        Device? device = (Device)state;
        Task.Run(async () =>
        {
            using var mail = new MimeMessage();
            var builder = new BodyBuilder();
            mail.From.Add(new MailboxAddress("smarthome@susch.eu", "smarthome@susch.eu"));
            mail.To.Add(MailboxAddress.Parse("mail@susch.eu"));

            mail.Subject = "Gerät sendet keine Daten";
            builder.TextBody = $"Das Gerät {device.FriendlyName} mit der Id {device.Id} scheint nicht mehr verbunden zu sein";
            builder.HtmlBody = $"Das Gerät {device.FriendlyName} mit der Id {device.Id} scheint nicht mehr verbunden zu sein";
            mail.Body = builder.ToMessageBody();

            using var client = new SmtpClient(new ProtocolLogger(Console.OpenStandardOutput()));
            client.CheckCertificateRevocation = true;
            client.ServerCertificateValidationCallback = (_, __, ___, ____) => true;

            await client.ConnectAsync("mail.gallimathias.de", 465, useSsl: true);
            await client.AuthenticateAsync("smarthome@susch.eu", "ocj+V}#R0c=>_`,R4:Ud'U;(e&qOZytoz3$Um]jpFxfR{1CN=YIph0x~.+?<F|KEKyw\\8Fi9/J}h^_uk4|-z9ybeFz(p',_MX))_t1!IGS!OFq!;:A[!#U=]");

            await client.SendAsync(mail);
            await client.DisconnectAsync(true);

        }).GetAwaiter().GetResult();
    }
}
