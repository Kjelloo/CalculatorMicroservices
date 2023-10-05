using EasyNetQ;

namespace SharedModels.Helpers;

public static class ConnectionHelper
{
    public static IBus GetRmqConnection()
    {
        return RabbitHutch.CreateBus("host=rabbitmq;username=application;password=password");
    }
}