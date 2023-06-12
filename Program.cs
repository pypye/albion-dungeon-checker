using Albion.Network;
using PacketDotNet;
using SharpPcap;
using System;
using System.Threading;

class Program
{
    private static IPhotonReceiver receiver;
    static public void Main(string[] args)
    {
        ReceiverBuilder builder = ReceiverBuilder.Create();

        receiver = builder.Build();

        Console.WriteLine("[Info] Starting Cluster Checker...");
        Console.WriteLine("[Info] Please run this before enter dunegeon.");

        CaptureDeviceList devices = CaptureDeviceList.Instance;
        foreach (var device in devices)
        {
            new Thread(() =>
            {
                device.OnPacketArrival += new PacketArrivalEventHandler(PacketHandler);
                device.Open(DeviceModes.Promiscuous, 1000);
                device.StartCapture();
            })
            .Start();
        }

        Console.Read();
    }

    private static void PacketHandler(object sender, PacketCapture e)
    {
        try
        {
            UdpPacket packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data).Extract<UdpPacket>();
            if (packet != null && (packet.SourcePort == 5056 || packet.DestinationPort == 5056))
            {
                receiver.ReceivePacket(packet.PayloadData);
            }
        }
        catch (Exception)
        {
            return;
        }

    }
}