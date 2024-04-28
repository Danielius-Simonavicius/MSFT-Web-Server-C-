using WebServer.Models;

namespace WebServer.Services;

public class MessengerService: IMessengerService
{

    private List<IMessengerListener> websiteAddedListeners = new();
    
    public void AddNewWebSiteAddedListener(IMessengerListener listener)
    {
        websiteAddedListeners.Add(listener);
    }

    public void RemoveWebSiteAddedListener(IMessengerListener listener)
    {
        websiteAddedListeners.Remove(listener);
    }

    public void SendNewWebsiteEvent(WebsiteConfigModel website)
    {
        foreach (var websiteAddedListener in websiteAddedListeners)
        {
            websiteAddedListener.NewWebSiteAdded(website);
        }
    }

    public void WebSiteRemovedEvent(WebsiteConfigModel website)
    {
        foreach (var websiteAddedListener in websiteAddedListeners)
        {
            websiteAddedListener.WebSiteRemoved(website);
        }
    }
}