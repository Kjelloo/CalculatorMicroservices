using EasyNetQ;

namespace Monitoring;

public class ConnectionHelper
{
    public static IBus GetRMQConnection()
    {
        return RabbitHutch.CreateBus("host=localhost;username=application;password=password");
    }
}