namespace ClamScanner.Services;

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

public class Clamd
{
    private readonly string _host = "clamd";
    private readonly int _port = 3310;

    public async Task<bool> IsCleanAsync(Stream fileStream)
    {
        using var client = new TcpClient();

        await client.ConnectAsync(_host, _port);

        using NetworkStream stream = client.GetStream();

        // iniciar stream
        await stream.WriteAsync("zINSTREAM\0"u8.ToArray());

        byte[] buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
        {
            byte[] length = new byte[4];

            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)bytesRead);

            await stream.WriteAsync(length);
            await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        // finalizar stream
        await stream.WriteAsync(new byte[4]);

        byte[] responseBuffer = new byte[1024];

        int responseSize = await stream.ReadAsync(responseBuffer);

        string result = Encoding.UTF8.GetString(responseBuffer, 0, responseSize);

        return result.Contains("OK");
    }
}