using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using PhotonPackageParser;
namespace Albion.Network;


internal sealed class AlbionParser : PhotonParser, IPhotonReceiver
{
    private readonly HandlersCollection _handlers;

    public AlbionParser()
    {
        _handlers = new HandlersCollection();
    }

    public void AddHandler<TPacket>(PacketHandler<TPacket> handler)
    {
        _handlers.Add(handler);
    }

    protected override void OnEvent(byte code, Dictionary<byte, object> parameters)
    {
        short eventCode = ParseEventCode(parameters);

        if (eventCode <= -1)
        {
            return;
        }

        var eventPacket = new EventPacket(eventCode, parameters);
        _ = _handlers.HandleAsync(eventPacket);
    }

    protected override void OnRequest(byte operationCodeByte, Dictionary<byte, object> parameters)
    {
        short operationCode = ParseOperationCode(parameters);

        if (operationCode <= -1)
        {
            return;
        }

        var requestPacket = new RequestPacket(operationCode, parameters);
        _ = _handlers.HandleAsync(requestPacket);
    }

    protected override void OnResponse(byte operationCodeByte, short returnCode, string debugMessage, Dictionary<byte, object> parameters)
    {
        short operationCode = ParseOperationCode(parameters);

        if (operationCode <= -1)
        {
            return;
        }

        var responsePacket = new ResponsePacket(operationCode, parameters);
        if (operationCode == 35)
        {
            try
            {
                if (parameters.ContainsKey(3))
                {
                    Console.Clear();
                    String data = System.Text.Encoding.ASCII.GetString((byte[])parameters[3]);
                    String[] dataSplit = data.Split(new[] { "instanceslot_" }, StringSplitOptions.None);
                    Console.WriteLine("[Dungeon] " + parameters[0]);
                    DirectoryInfo d = new DirectoryInfo("Template");
                    FileInfo[] Files = d.GetFiles("*.xml");
                    for (int i = 1; i < dataSplit.Length; i++)
                    {
                        int startStr = 3;
                        int endStr = dataSplit[i].IndexOf((char)(0));
                        int x = endStr + 21;
                        String templateName = dataSplit[i].Substring(startStr, endStr - startStr);
                        if (templateName.Contains("BACKDROP") || templateName.Contains("START") || templateName.Contains("BASIC") || templateName.Contains("MASTER_01")) continue;
                        FileInfo selectFile = null;
                        foreach (FileInfo file in Files)
                        {
                            if (file.Name.Contains(templateName))
                            {
                                selectFile = file;
                                break;
                            }
                        }
                        if (selectFile == null) continue;
                        XmlDocument doc = new XmlDocument();
                        doc.Load(selectFile.FullName);
                        XmlNode layergroup = doc.DocumentElement.SelectSingleNode("//layergroup[@name='REWARD']");
                        if (layergroup == null)
                        {
                            if (templateName.Contains("EXIT")) Console.WriteLine("Exit: " + templateName + " ");
                            else Console.WriteLine("Unknown: " + templateName + " ");
                        }
                        else
                        {
                            Console.WriteLine("Boss: " + templateName + " ");
                            List<String> layerlist = new List<String>();
                            while (x < dataSplit[i].Length)
                            {
                                layerlist.Add(dataSplit[i].Substring(x, 8));
                                x += 9;
                            }
                            XmlNodeList layer = layergroup.SelectNodes("layer");
                            foreach (XmlNode tite in layer)
                            {
                                if (layerlist.Contains(tite.Attributes["id"].Value))
                                {
                                    XmlNodeList reward = tite.SelectNodes("tile");
                                    foreach (XmlNode f in reward)
                                    {
                                        Console.WriteLine("\tReward: " + f.Attributes["name"].Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Warning]" + e.Message);
            }


        }
        _ = _handlers.HandleAsync(responsePacket);
    }

    private static short ParseOperationCode(Dictionary<byte, object> parameters)
    {
        if (!parameters.TryGetValue(253, out object value))
        {
            // Other values are returned as -1 code.
            //throw new InvalidOperationException();
            return -1;
        }

        return (short)value;
    }

    private static short ParseEventCode(Dictionary<byte, object> parameters)
    {
        if (!parameters.TryGetValue(252, out object value))
        {
            // Other values are returned as -1 code.
            //throw new InvalidOperationException();
            return -1;
        }

        return (short)value;
    }
}