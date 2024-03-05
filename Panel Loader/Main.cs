using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;

namespace Crestron_SSH;

public class Main : CrestronControlSystem
{
    private const string RemoteTpPath = "/display/tp.vtz";
    private const string Username = "admin";
    private const string Password = "password";
    private const string Name = "Panel Loader";
    private string _filePath = String.Empty;
    private TsxCcsUcCodec100EthernetReservedSigs? _ethernetExtender;
    private bool _readyToLoad;

    public override void InitializeSystem()
    {
        _filePath = $"{Crestron.SimplSharp.CrestronIO.Directory.GetApplicationDirectory()}/Panel.vtz";

        Ts1070 _panel = new Ts1070(0x03, this);
        _panel.SigChange += PanelOnSigChange;
        _panel.ExtenderEthernetReservedSigs.Use();
        _panel.Register();
        _ethernetExtender = _panel.ExtenderEthernetReservedSigs;
        _ethernetExtender.DeviceExtenderSigChange += EthernetExtenderSigChange;
        
    }

    private void PanelOnSigChange(BasicTriList currentDevice, SigEventArgs args)
    {
        if (args.Sig.Type != eSigType.Bool)
            return;
        if (args.Sig.BoolValue == false)
            return;
        switch (args.Sig.Number)
        {
            case 101 :
                UploadUsingCrestronSsh();
                break;
            case 102 : 
                UploadUsingSshNet();
                break;
        }
    }

    private void UploadUsingSshNet()
    {
        if (!_readyToLoad)
        {
            Log("The touchpanel has not come online");
            return;
        }
        Log($"Uploading - Local path: {_filePath}, TP Path: {RemoteTpPath}, TP IP: {_ethernetExtender!.IpAddressFeedback.StringValue}");
        var uploadSuccess = RenciSsh.UploadFile(
            new Renci.SshNet.SftpClient(_ethernetExtender!.IpAddressFeedback.StringValue, 22, Username, Password),
            _filePath, RemoteTpPath, Name);

        if (!uploadSuccess)
        {
            Log("DisplayList Upload failure");
            return;
        }
        Log("Issuing project load");
        RenciSsh.RunCommand(
            new Renci.SshNet.SshClient(_ethernetExtender?.IpAddressFeedback.StringValue, 22, Username, Password), 
            "PROJECTLOAD", Name);
        Log("Done!");
    }

    private void UploadUsingCrestronSsh()
    {
        if (!_readyToLoad)
        {
            Log("The touchpanel has not come online");
            return;
        }
        Log($"Uploading - Local path: {_filePath}, TP Path: {RemoteTpPath}, TP IP: {_ethernetExtender!.IpAddressFeedback.StringValue}");
        var uploadSuccess = CrestronSsh.UploadFile(
            new SftpClient(_ethernetExtender!.IpAddressFeedback.StringValue, 22, Username, Password),
            _filePath, RemoteTpPath, Name);

        if (!uploadSuccess)
        {
            Log("DisplayList Upload failure");
            return;
        }
        Log("Issuing project load");
        CrestronSsh.RunCommand(
            new SshClient(_ethernetExtender.IpAddressFeedback.StringValue, 22, Username, Password), 
            "PROJECTLOAD", Name);
        Log("Done!");
    }

    private void EthernetExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
    {
        if (args.Event != eSigEvent.StringChange)
            return;
        if (args.Sig != _ethernetExtender?.IpAddressFeedback)
            return;
        
        Log($"TP IP: {_ethernetExtender.IpAddressFeedback.StringValue}");
        _readyToLoad = true;
    }
    
    private void Log(string message)
    {
        CrestronConsole.PrintLine($"{DateTime.Now} - Panel Loader - {message}");
    }
}