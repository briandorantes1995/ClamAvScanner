namespace ClamScanner.Services;

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

public class Clamd
{
    private readonly string _host = "clamd";
    private readonly int _port = 3310;

    // Guardamos el comando de inicio en una memoria estática de solo lectura para no instanciar memoria extra
    private static readonly ReadOnlyMemory<byte> InstreamCommand = "zINSTREAM\0"u8.ToArray();
    private static readonly ReadOnlyMemory<byte> TerminateCommand = new byte[4];

    public async Task<bool> IsCleanAsync(Stream fileStream)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_host, _port);

        using NetworkStream stream = client.GetStream();
        
        await stream.WriteAsync(InstreamCommand);
        
        byte[] buffer = new byte[524288]; 
        int bytesRead;

        byte[] lengthBuffer = new byte[4];

        while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
        {
         
            BinaryPrimitives.WriteUInt32BigEndian(lengthBuffer, (uint)bytesRead);
            
            await stream.WriteAsync(lengthBuffer.AsMemory());
            await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }
        
        await stream.WriteAsync(TerminateCommand);

        byte[] responseBuffer = new byte[256];
        int responseSize = await stream.ReadAsync(responseBuffer);

        string result = Encoding.UTF8.GetString(responseBuffer, 0, responseSize);

        return result.Contains("OK");
    }
}