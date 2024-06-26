using WebServer.Models;

namespace WebServer.Services;

public interface IMessengerListener
{
    void NewWebSiteAdded(WebsiteConfigModel website);
    void WebSiteRemoved(WebsiteConfigModel website);
    void ConfigChanged();

}

public interface IMessengerService
{
    void AddNewWebSiteAddedListener(IMessengerListener listener);
    void RemoveWebSiteAddedListener(IMessengerListener listener);
    void SendNewWebsiteEvent(WebsiteConfigModel website);
    void WebSiteRemovedEvent(WebsiteConfigModel website);

    void AddConfigChangedListener(IMessengerListener listener);
    void SendConfigChangedEvent();

}