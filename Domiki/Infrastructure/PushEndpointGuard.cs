using System.Net;
using System.Net.Sockets;
using System.Security;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Проверка адреса web-push подписки: допускает только внешние https-эндпоинты сервисов доставки,
/// отсекая приватные, петлевые и служебные адреса, чтобы браузерный <c>endpoint</c> не превратился
/// в серверный запрос к внутренней сети (SSRF).
/// </summary>
/// <remarks>
/// Три рубежа. <see cref="EnsureRegisterable"/> на приёме подписки делает только дешёвую
/// проверку без обращения к сети: схема, отсутствие явно внутреннего хоста-литерала. <see cref="IsSendable"/>
/// перед отправкой резолвит доменное имя и отсеивает подписку, если хоть один из полученных IP –
/// приватный или служебный. Между этой проверкой и подключением имя может начать резолвиться иначе
/// (DNS rebinding), поэтому решает клиент из <see cref="CreateHttpClient"/>: он резолвит хост сам в момент
/// подключения и открывает сокет только к адресам, прошедшим ту же проверку.
/// <para>
/// IPv6-адреса со вложенным IPv4 – NAT64 <c>64:ff9b::/96</c> и 6to4 <c>2002::/16</c> – транслятор или ретранслятор
/// превращает в обращение к вложенному IPv4, поэтому они проверяются по нему тем же правилом, что и голый IPv4.
/// Локальный NAT64 <c>64:ff9b:1::/48</c> (место IPv4 в нём задаёт оператор), IPv4-совместимые <c>::/96</c>,
/// Teredo <c>2001::/32</c> (путь идёт через произвольный ретранслятор), документационный <c>2001:db8::/32</c>
/// и discard <c>100::/64</c> отсекаются целиком: сервисы доставки web-push там не живут.
/// </para>
/// </remarks>
public static class PushEndpointGuard
{
    private static readonly IPNetwork Nat64WellKnownNetwork = IPNetwork.Parse("64:ff9b::/96");

    private static readonly IPNetwork SixToFourNetwork = IPNetwork.Parse("2002::/16");

    private static readonly IPNetwork[] ReservedIPv6Networks =
    [
        IPNetwork.Parse("::/96"),
        IPNetwork.Parse("64:ff9b:1::/48"),
        IPNetwork.Parse("100::/64"),
        IPNetwork.Parse("2001::/32"),
        IPNetwork.Parse("2001:db8::/32"),
    ];
    /// <summary>
    /// Проверяет адрес при регистрации подписки и бросает <see cref="BusinessException"/>, если он недопустим.
    /// </summary>
    /// <param name="endpoint">Адрес push-эндпоинта, присланный браузером.</param>
    public static void EnsureRegisterable(string endpoint)
    {
        if (!TryGetHttpsHost(endpoint, out var host) || IsForbiddenHostLiteral(host))
        {
            throw new BusinessException("Недопустимый адрес push-подписки");
        }
    }

    /// <summary>
    /// Проверяет адрес непосредственно перед отправкой уведомления, резолвя доменное имя в IP.
    /// </summary>
    /// <param name="endpoint">Адрес push-эндпоинта из сохранённой подписки.</param>
    /// <returns><see langword="true"/>, если адрес внешний и безопасен для серверного запроса; иначе <see langword="false"/>.</returns>
    public static bool IsSendable(string endpoint)
    {
        if (!TryGetHttpsHost(endpoint, out var host) || host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IPAddress.TryParse(host, out var literal))
        {
            return !IsPrivateOrReserved(literal);
        }

        IPAddress[] resolved;
        try
        {
            resolved = Dns.GetHostAddresses(host);
        }
        catch (SocketException)
        {
            return false;
        }

        return resolved.Length > 0 && resolved.All(ip => !IsPrivateOrReserved(ip));
    }

    /// <summary>
    /// Создаёт HTTP-клиент для отправки web-push, который подключается только к внешним IP.
    /// </summary>
    /// <remarks>
    /// Хост резолвится внутри <see cref="SocketsHttpHandler.ConnectCallback"/>, и сокет открывается к тем самым
    /// адресам, что прошли проверку, – второго резолва, которым мог бы воспользоваться DNS rebinding, нет. Отказ
    /// приходит вызывающему как <see cref="HttpRequestException"/> с причиной <see cref="SecurityException"/>.
    /// Прокси отключён: через него сокет открывался бы к прокси, а цель резолвил бы уже он. Клиент держит пул
    /// соединений, поэтому создаётся один на процесс.
    /// </remarks>
    /// <returns>Клиент, отказывающий в подключении к приватным, петлевым и служебным адресам.</returns>
    public static HttpClient CreateHttpClient()
    {
        return new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectCallback = ConnectToPublicAddressAsync,
        });
    }

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var endPoint = context.DnsEndPoint;
        var addresses = await Dns.GetHostAddressesAsync(endPoint.Host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(IsPrivateOrReserved))
        {
            throw new SecurityException("Адрес push-эндпоинта резолвится во внутренний или служебный IP");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, endPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool TryGetHttpsHost(string endpoint, out string host)
    {
        host = string.Empty;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        host = uri.DnsSafeHost;
        return !string.IsNullOrEmpty(host);
    }

    private static bool IsForbiddenHostLiteral(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
               || (IPAddress.TryParse(host, out var ip) && IsPrivateOrReserved(ip));
    }

    private static bool IsPrivateOrReserved(IPAddress address)
    {
        var ip = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var octets = ip.GetAddressBytes();
            return octets[0] == 0
                   || octets[0] == 10
                   || (octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31)
                   || (octets[0] == 192 && octets[1] == 168)
                   || (octets[0] == 169 && octets[1] == 254)
                   || (octets[0] == 100 && octets[1] >= 64 && octets[1] <= 127)
                   || (octets[0] == 192 && octets[1] == 0 && (octets[2] == 0 || octets[2] == 2))
                   || (octets[0] == 198 && (octets[1] == 18 || octets[1] == 19))
                   || (octets[0] == 198 && octets[1] == 51 && octets[2] == 100)
                   || (octets[0] == 203 && octets[1] == 0 && octets[2] == 113)
                   || octets[0] >= 224;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = ip.GetAddressBytes();
            if (Nat64WellKnownNetwork.Contains(ip))
            {
                return IsPrivateOrReserved(new IPAddress(bytes.AsSpan(12, 4)));
            }

            if (SixToFourNetwork.Contains(ip))
            {
                return IsPrivateOrReserved(new IPAddress(bytes.AsSpan(2, 4)));
            }

            return ip.IsIPv6LinkLocal
                   || ip.IsIPv6SiteLocal
                   || ip.IsIPv6Multicast
                   || (bytes[0] & 0xFE) == 0xFC
                   || ReservedIPv6Networks.Any(network => network.Contains(ip));
        }

        return true;
    }
}
